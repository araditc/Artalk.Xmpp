using System;

namespace Artalk.Xmpp {
	/// <summary>
	/// The exception that is thrown when a generic XMPP error condition has been encountered.
	/// </summary>
	public class XmppException : Exception {
		/// <summary>
		/// Initializes a new instance of the XmppException class.
		/// </summary>
		public XmppException() : base() { }

		/// <summary>
		/// Initializes a new instance of the XmppException class with its message
		/// string set to <paramref name="message"/>.
		/// </summary>
		/// <param name="message">A description of the error.</param>
		public XmppException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the XmppException class with its message
		/// string set to <paramref name="message"/> and a reference to the inner exception.
		/// </summary>
		/// <param name="message">A description of the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public XmppException(string message, Exception inner) : base(message, inner) { }
	}
}
