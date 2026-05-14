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

public class RegexFramedInputBuffer
{
	private readonly Regex[] frameRegexes;
	private readonly object receiveLock;

	private string inputBuffer;

	public event EventHandler<Match>? FrameReceived;

	public RegexFramedInputBuffer(IDataIO io, params Regex[] regexes)
	{
		frameRegexes = regexes;
		receiveLock = new object();

		inputBuffer = "";

		io.DataReceivedAsString += Io_DataReceivedAsString;
	}

	private void Io_DataReceivedAsString(object? sender, string data)
	{
		lock (receiveLock)
		{
			inputBuffer += data;

			Match? match;

			do
			{
				// Reset match found
				match = null;

				foreach (Regex regex in frameRegexes)
				{
					match = regex.Match(inputBuffer);

					if (match.Success)
					{
						// Remove up to end of match
						inputBuffer = inputBuffer.Substring(match.Index + match.Length);

						FrameReceived?.Invoke(this, match);
						break;
					}
				}
			}
			while (match != null && match.Success); // Repeat until no match is found
		}
	}
}
