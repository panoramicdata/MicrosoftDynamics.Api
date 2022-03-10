namespace MicrosoftDynamics.Api;

/// <summary>
/// Options
/// </summary>
public class MicrosoftDynamicsClientOptions
{
	/// <summary>
	/// The authentication URL
	/// </summary>
	public Uri? AuthenticationUri { get; set; }

	/// <summary>
	/// The authentication Client ID
	/// </summary>
	public string? ClientId { get; set; }

	/// <summary>
	/// The authentication Client Secret
	/// </summary>
	public string? ClientSecret { get; set; }

	/// <summary>
	/// The dynamics URL
	/// </summary>
	public Uri? Uri { get; set; }

	/// <summary>
	/// An optional access token.  Required if Authentication, ClientId or ClientSecret are not provided
	/// </summary>
	public string? AccessToken { get; set; }

	/// <summary>
	/// An optional logger
	/// </summary>
	public ILogger Logger { get; set; } = NullLogger.Instance;

	/// <summary>
	/// The OData API version
	/// </summary>
	public string OdataApiVersion { get; set; } = "9.1";

	/// <summary>
	/// Whether to log metadata at the trace level
	/// Disable for unit tests, as this is extremely verbose
	/// </summary>
	public bool LogMetadata { get; set; } = true;

	/// <summary>
	/// Validate the properties
	/// </summary>
	/// <exception cref="ConfigurationException">Thrown if the properties are invalid.</exception>
	public void Validate()
	{
		// Is the AccessToken set?
		if (AccessToken is not null)
		{
			// Yes.  Make sure other credentials are not set.
			if (AuthenticationUri is not null || ClientId is not null || ClientSecret is not null)
			{
				throw new ConfigurationException($"AccessToken is provided, so {nameof(AuthenticationUri)}, {nameof(ClientId)} and {nameof(ClientSecret)} should not be.");
			}
		}
		else
		{
			// No.  Make sure other credentials are set.
			if (AuthenticationUri is null || ClientId is null || ClientSecret is null)
			{
				throw new ConfigurationException($"AccessToken is not provided, so {nameof(AuthenticationUri)}, {nameof(ClientId)} and {nameof(ClientSecret)} all must be.");
			}
		}
	}
}
