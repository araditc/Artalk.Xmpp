using System.Security.Cryptography;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-384-PLUS authentication mechanism.
	/// </summary>
	internal class SaslScramSha384Plus : SaslScram {
		private SaslScramSha384Plus()
			: base("SCRAM-SHA-384-PLUS", 48, ComputeHmac, SHA384.HashData,
				ChannelBinding.TlsServerEndPoint) {
		}

		internal SaslScramSha384Plus(string username, string password, string cnonce)
			: base("SCRAM-SHA-384-PLUS", 48, ComputeHmac, SHA384.HashData,
				username, password, cnonce, ChannelBinding.TlsServerEndPoint) {
		}

		static byte[] ComputeHmac(byte[] key, byte[] data) {
			using (var hmac = new HMACSHA384(key)) {
				return hmac.ComputeHash(data);
			}
		}
	}
}
