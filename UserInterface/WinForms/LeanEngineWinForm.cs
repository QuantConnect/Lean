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
using System.Threading;
using System.Windows.Forms;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Util;
using QuantConnect.Views.Model;
using QuantConnect.Views.Presenter;
using QuantConnect.Views.View;
using Timer = System.Windows.Forms.Timer;

namespace QuantConnect.Views.WinForms
{
    /// <summary>
    ///     Primary Form for use with LEAN:
    /// </summary>
    public class LeanEngineWinForm : Form, ILeanEngineWinFormView
    {
        private static Thread _leanEngineThread;

        //Setup Configuration:
        public static string IconPath = "../../Icons/";

        //Form Elements
        private readonly Timer _polling;
        private GroupBox LogGroupBox;
        private RichTextBox TextBoxLog;
        private MenuStrip Menu;
        private ToolStripMenuItem FileMenuItem;
        private ToolStripMenuItem ExitMenuItem;
        private StatusStrip FormStatusStrip;
        private ToolStripStatusLabel FormToolStripStatusLabel;
        private ToolStripStatusLabel FormToolStripStatusStringLabel;
        private ToolStripStatusLabel StatisticsToolStripStatusLabel;
        private ToolStripProgressBar FormToolStripProgressBar;
        private LeanEngineWinFormPresenter _presenter;

        /// <summary>
        ///     Launch the Lean Engine Primary Form:
        /// </summary>
        public LeanEngineWinForm()
        {
            var model = new LeanEngineWinFormModel();
            _presenter = new LeanEngineWinFormPresenter(this, model);

            InitializeComponent();

            //Create Form:
            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Constants.Version;
            
            //Setup Console Log Area:
            TextBoxLog.Parent = LogGroupBox;

            Log.LogHandler = new RichTextBoxLogHandler(TextBoxLog);
            TextBoxLog.KeyUp += ConsoleOnKeyUp;

            ResumeLayout(false);

            //Form Events:
            Closed += ExitApplication;

            //Setup Polling Events:
            _polling = new Timer { Interval = 1000 };
            _polling.Tick += PollingTick;
            _polling.Start();

            //Trigger a timer event.
            _timer = new Timer { Interval = 1000 };
            _timer.Tick += TickerTick;


            //Setup Container Events:
            Load += OnLoad;
        }

        public Engine Engine { get; private set; }
        //Form Controls:
        public RichTextBox RichTextBoxLog { get; private set; }
        public IResultHandler ResultsHandler { get; set; }
        public event EventHandler TickerTick;
        public event EventHandler PollingTick;
        public event EventHandler ExitApplication;
        public event KeyEventHandler ConsoleOnKeyUp;

        public void OnPropertyChanged(LeanEngineWinFormModel model)
        {
            StatisticsToolStripStatusLabel.Text = model.StatusStripStatisticsText;
        }

        private void Exit(object sender, EventArgs eventArgs)
        {
            ExitApplication(sender, eventArgs);
        }

        /// <summary>
        ///     Initialization events on loading the container
        /// </summary>
        private void OnLoad(object sender, EventArgs eventArgs)
        {
            //Start Stats Counter:
            _timer.Start();

            //Complete load
            FormToolStripStatusLabel.Text = "LEAN Desktop v" + Constants.Version + " Load Complete.";

            //Load the Lean Engine
            Engine = LaunchLean();
        }

        /// <summary>
        ///     Launch the Desktop Interface
        /// </summary>
        /// <remarks>
        ///     This is a preliminary implementation of a UX for the Lean Engine. It is not considered complete or
        ///     production ready but is committed so the open source community can begin experimenting with custom UIs!
        /// </remarks>
        public static void Main()
        {
            const string algorithm = "BasicTemplateAlgorithm";

            Console.WriteLine("Running " + algorithm + "...");

            // Setup the configuration, since the UX is not in the 
            // lean directory we write a new config in the UX output directory.
            Config.Set("algorithm-type-name", algorithm);
            Config.Set("live-mode", "false");
            Config.Set("messaging-handler", "QuantConnect.Messaging.Messaging");
            Config.Set("job-queue-handler", "QuantConnect.Queues.JobQueue");
            Config.Set("api-handler", "QuantConnect.Api.Api");
            Config.Set("result-handler", "QuantConnect.Lean.Engine.Results.DesktopResultHandler");
            Config.Set("environment", "desktop");

            //Start default backtest.

            //Start GUI
            Application.Run(new LeanEngineWinForm());

        }

        /// <summary>
        ///     Launch the LEAN Engine in a separate thread.
        /// </summary>
        public static Engine LaunchLean()
        {
            //Launch the Lean Engine in another thread: this will run the algorithm specified above.

            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            var algorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            var engine = new Engine(systemHandlers, algorithmHandlers, Config.GetBool("live-mode"));
            _leanEngineThread = new Thread(() =>
            {
                string algorithmPath;
                var job = systemHandlers.JobQueue.NextJob(out algorithmPath);
                engine.Run(job, algorithmPath);
                systemHandlers.JobQueue.AcknowledgeJob(job);
            });
            _leanEngineThread.Start();

            return engine;
        }

        #region FormElementDeclarations

        //Menu Form elements:
        private readonly GroupBox _logGroupBox;
        private readonly MenuStrip _menu;
        private readonly ToolStripMenuItem _menuFile;
        private readonly ToolStripMenuItem _menuFileExit;

        //Status Stripe Elements:
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _statusStripLabel;
        private readonly ToolStripStatusLabel _statusStripStatistics;
        private readonly ToolStripProgressBar _statusStripProgress;
        private readonly ToolStripStatusLabel _statusStripSpring;

        //Timer;
        private readonly Timer _timer;

        #endregion

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LeanEngineWinForm));
            this.LogGroupBox = new System.Windows.Forms.GroupBox();
            this.TextBoxLog = new System.Windows.Forms.RichTextBox();
            this.Menu = new System.Windows.Forms.MenuStrip();
            this.FileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FormStatusStrip = new System.Windows.Forms.StatusStrip();
            this.FormToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FormToolStripStatusStringLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatisticsToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FormToolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.LogGroupBox.SuspendLayout();
            this.Menu.SuspendLayout();
            this.FormStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // LogGroupBox
            // 
            this.LogGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LogGroupBox.AutoSize = true;
            this.LogGroupBox.Controls.Add(this.FormStatusStrip);
            this.LogGroupBox.Controls.Add(this.TextBoxLog);
            this.LogGroupBox.Location = new System.Drawing.Point(10, 40);
            this.LogGroupBox.Name = "LogGroupBox";
            this.LogGroupBox.Size = new System.Drawing.Size(989, 668);
            this.LogGroupBox.TabIndex = 0;
            this.LogGroupBox.TabStop = false;
            this.LogGroupBox.Text = "Log";
            // 
            // TextBoxLog
            // 
            this.TextBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBoxLog.Location = new System.Drawing.Point(10, 20);
            this.TextBoxLog.Name = "TextBoxLog";
            this.TextBoxLog.ReadOnly = true;
            this.TextBoxLog.Size = new System.Drawing.Size(965, 629);
            this.TextBoxLog.TabIndex = 0;
            this.TextBoxLog.Text = "";
            // 
            // Menu
            // 
            this.Menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileMenuItem});
            this.Menu.Location = new System.Drawing.Point(0, 0);
            this.Menu.Name = "Menu";
            this.Menu.Size = new System.Drawing.Size(1008, 24);
            this.Menu.TabIndex = 1;
            this.Menu.Text = "menuStrip1";
            // 
            // FileMenuItem
            // 
            this.FileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
            this.FileMenuItem.Name = "FileMenuItem";
            this.FileMenuItem.Size = new System.Drawing.Size(37, 20);
            this.FileMenuItem.Text = "File";
            // 
            // ExitMenuItem
            // 
            this.ExitMenuItem.Image = global::QuantConnect.Views.Properties.Resources.application_exit_16;
            this.ExitMenuItem.Name = "ExitMenuItem";
            this.ExitMenuItem.Size = new System.Drawing.Size(152, 22);
            this.ExitMenuItem.Text = "Exit";
            this.ExitMenuItem.Click += new System.EventHandler(this.Exit);
            // 
            // FormStatusStrip
            // 
            this.FormStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FormToolStripStatusLabel,
            this.FormToolStripStatusStringLabel,
            this.StatisticsToolStripStatusLabel,
            this.FormToolStripProgressBar});
            this.FormStatusStrip.Location = new System.Drawing.Point(3, 643);
            this.FormStatusStrip.Name = "FormStatusStrip";
            this.FormStatusStrip.Size = new System.Drawing.Size(983, 22);
            this.FormStatusStrip.TabIndex = 1;
            // 
            // FormToolStripStatusLabel
            // 
            this.FormToolStripStatusLabel.Name = "FormToolStripStatusLabel";
            this.FormToolStripStatusLabel.Size = new System.Drawing.Size(105, 17);
            this.FormToolStripStatusLabel.Text = "Loading Complete";
            // 
            // FormToolStripStatusStringLabel
            // 
            this.FormToolStripStatusStringLabel.Name = "FormToolStripStatusStringLabel";
            this.FormToolStripStatusStringLabel.Size = new System.Drawing.Size(594, 17);
            this.FormToolStripStatusStringLabel.Spring = true;
            // 
            // StatisticsToolStripStatusLabel
            // 
            this.StatisticsToolStripStatusLabel.Name = "StatisticsToolStripStatusLabel";
            this.StatisticsToolStripStatusLabel.Size = new System.Drawing.Size(136, 17);
            this.StatisticsToolStripStatusLabel.Text = "Statistics: CPU:    Ram:    ";
            // 
            // FormToolStripProgressBar
            // 
            this.FormToolStripProgressBar.Name = "FormToolStripProgressBar";
            this.FormToolStripProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // LeanEngineWinForm
            // 
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.LogGroupBox);
            this.Controls.Add(this.Menu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.Menu;
            this.MaximumSize = new System.Drawing.Size(1024, 768);
            this.Name = "LeanEngineWinForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.LogGroupBox.ResumeLayout(false);
            this.LogGroupBox.PerformLayout();
            this.Menu.ResumeLayout(false);
            this.Menu.PerformLayout();
            this.FormStatusStrip.ResumeLayout(false);
            this.FormStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}