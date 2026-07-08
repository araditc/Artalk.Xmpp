using System;
using System.Collections.Generic;
using System.Xml;

namespace Artalk.Xmpp.Core.Sasl {
	internal sealed class Sasl2Feature {
		public const string Namespace = "urn:xmpp:sasl:2";

		readonly List<string> mechanisms;
		readonly List<XmlElement> inlineFeatures;
		readonly List<string> upgradeTasks;

		public IReadOnlyList<string> Mechanisms {
			get {
				return mechanisms;
			}
		}

		public IReadOnlyList<XmlElement> InlineFeatures {
			get {
				return inlineFeatures;
			}
		}

		public IReadOnlyList<string> UpgradeTasks {
			get {
				return upgradeTasks;
			}
		}

		Sasl2Feature(List<string> mechanisms, List<XmlElement> inlineFeatures,
			List<string> upgradeTasks) {
			this.mechanisms = mechanisms;
			this.inlineFeatures = inlineFeatures;
			this.upgradeTasks = upgradeTasks;
		}

		public static Sasl2Feature Parse(XmlElement features) {
			features.ThrowIfNull("features");
			foreach (XmlNode node in features.ChildNodes) {
				if (node is XmlElement element &&
					element.LocalName == "authentication" &&
					element.NamespaceURI == Namespace) {
					return ParseAuthentication(element);
				}
			}
			return null;
		}

		static Sasl2Feature ParseAuthentication(XmlElement authentication) {
			var mechanisms = new List<string>();
			var inlineFeatures = new List<XmlElement>();
			var upgradeTasks = new List<string>();
			foreach (XmlNode node in authentication.ChildNodes) {
				if (node is not XmlElement element)
					continue;
				if (element.LocalName == "mechanism" &&
					element.NamespaceURI == Namespace) {
					string mechanism = element.InnerText.Trim();
					if (!String.IsNullOrEmpty(mechanism))
						mechanisms.Add(mechanism);
					continue;
				}
				if (element.LocalName == "inline" &&
					element.NamespaceURI == Namespace) {
					foreach (XmlNode inlineNode in element.ChildNodes) {
						if (inlineNode is XmlElement inlineElement)
							inlineFeatures.Add((XmlElement) inlineElement.CloneNode(true));
					}
				}
				if (element.LocalName == "upgrade" &&
					element.NamespaceURI == SaslUpgradeTask.Namespace) {
					string task = element.InnerText.Trim();
					if (!String.IsNullOrEmpty(task))
						upgradeTasks.Add(task);
				}
			}
			return mechanisms.Count == 0 ? null :
				new Sasl2Feature(mechanisms, inlineFeatures, upgradeTasks);
		}
	}
}
