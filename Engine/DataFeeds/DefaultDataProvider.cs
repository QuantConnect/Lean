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
    /// Default file provider functionality that does not attempt to retrieve any data
    /// </summary>
    public class DefaultDataProvider : IDataProvider, IDisposable
    {
        /// <summary>
        /// Retrieves data from disc to be used in an algorithm
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string key)
        {
            try
            {
                return new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception exception)
            {
                if (exception is DirectoryNotFoundException
                    || exception is FileNotFoundException)
                {
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// The stream created by this type is passed up the stack to the IStreamReader
        /// The stream is closed when the StreamReader that wraps this stream is disposed</summary>
        public void Dispose()
        {
            //
        }
    }
}
