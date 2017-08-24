using WebKit;

namespace QuantConnect.Views.Mac
{
    /// <summary>
    /// WehViewInjectionHandler is a delegate that injects 1 or more 
    /// scripts into the Lean WebView after it has finished loading.   
    /// </summary>
    public class WebViewInjectionHandler : WebFrameLoadDelegate
    {
        private readonly string [] _scripts;
        private readonly WebView _webView;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="webView">the Lean webView</param>
        /// <param name="scripts">an array of script strings.</param>
        public WebViewInjectionHandler(WebView webView, string [] scripts)
        {
            _scripts = scripts;
            _webView = webView;
        }

        public override void FinishedLoad(WebView sender, WebFrame forFrame)
        {
            foreach (var script in _scripts)
            {
                _webView.StringByEvaluatingJavaScriptFromString(script);
            }
        }
    }
}


