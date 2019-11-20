using Artalk.Xmpp.Client;
using Artalk.Xmpp.Im;
using System;
using System.Windows.Forms;

namespace Artalk.WinClient
{
    public partial class MainForm : Form
    {
        ArtalkXmppClient client;
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            client = new ArtalkXmppClient(txtServer.Text, txtUsername.Text, txtPassword.Text);

            client.Message += OnNewMessage;
            client.Connect();
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
        }

        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            client.SendMessage(txtTo.Text, txtMessage.Text, null, null, MessageType.Chat, null);
        }

        private void OnNewMessage(object sender, MessageEventArgs e)
        {
            MessageBox.Show("Message from <" + e.Jid + ">: " + e.Message.Body);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client.Dispose();
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
        }
    }
}
