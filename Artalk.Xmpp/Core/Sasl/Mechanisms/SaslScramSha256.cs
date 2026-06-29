using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-256 authentication mechanism.
	/// </summary>
	internal class SaslScramSha256 : SaslScram {
		private SaslScramSha256()
			: base("SCRAM-SHA-256", 32, ComputeHmac, SHA256.HashData) {
		}

		internal SaslScramSha256(string username, string password, string cnonce)
			: base("SCRAM-SHA-256", 32, ComputeHmac, SHA256.HashData, username,
				password, cnonce) {
		}

		public SaslScramSha256(string username, string password)
			: base("SCRAM-SHA-256", 32, ComputeHmac, SHA256.HashData, username,
				password) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA256(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
