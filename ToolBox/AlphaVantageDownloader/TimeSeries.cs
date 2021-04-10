using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    internal class TimeSeries
    {
        [Name("time", "timestamp")]
        public DateTime Time { get; set; }

        [Name("open")]
        public decimal Open { get; set; }

        [Name("high")]
        public decimal High { get; set; }

        [Name("low")]
        public decimal Low { get; set; }

        [Name("close")]
        public decimal Close { get; set; }

        [Name("volume")]
        public decimal Volume { get; set; }
    }
}
