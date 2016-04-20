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
 *
*/

namespace QuantConnect.ToolBox.IQFeed
{
    public class IQCredentials
    {
        public IQCredentials(string loginId = "", string password = "", bool autoConnect = false, bool saveCredentials = true)
        {
            _loginId = loginId;
            _password = password;
            _autoConnect = autoConnect;
            _saveCredentials = saveCredentials;
        }
        public string LoginId { get { return _loginId; } set { _loginId = value; } }
        public string Password { get { return _password; } set { _password = value; } }
        public bool AutoConnect { get { return _autoConnect; } set { _autoConnect = value; } }
        public bool SaveCredentials { get { return _saveCredentials; } set { _saveCredentials = value; } }

        #region private
        private string _loginId;
        private string _password;
        private bool _autoConnect;
        private bool _saveCredentials;
        #endregion
    }

}
