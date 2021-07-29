using Artalk.Xmpp.Core.Sasl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		/// If true the session will be TLS/SSL-encrypted if the server supports it.
		/// </summary>
		public bool Tls {
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
			int port = 5222, bool tls = true, RemoteCertificateValidationCallback validate = null) {
				Hostname = hostname;
				Username = username;
				Password = password;
				Port = port;
				Tls = tls;
				Validate = validate;
		}

		/// <summary>
		/// Initializes a new instance of the XmppCore class.
		/// </summary>
		/// <param name="hostname">The hostname of the XMPP server to connect to.</param>
		/// <param name="port">The port number of the XMPP service of the server.</param>
		/// <param name="tls">If true the session will be TLS/SSL-encrypted if the server
		/// supports TLS/SSL-encryption.</param>
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
			RemoteCertificateValidationCallback validate = null) {
			Hostname = hostname;
			Port = port;
			Tls = tls;
			Validate = validate;
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
				client = new TcpClient(Hostname, Port);
				stream = client.GetStream();
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
		public Iq IqRequest(Iq request, int millisecondsTimeout = -1) {
			AssertValid();
			request.ThrowIfNull("request");
			if (request.Type != IqType.Set && request.Type != IqType.Get)
				throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
			// Generate a unique ID for the IQ request.
			request.Id = GetId();
			AutoResetEvent ev = new AutoResetEvent(false);
			Send(request);
			// Wait for event to be signaled by task that processes the incoming
			// XML stream.
			waitHandles[request.Id] = ev;
			int index = WaitHandle.WaitAny(new WaitHandle[] { ev, cancelIq.Token.WaitHandle },
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
			if (feats["starttls"] != null) {
				// TLS is mandatory and user opted out of it.
				if (feats["starttls"]["required"] != null && Tls == false)
					throw new AuthenticationException("The server requires TLS/SSL.");
				if(Tls)
					feats = StartTls(Hostname, Validate);
			}
			// If no Username has been provided, don't perform authentication.
			if (Username == null)
				return;
			// Construct a list of SASL mechanisms supported by the server.
			var m = feats["mechanisms"];
			if (m == null || !m.HasChildNodes)
				throw new AuthenticationException("No SASL mechanisms advertised.");
			var mech = m.FirstChild;
			var list = new HashSet<string>();
			while (mech != null) {
					list.Add(mech.InnerText);
				mech = mech.NextSibling;
			}
			// Continue with SASL authentication.
			try {
				feats = Authenticate(list, Username, Password, Hostname);
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
			// Complete TLS negotiation and switch to secure stream.
			SslStream sslStream = new SslStream(stream, false, validate ??
				((sender, cert, chain, err) => true));
			sslStream.AuthenticateAsClient(hostname);
			stream = sslStream;
			IsEncrypted = true;
			// Initiate a new stream to server.
			return InitiateStream(hostname);
		}

		/// <summary>
		/// Performs SASL authentication.
		/// </summary>
		/// <param name="mechanisms">An enumerable collection of SASL mechanisms
		/// supported by the server.</param>
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
		XmlElement Authenticate(IEnumerable<string> mechanisms, string username,
			string password, string hostname) {
				string name = SelectMechanism(mechanisms);
				SaslMechanism m = SaslFactory.Create(name);
				m.Properties.Add("Username", username);
				m.Properties.Add("Password", password);
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

		/// <summary>
		/// Selects the best SASL mechanism that we support from the list of mechanisms
		/// advertised by the server.
		/// </summary>
		/// <param name="mechanisms">An enumerable collection of SASL mechanisms
		/// advertised by the server.</param>
		/// <returns>The IANA name of the selcted SASL mechanism.</returns>
		/// <exception cref="SaslException">No supported mechanism could be found in
		/// the list of mechanisms advertised by the server.</exception>
		string SelectMechanism(IEnumerable<string> mechanisms) {
			// Precedence: SCRAM-SHA-1, DIGEST-MD5, PLAIN.
			string[] m = new string[] { "SCRAM-SHA-1", "DIGEST-MD5", "PLAIN" };
			for (int i = 0; i < m.Length; i++) {
				if (mechanisms.Contains(m[i], StringComparer.InvariantCultureIgnoreCase))
					return m[i];
			}
			throw new SaslException("No supported SASL mechanism found.");
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
				// Shut down the dispatcher task.
				cancelDispatch.Cancel();
				cancelDispatch = new CancellationTokenSource();
				// Unblock any threads blocking on pending IQ requests.
				cancelIq.Cancel();
				cancelIq = new CancellationTokenSource();
				// Raise the error event.
				if(!disposed)
					Error.Raise(this, new ErrorEventArgs(e));
			}
		}

		XmlElement Receive(params string[] expected) {
			var element = parser.NextElement(expected);
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
					// FIXME: What should we do if an exception is thrown in one of the
					// event handlers?
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
			iqResponses[id] = iq;
			// Signal the event if it's a blocking call.
			if (waitHandles.TryRemove(id, out ev))
				ev.Set();
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
			Send("</stream:stream>");
			Connected = false;
			Authenticated = false;
			Disconnected.Raise(this, new EventArgs());
		}
	}
}
