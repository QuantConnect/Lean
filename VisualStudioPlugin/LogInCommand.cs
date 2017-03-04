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
using System.IO;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// A command to perform QuantConnect authentication
    /// </summary>
    class LogInCommand
    {

        private CredentialsManager _credentialsManager = new CredentialsManager();

        private string _solutionFolder;
        private string _dataFolderPath;

        public LogInCommand(string solutionFolder)
        {
            _solutionFolder = solutionFolder;
            _dataFolderPath = PathUtils.GetDataFolder(solutionFolder);
        }

        /// <summary>
        /// Perform QuantConnect authentication
        /// </summary>
        /// <param name="serviceProvider">Visual Studio services provider</param>
        /// <returns>true if user logged into QuantConnect, false otherwise</returns>
        public bool DoLogIn(IServiceProvider serviceProvider)
        {
            
            var authorizationManager = AuthorizationManager.GetInstance();
            if (authorizationManager.IsLoggedIn())
            {
                return true;
            }

            if (LoggedInWithLastStorredPassword())
            {
                return true;
            }

            var logInDialog = new LogInDialog(authorizationManager, _solutionFolder);
            VsUtils.DisplayDialogWindow(logInDialog);

            var credentials = logInDialog.GetCredentials();

            if (credentials.HasValue)
            {
                _credentialsManager.SetCredentials(credentials.Value);
                VsUtils.DisplayInStatusBar(serviceProvider, "Logged into QuantConnect");
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool LoggedInWithLastStorredPassword()
        {
            var nullableCredentials =_credentialsManager.GetLastCredential(); 
            if (!nullableCredentials.HasValue)
            {
                return false;
            }

            var credentials = nullableCredentials.Value;
            return AuthorizationManager.GetInstance().LogIn(credentials, _dataFolderPath);
        }

        public void DoLogOut(IServiceProvider serviceProvider)
        {
            _credentialsManager.ForgetCredentials();
            AuthorizationManager.GetInstance().LogOut();
            VsUtils.DisplayInStatusBar(serviceProvider, "Logged out of QuantConnect");
        }
    }
}