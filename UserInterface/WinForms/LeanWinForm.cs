using System;
using System.Drawing;
using System.Windows.Forms;
using Gecko;
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
        private LeanEngineSystemHandlers _systemHandlers;
        private LeanEngineAlgorithmHandlers _algorithmHandlers;

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
            splitPanel.Panel1.Controls.Add(_browser);

            var url = string.Format("https://beta.quantconnect.com/terminal/embedded?user={0}&token={1}&bid={2}&pid={3}&version={4}",
                        job.UserId, job.Channel, job.AlgorithmId, job.ProjectId, Globals.Version);
            _browser.Navigate(url);

            //Setup Event Handlers:
            _messaging.DebugEvent += MessagingOnDebugEvent;
            _messaging.LogEvent += MessagingOnLogEvent;
            _messaging.RuntimeErrorEvent += MessagingOnRuntimeErrorEvent;
            _messaging.HandledErrorEvent += MessagingOnHandledErrorEvent;
            _messaging.BacktestResultEvent += MessagingOnBacktestResultEvent;
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
        }

        /// <summary>
        /// Trigger Exit at the end of the backtest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxLog_KeyUp(object sender, KeyEventArgs e)
        {
            //Environment.Exit(0);
        }

        /// <summary>
        /// Backtest result packet
        /// </summary>
        /// <param name="packet"></param>
        private void MessagingOnBacktestResultEvent(BacktestResultPacket packet)
        {
            if (packet.Progress == 1)
            {
                foreach (var pair in packet.Results.Statistics)
                {
                    Log.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
                    LogTextBox.AppendText("STATISTICS:: " + pair.Key + " " + pair.Value, Color.CornflowerBlue);
                }
            }
        }

        /// <summary>
        /// Display a handled error
        /// </summary>
        private void MessagingOnHandledErrorEvent(HandledErrorPacket packet)
        {
            var hstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            LogTextBox.AppendText(packet.Message + hstack, Color.DarkRed);
        }

        /// <summary>
        /// Display a runtime error
        /// </summary>
        private void MessagingOnRuntimeErrorEvent(RuntimeErrorPacket packet)
        {
            var rstack = (!string.IsNullOrEmpty(packet.StackTrace) ? (Environment.NewLine + " " + packet.StackTrace) : string.Empty);
            LogTextBox.AppendText(packet.Message + rstack, Color.DarkRed);
        }

        /// <summary>
        /// Display a log packet
        /// </summary>
        private void MessagingOnLogEvent(LogPacket packet)
        {
            LogTextBox.AppendText(packet.Message, Color.Black);
        }

        /// <summary>
        /// Display a debug packet
        /// </summary>
        /// <param name="packet"></param>
        private void MessagingOnDebugEvent(DebugPacket packet)
        {
            LogTextBox.AppendText(packet.Message, Color.Black);
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
