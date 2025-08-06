namespace EasyConnections;

public class UsernamePasswordAuthentication
{
	public readonly string username;
	public readonly string password;

	public UsernamePasswordAuthentication(string username, string password)
	{
		this.username = username;
		this.password = password;
	}

	public static UsernamePasswordAuthentication None { get; } = new UsernamePasswordAuthentication(string.Empty, string.Empty);
}
