using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml;

namespace Artalk.Xmpp.Core {
	internal sealed class BoshTransport : IDisposable {
		const string BoshNamespace = "http://jabber.org/protocol/httpbind";
		const string XboshNamespace = "urn:xmpp:xbosh";
		readonly Uri url;
		readonly string to;
		readonly HttpClient http;
		readonly bool ownsHttp;
		readonly Queue<XmlElement> received = new Queue<XmlElement>();
		readonly object sync = new object();
		ulong rid;
		string sid;
		bool disposed;

		public CultureInfo Language {
			get;
			private set;
		}

		public bool IsEncrypted {
			get {
				return String.Equals(url.Scheme, Uri.UriSchemeHttps,
					StringComparison.OrdinalIgnoreCase);
			}
		}

		public BoshTransport(Uri url, string to, HttpMessageHandler handler = null) {
			url.ThrowIfNull("url");
			to.ThrowIfNullOrEmpty("to");
			if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
				throw new ArgumentException("BOSH URL must use HTTP or HTTPS.", "url");
			this.url = url;
			this.to = to;
			rid = CreateInitialRid();
			if (handler == null) {
				http = new HttpClient();
				ownsHttp = true;
			} else {
				http = new HttpClient(handler, false);
			}
		}

		public XmlElement Open() {
			lock (sync) {
				XmlElement body = CreateBody()
					.Attr("to", to)
					.Attr("xml:lang", CultureInfo.CurrentCulture.Name)
					.Attr("wait", "60")
					.Attr("hold", "1")
					.Attr("ver", "1.6")
					.Attr("xmpp:version", "1.0")
					.Attr("xmlns:xmpp", XboshNamespace);
				XmlElement response = Post(body);
				sid = response.GetAttribute("sid");
				if (String.IsNullOrEmpty(sid))
					throw new IOException("The BOSH connection manager did not return a session id.");
				string lang = response.GetAttribute("xml:lang");
				if (!String.IsNullOrEmpty(lang))
					Language = new CultureInfo(lang);
				EnqueueChildren(response);
				return Receive("stream:features");
			}
		}

		public XmlElement Restart() {
			lock (sync) {
				EnsureOpen();
				XmlElement body = CreateBody()
					.Attr("sid", sid)
					.Attr("to", to)
					.Attr("xmpp:restart", "true")
					.Attr("xmlns:xmpp", XboshNamespace);
				XmlElement response = Post(body);
				EnqueueChildren(response);
				return Receive("stream:features");
			}
		}

		public void Send(XmlElement element) {
			element.ThrowIfNull("element");
			Send(element.ToXmlString());
		}

		public void Send(string xml) {
			xml.ThrowIfNull("xml");
			lock (sync) {
				EnsureOpen();
				XmlElement body = CreateBody().Attr("sid", sid);
				AddPayload(body, xml);
				XmlElement response = Post(body);
				EnqueueChildren(response);
			}
		}

		public XmlElement Receive(params string[] expected) {
			lock (sync) {
				EnsureOpen();
				while (received.Count == 0) {
					XmlElement response = Post(CreateBody().Attr("sid", sid));
					EnqueueChildren(response);
				}
				XmlElement element = received.Dequeue();
				if (element.Name == "stream:error") {
					string condition = element.FirstChild != null ?
						element.FirstChild.Name : "undefined";
					throw new IOException("Unrecoverable stream error: " + condition);
				}
				if (expected.Length > 0 && !expected.Contains(element.Name))
					throw new XmlException("Unexpected XML element: " + element.Name);
				return element;
			}
		}

		public void Close() {
			lock (sync) {
				if (disposed || String.IsNullOrEmpty(sid))
					return;
				try {
					Post(CreateBody().Attr("sid", sid).Attr("type", "terminate"));
				} finally {
					sid = null;
				}
			}
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			if (ownsHttp)
				http.Dispose();
		}

		XmlElement CreateBody() {
			return Xml.Element("body", BoshNamespace)
				.Attr("rid", (++rid).ToString(CultureInfo.InvariantCulture));
		}

		XmlElement Post(XmlElement body) {
			HttpContent content = new StringContent(body.ToXmlString(), Encoding.UTF8,
				"text/xml");
			HttpResponseMessage response = http.PostAsync(url, content)
				.GetAwaiter().GetResult();
			response.EnsureSuccessStatusCode();
			string xml = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			if (String.IsNullOrWhiteSpace(xml))
				throw new IOException("The BOSH connection manager returned an empty response.");
			XmlDocument document = new XmlDocument();
			document.LoadXml(xml);
			XmlElement responseBody = document.DocumentElement;
			if (responseBody == null || responseBody.LocalName != "body")
				throw new XmlException("Expected a BOSH body response.");
			string type = responseBody.GetAttribute("type");
			if (type == "terminate")
				throw new IOException("The BOSH connection manager terminated the session.");
			return responseBody;
		}

		void EnqueueChildren(XmlElement body) {
			foreach (XmlNode node in body.ChildNodes) {
				XmlElement element = node as XmlElement;
				if (element != null)
					received.Enqueue(element);
			}
		}

		void AddPayload(XmlElement body, string xml) {
			XmlDocument payload = new XmlDocument();
			payload.LoadXml(xml);
			XmlNode imported = body.OwnerDocument.ImportNode(payload.DocumentElement, true);
			body.AppendChild(imported);
		}

		void EnsureOpen() {
			if (disposed)
				throw new ObjectDisposedException(GetType().FullName);
			if (String.IsNullOrEmpty(sid))
				throw new InvalidOperationException("The BOSH session has not been opened.");
		}

		static ulong CreateInitialRid() {
			byte[] bytes = Guid.NewGuid().ToByteArray();
			ulong value = BitConverter.ToUInt64(bytes, 0);
			return value % 9007199254740991UL;
		}
	}
}
