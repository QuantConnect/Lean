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
using System.Windows.Media;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Interaction logic for LogInDialog.xaml
    /// </summary>
    public partial class LogInDialog : DialogWindow
    {
        private AuthorizationManager _authorizationManager;
        private Credentials? _credentials;
        private string _dataFolder;

        private Brush _userIdNormalBrush;
        private Brush _accessTokenNormalBrush;

        public LogInDialog(AuthorizationManager authorizationManager, string solutionFolder)
        {
            InitializeComponent();
            _authorizationManager = authorizationManager;
            _dataFolder = PathUtils.GetDataFolder(solutionFolder);

            _userIdNormalBrush = userIdBox.BorderBrush;
            _accessTokenNormalBrush = userIdBox.BorderBrush;
        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            logInButton.IsEnabled = false;
            var userId = userIdBox.Text;
            var accessToken = accessTokenBox.Password;
            var credentials = new Credentials(userId, accessToken);

            if (_authorizationManager.LogIn(credentials, _dataFolder))
            {
                _credentials = new Credentials(userId, accessToken);
                this.Close();
            }
            else
            {
                userIdBox.BorderBrush = Brushes.Red;
                accessTokenBox.BorderBrush = Brushes.Red;
            }
            logInButton.IsEnabled = true;
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /*
        private bool TryLogIn(Credentials credentials)
        {
            _authorizationManager.LogIn(credentials, _dataFolder);
            return _authorizationManager.IsLoggedIn();
        }
        */

        public Credentials? GetCredentials()
        {
            return _credentials;
        }

        private void inputField_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            userIdBox.BorderBrush = _userIdNormalBrush;
            accessTokenBox.BorderBrush = _accessTokenNormalBrush;
        }
    }

}
