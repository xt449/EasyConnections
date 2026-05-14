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
using System.Threading;

namespace EasyConnections;

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
