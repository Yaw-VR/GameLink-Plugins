using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLib.TelemetryHelper
{
    internal interface ITelemetry<TData, TConfig>
        //where TData : struct
        where TConfig : class, new()
    {
        event TelemetryBase<TData, TConfig>.LogEventHandler OnLog;

        int Send(TData data);
        TData Receive();


        Task<TData> ReceiveAsync(CancellationToken cancellationToken = default);

        Task BeginAsync(CancellationToken cancellationToken = default);

    }

    internal abstract class TelemetryBase<TData, TConfig> : ITelemetry<TData, TConfig>, IDisposable
        //where TData : struct
        where TConfig : class, new()
    {
        public TConfig Config { get; private set; }

        public delegate void LogEventHandler(object sender, string message);
        public event LogEventHandler OnLog;

        protected abstract void Configure(TConfig config);
        public abstract int Send(TData message);
        public abstract TData Receive();


        public IByteConverter<TData> Converter;

        public abstract void Dispose();

        public abstract bool IsConnected { get; }

        protected TelemetryBase(TConfig config, IByteConverter<TData> converter)
        {
            Converter = converter;
            //if(typeof(TData) == typeof(byte[]))
            //{
            //    Converter = (IByteConverter<TData>)(object)new RawBytesConverter();
            //}
            //else if (typeof(TData) == typeof(string))
            //{
            //    Converter = (IByteConverter<TData>)(object)new StringByteConverter(System.Text.Encoding.ASCII);
            //}
            //else if(IsValueType<TData>())
            //{
            //    Converter = new MarshalByteConverter<TData>();
            //}
            //else
            //{
            //    //throw new NotSupportedException($"Type {typeof(TData).Name} is not supported for telemetry conversion.");
            //}



            Config = config ?? new TConfig();
            Configure(Config);
        }

        protected void Log(string message)
        {
            OnLog?.Invoke(this, $"[{GetType().Name}] " + message);
        }

        public virtual Task<int> SendAsync(TData data, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Send(data), cancellationToken);
        }

        public virtual Task<TData> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>  Receive() , cancellationToken);
        }

        private bool IsStruct<T>()
        {
            return typeof(T).IsValueType && !typeof(T).IsPrimitive && !typeof(T).IsEnum;
        }

        public bool IsValueType<T>()
        {
            return typeof(T).IsValueType && typeof(T) != typeof(string) && !typeof(T).IsEnum;
        }

        //public abstract Task BeginAsync(CancellationToken cancellationToken = default);

        public virtual Task BeginAsync(CancellationToken cancellationToken = default)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _ = await this.ReceiveAsync(cancellationToken);
                    //await Task.Delay(1800, _cancellationTokenSource.Token);
                    //Thread.Sleep(1);
                }
            }, TaskCreationOptions.LongRunning);
        }
    }


}
