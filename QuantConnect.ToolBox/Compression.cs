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
using Ionic.Zip;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Collection of compression routines
    /// </summary>
    public class Compression
    {
        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// 
        /// </summary>
        /// <param name="zipPath"></param>
        /// <param name="filenamesAndData"></param>
        /// <returns></returns>
        public static bool Zip(string zipPath, System.Collections.Generic.Dictionary<string, string> filenamesAndData)
        {
            var success = true;
            var buffer = new byte[4096];
            try
            {
                using (var stream = new ZipOutputStream(System.IO.File.Create(zipPath)))
                {
                    foreach (var filename in filenamesAndData.Keys)
                    {
                        var file = filenamesAndData[filename].GetBytes();
                        var entry = stream.PutNextEntry(filename);
                        using (var ms = new System.IO.MemoryStream(file))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = ms.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, sourceBytes);
                            }
                            while (sourceBytes > 0);
                        }
                    }
                    stream.Flush();
                    stream.Close();
                }
            }
            catch (System.Exception err)
            {
                System.Console.WriteLine("Compression.ZipData(): " + err.Message);
                success = false;
            }
            return success;
        }
    }
}
