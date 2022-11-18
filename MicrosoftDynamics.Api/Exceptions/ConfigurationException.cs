namespace MicrosoftDynamics.Api.Exceptions;

public class ConfigurationException : Exception
{
	public ConfigurationException(string message) : base(message)
	{
	}

	public ConfigurationException()
	{
	}

	public ConfigurationException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
