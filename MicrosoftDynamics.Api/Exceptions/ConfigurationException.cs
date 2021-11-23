namespace MicrosoftDynamics.Api.Exceptions;

public class ConfigurationException : Exception
{
	public ConfigurationException(string message) : base(message)
	{
	}

	public ConfigurationException() : base()
	{
	}

	public ConfigurationException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
