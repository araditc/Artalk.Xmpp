using System;
using System.Collections.Generic;
using System.Xml;

namespace Artalk.Xmpp.Core.Sasl {
	internal static class Bind2Feature {
		public const string Namespace = "urn:xmpp:bind:0";

		public static bool IsBind2Element(XmlElement element) {
			return element != null &&
				element.LocalName == "bind" &&
				element.NamespaceURI == Namespace;
		}

		public static XmlElement CreateRequest(string tag = null,
			IEnumerable<XmlElement> inlineRequests = null) {
			var bind = Xml.Element("bind", Namespace);
			if (!String.IsNullOrEmpty(tag))
				bind.Child(Xml.Element("tag", Namespace).Text(tag));
			if (inlineRequests != null) {
				foreach (XmlElement request in inlineRequests) {
					if (request != null)
						bind.Child(request);
				}
			}
			return bind;
		}

		public static Jid GetAuthorizationIdentifier(XmlElement success) {
			success.ThrowIfNull("success");
			foreach (XmlNode node in success.ChildNodes) {
				if (node is XmlElement child &&
					child.LocalName == "authorization-identifier" &&
					child.NamespaceURI == Sasl2Feature.Namespace &&
					!String.IsNullOrWhiteSpace(child.InnerText)) {
					return new Jid(child.InnerText.Trim());
				}
			}
			return null;
		}
	}
}
