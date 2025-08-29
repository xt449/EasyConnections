using System;

namespace EasyConnections.Utility;

public class DelimiterFramedInputBuffer
{
	private readonly string[] frameDelimiters;
	private readonly object receiveLock;

	private string inputBuffer;

	public event EventHandler<string>? FrameReceived;

	public DelimiterFramedInputBuffer(IDataIO io, params string[] delimiters)
	{
		frameDelimiters = delimiters;
		receiveLock = new object();

		inputBuffer = "";

		io.DataReceivedAsString += Io_DataReceivedAsString;
	}

	private void Io_DataReceivedAsString(object? sender, string data)
	{
		lock (receiveLock)
		{
			inputBuffer += data;

			int delimiterEndIndex;

			do
			{
				// Reset delimeter found
				delimiterEndIndex = -1;

				foreach (string delimiter in frameDelimiters)
				{
					delimiterEndIndex = inputBuffer.IndexOf(delimiter);

					if (delimiterEndIndex != -1)
					{
						// Get first frame including delimiter
						string frame = inputBuffer.Substring(0, delimiterEndIndex);

						// Remove up to end of delimeter
						inputBuffer = inputBuffer.Substring(delimiterEndIndex + delimiter.Length);

						FrameReceived?.Invoke(this, frame);
						break;
					}
				}
			}
			while (delimiterEndIndex != -1); // Repeat until no delimeter is found
		}
	}
}
