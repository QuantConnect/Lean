using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class AStockAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private MacdAnalysis _macdAnalysis;
        public override void Initialize()
        {
            SetStartDate(2024, 1, 1);
            SetEndDate(2025, 12, 31);
            // 设置基准货币为人民币
            SetAccountCurrency("CNY"); 
            // 初始化CNY现金账户（假设初始金额为10万）
            SetCash("CNY", 10000000); 
            SetTimeZone(TimeZones.Utc);
            // 设置手续费模型
            SetBrokerageModel(new AStockBrokerageModel());

            var benchmarkSymbol = AddData<ApiDayCustomData>("sh.000001", Resolution.Daily, TimeZones.Utc).Symbol;
            SetBenchmark(benchmarkSymbol);

            _symbol = AddData<Api5MinCustomData>("sz.000001",Resolution.Minute, TimeZones.Utc).Symbol;

            var macd = MACD(_symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Minute);
            var closeIdentity = Identity(_symbol, Resolution.Minute, x => ((Api5MinCustomData)x).Close);
            
            // 无论是否LiveMode都进行预热
            WarmUpIndicators(macd, closeIdentity);
            _macdAnalysis = new MacdAnalysis(macd, closeIdentity);
        }
        private void WarmUpIndicators(MovingAverageConvergenceDivergence macd, IndicatorBase<IndicatorDataPoint> closeIdentity)
        {
            // 计算MACD所需最小数据量(26周期+9信号线)
            var requiredBars = 12600 + 9;
            var history = History<Api5MinCustomData>(_symbol, requiredBars * 2, Resolution.Minute);
            
            if (history == null || !history.Any())
            {
                Debug("无法获取历史数据用于预热");
                return;
            }
            
            Debug($"获取到 {history.Count()} 条历史数据用于预热");
            
            foreach (var bar in history.OrderBy(x => x.Time))
            {
                macd.Update(bar.Time, bar.Close);
                if (bar is Api5MinCustomData customData)
                {
                    closeIdentity.Update(bar.EndTime, customData.Close);
                }
            }
            
            // 只在循环外创建一次实例
            _macdAnalysis = new MacdAnalysis(macd, closeIdentity);
            Debug($"预热完成 - MACD.IsReady: {macd.IsReady}, CloseIdentity.IsReady: {closeIdentity.IsReady}");
        }
        public override void OnData(Slice data)
        {
            if (data == null || !data.ContainsKey(_symbol)) return;
            var currentData = data[_symbol];
            if (currentData == null) return;
            try
            {
                var time = ParseShanghaiTime(currentData.Date);
                var closePrice = currentData.Close;
                // 检查是否是第20条数据，若不是则按原逻辑输出日志
                if (_macdAnalysis != null && _macdAnalysis.Macd.IsReady && _macdAnalysis.CloseIdentity.IsReady)
                {
                    Log($"时间: {time}, 收盘价: {closePrice}, MACD: {_macdAnalysis.Macd.Current.Value}, 收盘价: {_macdAnalysis.CloseIdentity.Current.Value}, " +
                        $"{(_macdAnalysis.IsGoldenCross ? "金叉" : "false")},  {(_macdAnalysis.IsDeathCross ? "死叉" : "false")}, " +
                        $"{(_macdAnalysis.IsBullishDivergence ? "底背离" : "false")}, {(_macdAnalysis.IsBearishDivergence ? "顶背离" : "false")}, " +
                        $"{(_macdAnalysis.IsReversal ? "反转" : "false")}, {(_macdAnalysis.IsTrend ? "趋势" : "false")}, " +
                    $"K线收益率: {_macdAnalysis.KLineReturn}, 20日收益率分位数: {_macdAnalysis.TwentyDayReturnQuantile}");
                }
                else
                {
                    Log($"时间: {time}, 收盘价: {closePrice}, MACD指标或收盘价指标数据尚未准备好");
                }
            }
            catch (NullReferenceException ex)
            {
                Log($"OnData方法中发生空引用异常: {ex.Message}");
            }
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
                Log($"解析上海时间时发生空引用异常: {ex.Message}");
                return DateTime.MinValue;
            }
        }
    }
}