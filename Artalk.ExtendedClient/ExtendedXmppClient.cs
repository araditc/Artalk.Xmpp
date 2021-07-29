using System.Net.Security;
using System.Xml;
using Artalk.Xmpp.Client;

namespace Artalk.ExtendedClient
{
    public class ExtendedXmppClient : ArtalkXmppClient
    {
        private readonly PrivateXmlStorage _privateStorage;

        public ExtendedXmppClient(string hostname, string username, string password, int port = 5222, bool tls = true, RemoteCertificateValidationCallback validate = null)
            : base(hostname, username, password, port, tls, validate)
        {
            _privateStorage = LoadExtension<PrivateXmlStorage>();
        }

        public ExtendedXmppClient(string hostname, int port = 5222, bool tls = true, RemoteCertificateValidationCallback validate = null)
            : base(hostname, port, tls, validate)
        {
            _privateStorage = LoadExtension<PrivateXmlStorage>();
        }

        public void SaveXml(XmlElement data)
        {
            _privateStorage.SaveXml("exodus", "exodus:prefs", data);
        }

        public XmlElement LoadXml()
        {
            return _privateStorage.LoadXml("exodus", "exodus:prefs");
        }
    }
}
