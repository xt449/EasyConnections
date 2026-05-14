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
using System.Text;
using System.Threading.Tasks;

namespace EasyConnections;

public interface IDataIO
{
	event EventHandler<byte[]> DataReceivedAsBytes;
	event EventHandler<string> DataReceivedAsString;

	Encoding Encoding { get; }

	ValueTask SendBytesAsync(Memory<byte> buffer);
	ValueTask SendStringAsync(string text);
}
