using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class ApiDayCustomData : BaseData
    {
        [JsonProperty("Date")]
        public string Date;

        [JsonProperty("Open")]
        public decimal Open;

        [JsonProperty("Close")]
        public decimal Close;

        [JsonProperty("High")]
        public decimal High;

        [JsonProperty("Low")]
        public decimal Low;

        [JsonProperty("Volume")] 
        public decimal Volume;

        [JsonProperty("Amount")]
        public decimal Amount;

        [JsonProperty("StrategyName")]
        public string StrategyName;

        public override DateTime EndTime { get; set; }
        public override SubscriptionDataSource GetSource(
            SubscriptionDataConfig config,
            DateTime date,
            bool isLiveMode)
        {
            if (isLiveMode)
            {
                return new SubscriptionDataSource(
                    $"http://43.142.139.247/api/dayapi/{config.Symbol.Value}",
                    SubscriptionTransportMedium.Rest);
            }
            // 返回的是一个csv，正常历史数据只用请求一次呀。
            return new SubscriptionDataSource(
                $"http://43.142.139.247/api/dayapi/csv/{config.Symbol.Value}",
                SubscriptionTransportMedium.RemoteFile);
        }

        public override BaseData Reader(
            SubscriptionDataConfig config,
            string line,
            DateTime date,
            bool isLiveMode)
        {
            if (isLiveMode)
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<ApiDayCustomData>(line);
                    var a = DateTime.ParseExact(data.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(config.ExchangeTimeZone.Id);
                    data.EndTime = TimeZoneInfo.ConvertTimeToUtc(a, timeZoneInfo);
                    data.Symbol = config.Symbol;
                    data.Value = data.Close;
                    return data;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            try
            {
                var csv = line.Split(',');
                var data = new ApiDayCustomData
                {
                    Date = csv[0],
                    Open = Convert.ToDecimal(csv[1]),
                    Close = Convert.ToDecimal(csv[2]),
                    High = Convert.ToDecimal(csv[3]),
                    Low = Convert.ToDecimal(csv[4]),
                    Volume = Convert.ToDecimal(csv[5]),
                    Amount = Convert.ToDecimal(csv[6]),
                    StrategyName = csv[7]
                };
                var a = DateTime.ParseExact(data.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(config.ExchangeTimeZone.Id);
                data.EndTime = TimeZoneInfo.ConvertTimeToUtc(a, timeZoneInfo);
                data.Symbol = config.Symbol;
                data.Value = data.Close;
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}