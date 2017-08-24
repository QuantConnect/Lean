using System;
using System.Threading;
using Foundation;
using AppKit;
using CoreGraphics;
using CoreText;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Messaging;
using QuantConnect.Packets;

namespace QuantConnect.Views.Mac
{
    public class WebViewMessageHandler : IDesktopMessageHandler
    {
        private readonly QueueLogHandler _logging;

        private bool _liveMode = false;
        private AlgorithmNodePacket _job;
        private readonly WebKit.WebView _webView;
        private readonly NSTextView _logView;
        private readonly NSTextField _statusLabel;
        private readonly NSViewController _viewController;
        private readonly Timer _timer;

        public WebViewMessageHandler(NSViewController viewController, WebKit.WebView webView, NSTextView logView,
            NSTextField statusLabel)
        {
            _logging = new QueueLogHandler();
            _webView = webView;
            _viewController = viewController;
            _logView = logView;
            _statusLabel = statusLabel;

            // update logging and status every 250ms
            _timer = new Timer(x => timer_Tick(), null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
        }

        public void Initialize(AlgorithmNodePacket job)
        {
            _job = job;

            //Show warnings if the API token and UID aren't set.
            if (_job.UserId == 0)
            {
                var alert = new NSAlert
                {
                	AlertStyle = NSAlertStyle.Critical,
                	InformativeText = "Your user id is not set. Please check your config.json file 'job-user-id' property.",
                	MessageText = "LEAN Algorithmic Trading"
                };
                alert.RunModal();
            }
            if (_job.Channel == "")
            {
                var alert = new NSAlert
                {
                    AlertStyle = NSAlertStyle.Critical,
                    InformativeText = "Your API token is not set. Please check your config.json file 'api-access-token' property.",
                    MessageText = "LEAN Algorithmic Trading"
                };
                alert.RunModal();
            }

            _liveMode = job is LiveNodePacket;

            var url = GetUrl(job, _liveMode);
            var request = new NSUrlRequest(new NSUrl(url));

            // navigate to url (all UI should be on main thread)
            _viewController.InvokeOnMainThread(() => { _webView.MainFrame.LoadRequest(request); });
        }

        public void DisplayHandledErrorPacket(HandledErrorPacket packet)
        {
            var hstack = !string.IsNullOrEmpty(packet.StackTrace)
                ? Environment.NewLine + " " + packet.StackTrace
                : string.Empty;
            _logging.Error(packet.Message + hstack);
        }

        public void DisplayRuntimeErrorPacket(RuntimeErrorPacket packet)
        {
            var rstack = !string.IsNullOrEmpty(packet.StackTrace)
                ? Environment.NewLine + " " + packet.StackTrace
                : string.Empty;
            _logging.Error(packet.Message + rstack);
        }

        public void DisplayLogPacket(LogPacket packet)
        {
            _logging.Trace(packet.Message);
        }

        public void DisplayDebugPacket(DebugPacket packet)
        {
            _logging.Trace(packet.Message);
        }

        void IDesktopMessageHandler.DisplayBacktestResultsPacket(BacktestResultPacket packet)
        {
            if (packet.Progress != 1) return;

            //Remove previous event handler:
            var url = GetUrl(_job, _liveMode, true);

            //Generate JSON:
            var jObj = new JObject();
            const string dateFormat = "yyyy-MM-dd HH:mm:ss";
            dynamic final = jObj;
            final.dtPeriodStart = packet.PeriodStart.ToString(dateFormat);
            final.dtPeriodFinished = packet.PeriodFinish.AddDays(1).ToString(dateFormat);
            dynamic resultData = new JObject();
            resultData.version = 3;
            resultData.results = JObject.FromObject(packet.Results);
            resultData.statistics = JObject.FromObject(packet.Results.Statistics);
            resultData.iTradeableDates = 1;
            resultData.ranking = null;
            final.oResultData = resultData;
            var json = JsonConvert.SerializeObject(final);

            var request = new NSUrlRequest(new NSUrl(url));

            //var script = "window.jnBacktest = JSON.parse('" + json + "');";
            var scripts = new string[]
            {
                "window.jnBacktest = JSON.parse('" + json + "');",
                "$.holdReady(false)"
            };
            
            var handler = new WebViewInjectionHandler(_webView, scripts);
            _webView.FrameLoadDelegate = handler;

            // all UI should be on main thread
            _viewController.InvokeOnMainThread(() => { _webView.MainFrame.LoadRequest(request); });

            foreach (var pair in packet.Results.Statistics)
            {
                _logging.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
            }
        }

        /// <summary>
        /// Get the URL for the embedded charting
        /// </summary>
        /// <param name="job">Job packet for the URL</param>
        /// <param name="liveMode">Is this a live mode chart?</param>
        /// <param name="holdReady">Hold the ready signal to inject data</param>
        private static string GetUrl(AlgorithmNodePacket job, bool liveMode = false, bool holdReady = false)
        {
            var hold = holdReady == false ? "0" : "1";
            var embedPage = liveMode ? "embeddedLive" : "embedded";

            var url =
                $"https://www.quantconnect.com/terminal/{embedPage}?user={job.UserId}&token={job.Channel}&pid={job.ProjectId}&version={Globals.Version}&holdReady={hold}&bid={job.AlgorithmId}";

            return url;
        }

        private void timer_Tick()
        {
            _viewController.InvokeOnMainThread(() =>
            {
                _statusLabel.StringValue = string.Concat("Performance: CPU: ", OS.CpuUsage.NextValue().ToString("0.0"),
                    "%",
                    " Ram: ", OS.TotalPhysicalMemoryUsed, " Mb");

                if (_logging == null) return;

                LogEntry log;
                while (_logging.Logs.TryDequeue(out log))
                {
                    CGColor color;
                    switch (log.MessageType)
                    {
                        case LogType.Debug:
                            color = new CGColor(0.0f, 1.0f, 0.0f); // green
                            break;
                        case LogType.Trace:
                            color = new CGColor(0.0f, 0.0f, 1.0f); // blue
                            break;
                        case LogType.Error:
                            color = new CGColor(1.0f, 0.0f, 0.0f); // red
                            break;
                        default:
                            color = new CGColor(0.0f, 0.0f, 0.0f); // black
                            break;
                    }

                    var msg = new NSAttributedString(log + "\n",
                        new CTStringAttributes { ForegroundColor = color});
                    
                    // add log text to logView
                    _logView.TextStorage.Append(msg);
                    
                    // scroll to end of log
                    _logView.ScrollPoint(new CGPoint(1, _logView.Frame.Size.Height));
                }
            });
        }
    }
}