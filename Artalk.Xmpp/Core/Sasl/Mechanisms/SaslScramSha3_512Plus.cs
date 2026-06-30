using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA3-512-PLUS authentication mechanism.
	/// </summary>
	internal class SaslScramSha3_512Plus : SaslScram {
		private SaslScramSha3_512Plus()
			: base("SCRAM-SHA3-512-PLUS", 64, ComputeHmac, SHA3_512.HashData,
				ChannelBinding.TlsServerEndPoint) {
		}

		internal SaslScramSha3_512Plus(string username, string password, string cnonce)
			: base("SCRAM-SHA3-512-PLUS", 64, ComputeHmac, SHA3_512.HashData,
				username, password, cnonce, ChannelBinding.TlsServerEndPoint) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA3_512(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
