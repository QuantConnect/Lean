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
using System.IO.Compression;
using System.Text;
using QuantConnect.Logging;
using static System.FormattableString;

namespace QuantConnect
{
    /// <summary>
    /// Provides an implementation of <see cref="TextWriter"/> to write to a zip file
    /// </summary>
    public class ZipStreamWriter : TextWriter
    {
        private readonly string _filename;
        private readonly string _zipEntry;
        private readonly string _tempFilename;
        private ZipArchive _archive;
        private StreamWriter _writer;

        /// <summary>
        /// When overridden in a derived class, returns the character encoding in which the output is written.
        /// </summary>
        /// <returns>
        /// The character encoding in which the output is written.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipStreamWriter"/> class
        /// </summary>
        /// <param name="filename">The output zip file name</param>
        /// <param name="zipEntry">The file name in the zip file</param>
        public ZipStreamWriter(string filename, string zipEntry)
        {
            _filename = filename;
            _zipEntry = zipEntry;
            _tempFilename = $"{_filename}.tmp";
            File.Delete(_tempFilename);
            if (!File.Exists(filename))
            {
                _archive = ZipFile.Open(_tempFilename, ZipArchiveMode.Create);
                _writer = new StreamWriter(_archive.CreateEntry(zipEntry).Open());
            }
            else
            {
                File.Copy(filename, _tempFilename);
                _archive = ZipFile.Open(_tempFilename, ZipArchiveMode.Update);
                var entry = _archive.GetEntry(zipEntry) ?? _archive.CreateEntry(zipEntry);
                _writer = new StreamWriter(entry.Open());
                _writer.BaseStream.Seek(0L, SeekOrigin.End);
            }
        }

        /// <summary>
        /// Writes a character to the text string or stream.
        /// </summary>
        /// <param name="value">The character to write to the text stream. </param>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <filterpriority>1</filterpriority>
        public override void Write(char value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text string or stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value"/> is null, only the line terminator is written. </param>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <filterpriority>1</filterpriority>
        public override void WriteLine(string value)
        {
            _writer.WriteLine(value);
        }

        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            _writer.Flush();
            _writer.Dispose();
            _archive.Dispose();

            CopyTempFile(5, throwOnFailure: false);

            _archive = ZipFile.Open(_tempFilename, ZipArchiveMode.Update);
            var entry = _archive.GetEntry(_zipEntry) ?? _archive.CreateEntry(_zipEntry);
            _writer = new StreamWriter(entry.Open());
            _writer.BaseStream.Seek(0L, SeekOrigin.End);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.TextWriter"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected override void Dispose(bool disposing)
        {
            if (_writer == null || !disposing)
                return;
            _writer.Flush();
            _writer.Close();
            _writer.Dispose();
            _archive.Dispose();

            CopyTempFile(50, throwOnFailure: true);
            if (File.Exists(_tempFilename))
            {
                File.Delete(_tempFilename);
            }
        }

        private void CopyTempFile(int attempts, bool throwOnFailure)
        {
            if (!File.Exists(_tempFilename)) return;

            do
            {
                try
                {
                    File.Copy(_tempFilename, _filename, true);
                    return;
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
            }
            while (--attempts > 0);

            if (throwOnFailure)
            {
                throw new InvalidOperationException(Invariant($"Unable to save file: {_filename} after {attempts} attempts."));
            }
        }
    }
}