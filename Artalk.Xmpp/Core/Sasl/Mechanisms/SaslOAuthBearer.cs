using System;
using System.Globalization;
using System.Text;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements the SASL OAUTHBEARER authentication mechanism described in
	/// RFC 7628.
	/// </summary>
	internal class SaslOAuthBearer : SaslMechanism {
		const char Separator = '\x01';
		bool completed;
		int step;

		public override bool IsCompleted {
			get {
				return completed;
			}
		}

		public override bool HasInitial {
			get {
				return true;
			}
		}

		public override string Name {
			get {
				return "OAUTHBEARER";
			}
		}

		string Username {
			get {
				return Properties.ContainsKey("Username") ?
					Properties["Username"] as string : null;
			}
			set {
				Properties["Username"] = value;
			}
		}

		string Token {
			get {
				return Properties.ContainsKey("OAuthBearerToken") ?
					Properties["OAuthBearerToken"] as string : null;
			}
			set {
				Properties["OAuthBearerToken"] = value;
			}
		}

		string Hostname {
			get {
				return Properties.ContainsKey("Hostname") ?
					Properties["Hostname"] as string : null;
			}
			set {
				Properties["Hostname"] = value;
			}
		}

		int? Port {
			get {
				return Properties.ContainsKey("Port") ?
					(int?) Properties["Port"] : null;
			}
			set {
				Properties["Port"] = value;
			}
		}

		private SaslOAuthBearer() {
		}

		internal SaslOAuthBearer(string username, string token, string hostname,
			int? port = null) {
			username.ThrowIfNullOrEmpty("username");
			token.ThrowIfNullOrEmpty("token");
			Username = username;
			Token = token;
			Hostname = hostname;
			Port = port;
		}

		protected override byte[] ComputeResponse(byte[] challenge) {
			if (step > 0) {
				completed = true;
				return new byte[] { (byte) Separator };
			}
			if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Token))
				throw new SaslException("The username and OAuth bearer token must not " +
					"be null or empty.");

			string response = "n,a=" + EscapeAuthzid(Username) + "," + Separator;
			if (!String.IsNullOrEmpty(Hostname))
				response += "host=" + Hostname + Separator;
			if (Port.HasValue)
				response += "port=" + Port.Value.ToString(CultureInfo.InvariantCulture) +
					Separator;
			response += "auth=Bearer " + Token + Separator + Separator;
			step = step + 1;
			completed = true;
			return Encoding.UTF8.GetBytes(response);
		}

		static string EscapeAuthzid(string authzid) {
			return authzid
				.Replace("=", "=3D")
				.Replace(",", "=2C");
		}
	}
}
