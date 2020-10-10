using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.EODConverter
{
    public class EODConverterProgram
    {
        internal class Row
        {
            [Index(0)]
            public string Date { get; set; }
            [Index(1)]
            public Decimal Open { get; set; }
            [Index(2)]
            public Decimal High { get; set; }
            [Index(3)]
            public Decimal Low { get; set; }
            [Index(4)]
            public Decimal Close { get; set; }
            [Index(5)]
            public Decimal Volume { get; set; }
        }

        public static void EODConverter(string sourceDir, string destinationDir)
        {
            Market.Add("cad", 100);

            try 
            {
                foreach (var filename in Directory.GetFiles(sourceDir, "*.csv"))
                {
                    var ticker = Path.GetFileNameWithoutExtension(filename);

                    using (var reader = new StreamReader(filename))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Configuration.HasHeaderRecord = false;
                        var records = csv.GetRecords<Row>();

                        var symbol = Symbol.Create(ticker, SecurityType.Equity, "cad");

                        var bars =
                            records.Select(r =>
                                new TradeBar(
                                    DateTime.ParseExact(r.Date, "yyyyMMdd HH:mm", CultureInfo.InvariantCulture),
                                    symbol,
                                    r.Open,
                                    r.High,
                                    r.Low,
                                    r.Close,
                                    r.Volume));

                        var writer = new LeanDataWriter(Resolution.Daily, symbol, destinationDir);
                        writer.Write(bars);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
