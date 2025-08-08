using System;
using System.Text.RegularExpressions;

namespace EasyConnections.Utility;

public class FramingInputBuffer
{
	private readonly IDataIO io;
	private readonly Regex[] frameExpressions;

	private readonly object receiveLock;
	private string inputBuffer;

	public event EventHandler<Match>? FrameReceived;

	public FramingInputBuffer(IDataIO io, params Regex[] frameExpressions)
	{
		this.io = io;
		this.frameExpressions = frameExpressions;

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

				foreach (Regex regex in frameExpressions)
				{
					match = regex.Match(inputBuffer);

					if (match.Success)
					{
						// Remove up to end of match
						inputBuffer = inputBuffer.Remove(0, match.Index + match.Length);

						FrameReceived?.Invoke(this, match);
						break;
					}
				}
			}
			while (match != null && match.Success); // Repeat until no match is found
		}
	}
}
