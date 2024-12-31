using System;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSocketLibrary
{
	public interface IConnection
	{
		event EventHandler StatusConnected;
		event EventHandler StatusDisconnected;
		event EventHandler<byte[]> DataReceivedAsBytes;
		event EventHandler<string> DataReceivedAsString;

		bool Connected { get; }

		Encoding Encoding { get; }

		/// <summary>
		/// This can run forever if the endpoint is not online.<br/>
		/// Use <see cref="StatusConnected"/> to determine when the connection is made.
		/// </summary>
		void ConnectAsync();

		void Disconnect();

		ValueTask SendBytesAsync(Memory<byte> buffer);

		ValueTask SendStringAsync(string text);
	}
}
