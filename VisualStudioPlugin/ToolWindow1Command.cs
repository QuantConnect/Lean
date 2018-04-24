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
*/

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ToolWindow1Command : IBacktestObserver
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        private const int _commandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid _commandSet = new Guid("2ddf6e60-63e9-4a80-9985-a08b1af256cc");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        private static ToolWindow1Command _instance;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider _serviceProvider => _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToolWindow1Command(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            OleMenuCommandService commandService = _serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandId = new CommandID(_commandSet, _commandId);
                var menuItem = new MenuCommand(ShowToolWindow, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static ToolWindow1Command Initialize(Package package)
        {
            _instance = new ToolWindow1Command(package);
            return _instance;
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to false so that if the tool window does not exists it will not be created.
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            if (window == null)
            {
                window = _package.FindToolWindow(typeof(ToolWindow1), 0, true);
                if (window?.Frame == null)
                {
                    throw new NotSupportedException("Cannot create QuantConnect tool window");
                }
            }
            else
            {
                // If it already exists lets update
                var windowController = (ToolWindow1Control)window.Content;
                windowController.UpdateAvailableProjects();
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// Notifier for the ToolWindow to create a new backtest
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestCreated(int projectId, Api.Backtest backtestStatus)
        {
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            // Only notify if the toolWindow was open
            if (window != null)
            {
                var windowController = (ToolWindow1Control)window.Content;
                windowController.BacktestCreated(projectId, backtestStatus);
            }
        }

        /// <summary>
        /// Notifier for the ToolWindow to update backtest progress
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestStatusUpdated(int projectId, Api.Backtest backtestStatus)
        {
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            // Only notify if the toolWindow was open
            if (window != null)
            {
                var windowController = (ToolWindow1Control)window.Content;
                windowController.BacktestStatusUpdated(projectId, backtestStatus);
            }
        }

        /// <summary>
        /// Notifier for the ToolWindow to inform backtest has finished
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestStatus">Backtest current status</param>
        public void BacktestFinished(int projectId, Api.Backtest backtestStatus)
        {
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            // Only notify if the toolWindow was open
            if (window != null)
            {
                var windowController = (ToolWindow1Control)window.Content;
                windowController.BacktestFinished(projectId, backtestStatus);
            }
        }
    }
}
