using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA3-512 authentication mechanism.
	/// </summary>
	internal class SaslScramSha3_512 : SaslScram {
		private SaslScramSha3_512()
			: base("SCRAM-SHA3-512", 64, ComputeHmac, SHA3_512.HashData) {
		}

		internal SaslScramSha3_512(string username, string password, string cnonce)
			: base("SCRAM-SHA3-512", 64, ComputeHmac, SHA3_512.HashData,
				username, password, cnonce) {
		}

		public SaslScramSha3_512(string username, string password)
			: base("SCRAM-SHA3-512", 64, ComputeHmac, SHA3_512.HashData,
				username, password) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA3_512(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
