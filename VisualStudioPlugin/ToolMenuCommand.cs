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
using EnvDTE80;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Command handler for QuantConnect menus in the "Tools" section.
    /// </summary>
    internal sealed class ToolMenuCommand
    {
        /// <summary>
        /// Command IDs for buttons in the "Tools" section
        /// </summary>
        public const int LogInCommandId = 256;

        public const int LogOutCommandId = 512;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("fefaf282-a478-45f4-b89a-a8f15dd8aaff");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly QuantConnectPackage _package;

        private LogInCommand _logInCommand;
        private DTE2 _dte2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToolMenuCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package as QuantConnectPackage;
            _dte2 = ServiceProvider.GetService(typeof(SDTE)) as DTE2;
            _logInCommand = new LogInCommand();

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                RegisterLogInCommand(commandService);
                RegisterLogOutCommand(commandService);
            }
        }

        private void RegisterLogInCommand(OleMenuCommandService commandService)
        {
            var menuCommandID = new CommandID(CommandSet, LogInCommandId);
            var logInMenuItem = new OleMenuCommand(this.LogInCallback, menuCommandID);
            logInMenuItem.BeforeQueryStatus += (sender, evt) =>
            {
                logInMenuItem.Enabled = !AuthorizationManager.GetInstance().IsLoggedIn();
            };

            commandService.AddCommand(logInMenuItem);
        }

        private void RegisterLogOutCommand(OleMenuCommandService commandService)
        {
            var menuCommandID = new CommandID(CommandSet, LogOutCommandId);
            var logOutMenuItem = new OleMenuCommand(this.LogOutCallback, menuCommandID);
            logOutMenuItem.BeforeQueryStatus += (sender, evt) =>
            {
                logOutMenuItem.Enabled = AuthorizationManager.GetInstance().IsLoggedIn();
            };
            commandService.AddCommand(logOutMenuItem);
        }


        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ToolMenuCommand Instance
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
                return _package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ToolMenuCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void LogInCallback(object sender, EventArgs e)
        {
            _logInCommand.DoLogIn(this.ServiceProvider, _package.DataPath, explicitLogin: true);
        }

        private void LogOutCallback(object sender, EventArgs e)
        {
            _logInCommand.DoLogOut(this.ServiceProvider);
        }

    }
}
