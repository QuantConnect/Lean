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

using System.Collections.Generic;
using System.IO;
using Ionic.BZip2;
using Ionic.Zlib;

namespace QuantConnect.ToolBox
{
    public class GzipStreamProvider : IStreamProvider
    {
        private readonly Dictionary<string, Stream> _openedStreams = new Dictionary<string, Stream>(2);
        
        /// <summary>
        /// Opens the specified source as read to be consumed stream
        /// </summary>
        /// <param name="source">The source file to be opened</param>
        /// <returns>The stream representing the specified source</returns>
        public IEnumerable<Stream> Open(string source)
        {
            var stream = new GZipStream(File.OpenRead(source), CompressionMode.Decompress);
            _openedStreams.Add(source, stream);
            yield return stream;
        }

        /// <summary>
        /// Closes the specified source file stream
        /// </summary>
        /// <param name="source">The source file to be closed</param>
        public void Close(string source)
        {
            Stream stream;
            if (_openedStreams.TryGetValue(source, out stream))
            {
                stream.Close();
            }
            
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var keyValuePair in _openedStreams)
            {
                keyValuePair.Value.Close();
            }
        }
    }
}