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
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default file provider functionality that retrieves data from disc to be used in an algorithm
    /// </summary>
    public class DefaultDataProvider : IDataProvider, IDisposable
    {
        /// <summary>
        /// Event raised each time data fetch is finished (successfully or not)
        /// </summary>
        public event EventHandler<DataProviderNewDataRequestEventArgs> NewDataRequest;

        /// <summary>
        /// Retrieves data from disc to be used in an algorithm
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public virtual Stream Fetch(string key)
        {
            var success = true;
            try
            {
                return new FileStream(FileExtension.ToNormalizedPath(key), FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception exception)
            {
                success = false;
                if (exception is DirectoryNotFoundException
                    || exception is FileNotFoundException)
                {
                    return null;
                }

                throw;
            }
            finally
            {
                OnNewDataRequest(new DataProviderNewDataRequestEventArgs(key, success));
            }
        }

        /// <summary>
        /// The stream created by this type is passed up the stack to the IStreamReader
        /// The stream is closed when the StreamReader that wraps this stream is disposed</summary>
        public void Dispose()
        {
            //
        }

        /// <summary>
        /// Event invocator for the <see cref="NewDataRequest"/> event
        /// </summary>
        protected virtual void OnNewDataRequest(DataProviderNewDataRequestEventArgs e)
        {
            NewDataRequest?.Invoke(this, e);
        }
    }
}
