using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class MacdAnalysis
    {
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

        public MacdAnalysis(MovingAverageConvergenceDivergence macd, IndicatorBase<IndicatorDataPoint> closeIdentity)
        {
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
                var macdValue = Macd.Current.Value;
                var closePrice = CloseIdentity.Current.Value;
                var previousClosePrice = CloseIdentity.Samples > 1 ? CloseIdentity[1].Value : 0;
                var previousMacdValue = Macd.Samples > 1 ? Macd[1].Value : 0;
                // 计算K线收益率
                KLineReturn = previousClosePrice != 0 ? (closePrice - previousClosePrice) / previousClosePrice : 0;
            
                // 计算20日收益率分位数
                if (CloseIdentity.Samples >= 20)
                {
                    var returns = new List<decimal>();
                    // 从最近的样本开始，取20个样本计算收益率
                    for (int i = 0; i < 19; i++)
                    {
                        var current = CloseIdentity[i].Value;
                        var prev = CloseIdentity[i + 1].Value;
                        if (prev != 0)
                        {
                            returns.Add((current - prev) / prev);
                        }
                    }
                    if (returns.Count > 0)
                    {
                        // 对收益率列表进行排序
                        var sortedReturns = returns.OrderBy(x => x).ToList();
                        // 取当前收益率
                        var denominator = CloseIdentity[1].Value;
                        var currentReturn = denominator != 0 ? CloseIdentity[0].Value / denominator - 1 : 0;
                        int count = sortedReturns.Count(x => x < currentReturn);
                        int equal = sortedReturns.Count(x => x == currentReturn);
                        // 采用中位分位数算法
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
                // 处理空引用异常，可根据实际情况添加日志记录
                Console.WriteLine($"MacdAnalysis.UpdateStatus方法中发生空引用异常: {ex.Message}");
            }
        }
    }
}