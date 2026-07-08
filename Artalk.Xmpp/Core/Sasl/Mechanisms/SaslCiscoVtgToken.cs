using System;
using System.Text;

namespace Artalk.Xmpp.Core.Sasl.Mechanisms {
	/// <summary>
	/// Implements Cisco's SASL CISCO-VTG-TOKEN authentication mechanism.
	/// </summary>
	internal class SaslCiscoVtgToken : SaslMechanism {
		bool completed;

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
				return "CISCO-VTG-TOKEN";
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
				return Properties.ContainsKey("CiscoVtgToken") ?
					Properties["CiscoVtgToken"] as string : null;
			}
			set {
				Properties["CiscoVtgToken"] = value;
			}
		}

		private SaslCiscoVtgToken() {
		}

		internal SaslCiscoVtgToken(string username, string token) {
			username.ThrowIfNullOrEmpty("username");
			token.ThrowIfNullOrEmpty("token");
			Username = username;
			Token = token;
		}

		protected override byte[] ComputeResponse(byte[] challenge) {
			if (completed)
				return Array.Empty<byte>();
			if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Token))
				throw new SaslException("The username and Cisco VTG token must not " +
					"be null or empty.");

			completed = true;
			return Encoding.UTF8.GetBytes("userid=" + Username + "\0token=" + Token);
		}
	}
}
