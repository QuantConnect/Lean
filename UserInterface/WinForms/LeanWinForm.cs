using System;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Messaging;
using QuantConnect.Packets;
using Gecko;
using Gecko.JQuery;

namespace QuantConnect.Views.WinForms
{
    public partial class LeanWinForm : Form, IDesktopMessageHandler
    {
        private readonly GeckoWebBrowser _geckoBrowser;
        private readonly WebBrowser _monoBrowser;
        private readonly QueueLogHandler _logging;

        private bool _liveMode = false;
        private AlgorithmNodePacket _job;

        /// <summary>
        /// Create the UX.
        /// </summary>
        public LeanWinForm()
        {
            InitializeComponent();

            //Form Setup:
            CenterToScreen();
            WindowState = FormWindowState.Maximized;
            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Globals.Version;

            //GECKO WEB BROWSER: Create the browser control
            // https://www.nuget.org/packages/GeckoFX/
            // -> If you don't have IE.
#if !__MonoCS__
            Gecko.Xpcom.Initialize();

            _geckoBrowser = new GeckoWebBrowser { Dock = DockStyle.Fill, Name = "browser" };
            splitPanel.Panel1.Controls.Add(_geckoBrowser);
#else
            // MONO WEB BROWSER: Create the browser control
            // Default shipped with VS and Mono. Works OK in Windows, and compiles in linux.
            _monoBrowser = new WebBrowser() {Dock = DockStyle.Fill, Name = "Browser"};
            splitPanel.Panel1.Controls.Add(_monoBrowser);
#endif

            _logging = new QueueLogHandler();
        }

        /// <summary>
        /// This method is called when a new job is received.
        /// </summary>
        /// <param name="job">The job that is being executed</param>
        public void Initialize(AlgorithmNodePacket job)
        {
            _job = job;

            //Show warnings if the API token and UID aren't set.
            if (_job.UserId == 0)
            {
                MessageBox.Show("Your user id is not set. Please check your config.json file 'job-user-id' property.", "LEAN Algorithmic Trading", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (_job.Channel == "")
            {
                MessageBox.Show("Your API token is not set. Please check your config.json file 'api-access-token' property.", "LEAN Algorithmic Trading", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _liveMode = job is LiveNodePacket;
            var url = GetUrl(job, _liveMode);

#if !__MonoCS__

            _geckoBrowser.Navigate(url);
#else
            _monoBrowser.Navigate(url);
#endif

        }

        /// <summary>
        /// Displays the Backtest results packet
        /// </summary>
        /// <param name="packet">Backtest results</param>
        public void DisplayBacktestResultsPacket(BacktestResultPacket packet)
        {
            if (packet.Progress != 1) return;

            //Remove previous event handler:
            var url = GetUrl(_job, _liveMode, true);

            //Generate JSON:
            var jObj = new JObject();
            var dateFormat = "yyyy-MM-dd HH:mm:ss";
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

            //GECKO RESULT SET:
#if !__MonoCS__
            _geckoBrowser.DOMContentLoaded += (sender, args) =>
            {
                var executor = new JQueryExecutor(_geckoBrowser.Window);
                executor.ExecuteJQuery("window.jnBacktest = JSON.parse('" + json + "');");
                executor.ExecuteJQuery("$.holdReady(false)");
            };
            _geckoBrowser.Navigate(url);
#else
            //MONO WEB BROWSER RESULT SET:
            _monoBrowser.DocumentCompleted += (sender, args) =>
            {
                if (_monoBrowser.Document == null) return;
                _monoBrowser.Document.InvokeScript("eval", new object[] { "window.jnBacktest = JSON.parse('" + json + "');" });
                _monoBrowser.Document.InvokeScript("eval", new object[] { "$.holdReady(false)" });
            };
            _monoBrowser.Navigate(url);
#endif

            foreach (var pair in packet.Results.Statistics)
            {
                _logging.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
            }
        }

        /// <summary>
        /// Display a handled error
        /// </summary>
        public void DisplayHandledErrorPacket(HandledErrorPacket packet)
        {
            var hstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            _logging.Error(packet.Message + hstack);
        }

        /// <summary>
        /// Display a runtime error
        /// </summary>
        public void DisplayRuntimeErrorPacket(RuntimeErrorPacket packet)
        {
            var rstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            _logging.Error(packet.Message + rstack);
        }

        /// <summary>
        /// Display a log packet
        /// </summary>
        public void DisplayLogPacket(LogPacket packet)
        {
            _logging.Trace(packet.Message);
        }

        /// <summary>
        /// Display a debug packet
        /// </summary>
        /// <param name="packet"></param>
        public void DisplayDebugPacket(DebugPacket packet)
        {
            _logging.Trace(packet.Message);
        }

        /// <summary>
        /// Get the URL for the embedded charting
        /// </summary>
        /// <param name="job">Job packet for the URL</param>
        /// <param name="liveMode">Is this a live mode chart?</param>
        /// <param name="holdReady">Hold the ready signal to inject data</param>
        private static string GetUrl(AlgorithmNodePacket job, bool liveMode = false, bool holdReady = false)
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
        /// Update the status label at the bottom of the form
        /// </summary>
        private void timer_Tick(object sender, EventArgs e)
        {
            StatisticsToolStripStatusLabel.Text = string.Concat("Performance: CPU: ", OS.CpuUsage.ToString("0.0"), "%",
                                                                " Ram: ", OS.TotalPhysicalMemoryUsed, " Mb");

            if (_logging == null) return;

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

        /// <summary>
        /// Closing the form exit the LEAN engine too.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeanWinForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Trace("LeanWinForm(): Form closed.");
#if !__MonoCS__
            _geckoBrowser.Dispose();
#endif
            Environment.Exit(0);
        }
    }
}