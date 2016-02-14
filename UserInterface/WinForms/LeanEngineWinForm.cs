/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Util;
using Timer = System.Windows.Forms.Timer;

namespace QuantConnect.Views.WinForms
{
    /// <summary>
    /// Primary Form for use with LEAN:
    /// </summary>
    public partial class LeanEngineWinForm : Form
    {
        private readonly Engine _engine;
        //Form Controls:
        private RichTextBox _console;
        #region FormElementDeclarations

        //Menu Form elements:
        private GroupBox _logGroupBox;
        private MenuStrip _menu;
        private ToolStripMenuItem _menuFile;
        private ToolStripMenuItem _menuFileOpen;
        private ToolStripSeparator _menuFileSeparator;
        private ToolStripMenuItem _menuFileNewBacktest;
        private ToolStripMenuItem _menuFileExit;
        private ToolStripMenuItem _menuView;
        private ToolStripMenuItem _menuViewToolBar;
        private ToolStripMenuItem _menuViewStatusBar;
        private ToolStripMenuItem _menuData;
        private ToolStripMenuItem _menuDataOpenFolder;
        private ToolStripMenuItem _menuDataDownloadData;
        private ToolStripMenuItem _menuTools;
        private ToolStripMenuItem _menuToolsSettings;
        private ToolStripMenuItem _menuWindows;
        private ToolStripMenuItem _menuWindowsCascade;
        private ToolStripMenuItem _menuWindowsTileVertical;
        private ToolStripMenuItem _menuWindowsTileHorizontal;
        private ToolStripMenuItem _menuHelp;
        private ToolStripMenuItem _menuHelpAbout;
        //Toolstrip form elements:
        private ToolStrip _toolStrip;
        private ToolStripButton _toolStripOpen;
        private ToolStripSeparator _toolStripSeparator;
        private ToolStripButton _toolStripNewBacktest;
        //Status Stripe Elements:
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusStripLabel;
        private ToolStripStatusLabel _statusStripStatistics;
        private ToolStripProgressBar _statusStripProgress;
        
        //Timer;
        private Timer _timer;

        #endregion

        //Form Business Logic:
        private Timer _polling;
        private IResultHandler _resultsHandler;
        private bool _isComplete = false;
        private static Thread _leanEngineThread;
        private GroupBox groupBox1;

        //Setup Configuration:
        public static string IconPath = "../../Icons/";

        /// <summary>
        /// Launch the Lean Engine Primary Form:
        /// </summary>
        /// <param name="engine">Accept the engine instance we just launched</param>
        public LeanEngineWinForm(Engine engine)
        {
            _engine = engine;
            //Setup the State:
            _resultsHandler = engine.AlgorithmHandlers.Results;

            //Create Form:
            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Constants.Version;
            Size = new Size(1024,768);
            MinimumSize = new Size(1024, 768);
            CenterToScreen();
            WindowState = FormWindowState.Maximized;
            Icon = new Icon("../../../lean.ico");
            var openIcon = Image.FromFile(Path.Combine(IconPath, "folder-open-16.png"));

            //Setup Console Log Area:
            _console = new RichTextBox();
            _console.Parent = this;
            _console.ReadOnly = true;
            _console.Multiline = true;
            _console.Location = new Point(0, 384);
            _console.Size = new Size(1024,322);
            _console.AutoSize = true;
            _console.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom);
            _console.Parent = this;
            _console.KeyUp += ConsoleOnKeyUp;
            _toolStrip = new ToolStrip();
            _toolStripOpen = new ToolStripButton("Open Algorithm", openIcon);
            _toolStripSeparator = new ToolStripSeparator();

            var newBacktestIcon = Image.FromFile(Path.Combine(IconPath, "office-chart-area-16.png"));

            _toolStripNewBacktest = new ToolStripButton("Launch Backtest", newBacktestIcon) { Enabled = false };
            _toolStrip.Items.AddRange(new ToolStripItem[] { _toolStripOpen, _toolStripSeparator, _toolStripNewBacktest });
            
            //Add the menu items to the tool strip
            _menu = new MenuStrip();
            _menuFile = new ToolStripMenuItem("&File");
            _menu.Items.AddRange(new ToolStripItem[] { _menuFile });
            Controls.Add(_menu);
            MainMenuStrip = _menu;

            //Create and add the status strip:
            _statusStrip = new StatusStrip();
            _statusStripLabel = new ToolStripStatusLabel("Loading Complete");
            _statusStripProgress = new ToolStripProgressBar();
            _statusStripStatistics = new ToolStripStatusLabel("Statistics: CPU:    Ram:    ");
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusStripLabel, _statusStripStatistics, _statusStripProgress });
            Controls.Add(_statusStrip);
            
            Name = "LeanEngineWinForm";
            ResumeLayout(false);

            //Form Events:
            Closed += OnClosed;

            //Setup Polling Events:
            _polling = new Timer { Interval = 1000 };
            _polling.Tick += PollingOnTick;
            _polling.Start();

            //Trigger a timer event.
            _timer = new Timer { Interval = 1000 };
            _timer.Tick += TimerOnTick;


            //Setup Container Events:
            Load += OnLoad;
        }
        
        /// <summary>
        /// Initialization events on loading the container
        /// </summary>
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            //Start Stats Counter:
            _timer.Start();

            //Complete load
            _statusStripLabel.Text = "LEAN Desktop v" + Constants.Version + " Load Complete.";
        }


        /// <summary>
        /// Launch the Desktop Interface
        /// </summary>
        /// <remarks>
        ///     This is a preliminary implementation of a UX for the Lean Engine. It is not considered complete or 
        ///     production ready but is committed so the open source community can begin experimenting with custom UIs!
        /// </remarks>
        static public void Main()
        {
            string algorithm = "BasicTemplateAlgorithm";

            Console.WriteLine("Running " + algorithm + "...");

            // Setup the configuration, since the UX is not in the 
            // lean directory we write a new config in the UX output directory.
            // TODO > Most of this should be configured through a helper form in the UX.
            Config.Set("algorithm-type-name", algorithm);
            Config.Set("live-mode", "false");
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");
            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.DesktopResultHandler");
            Config.Set("environment", "desktop");

            //Start default backtest.
            var engine = LaunchLean();

            //Start GUI
            // steal the desktop result handler from the composer's instance
            Application.Run(new LeanEngineWinForm(engine));
        }

        /// <summary>
        /// Launch the LEAN Engine in a separate thread.
        /// </summary>
        private static Engine LaunchLean()
        {
            //Launch the Lean Engine in another thread: this will run the algorithm specified above.
            // TODO > This should only be launched when clicking a backtest/trade live button provided in the UX.

            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            var engine = new Engine(systemHandlers, algorithmHandlers, Config.GetBool("live-mode"));
            //_leanEngineThread = new Thread(() =>
            //{
            //    string algorithmPath;
            //    //var job = systemHandlers.JobQueue.NextJob(out algorithmPath);
            //    //engine.Run(job, algorithmPath);
            //    //systemHandlers.JobQueue.AcknowledgeJob(job);
            //});
            ////_leanEngineThread.Start();

            return engine;
        }


        /// <summary>
        /// Update performance counters
        /// </summary>
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            _statusStripStatistics.Text = "Performance: CPU: " + OS.CpuUsage.CounterName + " Ram: " + OS.TotalPhysicalMemoryUsed + " Mb";
        }

        /// <summary>
        /// Primary polling thread for the logging and chart display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void PollingOnTick(object sender, EventArgs eventArgs)
        {
            Packet message;
            if (_resultsHandler == null) return;
            while (_resultsHandler.Messages.TryDequeue(out message))
            {
                //Process the packet request:
                switch (message.Type)
                {
                    case PacketType.BacktestResult:
                        //Draw chart
                        break;

                    case PacketType.LiveResult:
                        //Draw streaming chart
                        break;

                    case PacketType.AlgorithmStatus:
                        //Algorithm status update
                        break;

                    case PacketType.RuntimeError:
                        var runError = message as RuntimeErrorPacket;
                        if (runError != null) AppendConsole(runError.Message, Color.Red);
                        break;

                    case PacketType.HandledError:
                        var handledError = message as HandledErrorPacket;
                        if (handledError != null) AppendConsole(handledError.Message, Color.Red);
                        break;

                    case PacketType.Log:
                        var log = message as LogPacket;
                        if (log != null) AppendConsole(log.Message);
                        break;

                    case PacketType.Debug:
                        var debug = message as DebugPacket;
                        if (debug != null) AppendConsole(debug.Message);
                        break;

                    case PacketType.OrderEvent:
                        //New order event.
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }


        /// <summary>
        /// Write to the console in specific font color.
        /// </summary>
        /// <param name="message">String to append</param>
        /// <param name="color">Defaults to black</param>
        private void AppendConsole(string message, Color color = default(Color))
        {
            message = DateTime.Now.ToString("u") + " " + message + Environment.NewLine;
            //Add to console:
            _console.AppendText(message, color);
            _console.Refresh();
        }

        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(52, 133);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 100);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Log";
            // 
            // LeanEngineWinForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.groupBox1);
            this.Name = "LeanEngineWinForm";
            this.ResumeLayout(false);

        }

    }
}
