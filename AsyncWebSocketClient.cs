// Copyright (c) 2026 Jonathan Talcott
// 
// This file is part of EasyConnections.
// 
// EasyConnections is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// EasyConnections is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with EasyConnections.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyConnections;

public class AsyncWebSocketClient
{
	private const int BUFFER_SIZE = 8192;

	private readonly Uri uri;
	private readonly HttpMessageInvoker? invoker;
	private readonly byte[] inputBuffer;

	private ClientWebSocket? webSocket;
	private bool socketUsed;

	public event EventHandler? StatusConnected;
	public event EventHandler? StatusDisconnected;

	public event EventHandler<byte[]>? BinaryReceived;
	public event EventHandler<string>? TextReceived;

	public bool Connected => webSocket?.State == WebSocketState.Open;

	public AsyncWebSocketClient(Uri uri, HttpMessageInvoker? invoker = null)
	{
		this.uri = uri;
		this.invoker = invoker;

		inputBuffer = new byte[BUFFER_SIZE];
	}

	/// <summary>
	/// This will disconnect and reconnect
	/// </summary>
	public async ValueTask ConnectAsync(CancellationToken cancellationToken)
	{
		// If socket initialized and has connected
		if (webSocket != null && socketUsed)
		{
			// Dispose before reconnect
			webSocket.Dispose();
			webSocket = null;
		}

		// Initialize socket
		webSocket = new ClientWebSocket();
		socketUsed = false;

		try
		{
			await webSocket.ConnectAsync(uri, invoker, cancellationToken);
			socketUsed = true;
		}
		catch (SocketException)
		{
			// Ignore
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
		if (webSocket == null)
		{
			return;
		}

		// Socket was connected and has not yet been disconnected
		if (socketUsed)
		{
			webSocket.Dispose();
			webSocket = null;

			// Mark socket as being disconnected
			socketUsed = false;

			// Trigger event
			StatusDisconnected?.Invoke(this, EventArgs.Empty);
		}
	}

	public async ValueTask SendBinaryAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		// Disconnected
		if (webSocket == null)
		{
			return;
		}

		try
		{
			await webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
		}
		catch (SocketException)
		{
			// Ignore
		}
	}

	public ValueTask SendTextAsync(string text, CancellationToken cancellationToken = default) => SendBinaryAsync(Encoding.UTF8.GetBytes(text).AsMemory(), cancellationToken);

	// private

	private async void StartReceivingData()
	{
		// Disconnected
		if (webSocket == null)
		{
			return;
		}

		using (MemoryStream inputStream = new MemoryStream())
		{
			WebSocketReceiveResult result;

			// Receive until end of message
			do
			{
				result = await webSocket.ReceiveAsync(inputBuffer, CancellationToken.None);

				// Handle disconnect
				if (result.MessageType == WebSocketMessageType.Close)
				{
					Disconnect();
					return;
				}

				// Write buffer to stream
				inputStream.Write(inputBuffer, 0, result.Count);
			}
			while (!result.EndOfMessage);

			if (result.MessageType == WebSocketMessageType.Binary)
			{
				BinaryReceived?.Invoke(this, inputStream.ToArray());
			}
			else if (result.MessageType == WebSocketMessageType.Text)
			{
				TextReceived?.Invoke(this, Encoding.UTF8.GetString(inputStream.ToArray()));
			}
		}

		// Receive next data
		StartReceivingData();
	}
}
