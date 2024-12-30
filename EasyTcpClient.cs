using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketLibrary
{
    public class EasyTcpClient : IConnection
    {
        private readonly AsyncTcpClient client;
        private readonly int connectRetryTimeoutMs;
        private readonly int connectRetryIntervalMs;

        public event EventHandler StatusConnected
        {
            add => client.StatusConnected += value;
            remove => client.StatusConnected -= value;
        }

        public event EventHandler StatusDisconnected
        {
            add => client.StatusDisconnected += value;
            remove => client.StatusDisconnected -= value;
        }

        public event EventHandler<byte[]> DataReceivedAsBytes
        {
            add => client.DataReceived += value;
            remove => client.DataReceived -= value;
        }

        public event EventHandler<string>? DataReceivedAsString;

        public bool Connected
        {
            get => client.Connected;
        }

        public Encoding Encoding { get; init; }

        /// <param name="encoding">Encoding to be used for <see cref="DataReceivedAsString"/> and <see cref="SendStringAsync(string)"/></param>
        /// <param name="autoReconnect">Reconnect after <see cref="StatusDisconnected"/> is triggered</param>
        /// <param name="connectRetryTimeoutMs">Duration that the socket will attempt to connect before timing out</param>
        /// <param name="connectRetryIntervalMs">Interval that the socket will attempt to connect if the first attempt times out</param>
        public EasyTcpClient(string host, int port, Encoding encoding, bool autoReconnect = true, int connectRetryTimeoutMs = 4_000, int connectRetryIntervalMs = 1_000)
        {
            client = new AsyncTcpClient(host, port);

            client.DataReceived += Client_DataReceived;

            if (autoReconnect)
            {
                client.StatusDisconnected += Client_Disconnected;
            }

            this.connectRetryTimeoutMs = connectRetryTimeoutMs;
            this.connectRetryIntervalMs = connectRetryIntervalMs;

            Encoding = encoding;
        }

        public async void ConnectAsync()
        {
            await client.ConnectAsync(new CancellationTokenSource(connectRetryTimeoutMs).Token);

            // Retry loop
            while (!client.Connected)
            {
                // Wait before attempting to connect again
                await Task.Delay(connectRetryIntervalMs);

                // Try again
                await client.ConnectAsync(new CancellationTokenSource(connectRetryTimeoutMs).Token);
            }
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public async ValueTask SendBytesAsync(Memory<byte> data)
        {
            await client.SendAsync(data);
        }

        public async ValueTask SendStringAsync(string text)
        {
            await client.SendAsync(Encoding.GetBytes(text));
        }

        // private

        private void Client_Disconnected(object? sender, EventArgs args)
        {
            ConnectAsync();
        }

        private void Client_DataReceived(object? sender, byte[] data)
        {
            // Trigger event
            DataReceivedAsString?.Invoke(this, Encoding.GetString(data));
        }
    }
}
