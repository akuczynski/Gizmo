namespace Messages
{
    using System.Collections.Generic;

    public class LogedInUsersMessage : ServerMessage
    {
        public IList<User> Users { get; private set; }

        public LogedInUsersMessage(IList<User> users)
            : base(Gizmo.MessageCode.LoggedInUsers)
        {
            Users = users;
        }
    }
}