using Artalk.Xmpp.Core;
using System.Net.WebSockets;
using System.Text;
using System.Xml;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class WebSocketTransportTests {
	[TestMethod]
	public void OpenNegotiatesXmppSubprotocolAndReturnsFeatures() {
		var socket = new FakeWebSocketConnection(
			"<open xmlns='urn:ietf:params:xml:ns:xmpp-framing' " +
			"from='example.com' xml:lang='en' version='1.0'/>",
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<mechanisms xmlns='urn:ietf:params:xml:ns:xmpp-sasl'/>" +
			"</stream:features>");
		var transport = new WebSocketTransport(new Uri("wss://example.com/xmpp"),
			"example.com", socket);

		XmlElement features = transport.Open();

		Assert.AreEqual("wss://example.com/xmpp", socket.ConnectedUri!.ToString());
		CollectionAssert.Contains(socket.SubProtocols, "xmpp");
		Assert.AreEqual("stream:features", features.Name);
		Assert.AreEqual("en", transport.Language.Name);
		StringAssert.Contains(socket.Sent[0],
			"xmlns='urn:ietf:params:xml:ns:xmpp-framing'");
		StringAssert.Contains(socket.Sent[0], "to='example.com'");
		StringAssert.Contains(socket.Sent[0], "version='1.0'");
	}

	[TestMethod]
	public void SendAndReceiveUseStandaloneTextFrames() {
		var socket = new FakeWebSocketConnection(
			"<open xmlns='urn:ietf:params:xml:ns:xmpp-framing'/>",
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'/>",
			"<message xmlns='jabber:client'><body>Hello</body></message>");
		var transport = new WebSocketTransport(new Uri("ws://example.com/xmpp"),
			"example.com", socket);
		transport.Open();

		transport.Send("<iq id='bind-0' type='set'/>");
		XmlElement message = transport.Receive("message");

		Assert.AreEqual("<iq id='bind-0' type='set'/>", socket.Sent[1]);
		Assert.AreEqual("message", message.Name);
		Assert.AreEqual("Hello", message["body"]!.InnerText);
	}

	[TestMethod]
	public void RestartSendsAnotherOpenFrame() {
		var socket = new FakeWebSocketConnection(
			"<open xmlns='urn:ietf:params:xml:ns:xmpp-framing'/>",
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'/>",
			"<open xmlns='urn:ietf:params:xml:ns:xmpp-framing'/>",
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'/>" +
			"</stream:features>");
		var transport = new WebSocketTransport(new Uri("wss://example.com/xmpp"),
			"example.com", socket);
		transport.Open();

		XmlElement features = transport.Restart();

		Assert.HasCount(2, socket.Sent);
		Assert.AreEqual("stream:features", features.Name);
		Assert.IsNotNull(features["bind"]);
	}

	[TestMethod]
	public void CloseSendsXmppCloseFrameAndWebSocketClose() {
		var socket = new FakeWebSocketConnection(
			"<open xmlns='urn:ietf:params:xml:ns:xmpp-framing'/>",
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'/>");
		var transport = new WebSocketTransport(new Uri("wss://example.com/xmpp"),
			"example.com", socket);
		transport.Open();

		transport.Close();

		StringAssert.Contains(socket.Sent.Last(),
			"<close xmlns='urn:ietf:params:xml:ns:xmpp-framing'");
		Assert.IsTrue(socket.CloseOutputCalled);
	}

	[TestMethod]
	public void ConstructorRejectsNonWebSocketUri() {
		Assert.ThrowsExactly<ArgumentException>(() =>
			new WebSocketTransport(new Uri("https://example.com/xmpp"),
				"example.com", new FakeWebSocketConnection()));
	}

	sealed class FakeWebSocketConnection : WebSocketTransport.IWebSocketConnection {
		readonly Queue<string> received;

		public List<string> SubProtocols {
			get;
		} = new List<string>();

		public List<string> Sent {
			get;
		} = new List<string>();

		public Uri? ConnectedUri {
			get;
			private set;
		}

		public bool CloseOutputCalled {
			get;
			private set;
		}

		public string SubProtocol => SubProtocols.Contains("xmpp") ? "xmpp" : String.Empty;

		public FakeWebSocketConnection(params string[] received) {
			this.received = new Queue<string>(received);
		}

		public void AddSubProtocol(string subProtocol) {
			SubProtocols.Add(subProtocol);
		}

		public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) {
			ConnectedUri = uri;
			return Task.CompletedTask;
		}

		public Task SendAsync(ArraySegment<byte> buffer,
			WebSocketMessageType messageType, bool endOfMessage,
			CancellationToken cancellationToken) {
			Sent.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset,
				buffer.Count));
			return Task.CompletedTask;
		}

		public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
			CancellationToken cancellationToken) {
			string xml = received.Dequeue();
			byte[] bytes = Encoding.UTF8.GetBytes(xml);
			Buffer.BlockCopy(bytes, 0, buffer.Array!, buffer.Offset, bytes.Length);
			return Task.FromResult(new WebSocketReceiveResult(bytes.Length,
				WebSocketMessageType.Text, true));
		}

		public Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
			string statusDescription, CancellationToken cancellationToken) {
			CloseOutputCalled = true;
			return Task.CompletedTask;
		}

		public void Dispose() {
		}
	}
}
