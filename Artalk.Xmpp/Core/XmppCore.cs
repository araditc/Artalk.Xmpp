using Artalk.Xmpp.Core.Sasl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Artalk.Xmpp.Core {
	/// <summary>
	/// Implements the core features of the XMPP protocol.
	/// </summary>
	/// <remarks>For implementation details, refer to RFC 3920.</remarks>
	public class XmppCore : IDisposable {
		/// <summary>
		/// The TCP connection to the XMPP server.
		/// </summary>
		TcpClient client;
		/// <summary>
		/// The (network) stream used for sending and receiving XML data.
		/// </summary>
		Stream stream;
		BoshTransport bosh;
		WebSocketTransport webSocket;
		readonly HttpMessageHandler boshMessageHandler;
		bool boshStreamOpened;
		bool webSocketStreamOpened;
		byte[] tlsServerEndPointChannelBinding;
		/// <summary>
		/// The parser instance used for parsing incoming XMPP XML-stream data.
		/// </summary>
		StreamParser parser;
		/// <summary>
		/// True if the instance has been disposed of.
		/// </summary>
		bool disposed;
		/// <summary>
		/// Used for creating unique IQ stanza ids.
		/// </summary>
		int id;
		/// <summary>
		/// The port number of the XMPP service of the server.
		/// </summary>
		int port;
		/// <summary>
		/// The hostname of the XMPP server to connect to.
		/// </summary>
		string hostname;
		/// <summary>
		/// The username with which to authenticate.
		/// </summary>
		string username;
		/// <summary>
		/// The password with which to authenticate.
		/// </summary>
		string password;
		string oauthBearerToken;
		/// <summary>
		/// The resource to use for binding.
		/// </summary>
		string resource;
		/// <summary>
		/// Write lock for the network stream.
		/// </summary>
		readonly object writeLock = new object();
		/// <summary>
		/// A thread-safe dictionary of wait handles for pending IQ requests.
		/// </summary>
		ConcurrentDictionary<string, AutoResetEvent> waitHandles =
			new ConcurrentDictionary<string, AutoResetEvent>();
		/// <summary>
		/// A thread-safe dictionary of IQ responses for pending IQ requests.
		/// </summary>
		ConcurrentDictionary<string, Iq> iqResponses =
		 new ConcurrentDictionary<string, Iq>();
		/// <summary>
		/// A thread-safe dictionary of callback methods for asynchronous IQ requests.
		/// </summary>
		ConcurrentDictionary<string, Action<string, Iq>> iqCallbacks =
		 new ConcurrentDictionary<string, Action<string, Iq>>();
		/// <summary>
		/// A cancellation token source that is set when the listener threads shuts
		/// down due to an exception.
		/// </summary>
		CancellationTokenSource cancelIq = new CancellationTokenSource();
		/// <summary>
		/// A FIFO of stanzas waiting to be processed.
		/// </summary>
		BlockingCollection<Stanza> stanzaQueue = new BlockingCollection<Stanza>();
		/// <summary>
		/// A cancellation token source for cancelling the dispatcher, if neccessary.
		/// </summary>
		CancellationTokenSource cancelDispatch = new CancellationTokenSource();

		/// <summary>
		/// The hostname of the XMPP server to connect to.
		/// </summary>
		/// <exception cref="ArgumentNullException">The Hostname property is being
		/// set and the value is null.</exception>
		/// <exception cref="ArgumentException">The Hostname property is being set
		/// and the value is the empty string.</exception>
		public string Hostname {
			get {
				return hostname;
			}
			set {
				value.ThrowIfNullOrEmpty("Hostname");
				hostname = value;
			}
		}

		/// <summary>
		/// The port number of the XMPP service of the server.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">The Port property is being
		/// set and the value is not between 0 and 65536.</exception>
		public int Port {
			get {
				return port;
			}
			set {
				value.ThrowIfOutOfRange("Port", 0, 65536);
				port = value;
			}
		}

		/// <summary>
		/// The username with which to authenticate. In XMPP jargon this is known
		/// as the 'node' part of the JID.
		/// </summary>
		/// <exception cref="ArgumentNullException">The Username property is being
		/// set and the value is null.</exception>
		/// <exception cref="ArgumentException">The Username property is being set
		/// and the value is the empty string.</exception>
		public string Username {
			get {
				return username;
			}
			set {
				value.ThrowIfNullOrEmpty("Username");
				username = value;
			}
		}

		/// <summary>
		/// The password with which to authenticate.
		/// </summary>
		/// <exception cref="ArgumentNullException">The Password property is being
		/// set and the value is null.</exception>
		public string Password {
			get {
				return password;
			}
			set {
				value.ThrowIfNull("Password");
				password = value;
			}
		}

		/// <summary>
		/// An OAuth 2.0 bearer token for SASL OAUTHBEARER authentication.
		/// </summary>
		public string OAuthBearerToken {
			get {
				return oauthBearerToken;
			}
			set {
				oauthBearerToken = value;
			}
		}

		/// <summary>
		/// If true the session will be TLS/SSL-encrypted if the server supports it.
		/// </summary>
		public bool Tls {
			get;
			set;
		}

		/// <summary>
		/// If true TLS/SSL is negotiated immediately after opening the TCP connection.
		/// </summary>
		public bool DirectTls {
			get;
			set;
		}

		/// <summary>
		/// The BOSH connection manager URL. If this is set, the client connects
		/// through XMPP over BOSH instead of a TCP XML stream.
		/// </summary>
		public Uri BoshUrl {
			get;
			set;
		}

		/// <summary>
		/// The WebSocket endpoint URL. If this is set, the client connects through
		/// XMPP over WebSocket instead of a TCP XML stream.
		/// </summary>
		public Uri WebSocketUrl {
			get;
			set;
		}

		/// <summary>
		/// A delegate used for verifying the remote Secure Sockets Layer (SSL)
		/// certificate which is used for authentication.
		/// </summary>
		public RemoteCertificateValidationCallback Validate {
			get;
			set;
		}

		/// <summary>
		/// Determines whether the session with the server is TLS/SSL encrypted.
		/// </summary>
		public bool IsEncrypted {
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the server advertised legacy XMPP session establishment.
		/// </summary>
		public bool SessionSupported {
			get;
			private set;
		}

		/// <summary>
		/// The address of the Xmpp entity.
		/// </summary>
		public Jid Jid {
			get;
			private set;
		}

		/// <summary>
		/// The default language of the XML stream.
		/// </summary>
		public CultureInfo Language {
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the instance is connected to the XMPP server.
		/// </summary>
		public bool Connected {
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the instance has been authenticated.
		/// </summary>
		public bool Authenticated {
			get;
			private set;
		}

		/// <summary>
		/// The event that is raised when an unrecoverable error condition occurs.
		/// </summary>
		public event EventHandler<ErrorEventArgs> Error;

		/// <summary>
		/// The event that is raised when an IQ-request stanza has been received.
		/// </summary>
		public event EventHandler<IqEventArgs> Iq;

		/// <summary>
		/// The event that is raised when a Message stanza has been received.
		/// </summary>
		public event EventHandler<MessageEventArgs> Message;

		/// <summary>
		/// The event that is raised when a Presence stanza has been received.
		/// </summary>
		public event EventHandler<PresenceEventArgs> Presence;

		/// <summary>
		/// The event that is raised when a stanza has been sent
		/// </summary>
		/// <remarks>Useful for low-level debugging</remarks>
		public event EventHandler<StanzaXmlEventArgs> SendXml;

		/// <summary>
		/// The event that is raised when a stanza has been received
		/// </summary>
		/// <remarks>Useful for low-level debugging</remarks>
		public event EventHandler<StanzaXmlEventArgs> ReceiveXml;

		/// <summary>
		/// The event that is raised when a connection was closed
		/// </summary>
		public event EventHandler<EventArgs> Disconnected;

		/// <summary>
		/// Initializes a new instance of the XmppCore class.
		/// </summary>
		/// <param name="hostname">The hostname of the XMPP server to connect to.</param>
		/// <param name="username">The username with which to authenticate. In XMPP jargon
		/// this is known as the 'node' part of the JID.</param>
		/// <param name="password">The password with which to authenticate.</param>
		/// <param name="port">The port number of the XMPP service of the server.</param>
		/// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
		/// supports TLS/SSL-encryption.</param>
		/// <param name="directTls">If true TLS/SSL is negotiated immediately after
		/// opening the TCP connection.</param>
		/// <param name="validate">A delegate used for verifying the remote Secure Sockets
		/// Layer (SSL) certificate which is used for authentication. Can be null if not
		/// needed.</param>
		/// <exception cref="ArgumentNullException">The hostname parameter or the
		/// username parameter or the password parameter is null.</exception>
		/// <exception cref="ArgumentException">The hostname parameter or the username
		/// parameter is the empty string.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
		/// is not a valid port number.</exception>
		public XmppCore(string hostname, string username, string password,
			int port = 5222, bool tls = true, RemoteCertificateValidationCallback validate = null,
			bool directTls = false) {
				Hostname = hostname;
				Username = username;
				Password = password;
				Port = port;
				Tls = tls;
				Validate = validate;
				DirectTls = directTls;
		}

		/// <summary>
		/// Initializes a new instance of the XmppCore class for XMPP over BOSH.
		/// </summary>
		/// <param name="boshUrl">The BOSH connection manager URL.</param>
		/// <param name="hostname">The XMPP service domain.</param>
		/// <param name="username">The username with which to authenticate.</param>
		/// <param name="password">The password with which to authenticate.</param>
		public XmppCore(Uri boshUrl, string hostname, string username,
			string password) : this(hostname, username, password) {
			BoshUrl = boshUrl;
		}

		/// <summary>
		/// Initializes a new instance of the XmppCore class for a URI-based
		/// transport binding.
		/// </summary>
		/// <param name="url">The transport endpoint URL.</param>
		/// <param name="hostname">The XMPP service domain.</param>
		/// <param name="username">The username with which to authenticate.</param>
		/// <param name="password">The password with which to authenticate.</param>
		/// <param name="transportBinding">The URI-based XMPP transport binding.</param>
		public XmppCore(Uri url, string hostname, string username,
			string password, XmppTransportBinding transportBinding)
			: this(hostname, username, password) {
			SetTransportUrl(url, transportBinding);
		}

		/// <summary>
		/// Initializes a new instance of the XmppCore class.
		/// </summary>
		/// <param name="hostname">The hostname of the XMPP server to connect to.</param>
		/// <param name="port">The port number of the XMPP service of the server.</param>
		/// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
		/// supports TLS/SSL-encryption.</param>
		/// <param name="directTls">If true TLS/SSL is negotiated immediately after
		/// opening the TCP connection.</param>
		/// <param name="validate">A delegate used for verifying the remote Secure Sockets
		/// Layer (SSL) certificate which is used for authentication. Can be null if not
		/// needed.</param>
		/// <exception cref="ArgumentNullException">The hostname parameter is
		/// null.</exception>
		/// <exception cref="ArgumentException">The hostname parameter is the empty
		/// string.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The value of the port parameter
		/// is not a valid port number.</exception>
		public XmppCore(string hostname, int port = 5222, bool tls = true,
			RemoteCertificateValidationCallback validate = null, bool directTls = false) {
			Hostname = hostname;
			Port = port;
			Tls = tls;
			Validate = validate;
			DirectTls = directTls;
		}

		/// <summary>
		/// Initializes a new instance of the XmppCore class for unauthenticated
		/// XMPP over BOSH.
		/// </summary>
		/// <param name="boshUrl">The BOSH connection manager URL.</param>
		/// <param name="hostname">The XMPP service domain.</param>
		public XmppCore(Uri boshUrl, string hostname) : this(hostname) {
			BoshUrl = boshUrl;
		}

		/// <summary>
		/// Initializes a new unauthenticated instance of the XmppCore class for a
		/// URI-based transport binding.
		/// </summary>
		/// <param name="url">The transport endpoint URL.</param>
		/// <param name="hostname">The XMPP service domain.</param>
		/// <param name="transportBinding">The URI-based XMPP transport binding.</param>
		public XmppCore(Uri url, string hostname,
			XmppTransportBinding transportBinding) : this(hostname) {
			SetTransportUrl(url, transportBinding);
		}

		internal XmppCore(Uri boshUrl, string hostname, string username,
			string password, HttpMessageHandler boshMessageHandler)
			: this(boshUrl, hostname, username, password) {
			this.boshMessageHandler = boshMessageHandler;
		}

		/// <summary>
		/// Establishes a connection to the XMPP server.
		/// </summary>
		/// <param name="resource">The resource identifier to bind with. If this is null,
		/// it is assigned by the server.</param>
		/// <exception cref="SocketException">An error occurred while accessing the socket
		/// used for establishing the connection to the XMPP server. Use the ErrorCode
		/// property to obtain the specific error code.</exception>
		/// <exception cref="AuthenticationException">An authentication error occured while
		/// trying to establish a secure connection, or the provided credentials were
		/// rejected by the server, or the server requires TLS/SSL and TLS has been
		/// turned off.</exception>
		/// <exception cref="XmppException">An XMPP error occurred while negotiating the
		/// XML stream with the server.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <remarks>If a username has been supplied, this method automatically performs
		/// authentication.</remarks>
		public void Connect(string resource = null) {
			if (disposed)
				throw new ObjectDisposedException(GetType().FullName);
			this.resource = resource;
			try {
				if (BoshUrl != null && WebSocketUrl != null)
					throw new InvalidOperationException("Only one URI-based XMPP transport can be configured.");
				if (BoshUrl != null) {
					bosh = new BoshTransport(BoshUrl, Hostname, boshMessageHandler);
					boshStreamOpened = false;
					IsEncrypted = bosh.IsEncrypted;
					tlsServerEndPointChannelBinding = null;
				} else if (WebSocketUrl != null) {
					webSocket = new WebSocketTransport(WebSocketUrl, Hostname);
					webSocketStreamOpened = false;
					IsEncrypted = webSocket.IsEncrypted;
					tlsServerEndPointChannelBinding = null;
				} else {
					client = new TcpClient(Hostname, Port);
					stream = client.GetStream();
					IsEncrypted = false;
					tlsServerEndPointChannelBinding = null;
				}
				SessionSupported = false;
				if (DirectTls && bosh == null && webSocket == null)
					SecureStream(Hostname, Validate);
				// Sets up the connection which includes TLS and possibly SASL negotiation.
				SetupConnection(this.resource);
				// We are connected.
				Connected = true;
				// Set up the listener and dispatcher tasks.
				Task.Factory.StartNew(ReadXmlStream, TaskCreationOptions.LongRunning);
				Task.Factory.StartNew(DispatchEvents, TaskCreationOptions.LongRunning);
			} catch (XmlException e) {
				throw new XmppException("The XML stream could not be negotiated.", e);
			}
		}

		/// <summary>
		/// Authenticates with the XMPP server using the specified username and
		/// password.
		/// </summary>
		/// <param name="username">The username to authenticate with.</param>
		/// <param name="password">The password to authenticate with.</param>
		/// <exception cref="ArgumentNullException">The username parameter or the
		/// password parameter is null.</exception>
		/// <exception cref="SocketException">An error occurred while accessing the socket
		/// used for establishing the connection to the XMPP server. Use the ErrorCode
		/// property to obtain the specific error code.</exception>
		/// <exception cref="AuthenticationException">An authentication error occured while
		/// trying to establish a secure connection, or the provided credentials were
		/// rejected by the server, or the server requires TLS/SSL and TLS has been
		/// turned off.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="XmppException">Authentication has already been performed, or
		/// an XMPP error occurred while negotiating the XML stream with the
		/// server.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		public void Authenticate(string username, string password) {
			AssertValid();
			username.ThrowIfNull("username");
			password.ThrowIfNull("password");
			if (Authenticated)
				throw new XmppException("Authentication has already been performed.");
			// Unfortunately, SASL authentication does not follow the standard XMPP
			// IQ-semantics. At this stage it really is easier to simply perform a
			// reconnect.
			Username = username;
			Password = password;
			Disconnect();
			Connect(this.resource);
		}

		/// <summary>
		/// Sends a Message stanza with the specified attributes and content to the
		/// server.
		/// </summary>
		/// <param name="to">The JID of the intended recipient for the stanza.</param>
		/// <param name="from">The JID of the sender.</param>
		/// <param name="data">he content of the stanza.</param>
		/// <param name="id">The ID of the stanza.</param>
		/// <param name="language">The language of the XML character data of
		/// the stanza.</param>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void SendMessage(Jid to = null, Jid from = null, XmlElement data = null,
			string id = null, CultureInfo language = null) {
				AssertValid();
				Send(new Message(to, from, data, id, language));
		}

		/// <summary>
		/// Sends the specified message stanza to the server.
		/// </summary>
		/// <param name="message">The message stanza to send to the server.</param>
		/// <exception cref="ArgumentNullException">The message parameter is
		/// null.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void SendMessage(Message message) {
			AssertValid();
			message.ThrowIfNull("message");
			Send(message);
		}

		/// <summary>
		/// Sends a Presence stanza with the specified attributes and content to the
		/// server.
		/// </summary>
		/// <param name="to">The JID of the intended recipient for the stanza.</param>
		/// <param name="from">The JID of the sender.</param>
		/// <param name="data">he content of the stanza.</param>
		/// <param name="id">The ID of the stanza.</param>
		/// <param name="language">The language of the XML character data of
		/// the stanza.</param>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void SendPresence(Jid to = null, Jid from = null, string id = null,
			CultureInfo language = null, params XmlElement[] data) {
			AssertValid();
			Send(new Presence(to, from, id, language, data));
		}

		/// <summary>
		/// Sends the specified presence stanza to the server.
		/// </summary>
		/// <param name="presence">The presence stanza to send to the server.</param>
		/// <exception cref="ArgumentNullException">The presence parameter
		/// is null.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void SendPresence(Presence presence) {
			AssertValid();
			presence.ThrowIfNull("presence");
			Send(presence);
		}

		/// <summary>
		/// Performs an IQ set/get request and blocks until the response IQ comes in.
		/// </summary>
		/// <param name="type">The type of the request. This must be either
		/// IqType.Set or IqType.Get.</param>
		/// <param name="to">The JID of the intended recipient for the stanza.</param>
		/// <param name="from">The JID of the sender.</param>
		/// <param name="data">he content of the stanza.</param>
		/// <param name="language">The language of the XML character data of
		/// the stanza.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait
		/// for the arrival of the IQ response or -1 to wait indefinitely.</param>
		/// <returns>The IQ response sent by the server.</returns>
		/// <exception cref="ArgumentException">The type parameter is not
		/// IqType.Set or IqType.Get.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
		/// is a negative number other than -1, which represents an indefinite
		/// timeout.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network, or there was a failure reading from the network.</exception>
		/// <exception cref="TimeoutException">A timeout was specified and it
		/// expired.</exception>
		public Iq IqRequest(IqType type, Jid to = null, Jid from = null,
			XmlElement data = null, CultureInfo language = null,
			int millisecondsTimeout = -1) {
			AssertValid();
			return IqRequest(new Iq(type, null, to, from, data, language));
		}

		/// <summary>
		/// Performs an IQ set/get request and blocks until the response IQ comes in.
		/// </summary>
		/// <param name="request">The IQ request to send.</param>
		/// <param name="millisecondsTimeout">The number of milliseconds to wait
		/// for the arrival of the IQ response or -1 to wait indefinitely.</param>
		/// <returns>The IQ response sent by the server.</returns>
		/// <exception cref="ArgumentNullException">The request parameter is null.</exception>
		/// <exception cref="ArgumentException">The type parameter is not IqType.Set
		/// or IqType.Get.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The value of millisecondsTimeout
		/// is a negative number other than -1, which represents an indefinite
		/// timeout.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network, or there was a failure reading from the network.</exception>
		/// <exception cref="TimeoutException">A timeout was specified and it
		/// expired.</exception>
		public Iq IqRequest(Iq request, int millisecondsTimeout = Timeout.Infinite) {
			AssertValid();
			request.ThrowIfNull("request");
			if (request.Type != IqType.Set && request.Type != IqType.Get)
				throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
			// Generate a unique ID for the IQ request.
			request.Id = GetId();
			AutoResetEvent ev = new AutoResetEvent(false);
			waitHandles[request.Id] = ev;
			try {
				Send(request);
				// Wait for event to be signaled by task that processes the incoming
				// XML stream.
				int index = WaitHandle.WaitAny(
					new WaitHandle[] { ev, cancelIq.Token.WaitHandle },
					millisecondsTimeout);
				if (index == WaitHandle.WaitTimeout)
					throw new TimeoutException();
				// Reader task errored out.
				if (index == 1)
					throw new IOException("The incoming XML stream could not read.");
				// Fetch response stanza.
				Iq response;
				if (iqResponses.TryRemove(request.Id, out response))
					return response;
				// Shouldn't happen.
				throw new InvalidOperationException();
			} finally {
				AutoResetEvent removed;
				if (waitHandles.TryRemove(request.Id, out removed))
					removed.Dispose();
				else
					ev.Dispose();
			}
		}

		/// <summary>
		/// Performs an IQ set/get request asynchronously and optionally invokes a
		/// callback method when the IQ response comes in.
		/// </summary>
		/// <param name="type">The type of the request. This must be either
		/// IqType.Set or IqType.Get.</param>
		/// <param name="to">The JID of the intended recipient for the stanza.</param>
		/// <param name="from">The JID of the sender.</param>
		/// <param name="data">he content of the stanza.</param>
		/// <param name="language">The language of the XML character data of
		/// the stanza.</param>
		/// <param name="callback">A callback method which is invoked once the
		/// IQ response from the server comes in.</param>
		/// <returns>The ID value of the pending IQ stanza request.</returns>
		/// <exception cref="ArgumentException">The type parameter is not IqType.Set
		/// or IqType.Get.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public string IqRequestAsync(IqType type, Jid to = null, Jid from = null,
			XmlElement data = null, CultureInfo language = null,
			Action<string, Iq> callback = null) {
			AssertValid();
			return IqRequestAsync(new Iq(type, null, to, from, data, language), callback);
		}

		/// <summary>
		/// Performs an IQ set/get request asynchronously and optionally invokes a
		/// callback method when the IQ response comes in.
		/// </summary>
		/// <param name="request">The IQ request to send.</param>
		/// <param name="callback">A callback method which is invoked once the
		/// IQ response from the server comes in.</param>
		/// <returns>The ID value of the pending IQ stanza request.</returns>
		/// <exception cref="ArgumentNullException">The request parameter is null.</exception>
		/// <exception cref="ArgumentException">The type parameter is not IqType.Set
		/// or IqType.Get.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public string IqRequestAsync(Iq request, Action<string, Iq> callback = null) {
			AssertValid();
			request.ThrowIfNull("request");
			if (request.Type != IqType.Set && request.Type != IqType.Get)
				throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
			request.Id = GetId();
			// Register the callback.
			if (callback != null)
				iqCallbacks[request.Id] = callback;
			Send(request);
			return request.Id;
		}

		/// <summary>
		/// Sends an IQ response for the IQ request with the specified id.
		/// </summary>
		/// <param name="type">The type of the response. This must be either
		/// IqType.Result or IqType.Error.</param>
		/// <param name="id">The id of the IQ request.</param>
		/// <param name="to">The JID of the intended recipient for the stanza.</param>
		/// <param name="from">The JID of the sender.</param>
		/// <param name="data">he content of the stanza.</param>
		/// <param name="language">The language of the XML character data of
		/// the stanza.</param>
		/// <exception cref="ArgumentException">The type parameter is not IqType.Result
		/// or IqType.Error.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void IqResponse(IqType type, string id, Jid to = null, Jid from = null,
			XmlElement data = null, CultureInfo language = null) {
			AssertValid();
			IqResponse(new Iq(type, id, to, from, data, null));
		}

		/// <summary>
		/// Sends an IQ response for the IQ request with the specified id.
		/// </summary>
		/// <param name="response">The IQ response to send.</param>
		/// <exception cref="ArgumentNullException">The response parameter is
		/// null.</exception>
		/// <exception cref="ArgumentException">The Type property of the response
		/// parameter is not IqType.Result or IqType.Error.</exception>
		/// <exception cref="ObjectDisposedException">The XmppCore object has been
		/// disposed.</exception>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void IqResponse(Iq response) {
			AssertValid();
			response.ThrowIfNull("response");
			if (string.IsNullOrEmpty(response.Id))
				throw new ArgumentException("The IQ response should have id equal to request.");
			if (response.Type != IqType.Result && response.Type != IqType.Error)
				throw new ArgumentException("The IQ type must be either 'result' or 'error'.");
			Send(response);
		}

		/// <summary>
		/// Closes the connection with the XMPP server. This automatically disposes
		/// of the object.
		/// </summary>
		/// <exception cref="InvalidOperationException">The XmppCore instance is not
		/// connected to a remote host.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network.</exception>
		public void Close() {
			AssertValid();
			// Close the XML stream.
			Disconnect();
			Dispose();
		}

		/// <summary>
		/// Releases all resources used by the current instance of the XmppCore class.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases all resources used by the current instance of the XmppCore
		/// class, optionally disposing of managed resource.
		/// </summary>
		/// <param name="disposing">true to dispose of managed resources, otherwise
		/// false.</param>
		protected virtual void Dispose(bool disposing) {
			if (!disposed) {
				// Indicate that the instance has been disposed.
				disposed = true;
				// Get rid of managed resources.
				if (disposing) {
					if (parser != null)
						parser.Close();
					parser = null;
					if (bosh != null)
						bosh.Dispose();
					bosh = null;
					if (webSocket != null)
						webSocket.Dispose();
					webSocket = null;
					if (client != null)
						client.Close();
					client = null;
				}
				// Get rid of unmanaged resources.
			}
		}

		/// <summary>
		/// Asserts the instance has not been disposed of and is connected to the
		/// XMPP server.
		/// </summary>
		void AssertValid() {
			if (disposed)
				throw new ObjectDisposedException(GetType().FullName);
			if (!Connected)
				throw new InvalidOperationException("Not connected to XMPP server.");
		}

		/// <summary>
		/// Negotiates an XML stream over which XML stanzas can be sent.
		/// </summary>
		/// <param name="resource">The resource identifier to bind with. If this is null,
		/// it is assigned by the server.</param>
		/// <exception cref="XmppException">The resource binding process failed.</exception>
		/// <exception cref="XmlException">Invalid or unexpected XML data has been
		/// received from the XMPP server.</exception>
		/// <exception cref="AuthenticationException">An authentication error occured while
		/// trying to establish a secure connection, or the provided credentials were
		/// rejected by the server, or the server requires TLS/SSL and TLS has been
		/// turned off.</exception>
		void SetupConnection(string resource = null) {
			// Request the initial stream.
			XmlElement feats = InitiateStream(Hostname);
			// Server supports TLS/SSL via STARTTLS.
			if (bosh == null && feats["starttls"] != null && !IsEncrypted) {
				// TLS is mandatory and user opted out of it.
				if (feats["starttls"]["required"] != null && Tls == false)
					throw new AuthenticationException("The server requires TLS/SSL.");
				if(Tls)
					feats = StartTls(Hostname, Validate);
			}
			// If no Username has been provided, don't perform authentication.
			if (Username == null)
				return;
			Sasl2Feature sasl2Feature = Sasl2Feature.Parse(feats);
			HashSet<string> channelBindingTypes = GetChannelBindingTypes(feats);
			// Continue with SASL authentication.
			try {
				if (sasl2Feature != null && IsEncrypted) {
					feats = Authenticate(sasl2Feature, channelBindingTypes, Username,
						Password, Hostname);
				} else {
					// Construct a list of legacy SASL mechanisms supported by the server.
					var list = GetSaslMechanisms(feats);
					if (list.Count == 0)
						throw new AuthenticationException("No SASL mechanisms advertised.");
					feats = Authenticate(list, channelBindingTypes, Username, Password,
						Hostname);
				}
				SessionSupported = feats["session"] != null;
				// FIXME: How is the client's JID constructed if the server does not support
				// resource binding?
				if (feats["bind"] != null)
					Jid = BindResource(resource);
			} catch (SaslException e) {
				throw new AuthenticationException("Authentication failed.", e);
			}
		}

		/// <summary>
		/// Initiates an XML stream with the specified entity.
		/// </summary>
		/// <param name="hostname">The name of the receiving entity with which to
		/// initiate an XML stream.</param>
		/// <returns>The 'stream:features' XML element as received from the
		/// receiving entity upon stream establishment.</returns>
		/// <exception cref="XmlException">The XML parser has encountered invalid
		/// or unexpected XML data.</exception>
		/// <exception cref="CultureNotFoundException">The culture specified by the
		/// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network, or there was a failure while reading from the network.</exception>
		XmlElement InitiateStream(string hostname) {
			if (bosh != null) {
				XmlElement feats = boshStreamOpened ? bosh.Restart() : bosh.Open();
				boshStreamOpened = true;
				Language = bosh.Language ?? new CultureInfo("en");
				return feats;
			}
			if (webSocket != null) {
				XmlElement feats = webSocketStreamOpened ?
					webSocket.Restart() : webSocket.Open();
				webSocketStreamOpened = true;
				Language = webSocket.Language ?? new CultureInfo("en");
				return feats;
			}
			var xml = Xml.Element("stream:stream", "jabber:client")
				.Attr("to", hostname)
				.Attr("version", "1.0")
				.Attr("xmlns:stream", "http://etherx.jabber.org/streams")
				.Attr("xml:lang", CultureInfo.CurrentCulture.Name);
			Send(xml.ToXmlString(xmlDeclaration: true, leaveOpen: true));
			// Create a new parser instance.
			if (parser != null)
				parser.Close();
			parser = new StreamParser(stream, true);
			// Remember the default language of the stream. The server is required to
			// include this, but we make sure nonetheless.
			Language = parser.Language ?? new CultureInfo("en");
			// The first element of the stream must be <stream:features>.
			return Receive("stream:features");
		}

		/// <summary>
		/// Secures the network stream by negotiating TLS-encryption with the server.
		/// </summary>
		/// <param name="hostname">The hostname of the XMPP server.</param>
		/// <param name="validate">A delegate used for verifying the remote Secure
		/// Sockets Layer (SSL) certificate which is used for authentication. Can be
		/// null if not needed.</param>
		/// <returns>The 'stream:features' XML element as received from the
		/// receiving entity upon establishment of a new XML stream.</returns>
		/// <exception cref="AuthenticationException">An
		/// authentication error occured while trying to establish a secure
		/// connection.</exception>
		/// <exception cref="XmlException">The XML parser has encountered invalid
		/// or unexpected XML data.</exception>
		/// <exception cref="CultureNotFoundException">The culture specified by the
		/// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network, or there was a failure while reading from the network.</exception>
		XmlElement StartTls(string hostname,
			RemoteCertificateValidationCallback validate) {
			// Send STARTTLS command and ensure the server acknowledges the request.
			SendAndReceive(Xml.Element("starttls",
				"urn:ietf:params:xml:ns:xmpp-tls"), "proceed");
			SecureStream(hostname, validate);
			// Initiate a new stream to server.
			return InitiateStream(hostname);
		}

		void SecureStream(string hostname, RemoteCertificateValidationCallback validate) {
			SslStream sslStream = new SslStream(stream, false, validate);
			sslStream.AuthenticateAsClient(hostname);
			stream = sslStream;
			IsEncrypted = true;
			tlsServerEndPointChannelBinding = sslStream.RemoteCertificate == null ?
				null : ChannelBinding.CreateTlsServerEndPoint(sslStream.RemoteCertificate);
		}

		/// <summary>
		/// Performs SASL authentication.
		/// </summary>
		/// <param name="mechanisms">An enumerable collection of SASL mechanisms
		/// supported by the server.</param>
		/// <param name="channelBindingTypes">An enumerable collection of SASL
		/// channel-binding types advertised by XEP-0440, or null if the feature was
		/// not advertised.</param>
		/// <param name="username">The username to authenticate with.</param>
		/// <param name="password">The password to authenticate with.</param>
		/// <param name="hostname">The hostname of the XMPP server.</param>
		/// <returns>The 'stream:features' XML element as received from the
		/// receiving entity upon establishment of a new XML stream.</returns>
		/// <remarks>Refer to RFC 3920, Section 6 (Use of SASL).</remarks>
		/// <exception cref="SaslException">A SASL error condition occured.</exception>
		/// <exception cref="XmlException">The XML parser has encountered invalid
		/// or unexpected XML data.</exception>
		/// <exception cref="CultureNotFoundException">The culture specified by the
		/// XML-stream in it's 'xml:lang' attribute could not be found.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network, or there was a failure while reading from the network.</exception>
		XmlElement Authenticate(IEnumerable<string> mechanisms,
			IEnumerable<string> channelBindingTypes, string username, string password,
			string hostname) {
				string name = SelectMechanism(mechanisms, channelBindingTypes);
				SaslMechanism m = CreateSaslMechanism(name, username, password,
					hostname);
				ConfigureSaslDowngradeProtection(m, name, mechanisms,
					channelBindingTypes);
				var xml = Xml.Element("auth", "urn:ietf:params:xml:ns:xmpp-sasl")
					.Attr("mechanism", name)
					.Text(m.HasInitial ? m.GetResponse(String.Empty) : String.Empty);
				Send(xml);
				while (true) {
					XmlElement ret = Receive("challenge", "success", "failure");
					if (ret.Name == "failure")
						throw new SaslException("SASL authentication failed.");
					if (ret.Name == "success" && m.IsCompleted)
						break;
					// Server has successfully authenticated us, but mechanism still needs
					// to verify server's signature.
					string response = m.GetResponse(ret.InnerText);
					// If the response is the empty string, the server's signature has been
					// verified.
					if (ret.Name == "success") {
						if (response == String.Empty)
							break;
						throw new SaslException("Could not verify server's signature.");
					}
					xml = Xml.Element("response",
						"urn:ietf:params:xml:ns:xmpp-sasl").Text(response);
					Send(xml);
				}
				// The instance is now authenticated.
				Authenticated = true;
				// Finally, initiate a new XML-stream.
				return InitiateStream(hostname);
		}

		XmlElement Authenticate(Sasl2Feature feature,
			IEnumerable<string> channelBindingTypes, string username, string password,
			string hostname) {
				string name = SelectMechanism(feature.Mechanisms, channelBindingTypes);
				SaslMechanism m = CreateSaslMechanism(name, username, password,
					hostname);
				ConfigureSaslDowngradeProtection(m, name, feature.Mechanisms,
					channelBindingTypes);
				IReadOnlyList<string> upgradeTasks =
					SaslUpgradeTask.SelectSupported(feature.UpgradeTasks, password);
				Send(CreateSasl2AuthenticateElement(m, upgradeTasks));
				while (true) {
					XmlElement ret = Receive("challenge", "success", "failure",
						"continue");
					if (ret.NamespaceURI != Sasl2Feature.Namespace)
						throw new SaslException("Unexpected SASL2 response namespace.");
					if (ret.LocalName == "failure")
						throw new SaslException("SASL2 authentication failed.");
					if (ret.LocalName == "continue") {
						string additionalData = GetSasl2AdditionalData(ret);
						if (additionalData != null)
							CompleteServerSignature(m, additionalData);
						CompleteSasl2UpgradeTask(ret, upgradeTasks, password);
						continue;
					}
					if (ret.LocalName == "success") {
						string additionalData = GetSasl2AdditionalData(ret);
						if (additionalData != null)
							CompleteServerSignature(m, additionalData);
						else if (!m.IsCompleted)
							throw new SaslException("SASL2 authentication success did " +
								"not include mechanism completion data.");
						Authenticated = true;
						return Receive("stream:features");
					}
					Send(Xml.Element("response", Sasl2Feature.Namespace).Text(
						m.GetResponse(ret.InnerText)));
				}
		}

		void CompleteSasl2UpgradeTask(XmlElement continueElement,
			IEnumerable<string> requestedUpgradeTasks, string password) {
				string task = GetSasl2Task(continueElement);
				var requested = new HashSet<string>(requestedUpgradeTasks,
					StringComparer.InvariantCultureIgnoreCase);
				if (!requested.Contains(task) || !SaslUpgradeTask.Supports(task))
					throw new SaslException("Unsupported SASL2 continuation task.");
				Send(SaslUpgradeTask.CreateNextElement(task));
				XmlElement ret = Receive("task-data", "failure");
				if (ret.NamespaceURI != Sasl2Feature.Namespace)
					throw new SaslException("Unexpected SASL2 task response namespace.");
				if (ret.LocalName == "failure")
					throw new SaslException("SASL2 upgrade task failed.");
				Send(SaslUpgradeTask.CreateHashTaskData(task, password, ret));
		}

		SaslMechanism CreateSaslMechanism(string name, string username,
			string password, string hostname) {
				SaslMechanism m = SaslFactory.Create(name);
				m.Properties.Add("Username", username);
				m.Properties.Add("Password", password);
				if (!String.IsNullOrEmpty(OAuthBearerToken)) {
					m.Properties.Add("OAuthBearerToken", OAuthBearerToken);
					m.Properties.Add("Hostname", hostname);
					m.Properties.Add("Port", Port);
				}
				if (name.EndsWith("-PLUS", StringComparison.InvariantCultureIgnoreCase)) {
					m.Properties.Add("ChannelBindingName", ChannelBinding.TlsServerEndPoint);
					m.Properties.Add("ChannelBindingData", tlsServerEndPointChannelBinding);
				}
				return m;
		}

		static void ConfigureSaslDowngradeProtection(SaslMechanism mechanism,
			string name, IEnumerable<string> advertisedMechanisms,
			IEnumerable<string> advertisedChannelBindingTypes) {
				string hash = SaslScramDowngradeProtection.Compute(name,
					advertisedMechanisms, advertisedChannelBindingTypes);
				if (hash != null)
					mechanism.Properties[SaslScramDowngradeProtection.PropertyName] =
						hash;
		}

		internal static XmlElement CreateSasl2AuthenticateElement(
			SaslMechanism mechanism, IEnumerable<string> upgradeTasks = null) {
				mechanism.ThrowIfNull("mechanism");
				var xml = Xml.Element("authenticate", Sasl2Feature.Namespace)
					.Attr("mechanism", mechanism.Name);
				if (upgradeTasks != null) {
					foreach (string task in upgradeTasks)
						xml.Child(SaslUpgradeTask.CreateUpgradeElement(task));
				}
				if (mechanism.HasInitial) {
					xml.Child(Xml.Element("initial-response", Sasl2Feature.Namespace)
						.Text(mechanism.GetResponse(String.Empty)));
				}
				return xml;
		}

		static void CompleteServerSignature(SaslMechanism mechanism,
			string additionalData) {
				string response = mechanism.GetResponse(additionalData);
				if (response != String.Empty)
					throw new SaslException("Could not verify server's signature.");
		}

		static string GetSasl2AdditionalData(XmlElement element) {
			foreach (XmlNode node in element.ChildNodes) {
				if (node is XmlElement child &&
					child.LocalName == "additional-data" &&
					child.NamespaceURI == Sasl2Feature.Namespace) {
					return child.InnerText;
				}
			}
			return null;
		}

		static string GetSasl2Task(XmlElement continueElement) {
			var tasks = new List<string>();
			foreach (XmlNode node in continueElement.ChildNodes) {
				if (node is not XmlElement tasksElement ||
					tasksElement.LocalName != "tasks" ||
					tasksElement.NamespaceURI != Sasl2Feature.Namespace) {
					continue;
				}
				foreach (XmlNode taskNode in tasksElement.ChildNodes) {
					if (taskNode is XmlElement taskElement &&
						taskElement.LocalName == "task" &&
						taskElement.NamespaceURI == Sasl2Feature.Namespace) {
						string task = taskElement.InnerText.Trim();
						if (!String.IsNullOrEmpty(task))
							tasks.Add(task);
					}
				}
			}
			if (tasks.Count != 1)
				throw new SaslException("SASL2 upgrade continuation must contain " +
					"exactly one task.");
			return tasks[0];
		}

		/// <summary>
		/// Selects the best SASL mechanism that we support from the list of mechanisms
		/// advertised by the server.
		/// </summary>
		/// <param name="mechanisms">An enumerable collection of SASL mechanisms
		/// advertised by the server.</param>
		/// <param name="channelBindingTypes">An enumerable collection of SASL
		/// channel-binding types advertised by XEP-0440, or null if the feature was
		/// not advertised.</param>
		/// <returns>The IANA name of the selcted SASL mechanism.</returns>
		/// <exception cref="SaslException">No supported mechanism could be found in
		/// the list of mechanisms advertised by the server.</exception>
		string SelectMechanism(IEnumerable<string> mechanisms,
			IEnumerable<string> channelBindingTypes) {
			mechanisms.ThrowIfNull("mechanisms");
			HashSet<string> advertisedMechanisms = new HashSet<string>(mechanisms,
				StringComparer.InvariantCultureIgnoreCase);
			HashSet<string> advertisedChannelBindingTypes =
				channelBindingTypes == null ? null : new HashSet<string>(
					channelBindingTypes, StringComparer.InvariantCultureIgnoreCase);
			bool channelBindingFeatureAdvertised = advertisedChannelBindingTypes != null;
			bool plusMechanismAdvertised = advertisedMechanisms.Any(mechanism =>
				mechanism.EndsWith("-PLUS", StringComparison.InvariantCultureIgnoreCase));
			if (channelBindingFeatureAdvertised &&
				advertisedChannelBindingTypes.Count > 0 &&
				!plusMechanismAdvertised) {
				throw new SaslException("The server advertised SASL channel-binding " +
					"types but no channel-binding SASL mechanism.");
			}
			var m = new List<string>();
			if (!String.IsNullOrEmpty(OAuthBearerToken))
				m.Add("OAUTHBEARER");
			if (Password != null) {
				if (tlsServerEndPointChannelBinding != null &&
					(!channelBindingFeatureAdvertised ||
					advertisedChannelBindingTypes.Contains(
						ChannelBinding.TlsServerEndPoint))) {
					m.Add("SCRAM-SHA3-512-PLUS");
					m.Add("SCRAM-SHA-512-PLUS");
					m.Add("SCRAM-SHA-384-PLUS");
					m.Add("SCRAM-SHA-256-PLUS");
					m.Add("SCRAM-SHA-224-PLUS");
					m.Add("SCRAM-SHA-1-PLUS");
				}
				m.AddRange(new string[] {
					"SCRAM-SHA3-512",
					"SCRAM-SHA-512",
					"SCRAM-SHA-384",
					"SCRAM-SHA-256",
					"SCRAM-SHA-224",
					"SCRAM-SHA-1",
					"DIGEST-MD5",
					"PLAIN"
				});
			}
			for (int i = 0; i < m.Count; i++) {
				if (advertisedMechanisms.Contains(m[i]))
					return m[i];
			}
			throw new SaslException("No supported SASL mechanism found.");
		}

		static HashSet<string> GetChannelBindingTypes(XmlElement features) {
			features.ThrowIfNull("features");
			foreach (XmlNode node in features.ChildNodes) {
				if (node is not XmlElement element ||
					element.LocalName != "sasl-channel-binding" ||
					element.NamespaceURI != ChannelBinding.Namespace) {
					continue;
				}
				var types = new HashSet<string>(
					StringComparer.InvariantCultureIgnoreCase);
				foreach (XmlNode childNode in element.ChildNodes) {
					if (childNode is XmlElement child &&
						child.LocalName == "channel-binding" &&
						child.NamespaceURI == ChannelBinding.Namespace) {
						string type = child.GetAttribute("type");
						if (!String.IsNullOrWhiteSpace(type))
							types.Add(type);
					}
				}
				return types;
			}
			return null;
		}

		static HashSet<string> GetSaslMechanisms(XmlElement features) {
			features.ThrowIfNull("features");
			var mechanisms = new HashSet<string>(
				StringComparer.InvariantCultureIgnoreCase);
			foreach (XmlNode node in features.ChildNodes) {
				if (node is not XmlElement element ||
					element.LocalName != "mechanisms" ||
					element.NamespaceURI != "urn:ietf:params:xml:ns:xmpp-sasl") {
					continue;
				}
				foreach (XmlNode childNode in element.ChildNodes) {
					if (childNode is XmlElement child &&
						child.LocalName == "mechanism" &&
						child.NamespaceURI == "urn:ietf:params:xml:ns:xmpp-sasl" &&
						!String.IsNullOrWhiteSpace(child.InnerText)) {
						mechanisms.Add(child.InnerText.Trim());
					}
				}
				break;
			}
			return mechanisms;
		}

		/// <summary>
		/// Performs resource binding and returns the 'full JID' with which this
		/// session associated.
		/// </summary>
		/// <param name="resourceName">The resource identifier to bind to. If this
		/// is null, the server generates a random identifier.</param>
		/// <returns>The full JID to which this session has been bound.</returns>
		/// <remarks>Refer to RFC 3920, Section 7 (Resource Binding).</remarks>
		/// <exception cref="XmppException">The resource binding process
		/// failed due to an erroneous server response.</exception>
		/// <exception cref="XmlException">The XML parser has encountered invalid
		/// or unexpected XML data.</exception>
		/// <exception cref="IOException">There was a failure while writing to the
		/// network, or there was a failure while reading from the network.</exception>
		Jid BindResource(string resourceName = null) {
			var xml = Xml.Element("iq")
				.Attr("type", "set")
				.Attr("id", "bind-0");
			var bind = Xml.Element("bind", "urn:ietf:params:xml:ns:xmpp-bind");
			if (resourceName != null)
				bind.Child(Xml.Element("resource").Text(resourceName));
			xml.Child(bind);
			XmlElement res = SendAndReceive(xml, "iq");
			if (res["bind"] == null || res["bind"]["jid"] == null)
				throw new XmppException("Erroneous server response.");
			return new Jid(res["bind"]["jid"].InnerText);
		}

		/// <summary>
		/// Serializes and sends the specified XML element to the server.
		/// </summary>
		/// <param name="element">The XML element to send.</param>
		/// <exception cref="ArgumentNullException">The element parameter
		/// is null.</exception>
		/// <exception cref="IOException">There was a failure while writing
		/// to the network.</exception>
		void Send(XmlElement element) {
			element.ThrowIfNull("element");
			Send(element.ToXmlString());
		}

		/// <summary>
		/// Sends the specified string to the server.
		/// </summary>
		/// <param name="xml">The string to send.</param>
		/// <exception cref="ArgumentNullException">The xml parameter is null.</exception>
		/// <exception cref="IOException">There was a failure while writing to
		/// the network.</exception>
		void Send(string xml) {
			xml.ThrowIfNull("xml");
			SendXml.Raise(this, new StanzaXmlEventArgs(xml));
			if (bosh != null) {
				bosh.Send(xml);
				return;
			}
			if (webSocket != null) {
				webSocket.Send(xml);
				return;
			}
			// XMPP is guaranteed to be UTF-8.
			byte[] buf = Encoding.UTF8.GetBytes(xml);
			lock (writeLock) {
				stream.Write(buf, 0, buf.Length);
			}
		}

		/// <summary>
		/// Sends the specified stanza to the server.
		/// </summary>
		/// <param name="stanza">The stanza to send.</param>
		/// <exception cref="ArgumentNullException">The stanza parameter is null.</exception>
		/// <exception cref="IOException">There was a failure while writing to
		/// the network.</exception>
		void Send(Stanza stanza) {
			stanza.ThrowIfNull("stanza");
			Send(stanza.ToString());
		}

		/// <summary>
		/// Serializes and sends the specified XML element to the server and
		/// subsequently waits for a response.
		/// </summary>
		/// <param name="element">The XML element to send.</param>
		/// <param name="expected">A list of element names that are expected. If
		/// provided, and the read element does not match any of the provided names,
		/// an XmmpException is thrown.</param>
		/// <returns>The XML element read from the stream.</returns>
		/// <exception cref="XmlException">The XML parser has encountered invalid
		/// or unexpected XML data.</exception>		
		/// <exception cref="ArgumentNullException">The element parameter is null.</exception>
		/// <exception cref="IOException">There was a failure while writing to
		/// the network, or there was a failure while reading from the network.</exception>
		XmlElement SendAndReceive(XmlElement element,
			params string[] expected) {
			Send(element);
			return Receive(expected);
		}

		/// <summary>
		/// Listens for incoming XML stanzas and raises the appropriate events.
		/// </summary>
		/// <remarks>This runs in the context of a separate thread. In case of an
		/// exception, the Error event is raised and the thread is shutdown.</remarks>
		void ReadXmlStream() {
			try {
				while (true) {
					XmlElement elem = Receive("iq", "message", "presence");
					// Parse element and dispatch.
					switch (elem.Name) {
						case "iq":
							Iq iq = new Iq(elem);
							if (iq.IsRequest)
								stanzaQueue.Add(iq);
							else
								HandleIqResponse(iq);
							break;
						case "message":
							stanzaQueue.Add(new Message(elem));
							break;
						case "presence":
							stanzaQueue.Add(new Presence(elem));
							break;
					}
				}
			} catch (Exception e) {
				bool wasConnected = Connected;
				Connected = false;
				Authenticated = false;
				// Shut down the dispatcher task.
				cancelDispatch.Cancel();
				cancelDispatch = new CancellationTokenSource();
				// Unblock any threads blocking on pending IQ requests.
				cancelIq.Cancel();
				cancelIq = new CancellationTokenSource();
				if(!disposed && wasConnected)
					Disconnected.Raise(this, new EventArgs());
				if(!disposed)
					Error.Raise(this, new ErrorEventArgs(e));
			}
		}

		XmlElement Receive(params string[] expected) {
			var element = bosh != null ? bosh.Receive(expected) :
				webSocket != null ? webSocket.Receive(expected) :
				parser.NextElement(expected);
			if (element != null) {
				ReceiveXml.Raise(this, new StanzaXmlEventArgs(element.ToXmlString()));
			}

			return element;
		}

		/// <summary>
		/// Continously removes stanzas from the FIFO of incoming stanzas and raises
		/// the respective events.
		/// </summary>
		/// <remarks>This runs in the context of a separate thread. All stanza events
		/// are streamlined and execute in the context of this thread.</remarks>
		void DispatchEvents() {
			while (true) {
				try {
					Stanza stanza = stanzaQueue.Take(cancelDispatch.Token);
					if (stanza is Iq)
						Iq.Raise(this, new IqEventArgs(stanza as Iq));
					else if (stanza is Message)
						Message.Raise(this, new MessageEventArgs(stanza as Message));
					else if (stanza is Presence)
						Presence.Raise(this, new PresenceEventArgs(stanza as Presence));
				} catch(OperationCanceledException) {
					// Quit the task if it's been cancelled.
					return;
				} catch(Exception e) {
					Error.Raise(this, new ErrorEventArgs(e));
				}
			}
		}

		/// <summary>
		/// Handles incoming IQ responses for previously issued IQ requests.
		/// </summary>
		/// <param name="iq">The received IQ response stanza.</param>
		void HandleIqResponse(Iq iq) {
			string id = iq.Id;
			AutoResetEvent ev;
			Action<string, Iq> cb;
			// Signal the event if it's a blocking call.
			if (waitHandles.TryRemove(id, out ev)) {
				iqResponses[id] = iq;
				ev.Set();
			}
			// Call the callback if it's an asynchronous call.
			else if (iqCallbacks.TryRemove(id, out cb))
				Task.Factory.StartNew(() => { cb(id, iq); });
		}

		/// <summary>
		/// Generates a unique id.
		/// </summary>
		/// <returns>A unique id.</returns>
		string GetId() {
			Interlocked.Increment(ref id);
			return id.ToString();
		}

		/// <summary>
		/// Disconnects from the XMPP server.
		/// </summary>
		void Disconnect() {
			if (!Connected)
				return;
			// Close the XML stream.
			if (bosh != null)
				bosh.Close();
			else if (webSocket != null)
				webSocket.Close();
			else
				Send("</stream:stream>");
			Connected = false;
			Authenticated = false;
			Disconnected.Raise(this, new EventArgs());
		}

		void SetTransportUrl(Uri url, XmppTransportBinding transportBinding) {
			url.ThrowIfNull("url");
			switch (transportBinding) {
				case XmppTransportBinding.Bosh:
					BoshUrl = url;
					break;
				case XmppTransportBinding.WebSocket:
					WebSocketUrl = url;
					break;
				default:
					throw new ArgumentOutOfRangeException("transportBinding");
			}
		}
	}
}
