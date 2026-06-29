using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-512 authentication mechanism.
	/// </summary>
	internal class SaslScramSha512 : SaslScram {
		private SaslScramSha512()
			: base("SCRAM-SHA-512", 64, ComputeHmac, SHA512.HashData) {
		}

		internal SaslScramSha512(string username, string password, string cnonce)
			: base("SCRAM-SHA-512", 64, ComputeHmac, SHA512.HashData, username,
				password, cnonce) {
		}

		public SaslScramSha512(string username, string password)
			: base("SCRAM-SHA-512", 64, ComputeHmac, SHA512.HashData, username,
				password) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA512(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
