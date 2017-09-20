using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QuantConnect.Data;
using QuantConnect.Data.Custom;

namespace QuantConnect.ToolBox
{
    public class FxcmVolumeWriter
    {
        private readonly Resolution _resolution;
        private readonly Symbol _symbol;

        public string FolderPath { get; }

        public FxcmVolumeWriter(Resolution resolution, Symbol symbol, string dataDirectory)
        {
            _symbol = symbol;
            _resolution = resolution;
            var market = _symbol.ID.Market;
            FolderPath =
                Path.Combine(new[] {dataDirectory, "forex", market.ToLower(), _resolution.ToString().ToLower()});
            if (_resolution == Resolution.Minute)
            {
                FolderPath = Path.Combine(FolderPath, _symbol.Value.ToLower());
            }
        }

        public void Write(IEnumerable<BaseData> data, bool update = false)
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
            if (update && _resolution != Resolution.Minute)
            {
                data = ReadActualDataAndAppendNewData(data);
            }
            // Seems the data has some duplicate values! This makes the writer throws an error. So, just in case, we clean the data from duplicates.
            data = data.GroupBy(x => x.Time)
                       .Select(g => g.First());
            if (_resolution == Resolution.Minute)
            {
                WriteMinuteData(data);
            }
            else
            {
                WriteHourAndDailyData(data);
            }
        }

        private IEnumerable<BaseData> ReadActualDataAndAppendNewData(IEnumerable<BaseData> data)
        {
            // Read the actual data
            var zipFilePath = Path.Combine(FolderPath, _symbol.Value.ToLower() + "_volume.zip");
            var actualDataString = Compression.ReadLines(zipFilePath);
            var actualData = new List<FxcmVolume>();
            foreach (var line in actualDataString)
            {
                var obs = line.Split(',');
                var time = DateTime.ParseExact(obs[0], "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
                actualData.Add(new FxcmVolume
                {
                    DataType = MarketDataType.Base,
                    Symbol = _symbol,
                    Time = time,
                    Value = long.Parse(obs[1]),
                    Transactions = int.Parse(obs[2])
                });
            }
            return actualData.Concat(data);
        }

        private void WriteHourAndDailyData(IEnumerable<BaseData> data)
        {
            var sb = new StringBuilder();

            var volData = data.Cast<FxcmVolume>();
            foreach (var obs in volData)
            {
                sb.AppendLine(string.Format("{0:yyyyMMdd HH:mm},{1},{2}", obs.Time, obs.Value,
                                            obs.Transactions));
            }
            var data_to_save = sb.ToString();

            var filename = _symbol.Value.ToLower() + "_volume";
            var csvFilePath = Path.Combine(FolderPath, filename + ".csv");
            var zipFilePath = csvFilePath.Replace(".csv", ".zip");

            File.WriteAllText(csvFilePath, data_to_save);
            // Write out this data string to a zip file
            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
            Compression.Zip(csvFilePath, filename);
        }

        private void WriteMinuteData(IEnumerable<BaseData> data)
        {
            var sb = new StringBuilder();
            var volData = data.Cast<FxcmVolume>();
            var dataByDay = volData.GroupBy(o => o.Time.Date);
            foreach (var dayOfData in dataByDay)
            {
                foreach (var obs in dayOfData)
                {
                    sb.AppendLine(string.Format("{0},{1},{2}", obs.Time.TimeOfDay.TotalMilliseconds, obs.Value,
                                                obs.Transactions));
                }
                var filename = string.Format("{0:yyyyMMdd}_volume.csv", dayOfData.Key);
                var filePath = Path.Combine(FolderPath, filename);
                File.WriteAllText(filePath, sb.ToString());
                // Write out this data string to a zip file
                Compression.Zip(filePath, filename);
                sb.Clear();
            }
        }
    }
}