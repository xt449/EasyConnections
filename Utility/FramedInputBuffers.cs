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
