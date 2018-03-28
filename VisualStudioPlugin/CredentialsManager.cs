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
    /// <summary>
    /// Class for storring and retrieving credentils from local Windows credentials store
    /// </summary>
    internal static class CredentialsManager
    {
        private const string _credentialTarget = "QuantConnectPlugin";

        /// <summary>
        /// Get latest storred QuantConnect credentials.
        /// </summary>
        /// <returns>Latest storred credentials if they exist, null otherwise</returns>
        public static Credentials? GetLastCredential()
        {
            var credential = new Credential { Target = _credentialTarget };
            if (!credential.Load())
            {
                return null;
            }

            return new Credentials(credential.Username, credential.Password);
        }

        /// <summary>
        /// Store latests credentials. Overwrites last storred credentials.
        /// </summary>
        /// <param name="credentials">Credentials to store</param>
        public static void StoreCredentials(Credentials credentials)
        {
            var credential = new Credential
            {
                Target = _credentialTarget,
                Username = credentials.UserId,
                Password = credentials.AccessToken,
                PersistanceType = PersistanceType.LocalComputer
            };
            
            credential.Save();
        }

        /// <summary>
        /// Remove last storred credentials. After this GetLastCredential will return null.
        /// </summary>
        public static void ForgetCredentials()
        {
            var credential = new Credential { Target = _credentialTarget };
            credential.Delete();
        }
    }
}
