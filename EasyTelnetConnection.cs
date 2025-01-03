using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSocketLibrary
{
	public class EasyTelnetConnection : IConnection
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

		public event EventHandler<byte[]>? DataReceivedAsBytes;

		public event EventHandler<string>? DataReceivedAsString;

		public bool Connected => client.Connected;

		public Encoding Encoding { get; init; }

		/// <param name="encoding">Encoding to be used for <see cref="DataReceivedAsString"/> and <see cref="SendStringAsync(string)"/></param>
		/// <param name="autoReconnect">Reconnect after <see cref="StatusDisconnected"/> is triggered</param>
		/// <param name="connectRetryTimeoutMs">Duration that the socket will attempt to connect before timing out</param>
		/// <param name="connectRetryIntervalMs">Interval that the socket will attempt to connect if the first attempt times out</param>
		public EasyTelnetConnection(string host, int port, Encoding encoding, bool autoReconnect = true, int connectRetryTimeoutMs = 4_000, int connectRetryIntervalMs = 1_000)
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

		public async Task ConnectAsync()
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

		public void Disconnect() => client.Disconnect();

		public async ValueTask SendBytesAsync(Memory<byte> data)
		{
			await client.SendAsync(EscapeIAC(data));
		}

		public async ValueTask SendStringAsync(string text)
		{
			await client.SendAsync(EscapeIAC(Encoding.GetBytes(text)));
		}

		// private

		private void Client_Disconnected(object? sender, EventArgs args)
		{
			_ = ConnectAsync();
		}

		private void Client_DataReceived(object? sender, byte[] data)
		{
			byte[] buffer = ParseAndRespond(data);

			// Trigger event
			DataReceivedAsBytes?.Invoke(this, buffer);
			DataReceivedAsString?.Invoke(this, Encoding.GetString(buffer));
		}

		private byte[] ParseAndRespond(byte[] data)
		{
			MemoryStream outputBuffer = new MemoryStream();

			MemoryStream buffer = new MemoryStream(data.Length);
			int index = 0;

			while (index < data.Length)
			{
				byte currentByte = data[index];

				if (currentByte == IAC)
				{
					// Handle command sequence

					index++;
					if (index >= data.Length)
					{
						// TODO - Packet fragmentation may cause data to be lost and/or misinterpreted 
						// End of data
						break;
					}

					byte command = data[index];

					if (command == IAC)
					{
						// Escaped literal IAC, so append as escaped
						buffer.WriteByte(IAC);

						// Next byte
						continue;
					}

					index++;
					if (index >= data.Length)
					{
						// TODO - Packet fragmentation may cause data to be lost and/or misinterpreted 
						// End of data
						break;
					}

					byte option = data[index];

					if (command == COMMAND_DO)
					{
						if (option == OPTION_SGA)
						{
							// Respond YES to DO SGA
							outputBuffer.WriteByte(IAC);
							outputBuffer.WriteByte(COMMAND_WILL);
							outputBuffer.WriteByte(OPTION_SGA);
						}
						else
						{
							// Respond NO to DO anything else
							outputBuffer.WriteByte(IAC);
							outputBuffer.WriteByte(COMMAND_WONT);
							outputBuffer.WriteByte(OPTION_SGA);
						}
					}
					else if (command == COMMAND_WILL)
					{
						if (option == OPTION_SGA)
						{
							// Respond YES to WILL SGA
							outputBuffer.WriteByte(IAC);
							outputBuffer.WriteByte(COMMAND_DO);
							outputBuffer.WriteByte(OPTION_SGA);
						}
						else
						{
							// Respond NO to WILL anything else
							outputBuffer.WriteByte(IAC);
							outputBuffer.WriteByte(COMMAND_DONT);
							outputBuffer.WriteByte(OPTION_SGA);
						}
					}
				}
				else
				{
					// Normal data
					buffer.WriteByte(currentByte);
				}

				index++;
			}

			// Send responses
			_ = client.SendAsync(outputBuffer.ToArray());

			return buffer.ToArray();
		}

		private static Memory<byte> EscapeIAC(Memory<byte> data)
		{
			Span<byte> span = data.Span;

			// Size for worst-case scenario
			MemoryStream buffer = new MemoryStream(span.Length * 2);

			foreach (byte b in span)
			{
				// If IAC
				if (b == IAC)
				{
					// Escape
					buffer.WriteByte(IAC);
					buffer.WriteByte(IAC);
				}
				else
				{
					buffer.WriteByte(b);
				}
			}

			return buffer.ToArray();
		}

		private const byte IAC = 255; // Interpret as command

		private const byte COMMAND_WILL = 251;
		private const byte COMMAND_WONT = 252;
		private const byte COMMAND_DO = 253;
		private const byte COMMAND_DONT = 254;

		private const byte OPTION_SGA = 3; // Supress go ahead
	}
}
