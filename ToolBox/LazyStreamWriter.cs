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
using System.IO;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// This class wraps a <see cref="StreamWriter"/> so that the StreamWriter is only
    /// instantiated until WriteLine() is called.  This ensures that the file the StreamWriter is
    /// writing to is only created if something is written to it. A StreamWriter will create a empty file
    /// as soon as it is instantiated.
    /// </summary>
    public class LazyStreamWriter
    {
        private StreamWriter _streamWriter;
        private readonly string _path;

        /// <summary>
        /// Constructor for the <see cref="LazyStreamWriter"/>
        /// </summary>
        /// <param name="path">Path to the file that should be created</param>
        public LazyStreamWriter(string path)
        {
            _path = path;
        }

        /// <summary>
        /// Wraps the WriteLine method of the StreamWriter.
        /// </summary>
        /// <param name="line">The line to write</param>
        /// <remarks>Will instantiate the StreamWriter if this is the first time this method is called</remarks>
        public void WriteLine(string line)
        {
            PrepareStreamWriter();

            _streamWriter.WriteLine(line);
        }

        /// <summary>
        /// Wraps the <see cref="StreamWriter.Flush()"/> method
        /// </summary>
        public void Flush()
        {
            if (_streamWriter != null)
            {
                _streamWriter.Flush();
            }
        }

        /// <summary>
        /// Wraps the <see cref="StreamWriter.Close()"/> method
        /// </summary>
        public void Close()
        {
            if (_streamWriter != null)
            {
                _streamWriter.Close();
            }
        }

        /// <summary>
        /// Checks if the StreamWriter is instantiated. If not, it will instantiate the StreamWriter
        /// </summary>
        private void PrepareStreamWriter()
        {
            if (_streamWriter == null)
            {
                _streamWriter = new StreamWriter(_path);
            }
        }
    }
}
