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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Ionic.Zip;
using QuantConnect.Logging;
using ZipEntry = ICSharpCode.SharpZipLib.Zip.ZipEntry;
using ZipFile = Ionic.Zip.ZipFile;
using ZipInputStream = ICSharpCode.SharpZipLib.Zip.ZipInputStream;
using ZipOutputStream = ICSharpCode.SharpZipLib.Zip.ZipOutputStream;

namespace QuantConnect
{
    /// <summary>
    /// Compression class manages the opening and extraction of compressed files (zip, tar, tar.gz).
    /// </summary>
    /// <remarks>QuantConnect's data library is stored in zip format locally on the hard drive.</remarks>
    public static class Compression
    {
        /// <summary>
        /// Global Flag :: Operating System
        /// </summary>
        private static bool IsLinux
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        /// <summary>
        /// Create a zip file of the supplied file names and string data source
        /// </summary>
        /// <param name="zipPath">Output location to save the file.</param>
        /// <param name="filenamesAndData">File names and data in a dictionary format.</param>
        /// <returns>True on successfully creating the zip file.</returns>
        public static bool ZipData(string zipPath, Dictionary<string, string> filenamesAndData)
        {
            try
            {
                //Create our output
                using (var stream = new ZipOutputStream(File.Create(zipPath)))
                {
                    stream.SetLevel(0);
                    foreach (var kvp in filenamesAndData)
                    {
                        var filename = kvp.Key;
                        //Create the space in the zip file:
                        var entry = new ZipEntry(filename);
                        var bytes = Encoding.Default.GetBytes(kvp.Value);
                        stream.PutNextEntry(entry);
                        stream.Write(bytes, 0, bytes.Length);
                        stream.CloseEntry();
                    } // End For Each File.

                    //Close stream:
                    stream.Finish();
                    stream.Close();
                } // End Using
            }
            catch (Exception err)
            {
                Log.Error(err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a zip file of the supplied file names and data using a byte array
        /// </summary>
        /// <param name="zipPath">Output location to save the file.</param>
        /// <param name="filenamesAndData">File names and data in a dictionary format.</param>
        /// <returns>True on successfully saving the file</returns>
        public static bool ZipData(string zipPath, IEnumerable<KeyValuePair<string, byte[]>> filenamesAndData)
        {
            var success = true;
            var buffer = new byte[4096];

            try
            {
                //Create our output
                using (var stream = new ZipOutputStream(File.Create(zipPath)))
                {
                    foreach (var file in filenamesAndData)
                    {
                        //Create the space in the zip file:
                        var entry = new ZipEntry(file.Key);
                        //Get a Byte[] of the file data:
                        stream.PutNextEntry(entry);

                        using (var ms = new MemoryStream(file.Value))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = ms.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, sourceBytes);
                            }
                            while (sourceBytes > 0);
                        }
                    } // End For Each File.

                    //Close stream:
                    stream.Finish();
                    stream.Close();
                } // End Using
            }
            catch (Exception err)
            {
                Log.Error(err);
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Zips the specified lines of text into the zipPath
        /// </summary>
        /// <param name="zipPath">The destination zip file path</param>
        /// <param name="zipEntry">The entry name in the zip</param>
        /// <param name="lines">The lines to be written to the zip</param>
        /// <returns>True if successful, otherwise false</returns>
        public static bool ZipData(string zipPath, string zipEntry, IEnumerable<string> lines)
        {
            try
            {
                using (var stream = new ZipOutputStream(File.Create(zipPath)))
                using (var writer = new StreamWriter(stream))
                {
                    var entry = new ZipEntry(zipEntry);
                    stream.PutNextEntry(entry);
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                Log.Error(err);
                return false;
            }
        }

        /// <summary>
        /// Append the zip data to the file-entry specified.
        /// </summary>
        /// <param name="path">The zip file path</param>
        /// <param name="entry">The entry name</param>
        /// <param name="data">The entry data</param>
        /// <param name="overrideEntry">True if should override entry if it already exists</param>
        /// <returns>True on success</returns>
        public static bool ZipCreateAppendData(string path, string entry, string data, bool overrideEntry = false)
        {
            try
            {
                using (var zip = File.Exists(path) ? ZipFile.Read(path) : new ZipFile(path))
                {
                    if (zip.ContainsEntry(entry) && overrideEntry)
                    {
                        zip.RemoveEntry(entry);
                    }

                    zip.AddEntry(entry, data);
                    zip.UseZip64WhenSaving = Zip64Option.Always;
                    zip.Save();
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Append the zip data to the file-entry specified.
        /// </summary>
        /// <param name="path">The zip file path</param>
        /// <param name="entry">The entry name</param>
        /// <param name="data">The entry data</param>
        /// <param name="overrideEntry">True if should override entry if it already exists</param>
        /// <returns>True on success</returns>
        public static bool ZipCreateAppendData(string path, string entry, byte[] data, bool overrideEntry = false)
        {
            try
            {
                using (var zip = File.Exists(path) ? ZipFile.Read(path) : new ZipFile(path))
                {
                    if (overrideEntry && zip.ContainsEntry(entry))
                    {
                        zip.RemoveEntry(entry);
                    }

                    zip.AddEntry(entry, data);
                    zip.UseZip64WhenSaving = Zip64Option.Always;
                    zip.Save();
                }
            }
            catch (Exception err)
            {
                Log.Error(err, $"file: {path} entry: {entry}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Uncompress zip data byte array into a dictionary string array of filename-contents.
        /// </summary>
        /// <param name="zipData">Byte data array of zip compressed information</param>
        /// <param name="encoding">Specifies the encoding used to read the bytes. If not specified, defaults to ASCII</param>
        /// <returns>Uncompressed dictionary string-sting of files in the zip</returns>
        public static Dictionary<string, string> UnzipData(byte[] zipData, Encoding encoding = null)
        {
            using var stream = new MemoryStream(zipData);
            return UnzipDataAsync(stream, encoding).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Uncompress zip data byte array into a dictionary string array of filename-contents.
        /// </summary>
        /// <param name="stream">Stream data of zip compressed information</param>
        /// <param name="encoding">Specifies the encoding used to read the bytes. If not specified, defaults to ASCII</param>
        /// <returns>Uncompressed dictionary string-sting of files in the zip</returns>
        public static async Task<Dictionary<string, string>> UnzipDataAsync(Stream stream, Encoding encoding = null)
        {
            // Initialize:
            var data = new Dictionary<string, string>();

            try
            {
                //Read out the zipped data into a string, save in array:
                using (var zipStream = new ZipInputStream(stream))
                {
                    while (true)
                    {
                        //Get the next file
                        var entry = zipStream.GetNextEntry();

                        if (entry != null)
                        {
                            // Read the file into buffer:
                            var buffer = new byte[entry.Size];
                            await zipStream.ReadAsync(buffer, 0, (int)entry.Size).ConfigureAwait(false);

                            //Save into array:
                            var str = (encoding ?? Encoding.ASCII).GetString(buffer);
                            data[entry.Name] = str;
                        }
                        else
                        {
                            break;
                        }
                    }
                } // End Zip Stream.

            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return data;
        }

        /// <summary>
        /// Performs an in memory zip of the specified bytes
        /// </summary>
        /// <param name="bytes">The file contents in bytes to be zipped</param>
        /// <param name="zipEntryName">The zip entry name</param>
        /// <returns>The zipped file as a byte array</returns>
        public static byte[] ZipBytes(byte[] bytes, string zipEntryName)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry(zipEntryName);
                    using (var entryStream = entry.Open())
                    {
                        entryStream.Write(bytes, 0, bytes.Length);
                    }
                }
                // 'ToArray' after disposing of 'ZipArchive' since it finishes writing all the data
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Extract .gz files to disk
        /// </summary>
        /// <param name="gzipFileName"></param>
        /// <param name="targetDirectory"></param>
        public static string UnGZip(string gzipFileName, string targetDirectory)
        {
            // Use a 4K buffer. Any larger is a waste.
            var dataBuffer = new byte[4096];
            var newFileOutput = Path.Combine(targetDirectory, Path.GetFileNameWithoutExtension(gzipFileName));
            using (Stream fileStream = new FileStream(gzipFileName, FileMode.Open, FileAccess.Read))
            using (var gzipStream = new GZipInputStream(fileStream))
            using (var fileOutput = File.Create(newFileOutput))
            {
                StreamUtils.Copy(gzipStream, fileOutput, dataBuffer);
            }
            return newFileOutput;
        }

        /// <summary>
        /// Compress a given file and delete the original file. Automatically rename the file to name.zip.
        /// </summary>
        /// <param name="textPath">Path of the original file</param>
        /// <param name="zipEntryName">The name of the entry inside the zip file</param>
        /// <param name="deleteOriginal">Boolean flag to delete the original file after completion</param>
        /// <returns>String path for the new zip file</returns>
        public static string Zip(string textPath, string zipEntryName, bool deleteOriginal = true)
        {
            var zipPath = textPath.Replace(".csv", ".zip").Replace(".txt", ".zip");
            Zip(textPath, zipPath, zipEntryName, deleteOriginal);
            return zipPath;
        }

        /// <summary>
        /// Compresses the specified source file.
        /// </summary>
        /// <param name="source">The source file to be compressed</param>
        /// <param name="destination">The destination zip file path</param>
        /// <param name="zipEntryName">The zip entry name for the file</param>
        /// <param name="deleteOriginal">True to delete the source file upon completion</param>
        public static void Zip(string source, string destination, string zipEntryName, bool deleteOriginal)
        {
            try
            {
                var buffer = new byte[4096];
                using (var stream = new ZipOutputStream(File.Create(destination)))
                {
                    //Zip the text file.
                    var entry = new ZipEntry(zipEntryName);
                    stream.PutNextEntry(entry);

                    using (var fs = File.OpenRead(source))
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
                if (deleteOriginal)
                {
                    File.Delete(source);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Compress a given file and delete the original file. Automatically rename the file to name.zip.
        /// </summary>
        /// <param name="textPath">Path of the original file</param>
        /// <param name="deleteOriginal">Boolean flag to delete the original file after completion</param>
        /// <returns>String path for the new zip file</returns>
        public static string Zip(string textPath, bool deleteOriginal = true)
        {
            return Zip(textPath, Path.GetFileName(textPath), deleteOriginal);
        }

        /// <summary>
        /// Compress given data to the path given
        /// </summary>
        /// <param name="data">Data to write to zip</param>
        /// <param name="zipPath">Path to write to</param>
        /// <param name="zipEntry">Entry to save the data as</param>
        public static void Zip(string data, string zipPath, string zipEntry)
        {
            using (var stream = new ZipOutputStream(File.Create(zipPath)))
            {
                var entry = new ZipEntry(zipEntry);
                stream.PutNextEntry(entry);
                var buffer = new byte[4096];
                using (var dataReader = new MemoryStream(Encoding.Default.GetBytes(data)))
                {
                    int sourceBytes;
                    do
                    {
                        sourceBytes = dataReader.Read(buffer, 0, buffer.Length);
                        stream.Write(buffer, 0, sourceBytes);
                    }
                    while (sourceBytes > 0);
                }
            }
        }

        /// <summary>
        /// Zips the specified directory, preserving folder structure
        /// </summary>
        /// <param name="directory">The directory to be zipped</param>
        /// <param name="destination">The output zip file destination</param>
        /// <param name="includeRootInZip">True to include the root 'directory' in the zip, false otherwise</param>
        /// <returns>True on a successful zip, false otherwise</returns>
        public static bool ZipDirectory(string directory, string destination, bool includeRootInZip = true)
        {
            try
            {
                if (File.Exists(destination)) File.Delete(destination);
                System.IO.Compression.ZipFile.CreateFromDirectory(directory, destination, CompressionLevel.Fastest, includeRootInZip, new PathEncoder());
                return true;
            }
            catch (Exception err)
            {
                Log.Error(err);
                return false;
            }
        }

        /// <summary>
        /// Encode the paths as linux format for cross platform compatibility
        /// </summary>
        private class PathEncoder : UTF8Encoding
        {
            public override byte[] GetBytes(string s)
            {
                s = s.Replace("\\", "/");
                return base.GetBytes(s);
            }
        }

        /// <summary>
        /// Unzips the specified zip file to the specified directory
        /// </summary>
        /// <param name="zip">The zip to be unzipped</param>
        /// <param name="directory">The directory to place the unzipped files</param>
        /// <param name="overwrite">Flag specifying whether or not to overwrite existing files</param>
        public static bool Unzip(string zip, string directory, bool overwrite = false)
        {
            if (!File.Exists(zip)) return false;

            try
            {
                if (!overwrite)
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zip, directory);
                }
                else
                {
                    using (var archive = new ZipArchive(File.OpenRead(zip)))
                    {
                        foreach (var file in archive.Entries)
                        {
                            // skip directories
                            if (file.Name == "") continue;
                            var filepath = Path.Combine(directory, file.FullName);
                            if (IsLinux) filepath = filepath.Replace(@"\", "/");
                            var outputFile = new FileInfo(filepath);
                            if (!outputFile.Directory.Exists)
                            {
                                outputFile.Directory.Create();
                            }
                            file.ExtractToFile(outputFile.FullName, true);
                        }
                    }
                }

                return true;
            }
            catch (Exception err)
            {
                Log.Error(err);
                return false;
            }
        }

        /// <summary>
        /// Zips all files specified to a new zip at the destination path
        /// </summary>
        public static void ZipFiles(string destination, IEnumerable<string> files)
        {
            try
            {
                using (var zipStream = new ZipOutputStream(File.Create(destination)))
                {
                    var buffer = new byte[4096];
                    foreach (var file in files)
                    {
                        if (!File.Exists(file))
                        {
                            Log.Trace($"ZipFiles(): File does not exist: {file}");
                            continue;
                        }

                        var entry = new ZipEntry(Path.GetFileName(file));
                        zipStream.PutNextEntry(entry);
                        using (var fstream = File.OpenRead(file))
                        {
                            StreamUtils.Copy(fstream, zipStream, buffer);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Streams a local zip file using a streamreader.
        /// Important: the caller must call Dispose() on the returned ZipFile instance.
        /// </summary>
        /// <param name="filename">Location of the original zip file</param>
        /// <param name="zip">The ZipFile instance to be returned to the caller</param>
        /// <returns>Stream reader of the first file contents in the zip file</returns>
        public static StreamReader Unzip(string filename, out ZipFile zip)
        {
            return Unzip(filename, null, out zip);
        }

        /// <summary>
        /// Streams a local zip file using a streamreader.
        /// Important: the caller must call Dispose() on the returned ZipFile instance.
        /// </summary>
        /// <param name="filename">Location of the original zip file</param>
        /// <param name="zipEntryName">The zip entry name to open a reader for. Specify null to access the first entry</param>
        /// <param name="zip">The ZipFile instance to be returned to the caller</param>
        /// <returns>Stream reader of the first file contents in the zip file</returns>
        public static StreamReader Unzip(string filename, string zipEntryName, out ZipFile zip)
        {
            StreamReader reader = null;
            zip = null;

            try
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        zip = new ZipFile(filename);
                        var entry = zip.FirstOrDefault(x => zipEntryName == null || string.Compare(x.FileName, zipEntryName, StringComparison.OrdinalIgnoreCase) == 0);
                        if (entry == null)
                        {
                            // Unable to locate zip entry
                            return null;
                        }

                        reader = new StreamReader(entry.OpenReader());
                    }
                    catch (Exception err)
                    {
                        Log.Error(err, "Inner try/catch");
                        if (zip != null) zip.Dispose();
                        if (reader != null) reader.Close();
                    }
                }
                else
                {
                    Log.Error($"Data.UnZip(2): File doesn\'t exist: {filename}");
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "File: " + filename);
            }
            return reader;
        }

        /// <summary>
        /// Streams the unzipped file as key value pairs of file name to file contents.
        /// NOTE: When the returned enumerable finishes enumerating, the zip stream will be
        /// closed rendering all key value pair Value properties unaccessible. Ideally this
        /// would be enumerated depth first.
        /// </summary>
        /// <remarks>
        /// This method has the potential for a memory leak if each kvp.Value enumerable is not disposed
        /// </remarks>
        /// <param name="filename">The zip file to stream</param>
        /// <returns>The stream zip contents</returns>
        public static IEnumerable<KeyValuePair<string, List<string>>> Unzip(string filename)
        {
            if (!File.Exists(filename))
            {
                Log.Error($"Compression.Unzip(): File does not exist: {filename}");
                return Enumerable.Empty<KeyValuePair<string, List<string>>>();
            }

            try
            {
                return ReadLinesImpl(filename);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return Enumerable.Empty<KeyValuePair<string, List<string>>>();
        }

        /// <summary>
        /// Lazily unzips the specified stream
        /// </summary>
        /// <param name="stream">The zipped stream to be read</param>
        /// <returns>An enumerable whose elements are zip entry key value pairs with
        /// a key of the zip entry name and the value of the zip entry's file lines</returns>
        public static IEnumerable<KeyValuePair<string, List<string>>> Unzip(Stream stream)
        {
            using (var zip = ZipFile.Read(stream))
            {
                foreach (var entry in zip)
                {
                    yield return new KeyValuePair<string, List<string>>(entry.FileName, ReadZipEntry(entry));
                }
            }
        }

        /// <summary>
        /// Streams each line from the first zip entry in the specified zip file
        /// </summary>
        /// <param name="filename">The zip file path to stream</param>
        /// <returns>An enumerable containing each line from the first unzipped entry</returns>
        public static List<string> ReadLines(string filename)
        {
            if (!File.Exists(filename))
            {
                Log.Error($"Compression.ReadFirstZipEntry(): File does not exist: {filename}");
                return new List<string>();
            }

            try
            {
                return ReadLinesImpl(filename, firstEntryOnly: true).Single().Value;
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return new List<string>();
        }

        private static IEnumerable<KeyValuePair<string, List<string>>> ReadLinesImpl(string filename, bool firstEntryOnly = false)
        {
            using (var zip = ZipFile.Read(filename))
            {
                for (var i = 0; i < zip.Count; i++)
                {
                    var entry = zip[i];
                    yield return new KeyValuePair<string, List<string>>(entry.FileName, ReadZipEntry(entry));
                    if (firstEntryOnly)
                    {
                        yield break;
                    }
                }
            }
        }

        private static List<string> ReadZipEntry(Ionic.Zip.ZipEntry entry)
        {
            var result = new List<string>();
            using var entryReader = new StreamReader(entry.OpenReader());
            var line = entryReader.ReadLine();
            while (line != null)
            {
                result.Add(line);
                line = entryReader.ReadLine();
            }
            return result;
        }

        /// <summary>
        /// Unzip a local file and return its contents via streamreader:
        /// </summary>
        public static StreamReader UnzipStreamToStreamReader(Stream zipstream)
        {
            StreamReader reader = null;
            try
            {
                //Initialise:
                MemoryStream file;

                //If file exists, open a zip stream for it.
                using (var zipStream = new ZipInputStream(zipstream))
                {
                    //Read the file entry into buffer:
                    var entry = zipStream.GetNextEntry();
                    var buffer = new byte[entry.Size];
                    zipStream.Read(buffer, 0, (int)entry.Size);

                    //Load the buffer into a memory stream.
                    file = new MemoryStream(buffer);
                }

                //Open the memory stream with a stream reader.
                reader = new StreamReader(file);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return reader;
        } // End UnZip

        /// <summary>
        /// Unzip a stream that represents a zip file and return the first entry as a stream
        /// </summary>
        public static Stream UnzipStream(Stream zipstream, out ZipFile zipFile, string entryName = null)
        {
            zipFile = ZipFile.Read(zipstream);

            try
            {
                Ionic.Zip.ZipEntry entry;
                if (string.IsNullOrEmpty(entryName))
                {
                    //Read the file entry into buffer:
                    entry = zipFile.Entries.FirstOrDefault();
                }
                else
                {
                    // Attempt to find our specific entry
                    if (!zipFile.ContainsEntry(entryName))
                    {
                        return null;
                    }
                    entry = zipFile[entryName];
                }

                if (entry != null)
                {
                    return entry.OpenReader();
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return null;
        } // End UnZip

        /// <summary>
        /// Unzip the given byte array and return the created file names.
        /// </summary>
        /// <param name="zipData">A byte array containing the zip</param>
        /// <param name="outputFolder">The target output folder</param>
        /// <returns>List of unzipped file names</returns>
        public static List<string> UnzipToFolder(byte[] zipData, string outputFolder)
        {
            var stream = new MemoryStream(zipData);
            return UnzipToFolder(stream, outputFolder);
        }

        /// <summary>
        /// Unzip a local file and return the created file names
        /// </summary>
        /// <param name="zipFile">Location of the zip on the HD</param>
        /// <returns>List of unzipped file names</returns>
        public static List<string> UnzipToFolder(string zipFile)
        {
            var outFolder = Path.GetDirectoryName(zipFile);
            var stream = File.OpenRead(zipFile);
            return UnzipToFolder(stream, outFolder);
        }

        /// <summary>
        /// Unzip the given data stream into the target output folder and return the created file names
        /// </summary>
        /// <param name="dataStream">The zip data stream</param>
        /// <param name="outFolder">The target output folder</param>
        /// <returns>List of unzipped file names</returns>
        private static List<string> UnzipToFolder(Stream dataStream, string outFolder)
        {
            //1. Initialize:
            var files = new List<string>();
            if (string.IsNullOrEmpty(outFolder))
            {
                outFolder = Directory.GetCurrentDirectory();
            }
            ICSharpCode.SharpZipLib.Zip.ZipFile zf = null;

            try
            {
                zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(dataStream);

                foreach (ZipEntry zipEntry in zf)
                {
                    //Ignore Directories
                    if (!zipEntry.IsFile) continue;

                    var buffer = new byte[4096]; // 4K is optimum
                    var zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, zipEntry.Name);

                    var targetFile = new FileInfo(fullZipToPath);
                    if (targetFile.Directory != null && !targetFile.Directory.Exists)
                    {
                        targetFile.Directory.Create();
                    }

                    //Save the file name for later:
                    files.Add(fullZipToPath);

                    //Copy the data in buffer chunks
                    using (var streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            catch
            {
                // lets catch the exception just to log some information about the zip file
                Log.Error($"Compression.UnzipToFolder(): Failure: outFolder: {outFolder} - files: {string.Join(",", files)}");
                throw;
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
            return files;
        } // End UnZip

        /// <summary>
        /// Extracts all file from a zip archive and copies them to a destination folder.
        /// </summary>
        /// <param name="source">The source zip file.</param>
        /// <param name="destination">The destination folder to extract the file to.</param>
        public static void UnTarFiles(string source, string destination)
        {
            var inStream = File.OpenRead(source);
            var tarArchive = TarArchive.CreateInputTarArchive(inStream);
            tarArchive.ExtractContents(destination);
            tarArchive.Close();
            inStream.Close();
        }

        /// <summary>
        /// Extract tar.gz files to disk
        /// </summary>
        /// <param name="source">Tar.gz source file</param>
        /// <param name="destination">Location folder to unzip to</param>
        public static void UnTarGzFiles(string source, string destination)
        {
            var inStream = File.OpenRead(source);
            var gzipStream = new GZipInputStream(inStream);
            var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(destination);
            tarArchive.Close();
            gzipStream.Close();
            inStream.Close();
        }

        /// <summary>
        /// Enumerate through the files of a TAR and get a list of KVP names-byte arrays
        /// </summary>
        /// <param name="stream">The input tar stream</param>
        /// <param name="isTarGz">True if the input stream is a .tar.gz or .tgz</param>
        /// <returns>An enumerable containing each tar entry and it's contents</returns>
        public static IEnumerable<KeyValuePair<string, byte[]>> UnTar(Stream stream, bool isTarGz)
        {
            using (var tar = new TarInputStream(isTarGz ? (Stream)new GZipInputStream(stream) : stream))
            {
                TarEntry entry;
                while ((entry = tar.GetNextEntry()) != null)
                {
                    if (entry.IsDirectory) continue;

                    using (var output = new MemoryStream())
                    {
                        tar.CopyEntryContents(output);
                        yield return new KeyValuePair<string, byte[]>(entry.Name, output.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate through the files of a TAR and get a list of KVP names-byte arrays.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, byte[]>> UnTar(string source)
        {
            //This is a tar.gz file.
            var gzip = (source.Substring(Math.Max(0, source.Length - 6)) == "tar.gz");

            using (var file = File.OpenRead(source))
            {
                var tarIn = new TarInputStream(file);

                if (gzip)
                {
                    var gzipStream = new GZipInputStream(file);
                    tarIn = new TarInputStream(gzipStream);
                }

                TarEntry tarEntry;
                while ((tarEntry = tarIn.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory) continue;

                    using (var stream = new MemoryStream())
                    {
                        tarIn.CopyEntryContents(stream);
                        yield return new KeyValuePair<string, byte[]>(tarEntry.Name, stream.ToArray());
                    }
                }
                tarIn.Close();
            }
        }

        /// <summary>
        /// Validates whether the zip is corrupted or not
        /// </summary>
        /// <param name="path">Path to the zip file</param>
        /// <returns>true if archive tests ok; false otherwise.</returns>
        public static bool ValidateZip(string path)
        {
            using (var zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(path))
            {
                return zip.TestArchive(true);
            }
        }

        /// <summary>
        /// Returns the entry file names contained in a zip file
        /// </summary>
        /// <param name="zipFileName">The zip file name</param>
        /// <returns>An IEnumerable of entry file names</returns>
        public static IEnumerable<string> GetZipEntryFileNames(string zipFileName)
        {
            using (var zip = ZipFile.Read(zipFileName))
            {
                return zip.EntryFileNames;
            }
        }

        /// <summary>
        /// Return the entry file names contained in a zip file
        /// </summary>
        /// <param name="zipFileStream">Stream to the file</param>
        /// <returns>IEnumerable of entry file names</returns>
        public static IEnumerable<string> GetZipEntryFileNames(Stream zipFileStream)
        {
            using (var zip = ZipFile.Read(zipFileStream))
            {
                return zip.EntryFileNames;
            }
        }

        /// <summary>
        /// Extracts a 7-zip archive to disk, using the 7-zip CLI utility
        /// </summary>
        /// <param name="inputFile">Path to the 7z file</param>
        /// <param name="outputDirectory">Directory to output contents of 7z</param>
        /// <param name="execTimeout">Timeout in seconds for how long we should wait for the extraction to complete</param>
        /// <exception cref="Exception">The extraction failed because of a timeout or the exit code was not 0</exception>
        public static void Extract7ZipArchive(string inputFile, string outputDirectory, int execTimeout = 60000)
        {
            var zipper = IsLinux ? "7z" : "C:/Program Files/7-Zip/7z.exe";
            var psi = new ProcessStartInfo(zipper, " e " + inputFile + " -o" + outputDirectory)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = false
            };

            var process = new Process();
            process.StartInfo = psi;
            process.Start();

            if (!process.WaitForExit(execTimeout))
            {
                throw new TimeoutException($"Timed out extracting 7Zip archive: {inputFile} ({execTimeout} seconds)");
            }
            if (process.ExitCode > 0)
            {
                throw new Exception($"Compression.Extract7ZipArchive(): 7Zip exited unsuccessfully (code {process.ExitCode})");
            }
        }
    }
}
