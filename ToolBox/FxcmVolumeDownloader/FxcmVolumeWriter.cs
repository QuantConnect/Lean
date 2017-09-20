using System;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuantConnect.ToolBox
{
    public class FxcmVolumeWriter
    {
        private readonly Symbol _symbol;
        private readonly string _market;
        private readonly string _dataDirectory;
        private readonly Resolution _resolution;

        public FxcmVolumeWriter(Resolution resolution, Symbol symbol, string dataDirectory)
        {
            _symbol = symbol;
            _resolution = resolution;
            _dataDirectory = dataDirectory;
            _market = _symbol.ID.Market;
            FolderPath = Path.Combine(new[] { _dataDirectory, "forex", _market.ToLower(), _resolution.ToString().ToLower() });
            if (_resolution == Resolution.Minute)
            {
                FolderPath = Path.Combine(FolderPath, _symbol.Value.ToLower());
            }
        }

        public string FolderPath { get; }

        public void Write(IEnumerable<BaseData> data, bool update=false)
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
            // TODO: read the file here merge and check for duplicates
            data = CheckForDuplicatesObservations(data);
            if (_resolution == Resolution.Minute)
            {
                WriteMinuteData(data);
            }
            else
            {
                WriteHourAndDailyData(data, update);
            }
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

        private void WriteHourAndDailyData(IEnumerable<BaseData> data, bool update = false)
        {
            var sb = new StringBuilder();

            var volData = data.Cast<FxcmVolume>();
            foreach (var obs in volData)
            {
                sb.AppendLine(string.Format("{0:yyyyMMdd HH:mm},{1},{2}", obs.Time, obs.Value,
                    obs.Transactions));
            }

            var data_to_save = string.Empty;
            var filename = _symbol.Value.ToLower() + "_volume";
            var csvFilePath = Path.Combine(FolderPath, filename + ".csv");

            if (update)
            {
                var zipFilePath = Path.Combine(FolderPath, filename + ".zip");
                using (var sr = Compression.UnzipStreamToStreamReader(File.OpenRead(zipFilePath)))
                {
                    data_to_save = sr.ReadToEnd() + sb;
                }
            }
            else
            {
                {
                    data_to_save = sb.ToString();
                }
            }

            File.WriteAllText(csvFilePath, data_to_save);
            // Write out this data string to a zip file
            Compression.Zip(csvFilePath, filename);

        }

        private IEnumerable<BaseData> CheckForDuplicatesObservations(IEnumerable<BaseData> data)
        {
            // Seems the data has some duplicate values! This makes the writer throws an error.
            var duplicates = data.GroupBy(x => x.Time)
                                 .Where(g => g.Count() > 1)
                                 .Select(g => g.Key)
                                 .ToList();
            if (duplicates.Count != 0)
            {
                var cleanData = data.ToList();
                foreach (var duplicate in duplicates)
                {
                    cleanData.RemoveAll(o => o.Time == duplicate);
                }
                return cleanData;
            }
            return data;
        }
    }
}