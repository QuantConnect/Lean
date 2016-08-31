using System;
using System.Threading;
using System.Windows.Forms;
using QuantConnect.Views.WinForms;

namespace QuantConnect.Views
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new Exception(
                    "Error: You must specify the port on which the application will open a TCP socket.");
            }

            var port = args[0];

            var form = new LeanWinForm();

            var desktopClient = new DesktopClient();

            var thread = new Thread(() => desktopClient.Run(port, form));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            Application.Run(form);

            // The above code is blocking.
            // Once it finishes, close the NetMQ client
            desktopClient.StopServer();
        }
    }
}