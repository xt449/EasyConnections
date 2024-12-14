using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MySocketLibrary
{
    internal class HeartbeatConnection : IConnection
    {
        private readonly IConnection inner;
        private readonly byte[] heartbeatPayload;
        private readonly int receiveTimeoutMs;
        private readonly int heartbeatTimeoutMs;

        private readonly Timer receiveTimer;
        private readonly Timer heartbeatTimer;

        public event EventHandler? HeartbeatTimedOut;

        public HeartbeatConnection(IConnection inner, Memory<byte> heartbeatPayload, int receiveTimeoutMs = 3_000, int heartbeatTimeoutMs = 5_000)
        {
            this.inner = inner;
            this.heartbeatPayload = heartbeatPayload.ToArray();
            this.receiveTimeoutMs = receiveTimeoutMs;
            this.heartbeatTimeoutMs = heartbeatTimeoutMs;

            receiveTimer = new Timer(ReceiveTimeout, null, Timeout.Infinite, Timeout.Infinite);
            heartbeatTimer = new Timer(HeartbeatTimeout, null, Timeout.Infinite, Timeout.Infinite);

            this.inner.StatusConnected += Inner_StatusConnected;
            this.inner.StatusDisconnected += Inner_StatusDisconnected;
            this.inner.DataReceivedAsBytes += Inner_DataReceivedAsBytes;
        }

        public HeartbeatConnection(IConnection inner, string heartbeatPayload, Encoding encoding, int receiveTimeoutMs = 3_000, int heartbeatTimeoutMs = 5_000)
            : this(inner, encoding.GetBytes(heartbeatPayload), receiveTimeoutMs, heartbeatTimeoutMs)
        {
        }

        public event EventHandler StatusConnected
        {
            add => inner.StatusConnected += value;
            remove => inner.StatusConnected -= value;
        }

        public event EventHandler StatusDisconnected
        {
            add => inner.StatusDisconnected += value;
            remove => inner.StatusDisconnected -= value;
        }

        public event EventHandler<byte[]> DataReceivedAsBytes
        {
            add => inner.DataReceivedAsBytes += value;
            remove => inner.DataReceivedAsBytes -= value;
        }

        public event EventHandler<string> DataReceivedAsString
        {
            add => inner.DataReceivedAsString += value;
            remove => inner.DataReceivedAsString -= value;
        }

        public bool Connected => inner.Connected;

        public void ConnectAsync() => inner.ConnectAsync();

        public void Disconnect() => inner.Disconnect();

        public ValueTask SendBytesAsync(Memory<byte> buffer) => inner.SendBytesAsync(buffer);

        public ValueTask SendStringAsync(string text) => inner.SendStringAsync(text);

        // private

        private void Inner_StatusConnected(object? sender, EventArgs e)
        {
            // Reset timer
            receiveTimer.Change(receiveTimeoutMs, Timeout.Infinite);

            // Stop timer
            heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void Inner_StatusDisconnected(object? sender, EventArgs e)
        {
            // Stop timer
            receiveTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Stop timer
            heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void Inner_DataReceivedAsBytes(object? sender, byte[] data)
        {
            // Reset timer
            receiveTimer.Change(receiveTimeoutMs, Timeout.Infinite);

            // Stop timer
            heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private async void ReceiveTimeout(object? _)
        {
            // Send heartbeat
            await inner.SendBytesAsync(heartbeatPayload);

            // Reset timer
            heartbeatTimer.Change(heartbeatTimeoutMs, Timeout.Infinite);
        }

        private void HeartbeatTimeout(object? _)
        {
            // Trigger event
            HeartbeatTimedOut?.Invoke(this, EventArgs.Empty);
        }
    }
}
