# Artalk.Xmpp
This repository contains an easy-to-use and well-documented .NET assembly for communicating with an XMPP server. It supports basic Instant Messaging and Presence funtionality as well as a variety of XMPP extensions.

# Introduction

**Artalk Xmpp** is an easy-to-use and well-documented .NET assembly for communicating with an XMPP server. It supports basic Instant Messaging and Presence funtionality as well as a variety of XMPP extensions.


# Supported XMPP Features

The library fully implements the  [XMPP Core](http://xmpp.org/rfcs/rfc3920.html)  and  [XMPP Im](http://xmpp.org/rfcs/rfc3920.html)  specifications and thusly provides the basic XMPP instant messaging (IM) and presence functionality. In addition, the library offers support for most of the optional protocol extensions. More specifically, the following features are supported:

-   TLS/SSL Support
-   [SASL Authentication](http://www.ietf.org/rfc/rfc4422.txt)  (PLAIN, DIGEST-MD5, and SCRAM-SHA-1)   
-   [User Avatars](http://xmpp.org/extensions/xep-0084.html)
-   [SOCKS5](http://xmpp.org/extensions/xep-0065.html)  and  [In-Band](http://xmpp.org/extensions/xep-0047.html)  File-Transfer   
-   [In-Band Registration](http://xmpp.org/extensions/xep-0077.html)   
-   [User Mood](http://xmpp.org/extensions/xep-0107.html)   
-   [User Tune](http://xmpp.org/extensions/xep-0118.html)
-   [User Activity](http://xmpp.org/extensions/xep-0108.html)
-   [Simplified Blocking](http://xmpp.org/extensions/xep-0191.html)

# Where to get it

The project is hosted at GitHub and can be found at  [https://github.com/araditc/Artalk.Xmpp](https://github.com/araditc/Artalk.Xmpp).
You can always get the latest binary package on  [Nuget](https://www.nuget.org/packages/Artalk.Xmpp/) . The documentation is also available for offline viewing as HTML or CHM .

# License

This library is released under the  MIT License. It can be used in private and commercial projects.

# Bug reports

Please send your bug reports to  [bugs@arad-itc.org](mailto:bugs@arad-itc.org)  or create a new issue on the GitHub project homepage.

About XMPP

This section provides a brief overview of the architecture of the Extensible Messaging and Presence Protocol (XMPP).

# ![Collapse image](http://smiley22.github.io/S22.Xmpp/Documentation/icons/collapse_all.gif)Introduction

The Extensible Messaging and Presence Protocol (XMPP) is an open XML-based protocol for near-real-time messaging, presence, and request-response services.

The protocol was originally named Jabber, and was developed by the Jabber open-source community in 1999 for near real-time, instant messaging (IM), presence information, and contact list maintenance. Designed to be extensible, the protocol has also been used for publish-subscribe systems, signalling for VoIP, video, file transfer, gaming, Internet of Things applications such as the smart grid, and social networking services.

# ![Collapse image](http://smiley22.github.io/S22.Xmpp/Documentation/icons/collapse_all.gif)Architecture

The XMPP network uses a client–server architecture (clients do not talk directly to one another). However, it is decentralized—by design, there is no central authoritative server, as there is with services such as AOL Instant Messenger or Windows Live Messenger.

Clients send and receive messages only to and from their respective server and it is the server's responsibility to route appropriately-addressed messages through the network to the intended user. For this purpose, every user on the network has a unique Jabber ID (usually abbreviated as  **JID**  ). To avoid requiring a central server to maintain a list of IDs, the JID is structured like an email address with a username and a domain name for the server where that user resides, separated by an at sign (@), such as username@example.com.

The following diagram provides a general overview of the XMPP architecture:

![Xmpp](https://raw.githubusercontent.com/araditc/Artalk.Xmpp/master/xmpp.png)

# See Also

#### Other Resources
[XMPP on Wikipedia](http://en.wikipedia.org/wiki/XMPP)

##Installation

This quick start shows you how to incorporate the َArtalk.Xmpp assembly into your application.

### Getting up and running

1.  Add a reference to the Artalk.Xmpp assembly: Open your project in Visual Studio and, in Solution Explorer, right-click References, and then click Add Reference. Locate Artalk.Xmpp.dll and add it.
2.  You can now start using the namespaces exposed by the Artalk.Xmpp assembly.
    
 ## Prerequisites

### To complete this quick start, you must have the following components:

1.  Visual Studio 2003 or newer.    
2.  A copy of the Artalk.Xmpp.dll assembly.

# Getting Started

This section provides a brief overview of the different namespaces and classes exposed by the Artalk.Xmpp assembly.

# Artalk.Xmpp.Core

The  Artalk.Xmpp.Core  namespace contains basic client-side components that provide a general framework for exchanging structured information in the form of XML data elements between any two network endpoints and correlates closely to the  _Extensible Messaging and Presence Protocol (XMPP): Core_ specification ([RFC 3920](http://xmpp.org/rfcs/rfc3920.html)).

As such, the classes of the  Artalk.Xmpp.Core  namespace provide building blocks for creating custom communication protocols based on the core XMPP concepts of XML streams and XML stanzas.

# Artalk.Xmpp.Im

The  Artalk.Xmpp.Im  namespace contains components that implement the basic instant messaging (IM) and presence functionality defined in the  _Extensible Messaging and Presence Protocol (XMPP): Instant Messaging and Presence_ specification ([RFC 3921](http://xmpp.org/rfcs/rfc3921.html)).

In other words,  Artalk.Xmpp.Im  provides a lean "bare bones" XMPP client that implements the minimum feature set that is expected from a conforming implementation. It can be used in situations where a full-scale implementation with support for various XMPP protocol extensions is not neccessary, and can also be used as a basis for creating XMPP clients that implement only a select set of extensions.

# Artalk.Xmpp.Client

The  Artalk.Xmpp.Client  namespace contains components that built on-top the classes of the  Artalk.Xmpp.Im  and the  Artalk.Xmpp.Core  namespaces to implement a feature-rich XMPP client.

# Howto: Connect to an XMPP server
The following example demonstrates how to connect to an XMPP server using the  ArtalkXmppClient class.

# Connecting using an existing XMPP account

If you already have an account on the server to which you want to connect, establishing a connection is as easy as initializing a new instance of the  ArtalkXmppClient  class und calling the  Connect  method as demonstrated in the following example application:

### C#

```C#                               
using Artalk.Xmpp.Client;
using System;
using System.Text.RegularExpressions;

namespace ArtalkTest {
    class Program {
        static void Main(string[] args) {
            string hostname = "artalk.ir";
            string username = "myusername";
            string password = "mysecretpassword";

            using (ArtalkXmppClient client = new ArtalkXmppClient(hostname, username, password)) {
                // Setup any event handlers.
                // ...
                client.Connect();
                
                Console.WriteLine("Type 'quit' to exit.");
                while (Console.ReadLine() != "quit");
            }
        }
    }
}
```

### Send Message

```C#
client.SendMessage(recipient, message);
```
Using the constructor of the ArtalkXmppClient class as in the example above will automatically negotiate TLS/SSL encryption if the server supports it, however this behaviour can be disabled by passing the constructor a value of false for the tls parameter.

## Howto: Setup XMPP event handlers

The following example demonstrates how to set up event handlers for the events exposed by the  ArtalkXmppClient  class.

### Hooking up to events

1.  The  ArtalkXmppClient  class exposes a variety of events that can be subscribed to in order to be notified of the receipt of new chat messages, status changes of a chat contact, incoming authorization requests, etc. The following piece of example code shows how to hook up to the  Message  event in order to be notified of the receipt of new chat-messages:
    
   ### C#
    
    ```C#
    using Artalk.Xmpp.Client;
    using System;
    
    namespace ArtalkTest {
        class Program {
            static void Main(string[] args) {
                string hostname = "artalk.ir";
                string username = "myusername";
                string password = "mypassword";
    
                using (XmppClient client = new XmppClient(hostname, username, password)) {
                    // Setup any event handlers before connecting.
                    client.Message += OnNewMessage;
                    // Connect and authenticate with the server.
                    client.Connect();
    
                    Console.WriteLine("Type 'quit' to exit.");
                    while (Console.ReadLine() != "quit");
                }
            }
    
            /// <summary>
            /// Invoked whenever a new chat-message has been received.
            /// </summary>
            static void OnNewMessage(object sender, MessageEventArgs e) {
                Console.WriteLine("Message from <" + e.Jid + ">: " + e.Message.Body);
            }
        }
    }
    ```
    
2.  Whenever a chat-message is sent to myusername@artalk.ir, the OnNewMessage method will be invoked. The  MessageEventArgs  parameter contains information about the sender of the message as well its content.
    

# Robust Programming

**Event handlers are invoked sequentially on a separate thread.**

You should not call blocking methods from within event handlers and you must not directly update UI controls from within an event handler. Rather, you should call the  [Invoke](http://msdn.microsoft.com/en-us/library/zyzhdc6b(v=vs.110).aspx)  method of the respective UI control to have the control updated on the thread that owns the control's underlying window handle.
