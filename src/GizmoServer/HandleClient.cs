namespace GizmoServer
{
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    public class HandleClient
    {
        private Server _server;

        public TcpClient ClientSocket { get; private set; }

        public HandleClient(Server server)
        {
            _server = server;
        }

        public void startClient(TcpClient inClientSocket)
        {
            ClientSocket = inClientSocket;
        
            Thread clientThread = new Thread(DoWork);
            clientThread.Start();
        }

        private void DoWork()
        {
            byte[] bytes = new byte[1024];
            var connected = true;

            while (connected)
            {
                try
                {
                    if (!ClientSocket.Connected)
                    {
                        Console.WriteLine(" >> Client disconnected");
                        connected = false;
                        _server.ConnectionLost(ClientSocket);
                    
                        break;
                    }

                    NetworkStream networkStream = ClientSocket.GetStream();

                    int bytesRead = networkStream.Read(bytes, 0, bytes.Length);
                    var message = Encoding.UTF8.GetString(bytes, 0, bytesRead);
                    _server.EncodeMessage(this, message);
                }
                catch
                {
                    // do nothing 
                }
            }
        }
    }
}