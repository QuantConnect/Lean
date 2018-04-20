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

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    internal partial class LoginDialog : DialogWindow
    {
        private readonly AuthorizationManager _authorizationManager;
        private Credentials? _credentials;

        private readonly Brush _userIdNormalBrush;
        private readonly Brush _accessTokenNormalBrush;
        private const string _needHelpURL = "https://www.quantconnect.com/docs#Visual-Studio-Plugin";
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Create LoginDialog
        /// </summary>
        /// <param name="authorizationManager">Authorization manager</param>
        /// <param name="previousCredentials">User previous credentials</param>
        /// <param name="serviceProvider">Service provider</param>
        public LoginDialog(AuthorizationManager authorizationManager, Credentials? previousCredentials, IServiceProvider serviceProvider)
        {
            VSActivityLog.Info("Created login dialog");
            InitializeComponent();
            _authorizationManager = authorizationManager;
            _serviceProvider = serviceProvider;
            DisplayPreviousCredentials(previousCredentials);
            _userIdNormalBrush = userIdBox.BorderBrush;
            _accessTokenNormalBrush = userIdBox.BorderBrush;
        }

        private void DisplayPreviousCredentials(Credentials? previousCredentials)
        {
            if (previousCredentials.HasValue)
            {
                userIdBox.Text = previousCredentials.Value.UserId;
                accessTokenBox.Password = previousCredentials.Value.AccessToken;
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            VSActivityLog.Info("Login button clicked");
            try
            {
                // Disable textbox passwordBox and login button
                userIdBox.IsReadOnly = true;
                accessTokenBox.IsEnabled = false;
                logInButton.IsEnabled = false;

                var userId = userIdBox.Text;
                var accessToken = accessTokenBox.Password;
                var credentials = new Credentials(userId, accessToken);
                if (await _authorizationManager.Login(credentials))
                {
                    VSActivityLog.Info("Logged in successfully");
                    VsUtils.DisplayInStatusBar(_serviceProvider, "Logged into QuantConnect");
                    _credentials = new Credentials(userId, accessToken);
                    Close();
                    return;
                }
            }
            catch (Exception exception)
            {
                VsUtils.ShowErrorMessageBox(_serviceProvider, "QuantConnect Login Exception", exception.ToString());
                VSActivityLog.Error(exception.ToString());
            }
            VsUtils.DisplayInStatusBar(_serviceProvider, "Failed to login");
            userIdBox.BorderBrush = Brushes.Red;
            accessTokenBox.BorderBrush = Brushes.Red;
            // Re enable button and textbox
            userIdBox.IsReadOnly = false;
            accessTokenBox.IsEnabled = true;
            logInButton.IsEnabled = true;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!_credentials.HasValue)
            {
                VsUtils.DisplayInStatusBar(_serviceProvider, "Login cancelled");
            }
        }

        /// <summary>
        /// Get credentials that were used to log in into QuantConnect
        /// </summary>
        /// <returns>Credentials if a user was authenticated, null if correct credentials were not provided</returns>
        public Credentials? GetCredentials()
        {
            return _credentials;
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            userIdBox.BorderBrush = _userIdNormalBrush;
            accessTokenBox.BorderBrush = _accessTokenNormalBrush;
            if (e.Key == Key.Return)
            {
                Login_Click(sender, e);
            }
        }

        protected override void InvokeDialogHelp()
        {
            Process.Start(_needHelpURL);
        }
    }
}
