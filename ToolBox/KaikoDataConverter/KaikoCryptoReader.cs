/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Ionic.Zlib;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZipEntry = Ionic.Zip.ZipEntry;

namespace QuantConnect.ToolBox.KaikoDataConverter
{
    /// <summary>
    /// Decompress single entry from Kaiko crypto raw data.
    /// </summary>
    public class EnumerableCompressedGz
    {
        /// <summary>
        /// Gets the raw data from entry.
        /// </summary>
        /// <param name="zipEntry">The zip entry.</param>
        /// <returns>IEnumerable with the zip entry content.</returns>
        public static IEnumerable<string> GetRawDataFromEntry(ZipEntry zipEntry)
        {
            using (var outerStream = new StreamReader(zipEntry.OpenReader()))
            using (var innerStream = new GZipStream(outerStream.BaseStream, CompressionMode.Decompress))
            {
                var decompressed = Decompress(innerStream);
                return Encoding.ASCII.GetString(decompressed).Split(new char[] { '\n' });
            }
        }

        /// <summary>
        /// Decompress the specified GZip stream.
        /// </summary>
        /// <param name="innerStream">The inner stream.</param>
        /// <returns>Array of bytes with the decompressed data.</returns>
        private static byte[] Decompress(GZipStream innerStream)
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memory = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = innerStream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return memory.ToArray();
            }
        }
    }
}