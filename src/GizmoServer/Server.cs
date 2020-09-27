namespace GizmoServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using Gizmo;
    using Messages;
    using Newtonsoft.Json;

    public class Server
    {
        private const int TcpPortNumber = 4545;
        
        private const int SleepTimer = 50000;

        private IList<HandleClient> _clients;

        private Dictionary<User, HandleClient> _logedInUsers;

        private object _lock;

        public Server()
        {
            _clients = new List<HandleClient>();

            _logedInUsers = new Dictionary<User, HandleClient>();
            _lock = new object();
        }

        public void Start()
        {
            TcpListener serverSocket = new TcpListener(TcpPortNumber);
            TcpClient clientSocket = default;
     
            serverSocket.Start();
            Console.WriteLine(" >> " + "Server Started");

            var sendToAllThread = new Thread(Broadcast);
            sendToAllThread.Start();

            while (true)
            {
                clientSocket = serverSocket.AcceptTcpClient();
                Console.WriteLine(" >> " + "New client!");

                HandleClient client = new HandleClient(this);
                _clients.Add(client);

                client.startClient(clientSocket);
            }

            clientSocket.Close();
            serverSocket.Stop();
            Console.WriteLine(" >> " + "exit");
            Console.ReadLine();
        }

        public void EncodeMessage(HandleClient client, string message)
        {
            if (!message.Contains('#'))
            {
                return;
            }

            var messageParts = message.Split('#');
            var messageCode = messageParts[0];
            var userName = messageParts[1];

            switch (messageCode)
            {
                case MessageCode.LogIn:
                {
                    Console.WriteLine($">> {userName} has logged in.");
                    var user = new User();
                    user.Name = userName;

                    if (!_logedInUsers.Keys.Any(x => x.Name.Equals(userName)))
                    {
                        lock (_lock)
                        {
                            _logedInUsers.Add(user, client);
                        }
                    }

                    break;
                }
                case MessageCode.LogOut:
                {
                    var user = _logedInUsers.Keys.FirstOrDefault(x => x.Name.Equals(userName));
                    if (user != null)
                    {
                        lock (_lock)
                        {
                            LogOutUser(user);
                        }
                    }

                    break;
                }
            }
        }

        public void ConnectionLost(TcpClient clientSocket)
        {
            User userToLogout = null;
            foreach (var user in _logedInUsers.Keys)
            {
                var connectionSocket = _logedInUsers[user];

                if (clientSocket.Equals(connectionSocket?.ClientSocket))
                {
                    userToLogout = user;
                }
            }

            LogOutUser(userToLogout);
        }

        private void Broadcast()
        {
            while (true)
            {
                var users = _logedInUsers.Keys.ToList();
                var data = new LogedInUsersMessage(users);
                var newMessage = CreateMessage(data);

                foreach (var client in _clients)
                {
                    SendMessage(client, newMessage);
                }

                Thread.Sleep(SleepTimer); 
            }
        }

        private void SendMessage(HandleClient client, string newMessage)
        {
            if (client.ClientSocket.Connected)
            {
                var stream = client.ClientSocket.GetStream();
                var ms = newMessage + "#";

                byte[] byteTime = Encoding.UTF8.GetBytes(ms);
                stream.Write(byteTime, 0, byteTime.Length);
            }
        }

        private string CreateMessage(object data)
        {
            return JsonConvert.SerializeObject(data);
        }

        private void SendNobodyIsLoggedInMessage()
        {
            var data = new ServerMessage(MessageCode.NobodyInLoggedIn);
            var newMessage = CreateMessage(data);

            var lastUser = _logedInUsers.First();
            var client = lastUser.Value;
            
            if (client.ClientSocket.Connected)
            {
                var stream = client.ClientSocket.GetStream();
                var ms = newMessage + "#";

                byte[] byteTime = Encoding.UTF8.GetBytes(ms);
                stream.Write(byteTime, 0, byteTime.Length);
            }
        }

        private void LogOutUser(User user)
        {
            Console.Out.WriteLine($">> {user.Name} has logged out.");

            _logedInUsers.Remove(user);

            // inform all other users 
            var users = _logedInUsers.Keys.ToList();
            var data = new LogoutMessage(user);
            var newMessage = CreateMessage(data);

            foreach (var client in _clients)
            {
                SendMessage(client, newMessage);
            }

            if (_logedInUsers.Count == 1)
            {
                SendNobodyIsLoggedInMessage();
            }
        }
    }
}