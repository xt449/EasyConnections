using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketLibrary
{
    public class EasySshConnection : IConnection
    {
        private readonly SshClient client;
        private readonly int connectRetryTimeoutMs;
        private readonly int connectRetryIntervalMs;
        private readonly bool autoReconnect;

        private ShellStream? shellStream;

        public event EventHandler? StatusConnected;

        public event EventHandler? StatusDisconnected;

        public event EventHandler<byte[]>? DataReceivedAsBytes;
        public event EventHandler<string>? DataReceivedAsString;

        public bool Connected
        {
            get => client.IsConnected;
        }

        public Encoding Encoding { get; init; }

        /// <param name="encoding">Encoding to be used for <see cref="DataReceivedAsString"/> and <see cref="SendStringAsync(string)"/></param>
        /// <param name="autoReconnect">Reconnect after <see cref="StatusDisconnected"/> is triggered</param>
        public EasySshConnection(string host, int port, string username, string password, Encoding encoding, bool autoReconnect = true, int connectRetryTimeoutMs = 4_000, int connectRetryIntervalMs = 1_000, int keepAliveIntervalMs = 30_000)
        {
            client = new SshClient(host, port, username, password);
            client.KeepAliveInterval = TimeSpan.FromMilliseconds(keepAliveIntervalMs);
            client.ErrorOccurred += Client_ErrorOccurred;

            client.ConnectionInfo.Encoding = encoding;

            this.autoReconnect = autoReconnect;
            this.connectRetryTimeoutMs = connectRetryTimeoutMs;
            this.connectRetryIntervalMs = connectRetryIntervalMs;

            Encoding = encoding;
        }

        public async void ConnectAsync()
        {
            await client.ConnectAsync(new CancellationTokenSource(connectRetryTimeoutMs).Token);

            // Retry loop
            while (!client.IsConnected)
            {
                // Wait before attempting to connect again
                await Task.Delay(connectRetryIntervalMs);

                // Try again
                await client.ConnectAsync(new CancellationTokenSource(connectRetryTimeoutMs).Token);
            }

            // Create stream after connected
            shellStream = client.CreateShellStreamNoTerminal();
            shellStream.Closed += ShellStream_Closed;
            shellStream.DataReceived += ShellStream_DataReceived;

            // Trigger event
            StatusConnected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect() => shellStream?.Close();

        public async ValueTask SendBytesAsync(Memory<byte> data)
        {
            if (shellStream == null)
            {
                return;
            }

            await shellStream.WriteAsync(data);
        }

        public async ValueTask SendStringAsync(string text)
        {
            if (shellStream == null)
            {
                return;
            }

            await shellStream.WriteAsync(Encoding.GetBytes(text));
        }

        // private

        private void Client_ErrorOccurred(object? sender, ExceptionEventArgs e)
        {
            // Trigger event
            StatusDisconnected?.Invoke(this, EventArgs.Empty);

            if (autoReconnect)
            {
                if (!client.IsConnected)
                {
                    ConnectAsync();
                }
            }
        }

        private void ShellStream_Closed(object? sender, EventArgs e)
        {
            // Trigger event
            StatusDisconnected?.Invoke(this, EventArgs.Empty);

            // Dispose
            shellStream?.Dispose();
            shellStream = null;

            if (autoReconnect)
            {
                if (!client.IsConnected)
                {
                    ConnectAsync();
                }
            }
        }

        private void ShellStream_DataReceived(object? sender, ShellDataEventArgs e)
        {
            // Check what kinda of data was received
            if (e.Data != null)
            {
                // Trigger events
                DataReceivedAsBytes?.Invoke(this, e.Data);
                DataReceivedAsString?.Invoke(this, Encoding.GetString(e.Data));
            }
            else
            {
                // Trigger events
                DataReceivedAsBytes?.Invoke(this, Encoding.GetBytes(e.Line));
                DataReceivedAsString?.Invoke(this, e.Line);
            }
        }
    }
}
