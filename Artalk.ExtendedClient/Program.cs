using System;
using System.Linq;
using System.Xml;

namespace Artalk.ExtendedClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 3 || args.Length > 5 || args[0] == "-h")
            {
                Console.WriteLine("Usage: ./Artalk.ExtendedClient.exe HOST LOGIN PASSWORD [PORT] [--direct-tls]");
                return;
            }

            var host = args[0];
            var login = args[1];
            var password = args[2];
            var port = args.Length >= 4 && int.TryParse(args[3], out var parsedPort)
                ? parsedPort
                : 5222;
            var directTls = args.Any(a => string.Equals(a, "--direct-tls", StringComparison.OrdinalIgnoreCase));
            var client = new ExtendedXmppClient(host, login, password, port: port, directTls: directTls);

            client.StatusChanged += (sender, e) =>
                Console.WriteLine($"Presence from {e.Jid}: {e.Status.Availability}");
            client.Connect("extended-client");

            Console.Write("Message to save: ");
            var message = Console.ReadLine();
            var messageElement = new XmlDocument().CreateElement("message");
            messageElement.InnerText = message ?? string.Empty;

            client.SaveXml(messageElement);
            client.Close();

            Console.WriteLine("Data saved. Connection closed.");
        }
    }
}
