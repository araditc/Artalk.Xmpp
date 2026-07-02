using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Artalk.Xmpp.Core {
	internal sealed class WebSocketTransport : IDisposable {
		const string FramingNamespace = "urn:ietf:params:xml:ns:xmpp-framing";
		const string SubProtocol = "xmpp";
		readonly Uri url;
		readonly string to;
		readonly IWebSocketConnection connection;
		readonly bool ownsConnection;
		readonly object sync = new object();
		bool opened;
		bool disposed;

		public CultureInfo Language {
			get;
			private set;
		}

		public bool IsEncrypted {
			get {
				return String.Equals(url.Scheme, "wss", StringComparison.OrdinalIgnoreCase);
			}
		}

		public WebSocketTransport(Uri url, string to)
			: this(url, to, new ClientWebSocketConnection(), true) {
		}

		internal WebSocketTransport(Uri url, string to, IWebSocketConnection connection,
			bool ownsConnection = false) {
			url.ThrowIfNull("url");
			to.ThrowIfNullOrEmpty("to");
			connection.ThrowIfNull("connection");
			if (url.Scheme != "ws" && url.Scheme != "wss")
				throw new ArgumentException("WebSocket URL must use WS or WSS.", "url");
			this.url = url;
			this.to = to;
			this.connection = connection;
			this.ownsConnection = ownsConnection;
		}

		public XmlElement Open() {
			lock (sync) {
				EnsureNotDisposed();
				if (!opened) {
					connection.AddSubProtocol(SubProtocol);
					connection.ConnectAsync(url, CancellationToken.None)
						.GetAwaiter().GetResult();
					if (!String.Equals(connection.SubProtocol, SubProtocol,
						StringComparison.OrdinalIgnoreCase)) {
						throw new IOException("The WebSocket server did not accept the XMPP subprotocol.");
					}
				}
				SendOpen();
				ReadOpen();
				opened = true;
				return Receive("stream:features");
			}
		}

		public XmlElement Restart() {
			lock (sync) {
				EnsureOpen();
				SendOpen();
				ReadOpen();
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
				SendFrame(xml);
			}
		}

		public XmlElement Receive(params string[] expected) {
			lock (sync) {
				EnsureOpen();
				XmlElement element = ReceiveFrame();
				if (element.LocalName == "close" && element.NamespaceURI == FramingNamespace)
					throw new IOException("The WebSocket XMPP stream was closed.");
				if (element.Name == "stream:error" || element.Name == "error") {
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
				if (disposed || !opened)
					return;
				try {
					SendFrame(Xml.Element("close", FramingNamespace).ToXmlString());
					connection.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
						"XMPP stream closed", CancellationToken.None)
						.GetAwaiter().GetResult();
				} finally {
					opened = false;
				}
			}
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			if (ownsConnection)
				connection.Dispose();
		}

		void SendOpen() {
			var open = Xml.Element("open", FramingNamespace)
				.Attr("to", to)
				.Attr("version", "1.0")
				.Attr("xml:lang", CultureInfo.CurrentCulture.Name);
			SendFrame(open.ToXmlString());
		}

		void ReadOpen() {
			XmlElement open = ReceiveFrame();
			if (open.LocalName != "open" || open.NamespaceURI != FramingNamespace)
				throw new XmlException("Expected WebSocket XMPP open frame.");
			string lang = open.GetAttribute("xml:lang");
			if (!String.IsNullOrEmpty(lang))
				Language = new CultureInfo(lang);
		}

		void SendFrame(string xml) {
			byte[] bytes = Encoding.UTF8.GetBytes(xml);
			connection.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text,
				true, CancellationToken.None).GetAwaiter().GetResult();
		}

		XmlElement ReceiveFrame() {
			var buffer = new byte[4096];
			using var memory = new MemoryStream();
			WebSocketReceiveResult result;
			do {
				result = connection.ReceiveAsync(new ArraySegment<byte>(buffer),
					CancellationToken.None).GetAwaiter().GetResult();
				if (result.MessageType == WebSocketMessageType.Close)
					throw new IOException("The WebSocket connection was closed.");
				if (result.MessageType != WebSocketMessageType.Text)
					throw new IOException("Expected a WebSocket text frame.");
				memory.Write(buffer, 0, result.Count);
			} while (!result.EndOfMessage);

			string xml = Encoding.UTF8.GetString(memory.ToArray());
			XmlDocument document = new XmlDocument();
			document.LoadXml(xml);
			return document.DocumentElement;
		}

		void EnsureOpen() {
			EnsureNotDisposed();
			if (!opened)
				throw new InvalidOperationException("The WebSocket XMPP stream has not been opened.");
		}

		void EnsureNotDisposed() {
			if (disposed)
				throw new ObjectDisposedException(GetType().FullName);
		}

		internal interface IWebSocketConnection : IDisposable {
			string SubProtocol {
				get;
			}

			void AddSubProtocol(string subProtocol);
			Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
			Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
				bool endOfMessage, CancellationToken cancellationToken);
			Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
				CancellationToken cancellationToken);
			Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
				string statusDescription, CancellationToken cancellationToken);
		}

		sealed class ClientWebSocketConnection : IWebSocketConnection {
			readonly ClientWebSocket socket = new ClientWebSocket();

			public string SubProtocol => socket.SubProtocol;

			public void AddSubProtocol(string subProtocol) {
				socket.Options.AddSubProtocol(subProtocol);
			}

			public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) {
				return socket.ConnectAsync(uri, cancellationToken);
			}

			public Task SendAsync(ArraySegment<byte> buffer,
				WebSocketMessageType messageType, bool endOfMessage,
				CancellationToken cancellationToken) {
				return socket.SendAsync(buffer, messageType, endOfMessage,
					cancellationToken);
			}

			public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
				CancellationToken cancellationToken) {
				return socket.ReceiveAsync(buffer, cancellationToken);
			}

			public Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
				string statusDescription, CancellationToken cancellationToken) {
				return socket.CloseOutputAsync(closeStatus, statusDescription,
					cancellationToken);
			}

			public void Dispose() {
				socket.Dispose();
			}
		}
	}
}
