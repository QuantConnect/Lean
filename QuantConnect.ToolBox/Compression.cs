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

        /// <summary>
        /// Compress a given file and delete the original file. Automatically rename the file to name.zip.
        /// </summary>
        /// <param name="textPath">Path of the original file</param>
        /// <param name="deleteOriginal">Boolean flag to delete the original file after completion</param>
        /// <returns>String path for the new zip file</returns>
        public static string Zip(string textPath, bool deleteOriginal = true)
        {
            var buffer = new byte[4096];
            var zipPath = textPath.Replace(".csv", ".zip").Replace(".txt", ".zip");

            using (var stream = new ZipOutputStream(File.Create(zipPath)))
            {
                stream.PutNextEntry(Path.GetFileName(textPath));

                // copy everything from the file to the zip
                using (var fs = File.OpenRead(textPath))
                {
                    int sourceBytes;
                    do
                    {
                        sourceBytes = fs.Read(buffer, 0, buffer.Length);
                        stream.Write(buffer, 0, sourceBytes);
                    }
                    while (sourceBytes > 0);
                }
            }

            //Delete the old text file:
            if (deleteOriginal) File.Delete(textPath);
            return zipPath;
        }

        /// <summary>
        /// Streams a local zip file using a streamreader.
        /// Important: the caller must call Dispose() on the returned ZipFile instance.
        /// </summary>
        /// <param name="filename">Location of the original zip file</param>
        /// <param name="zip">The ZipFile instance to be returned to the caller</param>
        /// <returns>Stream reader of the first file contents in the zip file</returns>
        public static StreamReader Unzip(string filename, out Ionic.Zip.ZipFile zip)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("The specified file was not found", filename);
            }

            zip = null;
            StreamReader reader = null;

            try
            {
                zip = new ZipFile(filename);
                reader = new StreamReader(zip[0].OpenReader());
            }
            catch (Exception)
            {
                if (zip != null) zip.Dispose();
                if (reader != null) reader.Close();

                throw;
            }
            return reader;
        }
    }
}
