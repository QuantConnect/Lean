using System;
using System.Drawing;
using System.Windows.Forms;
using Gecko;
using Gecko.JQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Messaging;
using QuantConnect.Packets;

namespace QuantConnect.Views.WinForms
{
    public partial class LeanWinForm : Form
    {
        private Engine _engine;
        private AlgorithmNodePacket _job;
        private GeckoWebBrowser _browser;
        private EventMessagingHandler _messaging;
        private QueueLogHandler _logging;
        private LeanEngineSystemHandlers _systemHandlers;
        private LeanEngineAlgorithmHandlers _algorithmHandlers;
        
        /// <summary>
        /// Create the UX.
        /// </summary>
        /// <param name="systemHandlers"></param>
        /// <param name="algorithmHandlers"></param>
        /// <param name="job"></param>
        public LeanWinForm(LeanEngineSystemHandlers systemHandlers, LeanEngineAlgorithmHandlers algorithmHandlers, AlgorithmNodePacket job)
        {
            InitializeComponent();

            //Form Setup:
            CenterToScreen();
            WindowState = FormWindowState.Maximized;
            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Globals.Version;

            //Save off the messaging event handler we need:
            _job = job;
            _systemHandlers = systemHandlers;
            _algorithmHandlers = algorithmHandlers;
            _messaging = (EventMessagingHandler)systemHandlers.Notify;

            //Create the browser control
            _browser = new GeckoWebBrowser { Dock = DockStyle.Fill, Name = "browser" };
            _browser.DOMContentLoaded += BrowserOnDomContentLoaded;
            splitPanel.Panel1.Controls.Add(_browser);

            var url = GetURL(job, false, false);
            _browser.Navigate(url);

            //Setup Event Handlers:
            _messaging.DebugEvent += MessagingOnDebugEvent;
            _messaging.LogEvent += MessagingOnLogEvent;
            _messaging.RuntimeErrorEvent += MessagingOnRuntimeErrorEvent;
            _messaging.HandledErrorEvent += MessagingOnHandledErrorEvent;
            _messaging.BacktestResultEvent += MessagingOnBacktestResultEvent;

            _logging = Log.LogHandler as QueueLogHandler;
        }

        /// <summary>
        /// Get the URL for the embedded charting
        /// </summary>
        /// <param name="job">Job packet for the URL</param>
        /// <param name="liveMode">Is this a live mode chart?</param>
        /// <param name="holdReady">Hold the ready signal to inject data</param>
        private string GetURL(AlgorithmNodePacket job, bool liveMode = false, bool holdReady = false)
        {
            var url = "";
            var hold = holdReady == false ? "0" : "1";
            var embedPage = liveMode ? "embeddedLive" : "embedded";

            url = string.Format(
                "https://www.quantconnect.com/terminal/{0}?user={1}&token={2}&pid={3}&version={4}&holdReady={5}&bid={6}",
                embedPage, job.UserId, job.Channel, job.ProjectId, Globals.Version, hold, job.AlgorithmId);

            return url;
        }
        

        /// <summary>
        /// Browser content has completely loaded.
        /// </summary>
        private void BrowserOnDomContentLoaded(object sender, DomEventArgs domEventArgs)
        {
            _messaging.OnConsumerReadyEvent();
        }

        /// <summary>
        /// Onload Form Initialization
        /// </summary>
        private void LeanWinForm_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Update the status label at the bottom of the form
        /// </summary>
        private void timer_Tick(object sender, EventArgs e)
        {
            StatisticsToolStripStatusLabel.Text = string.Concat("Performance: CPU: ", OS.CpuUsage.NextValue().ToString("0.0"), "%",
                                                                " Ram: ", OS.TotalPhysicalMemoryUsed, " Mb");

            if (_logging != null)
            {
                LogEntry log;
                while (_logging.Logs.TryDequeue(out log))
                {
                    switch (log.MessageType)
                    {
                        case LogType.Debug:
                            LogTextBox.AppendText(log.ToString(), Color.Black);
                            break;
                        default:
                        case LogType.Trace:
                            LogTextBox.AppendText(log.ToString(), Color.Black);
                            break;
                        case LogType.Error:
                            LogTextBox.AppendText(log.ToString(), Color.DarkRed);
                            break;
                    }
                }
            }
        }
         
        /// <summary>
        /// Backtest result packet
        /// </summary>
        /// <param name="packet"></param>
        private void MessagingOnBacktestResultEvent(BacktestResultPacket packet)
        {
            if (packet.Progress == 1)
            {
                //Remove previous event handler:
                var url = GetURL(_job, false, true);
                _browser.Navigate(url);

                _browser.DOMContentLoaded += (sender, args) =>
                {
                    var executor = new JQueryExecutor(_browser.Window);

                    var jObj = new JObject();
                    var dateFormat = "yyyy-MM-dd HH:mm:ss";
                    dynamic final = jObj;
                    final.dtPeriodStart = packet.PeriodStart.ToString(dateFormat);
                    final.dtPeriodFinished = packet.PeriodFinish.AddDays(1).ToString(dateFormat);

                    dynamic resultData = new JObject();
                    resultData.version = "3";
                    resultData.results = JObject.FromObject(packet.Results);
                    resultData.statistics = JObject.FromObject(packet.Results.Statistics);
                    resultData.iTradeableDates = 1;
                    resultData.ranking = null;
                    final.oResultData = resultData;
                    
                    //Set packet 
                    var json = JsonConvert.SerializeObject(final);
                    executor.ExecuteJQuery("window.jnBacktest = JSON.parse('" + json + "');");
                    executor.ExecuteJQuery("$.holdReady(false)");
                };

                foreach (var pair in packet.Results.Statistics)
                {
                    _logging.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
                }
            }
        }

        /// <summary>
        /// Display a handled error
        /// </summary>
        private void MessagingOnHandledErrorEvent(HandledErrorPacket packet)
        {
            var hstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            _logging.Error(packet.Message + hstack);
            //LogTextBox.AppendText(, Color.DarkRed);
        }

        /// <summary>
        /// Display a runtime error
        /// </summary>
        private void MessagingOnRuntimeErrorEvent(RuntimeErrorPacket packet)
        {
            var rstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            _logging.Error(packet.Message + rstack);
            //LogTextBox.AppendText(packet.Message + rstack, Color.DarkRed);
        }

        /// <summary>
        /// Display a log packet
        /// </summary>
        private void MessagingOnLogEvent(LogPacket packet)
        {
            _logging.Trace(packet.Message);
            //LogTextBox.AppendText(packet.Message, Color.Black);
        }

        /// <summary>
        /// Display a debug packet
        /// </summary>
        /// <param name="packet"></param>
        private void MessagingOnDebugEvent(DebugPacket packet)
        {
            _logging.Trace(packet.Message);
            //LogTextBox.AppendText(packet.Message, Color.Black);
        }

        /// <summary>
        /// Closing the form exit the LEAN engine too.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeanWinForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Trace("LeanWinForm(): Form closed.");
            Environment.Exit(0);
        }
    }
}
