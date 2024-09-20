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

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Event arguments for the <see cref="IDataProvider.NewDataRequest"/> event
    /// </summary>
    public class DataProviderNewDataRequestEventArgs : EventArgs
    {
        /// <summary>
        /// Path to the fetched data
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Whether the data was fetched successfully
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Any error message that occurred during the fetch
        /// </summary>
        public string ErrorMessage  { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataProviderNewDataRequestEventArgs"/> class
        /// </summary>
        /// <param name="path">The path to the fetched data</param>
        /// <param name="succeeded">Whether the data was fetched successfully</param>
        /// <param name="errorMessage">Any error message that occured during the fetch</param>
        public DataProviderNewDataRequestEventArgs(string path, bool succeeded, string  errorMessage)
        {
            Path = path;
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }
    }
}
