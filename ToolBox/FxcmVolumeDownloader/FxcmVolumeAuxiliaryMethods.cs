using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Ionic.Zip;
using QuantConnect.Data.Custom;

namespace QuantConnect.ToolBox
{
    public static class FxcmVolumeAuxiliaryMethods
    {
        public static IEnumerable<FxcmVolume> GetFxcmVolumeFromZip(string dataZipFile)
        {
            var output = new List<FxcmVolume>();
            var data = ReadZipFileData(dataZipFile);
            foreach (var obs in data)
            {
                var time = DateTime.ParseExact(obs[0], "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
                output.Add(new FxcmVolume
                {
                    DataType = MarketDataType.Base,
                    Time = time,
                    Value = Parse.Long(obs[1]),
                    Transactions = Parse.Int(obs[2])
                });
            }
            return output;
        }

        public static List<string[]> ReadZipFolderData(string outputFolder)
        {
            var actualdata = new List<string[]>();
            var files = Directory.GetFiles(outputFolder, "*.zip");
            foreach (var file in files)
            {
                actualdata.AddRange(ReadZipFileData(file));
            }
            return actualdata;
        }

        public static List<string[]> ReadZipFileData(string dataZipFile)
        {
            var actualdata = new List<string[]>();
            using (ZipFile zip = ZipFile.Read(dataZipFile))
            using (var entryReader = new StreamReader(zip.First().OpenReader()))
            {
                while (!entryReader.EndOfStream)
                {
                    actualdata.Add(entryReader.ReadLine().Split(','));
                }
            }
            return actualdata;
        }

        public static DateTime? GetLastAvailableDateOfData(Symbol symbol, Resolution resolution, string folderPath)
        {
            if (!Directory.Exists(folderPath)) return null;
            DateTime? lastAvailableDate = null;
            switch (resolution)
            {
                case Resolution.Daily:
                case Resolution.Hour:
                    var expectedFilePath = Path.Combine(folderPath, $"{symbol.Value.ToLowerInvariant()}_volume.zip"
                    );
                    if (File.Exists(expectedFilePath))
                    {
                        var lastStrDate = ReadZipFileData(expectedFilePath).Last()     // last observation
                                                                           .First()    // first string (date)
                                                                           .Substring(startIndex: 0, length: 8);
                        lastAvailableDate = DateTime.ParseExact(lastStrDate, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    }
                    break;
                case Resolution.Minute:
                    var lastFileDate = Directory
                        .GetFiles(folderPath, "*_volume.zip")
                        .OrderBy(f => f)
                        .LastOrDefault();
                    if (lastFileDate != null)
                    {
                        lastFileDate = Path.GetFileNameWithoutExtension(lastFileDate)
                                           .Substring(startIndex: 0, length: 8);
                        lastAvailableDate = DateTime.ParseExact(lastFileDate, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                    }
                    break;
            }
            return lastAvailableDate;
        }
    }
}