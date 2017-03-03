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

using CredentialManagement;

namespace QuantConnect.VisualStudioPlugin
{
    class CredentialsManager
    {
        private const string CREDENTIAL_TARGET = "QuantConnectPlugin";

        public Credentials? GetLastCredential()
        {
            var cm = new Credential { Target = CREDENTIAL_TARGET };
            if (!cm.Load())
            {
                return null;
            }

            return new Credentials(cm.Username, cm.Password);
        }

        public void SetCredentials(Credentials credentials)
        {
            var credential = new Credential
            {
                Target = CREDENTIAL_TARGET,
                Username = credentials.UserId,
                Password = credentials.AccessToken,
                PersistanceType = PersistanceType.LocalComputer
            };
            
            credential.Save();
        }
    }
}
