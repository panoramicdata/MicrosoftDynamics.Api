using System.Runtime.Serialization;

namespace MicrosoftDynamics.Api
{
	[DataContract]
	public class BearerTokenResponse
	{
		[DataMember(Name = "token_type")]
		public string TokenType { get; set; } = string.Empty;

		[DataMember(Name = "expires_in")]
		public int ExpiresIn { get; set; }

		[DataMember(Name = "ext_expires_in")]
		public int ExtExpiresIn { get; set; }

		[DataMember(Name = "access_token")]
		public string AccessToken { get; set; } = string.Empty;
	}
}