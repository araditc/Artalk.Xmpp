using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-256-PLUS authentication mechanism.
	/// </summary>
	internal class SaslScramSha256Plus : SaslScram {
		private SaslScramSha256Plus()
			: base("SCRAM-SHA-256-PLUS", 32, ComputeHmac, SHA256.HashData,
				ChannelBinding.TlsServerEndPoint) {
		}

		internal SaslScramSha256Plus(string username, string password, string cnonce)
			: base("SCRAM-SHA-256-PLUS", 32, ComputeHmac, SHA256.HashData,
				username, password, cnonce, ChannelBinding.TlsServerEndPoint) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA256(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
