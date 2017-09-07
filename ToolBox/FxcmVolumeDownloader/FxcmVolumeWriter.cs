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
        }

        public string FolderPath
        {
            get
            {
                var folderPath = Path.Combine(new[] { _dataDirectory, "forex", _market.ToLower(), _resolution.ToString().ToLower() });
                if (_resolution == Resolution.Minute)
                {
                    folderPath = Path.Combine(FolderPath, _symbol.Value.ToLower());
                }
                return folderPath;
            }
            
        }

        public void Write(IEnumerable<BaseData> data)
        {
            if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
            if (_resolution == Resolution.Minute)
            {
                WriteMinuteData(data);
            }
            else
            {
                WriteHourAndDailyData(data);
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

        private void WriteHourAndDailyData(IEnumerable<BaseData> data, bool update=false)
        {
            var sb = new StringBuilder();

            var volData = data.Cast<FxcmVolume>();
            foreach (var obs in volData)
            {
                sb.AppendLine(string.Format("{0:yyyyMMdd HH:mm},{1},{2}", obs.Time, obs.Value,
                    obs.Transactions));
            }

            var filename = _symbol.Value.ToLower() + "_volume.csv";
            var filePath = Path.Combine(FolderPath, filename);
            File.WriteAllText(filePath, sb.ToString());
            // Write out this data string to a zip file
            Compression.Zip(filePath, filename);
        }
    }
}