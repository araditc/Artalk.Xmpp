using System;

namespace Artalk.Xmpp.Client {
	/// <summary>
	/// Provides data for the KeepAliveFailed event.
	/// </summary>
	public class KeepAliveFailedEventArgs : EventArgs {
		/// <summary>
		/// The exception that occurred while sending or waiting for a keepalive ping.
		/// </summary>
		public Exception Exception {
			get;
			private set;
		}

		/// <summary>
		/// Initializes a new instance of the KeepAliveFailedEventArgs class.
		/// </summary>
		/// <param name="exception">The exception that caused the keepalive failure.</param>
		public KeepAliveFailedEventArgs(Exception exception) {
			exception.ThrowIfNull("exception");
			Exception = exception;
		}
	}
}
