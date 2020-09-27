namespace Gizmo
{
    using System;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using Messages;
    using Newtonsoft.Json;

    public class Client : IDisposable
    {
        private const int PortNumber = 4545;

        private const string Host = "10.150.0.99"; // "localhost"; 

        private const string I4bDHCPServer = "10.0.8.13";

        private TcpClient _client;

        private Config _config;

        private bool _isInOffice;

        private Thread _listeningThread;

        private NetworkStream _networkStream;

        private NotifyIcon _notifyIcon;

        private const int SleepTimer = 50000;  


        public Client(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
            _config = new Config();

            do
            {
                Connect();
                if (_client == null || !_client.Connected)
                {
                    Thread.Sleep(SleepTimer);
                }
            } while (_client == null || !_client.Connected);

            StartListening();
        }

        public void Dispose()
        {
            _listeningThread.Abort();

            _client.Dispose();
        }

        private void Connect()
        {
            try
            {
                _client = new TcpClient(Host, PortNumber);
                _networkStream = _client.GetStream();
                _notifyIcon.Text = @"Połączono";

                LogInOffice();
            }
            catch
            {
                // do nothing
            }
        }

        private void StartListening()
        {
            _listeningThread = new Thread(ProcessServerMessages);
            _listeningThread.Start();
        }


        public void ProcessServerMessages()
        {
            byte[] bytes = new byte[1024];

            while (true)
            {
                if (_client.Connected)
                {
                    try
                    {
                        int bytesRead = _networkStream.Read(bytes, 0, bytes.Length);

                        var message = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                        DecodeMessage(message);
                    }
                    catch
                    {
                        // server was shutdown 
                        _notifyIcon.Text = @"Brak połączenia z serwerem";
                    }
                }
                else
                {
                    // reconnect 
                    Thread.Sleep(SleepTimer);
                    Connect();
                }
            }
        }

        private bool IsOverVPNConnection()
        {
            var i4bDhcpAdress = System.Net.IPAddress.Parse(I4bDHCPServer);

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {

                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                IPAddressCollection addresses = adapterProperties.DhcpServerAddresses;
                if (addresses.Count > 0)
                {
                    Console.WriteLine(adapter.Description);
                    foreach (IPAddress address in addresses)
                    {
                        if (address.Equals(i4bDhcpAdress))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void DecodeMessage(string message)
        {
            var messages = message.Split('#');
            foreach (var m in messages)
            {
                DecodeUserDataMessage(m);
            }
        }

        private void DecodeUserDataMessage(string message)
        {
            if (string.IsNullOrEmpty(message) || !IsInTheOffice())
            {
                return;
            }

            var serverMessage = JsonConvert.DeserializeObject<ServerMessage>(message);

            if (serverMessage.MessageCode.Equals(MessageCode.NobodyInLoggedIn))
            {
                ShowInfoTooltip("Gizmo", "Jesteś ostatnim pracownikiem w biurze !");
            }

            if (serverMessage.MessageCode.Equals(MessageCode.LogOut))
            {
                var dataMessage = JsonConvert.DeserializeObject<LogoutMessage>(message);
                var userName = dataMessage.User.Name;
                if (!userName.Equals(_config.User))
                {
                    ShowInfoTooltip("Gizmo", $"{userName} opuścił biuro.");
                }
            }
            else
            {
                var dataMessage = JsonConvert.DeserializeObject<LogedInUsersMessage>(message);
                StringBuilder messageText = new StringBuilder();

                foreach (var user in dataMessage.Users)
                {
                    if (!user.Name.Equals(_config.User))
                    {
                        messageText.AppendLine(user.Name);
                    }
                }

                if (messageText.Length > 0)
                {
                    _notifyIcon.Text = messageText.ToString();
                }
                else
                {
                    _notifyIcon.Text = @"Połączono";
                }
            }
        }

        private void SendMessage(string message)
        {
            byte[] byteTime = Encoding.UTF8.GetBytes(message);
            _networkStream.Write(byteTime, 0, byteTime.Length);
        }

        public void LogInOffice()
        {
            if (IsInTheOffice())
            {
                var message = CreateMessage(MessageCode.LogIn);
                SendMessage(message);

                _isInOffice = true;
            }
        }

        public void ExitOffice()
        {
            if (IsInTheOffice())
            {
                var message = MessageCode.LogOut + "#" + _config.User;
                SendMessage(message);

                _isInOffice = false;
            }
        }

        public void ChangeState()
        {
            IsOverVPNConnection();

            if (_isInOffice)
            {
                ExitOffice();
            }
            else
            {
                LogInOffice();
            }
        }

        private string CreateMessage(string messageCode)
        {
            return messageCode + "#" + _config.User;
        }

        private bool IsInTheOffice()
        {
            return !IsOverVPNConnection() && _client.Connected;
        }

        private void ShowInfoTooltip(string title, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _notifyIcon.ShowBalloonTip(20000, title, message, ToolTipIcon.Info);
            }
        }
    }
}