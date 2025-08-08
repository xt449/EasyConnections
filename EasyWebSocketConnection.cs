using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyConnections;

public class EasyWebSocketConnection : IConnection
{
	private readonly AsyncWebSocketClient client;
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

	public event EventHandler<string> DataReceivedAsString
	{
		add => client.TextReceived += value;
		remove => client.TextReceived -= value;
	}

	public bool Connected
	{
		get => client.Connected;
	}

	public Encoding Encoding { get; } = Encoding.UTF8;

	public UsernamePasswordAuthentication Authentication { get; } = UsernamePasswordAuthentication.None;

	/// <param name="authentication">May be used for authentication</param>
	/// <param name="autoReconnect">Reconnect after <see cref="StatusDisconnected"/> is triggered</param>
	/// <param name="connectRetryTimeoutMs">Duration that the socket will attempt to connect before timing out</param>
	/// <param name="connectRetryIntervalMs">Interval that the socket will attempt to connect if the first attempt times out</param>
	public EasyWebSocketConnection(Uri uri, HttpMessageInvoker? httpInvoker = null, bool autoReconnect = true, int connectRetryTimeoutMs = 4_000, int connectRetryIntervalMs = 1_000)
	{
		client = new AsyncWebSocketClient(uri, httpInvoker);

		client.TextReceived += Client_TextReceived;

		if (autoReconnect)
		{
			client.StatusDisconnected += Client_Disconnected;
		}

		this.connectRetryTimeoutMs = connectRetryTimeoutMs;
		this.connectRetryIntervalMs = connectRetryIntervalMs;
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

	public async ValueTask SendBytesAsync(Memory<byte> data) => await client.SendTextAsync(Encoding.GetString(data.Span));

	public async ValueTask SendStringAsync(string text) => await client.SendTextAsync(text);

	// private

	private void Client_Disconnected(object? sender, EventArgs args)
	{
		_ = ConnectAsync();
	}

	private void Client_TextReceived(object? sender, string text)
	{
		// Trigger event
		DataReceivedAsBytes?.Invoke(this, Encoding.GetBytes(text));
	}
}
