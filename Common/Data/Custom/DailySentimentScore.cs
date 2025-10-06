using System;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Custom
{
    public class DailySentimentScore : BaseData
    {
        public decimal Score { get; set; }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var file = $"alternative/sentiment/daily/{config.Symbol.Value.ToLowerInvariant()}.csv";
            var path = Globals.DataFolder.NormalizePath() + "/" + file;
            return new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("date")) return null;
            var parts = line.Split(',');
            var time = DateTime.Parse(parts[0]).Date;
            var score = parts.Length > 1 ? parts[1].ToDecimal() : 0m;

            return new DailySentimentScore
            {
                Symbol = config.Symbol,
                Time = time,
                EndTime = time.AddDays(1),
                Value = score,
                Score = score
            };
        }
    }
}

