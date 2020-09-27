namespace Messages
{
    public class LogoutMessage : ServerMessage
    {
        public User User { get; private set; }

        public LogoutMessage(User user)
            : base(Gizmo.MessageCode.LogOut)
        {
            User = user;
        }
    }
}