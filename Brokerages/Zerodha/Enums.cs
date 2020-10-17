using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Zerodha
{
   //public enum Segments
   // {
   //     Equity,
   //     Commodity,
   //     Futures,
   //     Currency
   // }

   // public enum TransactionTypes
   // {
   //     Buy,
   //     Sell
   // }

   // public enum OrderTypes
   // {
   //     MARKET,
   //     LIMIT,
   //     SL,
   //     SLM
   // }

   // public enum ProductTypes
   // {
   //     MIS,
   //     CNC,
   //     NRML
   // }

   // public enum VarietyTypes
   // {
   //     Regular,
   //     BO,
   //     CO,
   //     AMO
   // }

   // public enum TickerModes
   // {
   //     Full,
   //     Quote,
   //     LTP
   // }

   // public enum PositionTypes
   // {
   //     Day,
   //     Overnight
   // }

   // public enum ValidityTypes
   // {
   //     DAY,
   //     IOC,
   //     AMO
   // }

   // public enum Exchanges
   // {
   //     NSE,
   //     BSE,
   //     NFO,
   //     CDS,
   //     MCX
   // }

   // public enum CandleIntervals
   // {
   //     Minute,
   //     ThreeMinute,
   //     FiveMinute,
   //     TenMinute,
   //     FifteenMinute,
   //     ThirtyMinute,
   //     SixtyMinute,
   //     Day
   // }

   // public enum SIPFrequency
   // {
   //     Weekly,
   //     Monthly,
   //     Quarterly
   // }

   // public enum SIPStatus
   // {
   //     Active,
   //     Paused
   // }

   // public class Constants
   // {
   //     static Dictionary<string, List<string>> values = new Dictionary<string, List<string>>
   //     {
   //         [typeof(Segments).Name] = new List<string> { "equity", "commodity", "futures", "currency" },
   //         [typeof(TransactionTypes).Name] = new List<string> { "BUY", "SELL" },
   //         [typeof(OrderTypes).Name] = new List<string> { "MARKET", "LIMIT", "SL", "SL-M" },
   //         [typeof(ProductTypes).Name] = new List<string> { "MIS", "CNC", "NRML" },
   //         [typeof(VarietyTypes).Name] = new List<string> { "regular", "bo", "co", "amo" },
   //         [typeof(TickerModes).Name] = new List<string> { "full", "quote", "ltp" },
   //         [typeof(PositionTypes).Name] = new List<string> { "day", "overnight" },
   //         [typeof(ValidityTypes).Name] = new List<string> { "DAY", "IOC", "AMO" },
   //         [typeof(Exchanges).Name] = new List<string> { "NSE", "BSE", "NFO", "CDS", "MCX"},
   //         [typeof(CandleIntervals).Name] = new List<string> { "minute", "3minute", "5minute", "10minute", "15minute", "30minute", "60minute", "day" },
   //         [typeof(SIPFrequency).Name] = new List<string> { "weekly", "monthly", "quarterly" },
   //         [typeof(SIPStatus).Name] = new List<string> { "active", "paused" },
   //     };

   //     public static T ToEnum<T>(string value)
   //     {
   //         return (T)(object) values[typeof(T).Name].IndexOf(value);
   //     }

   //     public static string ToValue<T>(T enumValue)
   //     {
   //         if(enumValue == null)
   //             return "";
   //         var index = Enum.GetNames(typeof(T)).ToList().IndexOf(enumValue.ToString());
   //         return values[typeof(T).Name][index];
   //     }
    //}
}
