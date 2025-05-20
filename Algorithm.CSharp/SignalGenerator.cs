using System;
using System.Collections.Generic;
using System.Globalization;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class SignalGenerator : ISignalGenerator
    {
        private readonly Dictionary<Symbol, MacdAnalysis> _macdAnalysis;

        public SignalGenerator(Dictionary<Symbol, MacdAnalysis> macdAnalysis)
        {
            _macdAnalysis = macdAnalysis;
        }
        public  DateTime ParseShanghaiTime(string dateString)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));
            }
            catch (NullReferenceException ex)
            {
                System.Console.WriteLine($"解析上海时间时发生空引用异常: {ex.Message}");
                return DateTime.MinValue;
            }
        }
        public IEnumerable<TradingSignal> GenerateSignals(Slice data)
        {
            var signals = new List<TradingSignal>();
            if (data == null) return signals;

            foreach (var symbol in _macdAnalysis.Keys)
            {
                if (!data.ContainsKey(symbol)) continue;
                var currentData = data[symbol];
                if (currentData == null) continue;
                try
                {
                    var time = ParseShanghaiTime(currentData.Date);
                    var closePrice = currentData.Close;

                    var macdAnalysis = _macdAnalysis[symbol];
                    if (macdAnalysis != null && macdAnalysis.Macd.IsReady && macdAnalysis.CloseIdentity.IsReady)
                    {
                        System.Console.WriteLine($"{macdAnalysis.Name},{macdAnalysis.Industry} 时间: {time}, 收盘价: {closePrice}, MACD: {macdAnalysis.Macd.Current.Value}, 收盘价: {macdAnalysis.CloseIdentity.Current.Value}, " +
                            $"{(macdAnalysis.IsGoldenCross ? "金叉" : "false")},  {(macdAnalysis.IsDeathCross ? "死叉" : "false")}, " +
                            $"{(macdAnalysis.IsBullishDivergence ? "底背离" : "false")}, {(macdAnalysis.IsBearishDivergence ? "顶背离" : "false")}, " +
                            $"{(macdAnalysis.IsReversal ? "反转" : "false")}, {(macdAnalysis.IsTrend ? "趋势" : "false")}, " +
                            $"K线收益率: {macdAnalysis.KLineReturn}, 20日收益率分位数: {macdAnalysis.TwentyDayReturnQuantile}");
                            
                            if (macdAnalysis.IsGoldenCross)
                            {
                                signals.Add(new TradingSignal {
                                    Symbol = symbol,
                                    Type = SignalType.Buy,
                                    SuggestedPrice = data[symbol].Close,
                                    SignalTime = data[symbol].EndTime
                                });
                            }
                    }
                    else
                    {
                        System.Console.WriteLine($"时间: {time}, 收盘价: {closePrice}, MACD指标或收盘价指标数据尚未准备好");
                    }
                }
                catch (NullReferenceException ex)
                {
                    System.Console.WriteLine($"OnData方法中发生空引用异常: {ex.Message}");
                }
            }
            return signals;
        }
    }
}