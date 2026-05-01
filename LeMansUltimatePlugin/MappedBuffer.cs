/*
rF2SMMonitor is visual debugger for rF2 Shared Memory Plugin.

MappedBuffer implementation.  Implements writing and reading to/from rF2 shared memory.

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
*/

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;


namespace RFactor2Plugin
{
    ///////////////////////////////////////////
    // Mapped wrapper structures
    ///////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct rF2MappedBufferVersionBlock
    {
        // If both version variables are equal, buffer is not being written to, or we're extremely unlucky and second check is necessary.
        // If versions don't match, buffer is being written to, or is incomplete (game crash, or missed transition).
        public uint mVersionUpdateBegin;          // Incremented right before buffer is written to.
        public uint mVersionUpdateEnd;            // Incremented after buffer write is done.
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct rF2MappedBufferVersionBlockWithSize
    {
        public uint mVersionUpdateBegin;          // Incremented right before buffer is written to.
        public uint mVersionUpdateEnd;            // Incremented after buffer write is done.

        public int mBytesUpdatedHint;             // How many bytes of the structure were written during the last update.
                                                  // 0 means unknown (whole buffer should be considered as updated).
    }

    public class MappedBuffer<MappedBufferT>
    {
        const int NUM_MAX_RETRIEES = 10;
        readonly int RF2_BUFFER_VERSION_BLOCK_SIZE_BYTES = Marshal.SizeOf(typeof(rF2MappedBufferVersionBlock));
        readonly int RF2_BUFFER_VERSION_BLOCK_WITH_SIZE_SIZE_BYTES = Marshal.SizeOf(typeof(rF2MappedBufferVersionBlockWithSize));

        readonly int BUFFER_SIZE_BYTES;
        readonly string BUFFER_NAME;

        // Holds the entire byte array that can be marshalled to a MappedBufferT.  Partial updates
        // only read changed part of buffer, ignoring trailing uninteresting bytes.  However,
        // to marshal we still need to supply entire structure size.  So, on update new bytes are copied.
        private byte[] fullSizeBuffer = null;
        MemoryMappedFile memoryMappedFile = null;

        bool partial = false;
        bool skipUnchanged = false;
        public MappedBuffer(string buffName, bool partial, bool skipUnchanged)
        {
            BUFFER_SIZE_BYTES = Marshal.SizeOf(typeof(MappedBufferT));
            BUFFER_NAME = buffName;
            this.partial = partial;
            this.skipUnchanged = skipUnchanged;
        }

        // Write buffer ctor.
        public MappedBuffer(string buffName)
        {
            BUFFER_SIZE_BYTES = Marshal.SizeOf(typeof(MappedBufferT));
            BUFFER_NAME = buffName;
        }

        public void Connect()
        {
            memoryMappedFile = MemoryMappedFile.OpenExisting(BUFFER_NAME);

            // NOTE: Make sure that BUFFER_SIZE matches the structure size in the plugin (debug mode prints that).
            fullSizeBuffer = new byte[BUFFER_SIZE_BYTES];
        }

        public void Disconnect()
        {
            if (memoryMappedFile != null)
                memoryMappedFile.Dispose();

            memoryMappedFile = null;
            fullSizeBuffer = null;

            ClearStats();
        }

        // Read success statistics.
        int numReadRetriesPreCheck = 0;
        int numReadRetries = 0;
        int numReadRetriesOnCheck = 0;
        int numReadFailures = 0;
        int numStuckFrames = 0;
        int numReadsSucceeded = 0;
        int numSkippedNoChange = 0;
        uint stuckVersionBegin = 0;
        uint stuckVersionEnd = 0;
        uint lastSuccessVersionBegin = 0;
        uint lastSuccessVersionEnd = 0;
        int maxRetries = 0;

        public string GetStats()
        {
            return string.Format("R1: {0}    R2: {1}    R3: {2}    F: {3}    ST: {4}    MR: {5}    SK:{6}    S:{7}", numReadRetriesPreCheck, numReadRetries, numReadRetriesOnCheck, numReadFailures, numStuckFrames, maxRetries, numSkippedNoChange, numReadsSucceeded);
        }

        public void ClearStats()
        {
            numReadRetriesPreCheck = 0;
            numReadRetries = 0;
            numReadRetriesOnCheck = 0;
            numReadFailures = 0;
            numStuckFrames = 0;
            numReadsSucceeded = 0;
            numSkippedNoChange = 0;
            maxRetries = 0;
        }

        public void GetMappedDataUnsynchronized(ref MappedBufferT mappedData)
        {
            using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
            {
                var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                var sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(BUFFER_SIZE_BYTES);

                var handleBuffer = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                mappedData = (MappedBufferT)Marshal.PtrToStructure(handleBuffer.AddrOfPinnedObject(), typeof(MappedBufferT));
                handleBuffer.Free();
            }
        }

        private void GetHeaderBlock<HeaderBlockT>(BinaryReader sharedMemoryStream, int headerBlockBytes, ref HeaderBlockT headerBlock)
        {
            sharedMemoryStream.BaseStream.Position = 0;
            var sharedMemoryReadBufferHeader = sharedMemoryStream.ReadBytes(headerBlockBytes);

            var handleBufferHeader = GCHandle.Alloc(sharedMemoryReadBufferHeader, GCHandleType.Pinned);
            headerBlock = (HeaderBlockT)Marshal.PtrToStructure(handleBufferHeader.AddrOfPinnedObject(), typeof(HeaderBlockT));
            handleBufferHeader.Free();
        }

        public void GetMappedData(ref MappedBufferT mappedData)
        {
            // This method tries to ensure we read consistent buffer view in three steps.
            // 1. Pre-Check:
            //       - read version header and retry reading this buffer if begin/end versions don't match.  This reduces a chance of
            //         reading torn frame during full buffer read.  This saves CPU time.
            //       - return if version matches last failed read version (stuck frame).
            //       - return if version matches previously successfully read buffer.  This saves CPU time by avoiding the full read of most likely identical data.
            //
            // 2. Main Read: reads the main buffer + version block.  If versions don't match, retry.
            //
            // 3. Post-Check: read version header again and retry reading this buffer if begin/end versions don't match.  This covers corner case
            //                where buffer is being written to during the Main Read.
            //
            // While retrying, this method tries to avoid running CPU at 100%.
            //
            // There are multiple alternatives on what to do here:
            // * keep retrying - drawback is CPU being kept busy, but absolute minimum latency.
            // * Thread.Sleep(0)/Yield - drawback is CPU being kept busy, but almost minimum latency.  Compared to first option, gives other threads a chance to execute.
            // * Thread.Sleep(N) - relaxed approach, less CPU saturation but adds a bit of latency.
            // there are other options too.  Bearing in mind that minimum sleep on windows is ~16ms, which is around 66FPS, I doubt delay added matters much for Crew Chief at least.
            using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
            {
                uint currVersionBegin = 0;
                uint currVersionEnd = 0;

                var retry = 0;
                var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                byte[] sharedMemoryReadBuffer = null;
                var versionHeaderWithSize = new rF2MappedBufferVersionBlockWithSize();
                var versionHeader = new rF2MappedBufferVersionBlock();

                for (retry = 0; retry < NUM_MAX_RETRIEES; ++retry)
                {
                    var bufferSizeBytes = BUFFER_SIZE_BYTES;
                    // Read current buffer versions.
                    if (partial)
                    {
                        GetHeaderBlock<rF2MappedBufferVersionBlockWithSize>(sharedMemoryStream, RF2_BUFFER_VERSION_BLOCK_WITH_SIZE_SIZE_BYTES, ref versionHeaderWithSize);
                        currVersionBegin = versionHeaderWithSize.mVersionUpdateBegin;
                        currVersionEnd = versionHeaderWithSize.mVersionUpdateEnd;

                        bufferSizeBytes = versionHeaderWithSize.mBytesUpdatedHint != 0 ? versionHeaderWithSize.mBytesUpdatedHint : bufferSizeBytes;
                    }
                    else
                    {
                        GetHeaderBlock<rF2MappedBufferVersionBlock>(sharedMemoryStream, RF2_BUFFER_VERSION_BLOCK_SIZE_BYTES, ref versionHeader);
                        currVersionBegin = versionHeader.mVersionUpdateBegin;
                        currVersionEnd = versionHeader.mVersionUpdateEnd;
                    }

                    // If this is stale "out of sync" situation, that is, we're stuck in, no point in retrying here.
                    // Could be a bug in a game, plugin or a game crash.
                    if (currVersionBegin == stuckVersionBegin
                      && currVersionEnd == stuckVersionEnd)
                    {
                        ++numStuckFrames;
                        return;  // Failed.
                    }

                    // If version is the same as previously successfully read, do nothing.
                    if (skipUnchanged
                      && currVersionBegin == lastSuccessVersionBegin
                      && currVersionEnd == lastSuccessVersionEnd)
                    {
                        ++numSkippedNoChange;
                        return;
                    }

                    // Buffer version pre-check.  Verify if Begin/End versions match.
                    if (currVersionBegin != currVersionEnd)
                    {
                        Thread.Sleep(1);
                        ++numReadRetriesPreCheck;
                        continue;
                    }

                    // Read the mapped data.
                    sharedMemoryStream.BaseStream.Position = 0;
                    sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(bufferSizeBytes);

                    // Marshal version block.
                    var handleVersionBlock = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                    versionHeader = (rF2MappedBufferVersionBlock)Marshal.PtrToStructure(handleVersionBlock.AddrOfPinnedObject(), typeof(rF2MappedBufferVersionBlock));
                    handleVersionBlock.Free();

                    currVersionBegin = versionHeader.mVersionUpdateBegin;
                    currVersionEnd = versionHeader.mVersionUpdateEnd;

                    // Verify if Begin/End versions match:
                    if (versionHeader.mVersionUpdateBegin != versionHeader.mVersionUpdateEnd)
                    {
                        Thread.Sleep(1);
                        ++numReadRetries;
                        continue;
                    }

                    // Read the version header one last time.  This is for the case, that might not be even possible in reality,
                    // but it is possible in my head.  Since it is cheap, no harm reading again really, aside from retry that
                    // sometimes will be required if buffer is updated between checks.
                    //
                    // Anyway, the case is
                    // * Reader thread reads updateBegin version and continues to read buffer.
                    // * Simultaneously, Writer thread begins overwriting the buffer.
                    // * If Reader thread reads updateEnd before Writer thread finishes, it will look
                    //   like updateBegin == updateEnd.But we actually just read a partially overwritten buffer.
                    //
                    // Hence, this second check is needed here.  Even if writer thread still hasn't finished writing,
                    // we still will be able to detect this case because now updateBegin version changed, so we
                    // know Writer is updating the buffer.

                    GetHeaderBlock<rF2MappedBufferVersionBlock>(sharedMemoryStream, RF2_BUFFER_VERSION_BLOCK_SIZE_BYTES, ref versionHeader);

                    if (currVersionBegin != versionHeader.mVersionUpdateBegin
                      || currVersionEnd != versionHeader.mVersionUpdateEnd)
                    {
                        Thread.Sleep(1);
                        ++numReadRetriesOnCheck;
                        continue;
                    }

                    // Marshal rF2 State buffer
                    MarshalDataBuffer(partial, sharedMemoryReadBuffer, ref mappedData);

                    // Success.
                    maxRetries = Math.Max(maxRetries, retry);
                    ++numReadsSucceeded;
                    stuckVersionBegin = stuckVersionEnd = 0;

                    // Save succeessfully read version to avoid re-reading.
                    lastSuccessVersionBegin = currVersionBegin;
                    lastSuccessVersionEnd = currVersionEnd;

                    return;
                }

                // Failure.  Save the frame version.
                stuckVersionBegin = currVersionBegin;
                stuckVersionEnd = currVersionEnd;

                maxRetries = Math.Max(maxRetries, retry);
                ++numReadFailures;
            }
        }

        private void MarshalDataBuffer(bool partial, byte[] sharedMemoryReadBuffer, ref MappedBufferT mappedData)
        {
            if (partial)
            {
                // For marshalling to succeed we need to copy partial buffer into full size buffer.  While it is a bit of a waste, it still gives us gain
                // of shorter time window for version collisions while reading game data.
                Array.Copy(sharedMemoryReadBuffer, fullSizeBuffer, sharedMemoryReadBuffer.Length);
                var handlePartialBuffer = GCHandle.Alloc(fullSizeBuffer, GCHandleType.Pinned);
                mappedData = (MappedBufferT)Marshal.PtrToStructure(handlePartialBuffer.AddrOfPinnedObject(), typeof(MappedBufferT));
                handlePartialBuffer.Free();
            }
            else
            {
                var handleBuffer = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                mappedData = (MappedBufferT)Marshal.PtrToStructure(handleBuffer.AddrOfPinnedObject(), typeof(MappedBufferT));
                handleBuffer.Free();
            }
        }

        // Write buffer stuff
        public void PutMappedData(ref MappedBufferT mappedData)
        {
            using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
            {
                var sharedMemoryStream = new BinaryWriter(sharedMemoryStreamView);

                var size = Marshal.SizeOf(mappedData);
                var byteArray = new byte[size];

                var ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(mappedData, ptr, true);
                Marshal.Copy(ptr, byteArray, 0, size);

                sharedMemoryStream.Write(byteArray);

                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
