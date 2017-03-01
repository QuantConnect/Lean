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
using System.Windows;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Interaction logic for LogInDialog.xaml
    /// </summary>
    public partial class LogInDialog : DialogWindow
    {
        private AuthorizationManager authorizationManager;
        private Credentials? _credentials;

        public LogInDialog(AuthorizationManager authorizationManager)
        {
            InitializeComponent();
            this.authorizationManager = authorizationManager;
        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            logInButton.IsEnabled = false;
            string userId = userIdBox.Text;
            string accessToken = accessTokenBox.Password;

            if (authorizationManager.LogIn(userId, accessToken))
            {
                _credentials = new Credentials(userId, accessToken);
                this.Close();
            }
            else
            {
                userIdBox.BorderBrush = System.Windows.Media.Brushes.Red;
                accessTokenBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            logInButton.IsEnabled = true;
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool TryLogIn(string userId, string accessKey)
        {
            authorizationManager.LogIn(userId, accessKey);
            return authorizationManager.IsLoggedIn();
        }

        public Credentials? GetCredentials()
        {
            return _credentials;
        }
    }

}
