using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-384 authentication mechanism.
	/// </summary>
	internal class SaslScramSha384 : SaslScram {
		private SaslScramSha384()
			: base("SCRAM-SHA-384", 48, ComputeHmac, SHA384.HashData) {
		}

		internal SaslScramSha384(string username, string password, string cnonce)
			: base("SCRAM-SHA-384", 48, ComputeHmac, SHA384.HashData, username,
				password, cnonce) {
		}

		public SaslScramSha384(string username, string password)
			: base("SCRAM-SHA-384", 48, ComputeHmac, SHA384.HashData, username,
				password) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA384(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
