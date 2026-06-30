using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-512-PLUS authentication mechanism.
	/// </summary>
	internal class SaslScramSha512Plus : SaslScram {
		private SaslScramSha512Plus()
			: base("SCRAM-SHA-512-PLUS", 64, ComputeHmac, SHA512.HashData,
				ChannelBinding.TlsServerEndPoint) {
		}

		internal SaslScramSha512Plus(string username, string password, string cnonce)
			: base("SCRAM-SHA-512-PLUS", 64, ComputeHmac, SHA512.HashData,
				username, password, cnonce, ChannelBinding.TlsServerEndPoint) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA512(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
