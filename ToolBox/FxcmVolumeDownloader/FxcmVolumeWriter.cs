using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QuantConnect.Data;
using QuantConnect.Data.Custom;

namespace QuantConnect.ToolBox
{
    public class FxcmVolumeWriter
    {
        #region Fields

        private readonly Resolution _resolution;
        private readonly Symbol _symbol;

        #endregion

        public string FolderPath { get; }

        public FxcmVolumeWriter(Resolution resolution, Symbol symbol, string dataDirectory)
        {
            _symbol = symbol;
            _resolution = resolution;
            var market = _symbol.ID.Market;
            FolderPath =
                Path.Combine(new[] {dataDirectory, "forex", market.ToLowerInvariant(), _resolution.ToLower()});
            if (_resolution == Resolution.Minute)
            {
                FolderPath = Path.Combine(FolderPath, _symbol.Value.ToLowerInvariant());
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

        #region Private Methods

        private IEnumerable<BaseData> ReadActualDataAndAppendNewData(IEnumerable<BaseData> data)
        {
            // Read the actual data
            var zipFilePath = Path.Combine(FolderPath, _symbol.Value.ToLowerInvariant() + "_volume.zip");
            var actualData = FxcmVolumeAuxiliaryMethods.GetFxcmVolumeFromZip(zipFilePath);
            return actualData.Concat(data);
        }

        private void WriteHourAndDailyData(IEnumerable<BaseData> data)
        {
            var sb = new StringBuilder();

            var volData = data.Cast<FxcmVolume>();
            foreach (var obs in volData)
            {
                sb.AppendLine($"{obs.Time.ToStringInvariant("yyyyMMdd HH:mm")},{obs.Value.ToStringInvariant()},{obs.Transactions}");
            }
            var data_to_save = sb.ToString();

            var filename = _symbol.Value.ToLowerInvariant() + "_volume";
            var csvFilePath = Path.Combine(FolderPath, filename + ".csv");
            var zipFilePath = csvFilePath.Replace(".csv", ".zip");

            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
            File.WriteAllText(csvFilePath, data_to_save);
            // Write out this data string to a zip file
            Compression.Zip(csvFilePath, Path.GetFileName(csvFilePath));
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
                    sb.AppendLine($"{obs.Time.TimeOfDay.TotalMilliseconds.ToStringInvariant()},{obs.Value.ToStringInvariant()},{obs.Transactions}");
                }
                var filename = $"{dayOfData.Key.ToStringInvariant("yyyyMMdd")}_volume.csv";
                var filePath = Path.Combine(FolderPath, filename);
                File.WriteAllText(filePath, sb.ToString());
                // Write out this data string to a zip file
                Compression.Zip(filePath, filename);
                sb.Clear();
            }
        }

        #endregion
    }
}