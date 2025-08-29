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
