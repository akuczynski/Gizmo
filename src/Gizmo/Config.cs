namespace Gizmo
{
    using System.Configuration;

    public class Config
    {
        public string User
        {
            get
            {
                var value = ConfigurationManager.AppSettings["user"];
                return value;
            }
        }
    }
}