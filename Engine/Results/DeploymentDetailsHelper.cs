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
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Helper to share deployment details with the user and the algorithm, see <see cref="IResultHandler.AddDeploymentDetail"/>
    /// </summary>
    public static class DeploymentDetailsHelper
    {
        private static int _missingResultHandlerLogged;

        /// <summary>
        /// Adds or updates a deployment detail entry on the result handler loaded in the <see cref="Composer"/>, if any.
        /// Key value pairs the brokerage, data queue handler or any other component wants to share with the user,
        /// through the results, and the algorithm, for example account information.
        /// Sensitive data, like credentials, should never be added
        /// </summary>
        /// <remarks>Will never throw, callers are not expected to handle any failure sharing a deployment detail</remarks>
        /// <param name="key">The deployment detail key</param>
        /// <param name="value">The deployment detail value</param>
        public static void Add(string key, string value)
        {
            try
            {
                var resultHandler = Composer.Instance.GetPart<IResultHandler>();
                if (resultHandler == null)
                {
                    // we only log this once, else we would spam for every entry
                    if (Interlocked.Exchange(ref _missingResultHandlerLogged, 1) == 0)
                    {
                        Log.Error($"DeploymentDetailsHelper.Add(): no result handler was found, deployment details will be ignored");
                    }
                    return;
                }
                resultHandler.AddDeploymentDetail(key, value);
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Failed to add deployment detail '{key}'");
            }
        }
    }
}
