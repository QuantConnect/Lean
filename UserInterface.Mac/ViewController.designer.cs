// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace QuantConnect.Views.Mac
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        AppKit.NSTextView logView { get; set; }

        [Outlet]
        AppKit.NSTextField statusField { get; set; }

        [Outlet]
        WebKit.WebView webView { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (logView != null) {
                logView.Dispose ();
                logView = null;
            }

            if (statusField != null) {
                statusField.Dispose ();
                statusField = null;
            }

            if (webView != null) {
                webView.Dispose ();
                webView = null;
            }
        }
    }
}
