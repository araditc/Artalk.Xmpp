using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-256 authentication mechanism.
	/// </summary>
	internal class SaslScramSha256 : SaslScram {
		private SaslScramSha256()
			: base("SCRAM-SHA-256", HashAlgorithmName.SHA256, 32,
				key => new HMACSHA256(key)) {
		}

		internal SaslScramSha256(string username, string password, string cnonce)
			: base("SCRAM-SHA-256", HashAlgorithmName.SHA256, 32,
				key => new HMACSHA256(key), username, password, cnonce) {
		}

		public SaslScramSha256(string username, string password)
			: base("SCRAM-SHA-256", HashAlgorithmName.SHA256, 32,
				key => new HMACSHA256(key), username, password) {
		}
	}
}
