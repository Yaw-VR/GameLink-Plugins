using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib.TelemetryHelper
{
    internal class MmfTelemetryConfig
    {
        public string Name { get; set; } = "MmfTelemetry";

        public bool Create { get; set; } = false;

        public string MutexName { get; set; } = null;

        public bool IsGlobal { get; set; } = false;

        public MmfTelemetryConfig() { }

        public MmfTelemetryConfig(string name, bool isCreator = false, bool isGlobal = false)
        {
            Name = name; 
            Create = isCreator;
            IsGlobal = IsGlobal;
        }
    }

    internal static class MmfTelemetryExtensions
    {

        public static MemoryMappedFile SetSecurityInfo(this MemoryMappedFile mmf, SecurityInformation securityInformation = SecurityInformation.DACL_SECURITY_INFORMATION)
        {
            if (SetSecurityInfoByHandle(mmf.SafeMemoryMappedFileHandle, 1, (uint)securityInformation, null, null, null, null) != 0)
            {
                var errorCode = Marshal.GetLastWin32Error();

                throw new Exception($"MemoryMappedFile set security failed. Error code: {errorCode} - {new System.ComponentModel.Win32Exception(errorCode).Message}");
            }

            return mmf;
        }

        [DllImport("advapi32.dll", EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern uint SetSecurityInfoByHandle(SafeHandle handle, uint objectType, uint securityInformation, byte[] owner, byte[] group, byte[] dacl, byte[] sacl);


        public enum ObjectType : uint
        {
            SE_KERNEL_OBJECT = 1
        }

        public enum SecurityInformation : uint
        {
            OWNER_SECURITY_INFORMATION = 0x00000001,
            GROUP_SECURITY_INFORMATION = 0x00000002,
            DACL_SECURITY_INFORMATION = 0x00000004,
            SACL_SECURITY_INFORMATION = 0x00000008
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]

    internal class MmfTelemetry<TData> : TelemetryBase<TData, MmfTelemetryConfig>  
        where TData : struct
    {

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private int _dataSize = Marshal.SizeOf<TData>();

        private int _errorCode = 0;

        public int ErrorCode => _errorCode;

        public override bool IsConnected => _accessor != null && _errorCode == 0;

        Mutex _mutex = null;

        public MmfTelemetry(MmfTelemetryConfig config, IByteConverter<TData> converter) : base(config, converter)
        {
        }

        protected override void Configure(MmfTelemetryConfig config)
        {
            if (config.Create)
               _errorCode = CreateOrOpen().GetAwaiter().GetResult();
            else
            {
               _errorCode = TryOpen();
            }
        }



        public override TData Receive()
        {
            if (_errorCode > 0)
                return default;

            if (Config.MutexName != null)
            {
                if (_mutex == null)
                {
                    bool mutexCreated = false;
                    if (Config.Create)
                        _mutex = new Mutex(true, Config.MutexName, out mutexCreated);
                    else
                        mutexCreated = Mutex.TryOpenExisting(Config.MutexName, out _mutex);
                }
                
                if (_mutex != null)
                {
                    _mutex.WaitOne();
                }
            }
            TData data = default;

            _accessor?.Read(0, out data);
            if (Config.MutexName != null && _mutex != null)
            {
                _mutex.ReleaseMutex();

            }
            return data;

        }

        public override int Send(TData data)
        {
            if (_accessor != null && _errorCode == 0)
            {
                if (Config.MutexName != null)
                {
                    if (_mutex == null)
                    {
                        bool mutexCreated = false;
                        if (Config.Create)
                            _mutex = new Mutex(true, Config.MutexName, out mutexCreated);
                        else
                            mutexCreated = Mutex.TryOpenExisting(Config.MutexName, out _mutex);
                    }

                    if (_mutex != null)
                    {
                        _mutex.WaitOne();
                    }
                }
                _accessor?.Write(0, ref data);

                if (Config.MutexName != null && _mutex != null)
                {
                    _mutex.ReleaseMutex();
                }

                return _dataSize;
            }

            return 0;
        }

        public override void Dispose()
        {
            _mmf?.Dispose();
            _accessor?.Dispose();
            _mutex?.Dispose();
            _mutex = null;
        }

        public Task<int> CreateOrOpen()
        {
            if (_accessor != null)
            {
                return Task.FromResult(0);
            }

            return Task.Run(() =>
            {
                try
                {
                    string scope = Config.IsGlobal ? "Global\\" : "Local\\";


                    _mmf = MemoryMappedFile.CreateOrOpen(scope + Config.Name, Marshal.SizeOf<TData>()).SetSecurityInfo();
                    _accessor = _mmf.CreateViewAccessor();
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
                catch(Exception x)
                {
                    throw;
                }
            });
        }

        private int TryOpen()
        {
            try
            {
                _mmf = MemoryMappedFile.OpenExisting(Config.Name);
                _accessor = _mmf.CreateViewAccessor();
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
                using(var cts = new CancellationTokenSource(timeout))
                {                
                    do
                    {
                        result = TryOpen();
                        await Task.Delay(4000, cancellationToken);
                    } while (result != 0 || cancellationToken.IsCancellationRequested || cts.Token.IsCancellationRequested);
				}

                return result;


            }, cancellationToken);
        }

        public override Task BeginAsync(CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _ = await this.ReceiveAsync(cancellationToken);
                    //await Task.Delay(1800, _cancellationTokenSource.Token);
                    Thread.Sleep(3);
                }
            }, TaskCreationOptions.LongRunning);
        }

    }
}
