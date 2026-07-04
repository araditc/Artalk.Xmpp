using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace Artalk.Xmpp {
	/// <summary>
	/// Represents the address of an XMPP entity, also known as Jabber Identifier (JID).
	/// </summary>
	[Serializable]
	public sealed class Jid {
		const int MaxPartOctets = 1023;
		static readonly char[] LocalpartExcludedChars = { '"', '&', '\'', '/', ':', '<', '>', '@' };
		static readonly IdnMapping Idn = new IdnMapping {
			UseStd3AsciiRules = true
		};

		/// <summary>
		/// The domain identifier of the JID.
		/// </summary>
		public string Domain {
			get;
			private set;
		}

		/// <summary>
		/// The node identifier of the JID. This may be null or empty.
		/// </summary>
		public string Node {
			get;
			private set;
		}

		/// <summary>
		/// The resource identifier of the JID. This may be null or empty.
		/// </summary>
		public string Resource {
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the JID is a 'bare JID', i.e. a JID without resource
		/// identifier.
		/// </summary>
		public bool IsBareJid {
			get {
				return !String.IsNullOrEmpty(Domain) && String.IsNullOrEmpty(Resource);
			}
		}

		/// <summary>
		/// Determines whether the JID is a 'full JID', i.e. a JID with both a node
		/// and a resource identifier.
		/// </summary>
		public bool IsFullJid {
			get {
				return !String.IsNullOrEmpty(Domain) && !String.IsNullOrEmpty(Resource);
			}
		}

		/// <summary>
		/// Initializes a new instance of the JID class.
		/// </summary>
		/// <param name="jid">A string from which to construct the JID.</param>
		/// <exception cref="ArgumentNullException">The jid parameter is
		/// null.</exception>
		/// <exception cref="ArgumentException">The jid parameter does not
		/// represent a valid JID.</exception>
		public Jid(string jid) {
			jid.ThrowIfNullOrEmpty("jid");
			Parse(jid, out string domain, out string node, out string resource);
			Domain = domain;
			Node = node;
			Resource = resource;
		}

		/// <summary>
		/// Initializes a new instance of the JID class using the specified domain,
		/// node and optionally resource.
		/// </summary>
		/// <param name="domain">The domain of the JID.</param>
		/// <param name="node">The node of the JID.</param>
		/// <param name="resource">The resource of the JID. This may be omitted.</param>
		/// <exception cref="ArgumentNullException">The domain parameter is null.</exception>
		/// <exception cref="ArgumentException">The domain parameter is the
		/// empty string.</exception>
		public Jid(string domain, string node, string resource = null) {
			domain.ThrowIfNullOrEmpty("domain");
			Domain = NormalizeDomainpart(domain);
			Node = node != null ? NormalizeLocalpart(node) : null;
			Resource = resource != null ? NormalizeResourcepart(resource) : null;
		}

		static void Parse(string jid, out string domain, out string node,
			out string resource) {
			int slash = jid.IndexOf('/');
			string address = slash >= 0 ? jid.Substring(0, slash) : jid;
			resource = slash >= 0 ? jid.Substring(slash + 1) : null;
			if (resource == String.Empty)
				throw new ArgumentException("The resourcepart must not be empty.");

			int at = address.IndexOf('@');
			if (at >= 0) {
				node = address.Substring(0, at);
				domain = address.Substring(at + 1);
				if (node == String.Empty)
					throw new ArgumentException("The localpart must not be empty.");
				if (domain.IndexOf('@') >= 0)
					throw new ArgumentException("The domainpart must not contain '@'.");
			} else {
				node = null;
				domain = address;
			}

			domain = NormalizeDomainpart(domain);
			node = node != null ? NormalizeLocalpart(node) : null;
			resource = resource != null ? NormalizeResourcepart(resource) : null;
		}

		static string NormalizeDomainpart(string domain) {
			domain.ThrowIfNullOrEmpty("domain");
			string value = domain.TrimEnd('.');
			if (value == String.Empty)
				throw new ArgumentException("The domainpart must not be empty.");
			if (value.IndexOfAny(new[] { '@', '/' }) >= 0)
				throw new ArgumentException("The domainpart contains an invalid separator.");

			string normalized = NormalizeIpLiteral(value) ?? NormalizeDomainName(value);
			EnsureOctetLength(normalized, "domainpart");
			return normalized;
		}

		static string NormalizeDomainName(string domain) {
			try {
				string ascii = Idn.GetAscii(domain);
				string unicode = Idn.GetUnicode(ascii);
				return unicode.Normalize(NormalizationForm.FormC).ToLowerInvariant();
			} catch (ArgumentException ex) {
				throw new ArgumentException("The domainpart is not a valid IDN hostname.", ex);
			}
		}

		static string NormalizeIpLiteral(string domain) {
			if (IPAddress.TryParse(domain, out IPAddress address))
				return address.ToString().ToLowerInvariant();

			if (domain.Length > 2 && domain[0] == '[' && domain[domain.Length - 1] == ']') {
				string literal = domain.Substring(1, domain.Length - 2);
				if (IPAddress.TryParse(literal, out address) &&
					address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
					return "[" + address + "]";
			}
			return null;
		}

		static string NormalizeLocalpart(string localpart) {
			localpart.ThrowIfNullOrEmpty("localpart");
			if (localpart.IndexOfAny(LocalpartExcludedChars) >= 0)
				throw new ArgumentException("The localpart contains a character excluded by RFC 7622.");

			string normalized = localpart.Normalize(NormalizationForm.FormKC)
				.ToLowerInvariant()
				.Normalize(NormalizationForm.FormC);
			EnsureIdentifierCodePoints(normalized, "localpart");
			EnsureOctetLength(normalized, "localpart");
			return normalized;
		}

		static string NormalizeResourcepart(string resourcepart) {
			resourcepart.ThrowIfNullOrEmpty("resourcepart");
			string mapped = MapNonAsciiSpaces(resourcepart)
				.Normalize(NormalizationForm.FormC);
			EnsureIdentifierCodePoints(mapped, "resourcepart");
			EnsureOctetLength(mapped, "resourcepart");
			return mapped;
		}

		static string MapNonAsciiSpaces(string value) {
			StringBuilder builder = null;
			for (int i = 0; i < value.Length; i++) {
				if (value[i] != ' ' &&
					CharUnicodeInfo.GetUnicodeCategory(value, i) == UnicodeCategory.SpaceSeparator) {
					if (builder == null)
						builder = new StringBuilder(value);
					builder[i] = ' ';
				}
			}
			return builder != null ? builder.ToString() : value;
		}

		static void EnsureIdentifierCodePoints(string value, string partName) {
			for (int i = 0; i < value.Length; i++) {
				char c = value[i];
				if (Char.IsHighSurrogate(c)) {
					if (i + 1 >= value.Length || !Char.IsLowSurrogate(value[i + 1]))
						throw new ArgumentException("The " + partName + " contains an invalid Unicode surrogate.");
					i++;
					continue;
				}
				if (Char.IsLowSurrogate(c))
					throw new ArgumentException("The " + partName + " contains an invalid Unicode surrogate.");
				if (Char.IsControl(c))
					throw new ArgumentException("The " + partName + " must not contain control characters.");
			}
		}

		static void EnsureOctetLength(string value, string partName) {
			int octets = Encoding.UTF8.GetByteCount(value);
			if (octets == 0 || octets > MaxPartOctets)
				throw new ArgumentException("The " + partName + " must be between 1 and 1023 UTF-8 octets.");
		}

		/// <summary>
		/// Implicit conversion operator for type string to type Jid.
		/// </summary>
		/// <param name="jid">The string to convert into a Jid instance.</param>
		/// <returns>A Jid instance created from the specified string.</returns>
		public static implicit operator Jid(string jid) {
			return new Jid(jid);
		}

		/// <summary>
		/// Returns a new JID instance representing the 'bare JID' constructd from
		/// this JID.
		/// </summary>
		/// <returns>A bare JID constructed from this JID instance.</returns>
		public Jid GetBareJid() {
			return new Jid(Domain, Node);
		}

		/// <summary>
		/// Returns a textual representation of the JID.
		/// </summary>
		/// <returns>A textual representation of this JID instance.</returns>
		public override string ToString() {
			StringBuilder b = new StringBuilder();
			if (!String.IsNullOrEmpty(Node))
				b.Append(Node + "@");
			b.Append(Domain);
			if (!String.IsNullOrEmpty(Resource))
				b.Append("/" + Resource);
			return b.ToString();
		}

		/// <summary>
		/// Determines whether the specified object is equal to this Jid
		/// instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>True if the specified object is semantically equal to this
		/// Jid instance; Otherwise false.</returns>
		public override bool Equals(object obj) {
			if (obj == null)
				return false;
			Jid other = obj as Jid;
			if (other == null)
				return false;
			return Node == other.Node && Domain == other.Domain &&
				Resource == other.Resource;
		}

		/// <summary>
		/// Returns the hash code of this instance.
		/// </summary>
		/// <returns>The hash code of this JID instance.</returns>
		public override int GetHashCode() {
			int hash = 13;
			if(Node != null)
				hash = (hash * 7) + Node.GetHashCode();
			hash = (hash * 7) + Domain.GetHashCode();
			if (Resource != null)
				hash = (hash * 7) + Resource.GetHashCode();
			return hash;
		}

		/// <summary>
		/// Determines whether the specified Jid objects are equal.
		/// </summary>
		/// <param name="a">The first object.</param>
		/// <param name="b">The second object.</param>
		/// <returns>True if the specified objects are semantically equal;
		/// Otherwise false.</returns>
		public static bool operator ==(Jid a, Jid b) {
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object) a == null) || ((object) b == null))
				return false;
			return a.Node == b.Node && a.Domain == b.Domain &&
				a.Resource == b.Resource;
		}

		/// <summary>
		/// Determines whether the specified Jid objects are unequal.
		/// </summary>
		/// <param name="a">The first object.</param>
		/// <param name="b">The second object.</param>
		/// <returns>True if the specified objects are not semantically equal;
		/// Otherwise false.</returns>
		public static bool operator !=(Jid a, Jid b) {
			return !(a == b);
		}
	}
}
