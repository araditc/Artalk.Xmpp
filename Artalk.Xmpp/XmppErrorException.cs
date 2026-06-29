using System;

namespace Artalk.Xmpp {
	/// <summary>
	/// The exception that is thrown when a recoverable XMPP error condition
	/// has been encountered.
	/// </summary>
	public class XmppErrorException : Exception {
		/// <summary>
		/// The XMPP error.
		/// </summary>
		public XmppError Error {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the XmppErrorException class.
		/// </summary>
		/// <param name="error">The XMPP error that is the reason for the exception.</param>
		public XmppErrorException(XmppError error) : base() {
			error.ThrowIfNull("error");
			Error = error;
		}

		/// <summary>
		/// Initializes a new instance of the XmppErrorException class with its message
		/// string set to <paramref name="message"/>.
		/// </summary>
		/// <param name="error">The XMPP error that is the reason for the exception.</param>
		/// <param name="message">A description of the error.</param>
		public XmppErrorException(XmppError error, string message) : base(message) {
			error.ThrowIfNull("error");
			Error = error;
		}

		/// <summary>
		/// Initializes a new instance of the XmppErrorException class with its message
		/// string set to <paramref name="message"/> and a reference to the inner exception.
		/// </summary>
		/// <param name="error">The XMPP error that is the reason for the exception.</param>
		/// <param name="message">A description of the error.</param>
		/// <param name="inner">The exception that is the cause of the current exception.</param>
		public XmppErrorException(XmppError error, string message, Exception inner)
			: base(message, inner) {
			error.ThrowIfNull("error");
			Error = error;
		}
	}
}
