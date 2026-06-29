using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-1 authentication mechanism.
	/// </summary>
	internal class SaslScramSha1 : SaslScram {
		private SaslScramSha1()
			: base("SCRAM-SHA-1", 20, ComputeHmac, SHA1.HashData) {
		}

		internal SaslScramSha1(string username, string password, string cnonce)
			: base("SCRAM-SHA-1", 20, ComputeHmac, SHA1.HashData, username,
				password, cnonce) {
		}

		public SaslScramSha1(string username, string password)
			: base("SCRAM-SHA-1", 20, ComputeHmac, SHA1.HashData, username,
				password) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA1(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
