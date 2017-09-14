using System;
using System.Threading;
using AppKit;
using Foundation;

namespace QuantConnect.Views.Mac
{
    public partial class ViewController : NSViewController
    {

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // get port passed as CLI arg
            var port = NSProcessInfo.ProcessInfo.Arguments[1];

			webView.MainFrame.LoadHtmlString("Waiting for data...", null);

			// pass in NSViewController for InvokeOnMainThread context
            var messageHandler = new WebViewMessageHandler(this, webView, logView, statusField);

			var desktopClient = new DesktopClient();

			new Thread(() => {
			    desktopClient.Run(port, messageHandler);
			}).Start();


		}

    }
}
