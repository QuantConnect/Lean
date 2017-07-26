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

        public ForexVolumeWriter(Resolution resolution, Symbol symbol, string dataDirectory)
        {
            _symbol = symbol;
            _resolution = resolution;
            _dataDirectory = dataDirectory;
            _market = _symbol.ID.Market;
        }

        public void Write(IEnumerable<BaseData> data)
        {
            //var sb = new StringBuilder("DateTime,Volume,Transactions\n");
            var sb = new StringBuilder();

            var volData = data.Cast<ForexVolume>();
            foreach (var obs in volData)
            {
                sb.AppendLine(string.Format("{0:yyyyMMdd HH:mm},{1},{2}", obs.Time, obs.Value,
                    obs.Transactions));
            }
            // base/fxcmforexvolume/daily/eurusd.zip
            var folderPath = Path.Combine(new[] {_dataDirectory, "base", _market.ToLower(), _resolution.ToString().ToLower()});
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var filename = _symbol.Value.ToLower() + ".csv";
            var filePath = Path.Combine(folderPath, filename);
            File.WriteAllText(filePath, sb.ToString());
            // Write out this data string to a zip file
            Compression.Zip(filePath, filename);
        }
    }
}