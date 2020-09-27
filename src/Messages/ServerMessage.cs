namespace Messages
{
    public class ServerMessage
    {
        public string MessageCode { get; private set; }

        public ServerMessage(string messageCode)
        {
           MessageCode = messageCode;
        }
    }
}