//------------------------------------------------------------------------------
// <copyright file="SendForBacktestingcs.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;
using System.Collections.Generic;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SendForBacktestingcs
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int SendForBacktestingCommandId = 0x0100;
        public const int SaveToQuantConnectCommandId = 0x0110;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("00ce2ccb-74c7-42f4-bf63-52c573fc1532");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;
        private DTE2 dte2;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendForBacktestingcs"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SendForBacktestingcs(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            
            this.package = package;
            this.dte2 = ServiceProvider.GetService(typeof(SDTE)) as DTE2;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                RegisterSendForBacktesting(commandService);
                RegisterSaveToQuantConnect(commandService);
            }
        }

        private void RegisterSendForBacktesting(OleMenuCommandService commandService)
        {
            var menuCommandID = new CommandID(CommandSet, SendForBacktestingCommandId);
            OleMenuCommand oleMenuItem = new OleMenuCommand(new EventHandler(SendForBacktestingCallback), menuCommandID);
            commandService.AddCommand(oleMenuItem);
        }

        private void RegisterSaveToQuantConnect(OleMenuCommandService commandService)
        {
            var menuCommandID = new CommandID(CommandSet, SaveToQuantConnectCommandId);
            OleMenuCommand oleMenuItem = new OleMenuCommand(new EventHandler(SaveToQuantConnectCallback), menuCommandID);
            commandService.AddCommand(oleMenuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SendForBacktestingcs Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new SendForBacktestingcs(package);
        }

        private void SendForBacktestingCallback(object sender, EventArgs e)
        {
            List<string> files = GetSelectedFiles(sender);
            string message = string.Format(CultureInfo.CurrentCulture, "Send for backtesting {0}", string.Join(" ", files));
            string title = "SendToBacktesting";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void SaveToQuantConnectCallback(object sender, EventArgs e)
        {
            List<string> files = GetSelectedFiles(sender);
            string message = string.Format(CultureInfo.CurrentCulture, "Save {0}", string.Join(" ", files));
            string title = "SaveToQuantConnect";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private List<string> GetSelectedFiles(object sender)
        {
            var myCommand = sender as OleMenuCommand;

            List<string> selectedFiles = new List<string>();
            object[] selectedItems = (object[]) dte2.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (EnvDTE.UIHierarchyItem selectedUIHierarchyItem in selectedItems)
            {
                if (selectedUIHierarchyItem.Object is EnvDTE.ProjectItem)
                {
                    EnvDTE.ProjectItem item = selectedUIHierarchyItem.Object as EnvDTE.ProjectItem;
                    selectedFiles.Add(item.Name);
                }
            }
            return selectedFiles;
        }
    }
}
