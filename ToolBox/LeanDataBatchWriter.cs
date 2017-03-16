using System;
using System.IO;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// This is useful for large data processing where if the program stops 
    /// in the middle of execution, the data might be corrupted.
    /// By temporarily saving the file in one location and then moving it,
    /// we can guarantee data in the final location will not be corrupted.
    /// </summary>
    public class LeanDataBatchWriter : LeanDataWriter
    {
        private readonly string _tempDirectory;

        /// <summary>
        /// LeanDataWriter that uses a temporary directory
        /// </summary>
        /// <param name="resolution">The resolution of the data being written</param>
        /// <param name="symbol">The symbol that this data represents</param>
        /// <param name="dataDirectory">The final directory where the file will be written</param>
        /// <param name="tempDirectory">The temporary directory where files are written before they are moved to the final directory</param>
        /// <param name="dataType">The Tyoe of data being written</param>
        public LeanDataBatchWriter(Resolution resolution, Symbol symbol, string dataDirectory, string tempDirectory, TickType dataType = TickType.Trade) 
            : base(resolution, symbol, dataDirectory, dataType)
        {
            _tempDirectory = tempDirectory;
        }

        /// <summary>
        /// Write this file to disk.
        /// If a temporary directory is specified, write the file to the
        /// temp directory and copy it to the final location
        /// </summary>
        /// <param name="filePath">The full path to the new file</param>
        /// <param name="data">The data to write as a string</param>
        /// <param name="date">The date the data represents</param>
        protected override void WriteFile(string filePath, string data, DateTime date)
        {
            data = data.TrimEnd();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Log.Trace("LeanDataBatchWriter.Write(): Existing deleted: " + filePath);
            }

            // Create the directory if it doesnt exist
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // generate the file name
            var zipEntryName = LeanData.GenerateZipEntryName(Symbol.Value, SecurityType, date, Resolution, DataType);
 
            // Write to a temporary file
            var tempOutputFile = GetZipOutputFileName(_tempDirectory, date);
            Directory.CreateDirectory(Path.GetDirectoryName(tempOutputFile));
            Compression.Zip(data, tempOutputFile, zipEntryName);

            // Move temp file to the final destination with the appropriate name
            File.Move(tempOutputFile, filePath);

            // Clean up the temporary file
            File.Delete(tempOutputFile);

            Log.Trace("LeanDataBatchWriter.Write(): Created: {0}", filePath);
        }
    }
}
