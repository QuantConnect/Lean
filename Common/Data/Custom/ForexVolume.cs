using System;
using System.Globalization;
using NodaTime;
using QuantConnect.Util;

namespace QuantConnect.Data.Custom
{
    public class ForexVolume : BaseData
    {
        public int Transanctions { get; set; }

        /// <summary>
        ///     Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     String URL of source file.
        /// </returns>
        /// <exception cref="System.NotImplementedException">FOREX Volume data is not available in live mode, yet.</exception>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode) throw new NotImplementedException("FOREX Volume data is not available in live mode, yet.");
            var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, SecurityType.Base, "FXCMForexVolume",
                date, config.Resolution);
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            DateTime time;
            var obs = line.Split(',');
            if (config.Resolution == Resolution.Minute)
            {
                time = date.Date.AddMilliseconds(int.Parse(obs[0]));
            }
            else
            {
                time = DateTime.ParseExact(obs[0], "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
            }
            return new ForexVolume
            {
                DataType = MarketDataType.ForexVolume,
                Symbol = config.Symbol,
                Time = time,
                Value = long.Parse(obs[1]),
                Transanctions = int.Parse(obs[2])
            };
        }
    }
}