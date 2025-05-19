using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;
using QuantConnect.Indicators;
using System.Xml.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    public class MacdAnalysis
    {
        public string Name { get; private set; }
        public string Industry { get; private set; }    
        public MovingAverageConvergenceDivergence Macd { get; }
        public IndicatorBase<IndicatorDataPoint> CloseIdentity { get; }
        public bool IsGoldenCross { get; private set; }
        public bool IsDeathCross { get; private set; }
        public bool IsBullishDivergence { get; private set; }
        public bool IsBearishDivergence { get; private set; }
        public bool IsReversal { get; set; }
        public bool IsTrend { get; set; }
        // 新增字段：K线收益率
        public decimal KLineReturn { get; private set; }
        // 新增字段：20日收益率分位数
        public decimal TwentyDayReturnQuantile { get; private set; }

        public MacdAnalysis(MovingAverageConvergenceDivergence macd, IndicatorBase<IndicatorDataPoint> closeIdentity,string name ,string industry)
        {
            Name = name;
            Industry = industry;
            Macd = macd;
            CloseIdentity = closeIdentity;

            // 订阅指标更新事件，当指标有新数据时自动更新状态
            Macd.Updated += OnMacdUpdated;
            CloseIdentity.Updated += OnCloseIdentityUpdated;

            // 初始化时尝试更新状态
            UpdateStatus();
        }

        private void OnMacdUpdated(object sender, IndicatorDataPoint updated)
        {
            UpdateStatus();
        }

        private void OnCloseIdentityUpdated(object sender, IndicatorDataPoint updated)
        {
            UpdateStatus();
        }
        private void UpdateStatus()
        {
            try
            {
                var macdValue = Macd.Current?.Value ?? 0;
                var closePrice = CloseIdentity.Current?.Value ?? 0;
                var previousClosePrice = CloseIdentity.Samples > 1 ? CloseIdentity[1]?.Value ?? 0 : 0;
                var previousMacdValue = Macd.Samples > 1 ? Macd[1]?.Value ?? 0 : 0;

                // 计算K线收益率  
                KLineReturn = previousClosePrice != 0 ? (closePrice - previousClosePrice) / previousClosePrice : 0;

                // 计算20日收益率分位数  
                if (CloseIdentity.Samples >= 20)
                {
                    var returns = new List<decimal>();
                    for (int i = 0; i < 19; i++)
                    {
                        var current = CloseIdentity[i]?.Value ?? 0;
                        var prev = CloseIdentity[i + 1]?.Value ?? 0;
                        if (prev != 0)
                        {
                            returns.Add((current - prev) / prev);
                        }
                    }
                    if (returns.Count > 0)
                    {
                        var sortedReturns = returns.OrderBy(x => x).ToList();
                        var denominator = CloseIdentity[1]?.Value ?? 0;
                        var currentReturn = denominator != 0 ? CloseIdentity[0]?.Value / denominator - 1 : 0;
                        int count = sortedReturns.Count(x => x < currentReturn);
                        int equal = sortedReturns.Count(x => x == currentReturn);
                        TwentyDayReturnQuantile = (count + 0.5m * equal) / sortedReturns.Count;
                    }
                    else
                    {
                        TwentyDayReturnQuantile = 0;
                    }
                }
                else
                {
                    TwentyDayReturnQuantile = 0;
                }

                // 检测金叉  
                IsGoldenCross = closePrice > previousClosePrice && macdValue > previousMacdValue;
                // 检测死叉  
                IsDeathCross = closePrice < previousClosePrice && macdValue < previousMacdValue;
                // 检测顶背离  
                IsBearishDivergence = closePrice > previousClosePrice && macdValue < previousMacdValue;
                // 检测底背离  
                IsBullishDivergence = closePrice < previousClosePrice && macdValue > previousMacdValue;
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"MacdAnalysis.UpdateStatus方法中发生空引用异常: {ex.Message}");
            }
        }
    }
}
