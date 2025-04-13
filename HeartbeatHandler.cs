namespace EasyConnections;

using System;
using System.Threading;

public class HeartbeatHandler
{
	private readonly IConnection connection;
	private readonly byte[] heartbeatPayload;
	private readonly int receiveTimeoutMs;
	private readonly int heartbeatTimeoutMs;

	private readonly Timer receiveTimer;
	private readonly Timer heartbeatTimer;

	public event EventHandler? HeartbeatTimedOut;

	public HeartbeatHandler(IConnection connection, Memory<byte> heartbeatPayload, int receiveTimeoutMs = 3_000, int heartbeatTimeoutMs = 5_000)
	{
		this.connection = connection;
		this.heartbeatPayload = heartbeatPayload.ToArray();
		this.receiveTimeoutMs = receiveTimeoutMs;
		this.heartbeatTimeoutMs = heartbeatTimeoutMs;

		receiveTimer = new Timer(ReceiveTimeout, null, Timeout.Infinite, Timeout.Infinite);
		heartbeatTimer = new Timer(HeartbeatTimeout, null, Timeout.Infinite, Timeout.Infinite);

		this.connection.StatusConnected += Inner_StatusConnected;
		this.connection.StatusDisconnected += Inner_StatusDisconnected;
		this.connection.DataReceivedAsBytes += Inner_DataReceivedAsBytes;
	}

	public HeartbeatHandler(IConnection inner, string heartbeatPayload, int receiveTimeoutMs = 3_000, int heartbeatTimeoutMs = 5_000)
		: this(inner, inner.Encoding.GetBytes(heartbeatPayload), receiveTimeoutMs, heartbeatTimeoutMs)
	{
	}

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
		await connection.SendBytesAsync(heartbeatPayload);

		// Reset timer
		heartbeatTimer.Change(heartbeatTimeoutMs, Timeout.Infinite);
	}

	private void HeartbeatTimeout(object? _)
	{
		// Trigger event
		HeartbeatTimedOut?.Invoke(this, EventArgs.Empty);
	}
}
