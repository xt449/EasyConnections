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
using System.Threading.Tasks;

namespace EasyConnections;

public interface IConnection : IDataIO
{
	event EventHandler StatusConnected;
	event EventHandler StatusDisconnected;

	bool Connected { get; }

	UsernamePasswordAuthentication Authentication { get; }

	/// <summary>
	/// This can run forever if the endpoint is not online.<br/>
	/// Use <see cref="StatusConnected"/> to determine when the connection is made.
	/// </summary>
	Task ConnectAsync();

	void Disconnect();
}
