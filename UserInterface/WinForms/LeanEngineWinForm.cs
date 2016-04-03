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
using System.Threading;
using System.Windows.Forms;
using Gecko;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Messaging;
using QuantConnect.Packets;
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
        public GeckoWebBrowser Browser;
        //Setup Configuration:
        public static string IconPath = "../../Icons/";

        //Form Elements
        private GroupBox LogGroupBox;
        private RichTextBox TextBoxLog;
        private MenuStrip MenuStrip;
        private ToolStripMenuItem FileMenuItem;
        private ToolStripMenuItem ExitMenuItem;
        private StatusStrip FormStatusStrip;
        private ToolStripStatusLabel FormToolStripStatusLabel;
        private ToolStripStatusLabel FormToolStripStatusStringLabel;
        private ToolStripStatusLabel StatisticsToolStripStatusLabel;
        private ToolStripProgressBar FormToolStripProgressBar;
        private ToolStripContainer toolStripContainer1;
        private LeanEngineWinFormPresenter _presenter;
        
        /// <summary>
        ///     Launch the Lean Engine Primary Form:
        /// </summary>
        public LeanEngineWinForm()
        {
            Config.Set("messaging-handler", "QuantConnect.Messaging.EventMessagingHandler");
            var systemHandler = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            var messageHandler = (EventMessagingHandler)systemHandler.Notify;
            var model = new LeanEngineWinFormModel();
            _presenter = new LeanEngineWinFormPresenter(this, model, messageHandler);

            InitializeComponent();

            //Create Form:
            Text = "QuantConnect Lean Algorithmic Trading Engine: v" + Globals.Version;
            Size = new Size(1024,768);
            MinimumSize = new Size(1024, 768);
            CenterToScreen();
            WindowState = FormWindowState.Maximized;
            Icon = new Icon("../../../lean.ico");

            //Setup Console Log Area:
            TextBoxLog.Parent = LogGroupBox;
            
            Log.LogHandler = new RichTextBoxLogHandler(TextBoxLog);
            TextBoxLog.KeyUp += ConsoleOnKeyUp;

            ResumeLayout(false);

            //Form Events:
            Closed += ExitApplication;

            //Trigger a timer event.
            _timer = new Timer { Interval = 1000 };
            _timer.Tick += TickerTick;

            Browser = new GeckoWebBrowser { Dock = DockStyle.Fill, Name = "browser" };
            toolStripContainer1.ContentPanel.Controls.Add(Browser);

            //Setup Container Events:
            Load += OnLoad;
        }

        /// <returns>
        /// The text associated with this control.
        /// </returns>
        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        public Engine Engine { get; private set; }
        public LeanEngineSystemHandlers EngineSystemHandlers { get; set; }

        //Form Controls:
        public IResultHandler ResultsHandler { get; set; }
        public event EventHandler TickerTick;
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
            FormToolStripStatusLabel.Text = @"LEAN Desktop v" + Globals.Version + @" Load Complete.";

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

            Console.WriteLine(@"Running " + algorithm + @"...");

            // Setup the configuration, since the UX is not in the 
            // lean directory we write a new config in the UX output directory.
            Config.Set("algorithm-type-name", algorithm);
            Config.Set("live-mode", "false");
            Config.Set("messaging-handler", "QuantConnect.Messaging.EventMessagingHandler");
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

        //Timer;
        private readonly Timer _timer;

        #endregion

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LeanEngineWinForm));
            this.LogGroupBox = new System.Windows.Forms.GroupBox();
            this.FormStatusStrip = new System.Windows.Forms.StatusStrip();
            this.FormToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FormToolStripStatusStringLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.StatisticsToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.FormToolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.TextBoxLog = new System.Windows.Forms.RichTextBox();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.FileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.LogGroupBox.SuspendLayout();
            this.FormStatusStrip.SuspendLayout();
            this.MenuStrip.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
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
            this.LogGroupBox.Location = new System.Drawing.Point(10, 503);
            this.LogGroupBox.Name = "LogGroupBox";
            this.LogGroupBox.Size = new System.Drawing.Size(989, 205);
            this.LogGroupBox.TabIndex = 0;
            this.LogGroupBox.TabStop = false;
            this.LogGroupBox.Text = "Log";
            // 
            // FormStatusStrip
            // 
            this.FormStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FormToolStripStatusLabel,
            this.FormToolStripStatusStringLabel,
            this.StatisticsToolStripStatusLabel,
            this.FormToolStripProgressBar});
            this.FormStatusStrip.Location = new System.Drawing.Point(3, 180);
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
            this.FormToolStripStatusStringLabel.Size = new System.Drawing.Size(625, 17);
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
            // TextBoxLog
            // 
            this.TextBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBoxLog.Location = new System.Drawing.Point(10, 20);
            this.TextBoxLog.Name = "TextBoxLog";
            this.TextBoxLog.ReadOnly = true;
            this.TextBoxLog.Size = new System.Drawing.Size(965, 166);
            this.TextBoxLog.TabIndex = 0;
            this.TextBoxLog.Text = "";
            // 
            // MenuStrip
            // 
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileMenuItem});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(1008, 24);
            this.MenuStrip.TabIndex = 1;
            this.MenuStrip.Text = "menuStrip1";
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
            this.ExitMenuItem.Size = new System.Drawing.Size(92, 22);
            this.ExitMenuItem.Text = "Exit";
            this.ExitMenuItem.Click += new System.EventHandler(this.Exit);
            // 
            // toolStripContainer1
            // 
            this.toolStripContainer1.BottomToolStripPanelVisible = false;
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(1008, 469);
            this.toolStripContainer1.LeftToolStripPanelVisible = false;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 28);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.RightToolStripPanelVisible = false;
            this.toolStripContainer1.Size = new System.Drawing.Size(1008, 469);
            this.toolStripContainer1.TabIndex = 2;
            this.toolStripContainer1.Text = "toolStripContainer1";
            this.toolStripContainer1.TopToolStripPanelVisible = false;
            // 
            // LeanEngineWinForm
            // 
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.toolStripContainer1);
            this.Controls.Add(this.LogGroupBox);
            this.Controls.Add(this.MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenuStrip;
            this.MaximumSize = new System.Drawing.Size(1024, 768);
            this.Name = "LeanEngineWinForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.LogGroupBox.ResumeLayout(false);
            this.LogGroupBox.PerformLayout();
            this.FormStatusStrip.ResumeLayout(false);
            this.FormStatusStrip.PerformLayout();
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
