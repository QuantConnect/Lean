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

using System;
using System.IO;
using System.Text;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides an implementation of <see cref="TextWriter"/> that redirects Write(string) and WriteLine(string)
    /// </summary>
    public class FuncTextWriter : TextWriter
    {
        private readonly Action<string> _writer;

        /// <inheritdoc />
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTextWriter"/> that will direct
        /// messages to the algorithm's Debug function.
        /// </summary>
        /// <param name="writer">The algorithm hosting the Debug function where messages will be directed</param>
        public FuncTextWriter(Action<string> writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// Writes the string value using the delegate provided at construction
        /// </summary>
        /// <param name="value">The string value to be written</param>
        public override void Write(string value)
        {
            _writer(value);
        }

        /// <summary>
        /// Writes the string value using the delegate provided at construction
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(string value)
        {
            // these are grouped in a list so we don't need to add new line characters here
            _writer(value);
        }
    }
}
