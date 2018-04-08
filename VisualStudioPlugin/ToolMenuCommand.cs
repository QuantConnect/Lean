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

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Command handler for QuantConnect menus in the "Tools" section.
    /// </summary>
    internal sealed class ToolMenuCommand
    {
        /// <summary>
        /// Command IDs for 'Login' in the "Tools" menu
        /// </summary>
        private const int _loginCommandId = 256;

        /// <summary>
        /// Command IDs for 'Logout' in the "Tools" menu
        /// </summary>
        private const int _logoutCommandId = 512;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        private static readonly Guid _commandSet = new Guid("fefaf282-a478-45f4-b89a-a8f15dd8aaff");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly QuantConnectPackage _package;

        /// <summary>
        /// A command to perform QuantConnect authentication.
        /// </summary>
        private readonly AuthenticationCommand _authenticationCommand;

        /// <summary>
        /// Instance of the command.
        /// </summary>
        private static ToolMenuCommand _instance;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider _serviceProvider => _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToolMenuCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package as QuantConnectPackage;
            _authenticationCommand = new AuthenticationCommand();

            var commandService = _serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                RegisterLoginCommand(commandService);
                RegisterLogoutCommand(commandService);
            }
        }

        private void RegisterLoginCommand(OleMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(_commandSet, _loginCommandId);
            var loginMenuItem = new OleMenuCommand(LoginCallback, menuCommandId);
            loginMenuItem.BeforeQueryStatus += (sender, evt) =>
            {
                loginMenuItem.Enabled = !AuthorizationManager.GetInstance().IsLoggedIn();
            };

            commandService.AddCommand(loginMenuItem);
        }

        private void RegisterLogoutCommand(OleMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(_commandSet, _logoutCommandId);
            var logoutMenuItem = new OleMenuCommand(LogoutCallback, menuCommandId);
            logoutMenuItem.BeforeQueryStatus += (sender, evt) =>
            {
                logoutMenuItem.Enabled = AuthorizationManager.GetInstance().IsLoggedIn();
            };
            commandService.AddCommand(logoutMenuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            _instance = new ToolMenuCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item Login is clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void LoginCallback(object sender, EventArgs e)
        {
            await _authenticationCommand.Login(_serviceProvider, true);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item Logout is clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void LogoutCallback(object sender, EventArgs e)
        {
            _authenticationCommand.Logout(_serviceProvider);
        }
    }
}
