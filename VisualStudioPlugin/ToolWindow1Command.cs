//------------------------------------------------------------------------------
// <copyright file="ToolWindow1Command.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
            // The last flag is set to true so that if the tool window does not exists it will be created.
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
        /// <param name="backtestId">Target backtest id</param>
        /// <param name="backtestName">Backtest name</param>
        /// <param name="creationDateTime">Date and time of creation</param>
        public void NewBacktest(int projectId, string backtestId, string backtestName, DateTime creationDateTime)
        {
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            // Only notify if the toolWindow was open
            if (window != null)
            {
                var windowController = (ToolWindow1Control)window.Content;
                windowController.NewBacktest(projectId, backtestId, backtestName, creationDateTime);
            }
        }

        /// <summary>
        /// Notifier for the ToolWindow to update backtest progress
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestId">Target backtest id</param>
        /// <param name="progress">Current backtest progress</param>
        public void UpdateBacktest(int projectId, string backtestId, decimal progress)
        {
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            // Only notify if the toolWindow was open
            if (window != null)
            {
                var windowController = (ToolWindow1Control)window.Content;
                windowController.UpdateBacktest(projectId, backtestId, progress);
            }
        }

        /// <summary>
        /// Notifier for the ToolWindow to inform backtest has finished
        /// </summary>
        /// <param name="projectId">Target project id</param>
        /// <param name="backtestId">Target backtest id</param>
        /// <param name="succeeded">Result of the backtest</param>
        public void BacktestFinished(int projectId, string backtestId, bool succeeded)
        {
            var window = _package.FindToolWindow(typeof(ToolWindow1), 0, false);
            // Only notify if the toolWindow was open
            if (window != null)
            {
                var windowController = (ToolWindow1Control)window.Content;
                windowController.BacktestFinished(projectId, backtestId, succeeded);
            }
        }
    }
}
