using Artalk.Xmpp.Core;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Artalk.Xmpp.Tests;

[TestClass]
public sealed class BoshTransportTests {
	[TestMethod]
	public void OpenSendsInitialSessionRequestAndReturnsFeatures() {
		var handler = new FakeBoshHandler(
			"<body xmlns='http://jabber.org/protocol/httpbind' sid='abc123' " +
			"xml:lang='en'>" +
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<mechanisms xmlns='urn:ietf:params:xml:ns:xmpp-sasl'/>" +
			"</stream:features></body>");
		var transport = new BoshTransport(new Uri("https://example.com/http-bind"),
			"example.com", handler);

		var features = transport.Open();

		Assert.AreEqual("stream:features", features.Name);
		Assert.AreEqual("en", transport.Language.Name);
		StringAssert.Contains(handler.Requests[0],
			"xmlns='http://jabber.org/protocol/httpbind'");
		StringAssert.Contains(handler.Requests[0], "to='example.com'");
		StringAssert.Contains(handler.Requests[0], "hold='1'");
		StringAssert.Contains(handler.Requests[0], "xmpp:version='1.0'");
	}

	[TestMethod]
	public void SendPostsPayloadAndReceiveReadsQueuedResponse() {
		var handler = new FakeBoshHandler(
			"<body xmlns='http://jabber.org/protocol/httpbind' sid='abc123'>" +
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'/>" +
			"</body>",
			"<body xmlns='http://jabber.org/protocol/httpbind'>" +
			"<iq id='bind-0' type='result'/>" +
			"</body>");
		var transport = new BoshTransport(new Uri("https://example.com/http-bind"),
			"example.com", handler);
		transport.Open();

		transport.Send("<iq id='bind-0' type='set'/>");
		var response = transport.Receive("iq");

		Assert.AreEqual("iq", response.Name);
		Assert.AreEqual("bind-0", response.GetAttribute("id"));
		StringAssert.Contains(handler.Requests[1], "sid='abc123'");
		StringAssert.Contains(handler.Requests[1], "<iq id='bind-0' type='set'/>");
	}

	[TestMethod]
	public void RestartSendsXmppRestartRequest() {
		var handler = new FakeBoshHandler(
			"<body xmlns='http://jabber.org/protocol/httpbind' sid='abc123'>" +
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'/>" +
			"</body>",
			"<body xmlns='http://jabber.org/protocol/httpbind'>" +
			"<stream:features xmlns:stream='http://etherx.jabber.org/streams'>" +
			"<bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'/>" +
			"</stream:features></body>");
		var transport = new BoshTransport(new Uri("https://example.com/http-bind"),
			"example.com", handler);
		transport.Open();

		var features = transport.Restart();

		Assert.AreEqual("stream:features", features.Name);
		StringAssert.Contains(handler.Requests[1], "xmpp:restart='true'");
		StringAssert.Contains(handler.Requests[1], "sid='abc123'");
	}

	sealed class FakeBoshHandler : HttpMessageHandler {
		readonly Queue<string> responses;

		public List<string> Requests {
			get;
		} = new List<string>();

		public FakeBoshHandler(params string[] responses) {
			this.responses = new Queue<string>(responses);
		}

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken) {
			Requests.Add(request.Content!.ReadAsStringAsync(cancellationToken)
				.GetAwaiter().GetResult());
			string xml = responses.Dequeue();
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent(xml, Encoding.UTF8, "text/xml")
			});
		}
	}
}
