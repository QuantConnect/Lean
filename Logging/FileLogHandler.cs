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

namespace QuantConnect.Logging
{
    /// <summary>
    /// Provides an implementation of <see cref="ILogHandler"/> that writes all log messages to a file on disk.
    /// </summary>
    public class FileLogHandler : ILogHandler
    {
        private readonly Lazy<TextWriter> _writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogHandler"/> class to write messages to the specified file path.
        /// The file will be opened using <see cref="FileMode.Append"/>
        /// </summary>
        /// <param name="filepath">The file path use to save the log messages</param>
        public FileLogHandler(string filepath)
        {
            _writer = new Lazy<TextWriter>(
                () => new StreamWriter(File.Open(filepath, FileMode.Append, FileAccess.Write))
                );
        }

        /// <inheritdoc />
        public void Error(string text)
        {
            _writer.Value.WriteLine(GetMessage(text, "ERROR"));
            _writer.Value.FlushAsync();
        }

        /// <inheritdoc />
        public void Debug(string text)
        {
            _writer.Value.WriteLine(GetMessage(text, "DEBUG"));
        }

        /// <inheritdoc />
        public void Trace(string text)
        {
            _writer.Value.WriteLine(GetMessage(text, "TRACE"));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_writer.IsValueCreated)
            {
                _writer.Value.Dispose();
            }
        }

        private string GetMessage(string text, string level)
        {
            return string.Format("{0} {1}:: {2}", DateTime.UtcNow.ToString("o"), level, text);
        }
    }
}
