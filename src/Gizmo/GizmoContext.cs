namespace Gizmo
{
    using System;
    using System.Windows.Forms;

    public class GizmoContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;

        private Client _client;

        public GizmoContext()
        {
            // Initialize Tray Icon
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.Icon,
                ContextMenu = new ContextMenu(new[]
                {
                  //  new MenuItem("Zaloguj", LogInOffice),
                  //  new MenuItem("Wyloguj", ExitOffice), 
                    new MenuItem("Zamknij", Exit)
                }),


                Visible = true
            };

            _trayIcon.Text = @"Brak połączenia z serwerem";
            _trayIcon.DoubleClick += TrayIconOnDoubleClick;
            _client = new Client(_trayIcon);
        }

        private void TrayIconOnDoubleClick(object sender, EventArgs e)
        {
            _client.ChangeState();
        }

        private void ExitOffice(object sender, EventArgs e)
        {
            _client.ExitOffice();
        }


        private void LogInOffice(object sender, EventArgs e)
        {
            _client.LogInOffice();
        }

        void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _client.Dispose();
            Application.Exit();
        }
    }
}