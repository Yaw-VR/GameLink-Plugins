
using System.Threading;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;


using System.Runtime.InteropServices;
using System.IO;
using System;

namespace SharedLib.TelemetryHelper
{
    internal class MmfTelemetryConfig
    {
        public string Name { get; set; } = "MmfTelemetry";        
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]

    internal class MmfTelemetry<TData> : TelemetryBase<TData, MmfTelemetryConfig>
        where TData : struct
    {

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private int _dataSize = Marshal.SizeOf<TData>();

        public MmfTelemetry(MmfTelemetryConfig config) : base(config)
        {
        }

        protected override void Configure(MmfTelemetryConfig config)
        {

            _mmf = MemoryMappedFile.CreateOrOpen(config.Name, Marshal.SizeOf<TData>());

            _accessor = _mmf.CreateViewAccessor();

        }

        public int TryOpen()
        {
            try
            {
                _mmf = MemoryMappedFile.OpenExisting(Config.Name);
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 2;
            }
            catch (FileNotFoundException)
            {
                return 1;
            }
        }

        public Task<int> TryOpenAsync(int timeout = 0, CancellationToken cancellationToken = default)
        {
            
            return Task.Run(async () =>
            {
                int result = 1;
                var cts = new CancellationTokenSource(timeout);
                do
                {
                    result = TryOpen();
                    await Task.Delay(1000, cancellationToken);
                } while (result != 0 || cancellationToken.IsCancellationRequested || cts.Token.IsCancellationRequested);

                return result;
               
                
            }, cancellationToken);
        }

        public override TData Receive()
        {
            _accessor.Read(0, out TData data);
            return data;

        }



        public override int Send(TData data)
        {
            _accessor.Write(0, ref data);
            return _dataSize;
        }

        public override void Dispose()
        {
            _mmf?.Dispose();
            _accessor?.Dispose();
        }


        public override Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Receive());
        }

    }
}
