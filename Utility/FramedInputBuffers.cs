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
using System.Text.RegularExpressions;

namespace EasyConnections.Utility;

public static class FramedInputBuffers
{
	public static void AddDelimiterFramedReceiveListener(this IDataIO io, Action<string> listener, params string[] delimiters)
	{
		new DelimiterFramedInputBuffer(io, delimiters).FrameReceived += (_, data) => listener(data);
	}

	public static void AddRegexFramedReceiveListener(this IDataIO io, Action<Match> listener, params Regex[] regexes)
	{
		new RegexFramedInputBuffer(io, regexes).FrameReceived += (_, data) => listener(data);
	}
}
