using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MySocketLibrary
{
    internal class AsyncTcpClient
    {
        private const int BUFFER_SIZE = 8192;

        private readonly string host;
        private readonly int port;

        private readonly byte[] inputBuffer;

        private Socket? socket;
        private bool socketUsed;

        public event EventHandler? StatusConnected;
        public event EventHandler? StatusDisconnected;

        public event EventHandler<byte[]>? DataReceived;

        public bool Connected => socket?.Connected ?? false;

        public AsyncTcpClient(string host, int port)
        {
            this.host = host;
            this.port = port;

            inputBuffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// This will disconnect and reconnect
        /// </summary>
        public async ValueTask ConnectAsync(CancellationToken cancellationToken)
        {
            // If socket initialized and has connected
            if (socket != null && socketUsed)
            {
                // Dispose before reconnect
                socket.Dispose();
                socket = null;
            }

            // Initialize socket
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveBufferSize = BUFFER_SIZE,
                SendBufferSize = BUFFER_SIZE,
            };
            socketUsed = false;

            try
            {
                await socket.ConnectAsync(host, port, cancellationToken);
                socketUsed = true;
            }
            catch (SocketException)
            {
                // ignore
            }

            // Socket successfully connected
            if (socketUsed)
            {
                // Trigger event
                StatusConnected?.Invoke(this, EventArgs.Empty);

                // Begin receiving loop
                StartReceivingData();
            }
        }

        public void Disconnect()
        {
            // Already disconnected
            if (socket == null)
            {
                return;
            }

            // Socket was connected and has not yet been disconnected
            if (socketUsed)
            {
                socket.Dispose();
                socket = null;

                // Mark socket as being disconnected
                socketUsed = false;

                // Trigger event
                StatusDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (socket == null)
            {
                return 0;
            }

            try
            {
                return await socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (SocketException)
            {
                return 0;
            }
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (socket == null)
            {
                return 0;
            }

            try
            {
                return await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (SocketException)
            {
                return 0;
            }
        }

        // private

        private void StartReceivingData()
        {
            if (socket == null)
            {
                return;
            }

            socket.ReceiveAsync(inputBuffer, SocketFlags.None, CancellationToken.None).AsTask().ContinueWith(task =>
            {
                // Handle disconnect
                if (task.Result == 0)
                {
                    Disconnect();
                    return;
                }

                // Copy buffer to dedicated array
                byte[] data = new byte[task.Result];
                Array.Copy(inputBuffer, data, data.Length);

                // Trigger event
                DataReceived?.Invoke(this, data);

                // Receive next data
                StartReceivingData();
            });
        }
    }
}
