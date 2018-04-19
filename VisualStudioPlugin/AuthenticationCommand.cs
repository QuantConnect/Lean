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
using System.Threading.Tasks;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// A command to perform QuantConnect authentication
    /// </summary>
    internal class AuthenticationCommand
    {
        /// <summary>
        /// Perform QuantConnect authentication
        /// </summary>
        /// <param name="serviceProvider">Visual Studio services provider</param>
        /// <param name="explicitLogin">User explicitly clicked Log In button</param>
        /// <param name="showLoginDialog">If true LoginDialog will be shown, careful it blocks UI</param>
        /// <returns>true if user logged into QuantConnect, false otherwise</returns>
        public async Task<bool> Login(IServiceProvider serviceProvider, bool explicitLogin, bool showLoginDialog = true)
        {
            VSActivityLog.Info("Logging in");

            var authorizationManager = AuthorizationManager.GetInstance();
            if (authorizationManager.IsLoggedIn())
            {
                VSActivityLog.Info("Already logged in");
                return true;
            }

            var previousCredentials = CredentialsManager.GetLastCredential();
            if (!explicitLogin && await LoggedInWithLastStorredPassword(previousCredentials))
            {
                VSActivityLog.Info("Logged in with previously storred credentials");
                return true;
            }

            if (showLoginDialog)
            {
                return LoginWithDialog(serviceProvider, previousCredentials);
            }
            VsUtils.DisplayInStatusBar(serviceProvider, "Please login to QuantConnect");
            return false;
        }

        private bool LoginWithDialog(IServiceProvider serviceProvider, Credentials? previousCredentials)
        {
            var logInDialog = new LoginDialog(AuthorizationManager.GetInstance(), previousCredentials, serviceProvider);
            VsUtils.DisplayDialogWindow(logInDialog);

            var credentials = logInDialog.GetCredentials();

            if (credentials.HasValue)
            {
                VSActivityLog.Info("Storing credentials");
                CredentialsManager.StoreCredentials(credentials.Value);
                return true;
            }
            else
            {
                VSActivityLog.Info("Login cancelled");
                return false;
            }
        }

        private async Task<bool> LoggedInWithLastStorredPassword(Credentials? previousCredentials)
        {
            if (!previousCredentials.HasValue)
            {
                return false;
            }

            var credentials = previousCredentials.Value;
            return await AuthorizationManager.GetInstance().Login(credentials);
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