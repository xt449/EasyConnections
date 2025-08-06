using System;
using System.Threading.Tasks;

namespace EasyConnections;

public interface IConnection : IDataIO
{
	event EventHandler StatusConnected;
	event EventHandler StatusDisconnected;

	bool Connected { get; }

	/// <summary>
	/// This can run forever if the endpoint is not online.<br/>
	/// Use <see cref="StatusConnected"/> to determine when the connection is made.
	/// </summary>
	Task ConnectAsync();

	void Disconnect();
}
