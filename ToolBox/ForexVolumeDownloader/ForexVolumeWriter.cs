using QuantConnect.Data.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.FxVolumeDownloader
{
    public class ForexVolumeWriter
    {
        private readonly Symbol _symbol;
        private readonly string _market;
        private readonly string _dataDirectory;
        private readonly Resolution _resolution;
        private readonly string _folderPath;

        public ForexVolumeWriter(Resolution resolution, Symbol symbol, string dataDirectory)
        {
            _symbol = symbol;
            _resolution = resolution;
            _dataDirectory = dataDirectory;
            _market = _symbol.ID.Market;
            _folderPath = Path.Combine(new[] {_dataDirectory, "base", _market.ToLower(), _resolution.ToString().ToLower()});
        }

        public void Write(IEnumerable<BaseData> data)
        {
            if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
            if (_resolution == Resolution.Minute)
            {
            }
            else
            {
                WriteHourAndDailyData(data, _folderPath);
            }
        }

        private void WriteHourAndDailyData(IEnumerable<BaseData> data, string folderPath)
        {
            var sb = new StringBuilder();

            var volData = data.Cast<ForexVolume>();
            foreach (var obs in volData)
            {
                sb.AppendLine(string.Format("{0:yyyyMMdd HH:mm},{1},{2}", obs.Time, obs.Value,
                    obs.Transactions));
            }

            var filename = _symbol.Value.ToLower() + ".csv";
            var filePath = Path.Combine(folderPath, filename);
            File.WriteAllText(filePath, sb.ToString());
            // Write out this data string to a zip file
            Compression.Zip(filePath, filename);
        }
    }
}