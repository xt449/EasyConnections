using System;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSocketLibrary
{
	public interface IDataIO
	{
		event EventHandler<byte[]> DataReceivedAsBytes;
		event EventHandler<string> DataReceivedAsString;

		Encoding Encoding { get; }

		ValueTask SendBytesAsync(Memory<byte> buffer);
		ValueTask SendStringAsync(string text);
	}
}
