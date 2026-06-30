namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL SCRAM-SHA-224-PLUS authentication mechanism.
	/// </summary>
	internal class SaslScramSha224Plus : SaslScram {
		private SaslScramSha224Plus()
			: base("SCRAM-SHA-224-PLUS", 28, Sha224.ComputeHmac, Sha224.HashData,
				ChannelBinding.TlsServerEndPoint) {
		}

		internal SaslScramSha224Plus(string username, string password, string cnonce)
			: base("SCRAM-SHA-224-PLUS", 28, Sha224.ComputeHmac, Sha224.HashData,
				username, password, cnonce, ChannelBinding.TlsServerEndPoint) {
		}
	}
}
