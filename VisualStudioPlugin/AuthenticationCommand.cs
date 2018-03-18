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

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// A command to perform QuantConnect authentication
    /// </summary>
    class AuthenticationCommand
    {
        /// <summary>
        /// Log instance used to log into VisualStudio ActivityLog
        /// </summary>
        private static readonly Log _log = new Log(typeof(AuthenticationCommand));

        private readonly CredentialsManager _credentialsManager = new CredentialsManager();

        /// <summary>
        /// Perform QuantConnect authentication
        /// </summary>
        /// <param name="serviceProvider">Visual Studio services provider</param>
        /// <param name="dataFolderPath">Data folder path to use</param>
        /// <param name="explicitLogin">User explicitly clicked Log In button</param>
        /// <returns>true if user logged into QuantConnect, false otherwise</returns>
        public bool Login(IServiceProvider serviceProvider, string dataFolderPath, bool explicitLogin)
        {
            _log.Info("Logging in");

            if (!PathUtils.DataFolderPathValid(dataFolderPath))
            {
                VsUtils.ShowMessageBox(serviceProvider, "Incorrect data folder", 
                    $"Incorrect data folder path: {dataFolderPath}\nGo to 'Tools' -> 'Options' -> 'QuantConnect' to set it");
                return false;
            }

            var authorizationManager = AuthorizationManager.GetInstance();
            if (authorizationManager.IsLoggedIn())
            {
                _log.Info("Already logged in");
                return true;
            }

            var previousCredentials = _credentialsManager.GetLastCredential();
            if (!explicitLogin && LoggedInWithLastStorredPassword(previousCredentials, dataFolderPath))
            {
                _log.Info("Logged in with previously storred credentials");
                return true;
            }

            return LoginWithDialog(serviceProvider, previousCredentials, dataFolderPath);
        }

        private bool LoginWithDialog(IServiceProvider serviceProvider, Credentials? previousCredentials, string dataFolderPath)
        {
            var logInDialog = new LoginDialog(AuthorizationManager.GetInstance(), previousCredentials, dataFolderPath);
            VsUtils.DisplayDialogWindow(logInDialog);

            var credentials = logInDialog.GetCredentials();

            if (credentials.HasValue)
            {
                _log.Info("Logged in successfully");
                _log.Info("Storring credentials");
                _credentialsManager.StoreCredentials(credentials.Value);
                VsUtils.DisplayInStatusBar(serviceProvider, "Logged into QuantConnect. Using data folder: " + dataFolderPath);
                return true;
            }
            else
            {
                _log.Info("Login cancelled");
                return false;
            }
        }

        private bool LoggedInWithLastStorredPassword(Credentials? previousCredentials, string dataFolderPath)
        {
            if (!previousCredentials.HasValue)
            {
                return false;
            }

            var credentials = previousCredentials.Value;
            return AuthorizationManager.GetInstance().Login(credentials, dataFolderPath);
        }

        /// <summary>
        /// Log out QuantConnect API
        /// </summary>
        /// <param name="serviceProvider">Visual Studio service provider</param>
        public void Logout(IServiceProvider serviceProvider)
        {
            AuthorizationManager.GetInstance().Logout();
            VsUtils.DisplayInStatusBar(serviceProvider, "Logged out of QuantConnect");
        }
    }
}