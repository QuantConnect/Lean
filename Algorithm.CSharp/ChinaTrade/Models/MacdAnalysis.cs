using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;
using QuantConnect.Indicators;
using System.Xml.Linq;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class MacdAnalysis
    {
        public string Name { get; private set; }
        public string Industry { get; private set; } 
        // 记录买入时间
        public DateTime EntryTimeBuy { get; private set; }
        // 记录买入价格
        public decimal EntryPriceBuy { get; private set; }
        // 记录卖出时间
        public DateTime EntryTimeSell { get; private set; }
        // 记录卖出价格
        public decimal EntryPriceSell { get; private set; }

        // 新增方法用于记录买入信息
        public void RecordBuyEntry(DateTime time, decimal price)
        {
            EntryTimeBuy = time;
            EntryPriceBuy = price;
        }
        // 新增方法用于记录卖出信息
        public void RecordSellEntry(DateTime time, decimal price)
        {
            EntryTimeSell = time;
            EntryPriceSell = price;
        }
        public MovingAverageConvergenceDivergence Macd { get; }
        public IndicatorBase<IndicatorDataPoint> CloseIdentity { get; }

        // 0轴下方置信度金叉
        public bool IsLowerGoldenCross { get; private set; }
        // 0轴下方置信度死叉
        public bool IsLowerDeathCross { get; private set; }

        // 正常金叉（0轴）
        public bool IsGoldenCross { get; private set; }
        // 正常死叉（0轴）
        public bool IsDeathCross { get; private set; }

        // 0轴上方置信度金叉
        public bool IsUpperGoldenCross { get; private set; }
        // 0轴上方置信度死叉
        public bool IsUpperDeathCross { get; private set; }

        public bool IsBullishDivergence { get; private set; }
        public bool IsBearishDivergence { get; private set; }

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
                // 设置置信度
                const decimal tolerance = 0.0025m;
                decimal fast = Macd.Fast;
                decimal delta = (Macd.Current.Value - Macd.Signal.Current.Value) / (fast != 0 ? fast : 1);
                bool isSignificant = Math.Abs(fast) > 0.0001m;
                decimal prevDelta = Macd[1] != null && Macd.Signal[1] != null ? (Macd[1].Value - Macd.Signal[1].Value) / (fast != 0 ? fast : 1) : 0;

                // 检测金叉：MACD从下向上穿越信号线 ，0轴上方
                IsUpperGoldenCross = Macd.Samples > 1 &&
                                isSignificant &&
                                delta > tolerance &&
                                prevDelta <= tolerance
                                ;

                // 检测死叉：MACD从上向下穿越信号线 ，0轴上方
                IsUpperDeathCross = Macd.Samples > 1 &&
                                isSignificant &&
                                delta < tolerance &&
                                prevDelta >= tolerance
                                ;

                // 检测金叉：MACD从下向上穿越信号线 ，0轴下方
                IsLowerGoldenCross = Macd.Samples > 1 &&
                                isSignificant &&
                                delta > -tolerance &&
                                prevDelta <= -tolerance
                                ;
                // 检测死叉：MACD从上向下穿越信号线 ，0轴下方
                IsLowerDeathCross = Macd.Samples > 1 &&
                                isSignificant &&
                                delta < -tolerance &&
                                prevDelta >= -tolerance
                                ;

                // 检测金叉：MACD从下向上穿越信号线
                IsGoldenCross = Macd.Samples > 1 &&
                                Macd.Current.Value > Macd.Signal.Current.Value &&
                                Macd[1] != null && Macd.Signal[1] != null &&
                                Macd[1].Value <= Macd.Signal[1].Value;

                // 检测死叉：MACD从上向下穿越信号线
                IsDeathCross = Macd.Samples > 1 &&
                               Macd.Current.Value < Macd.Signal.Current.Value &&
                               Macd[1] != null && Macd.Signal[1] != null &&
                               Macd[1].Value >= Macd.Signal[1].Value;
                

                // 检测顶背离：价格创新高但MACD未创新高
                IsBearishDivergence = false;
                if (CloseIdentity.Samples > 2 && Macd.Samples > 2)
                {
                    // 增加空值判断，防止 NullReferenceException
                    var prevHigh = 0m;
                    if (CloseIdentity.Samples > 2 && CloseIdentity[1] != null && CloseIdentity[2] != null)
                    {
                        prevHigh = Math.Max(CloseIdentity[1].Value, CloseIdentity[2].Value);
                    }
                    var prevMacdHigh = 0m;
                    if (Macd.Samples > 2 && Macd[1] != null && Macd[2] != null)
                    {
                        prevMacdHigh = Math.Max(Macd[1].Value, Macd[2].Value);
                    }
                    if (closePrice > prevHigh && macdValue < prevMacdHigh)
                    {
                        IsBearishDivergence = true;
                    }
                }
                // 检测底背离：价格创新低但MACD未创新低
                IsBullishDivergence = false;
                if (CloseIdentity.Samples > 2 && Macd.Samples > 2)
                {
                    decimal? prevLow = null, prevMacdLow = null;
                    if (CloseIdentity[1] != null && CloseIdentity[2] != null)
                        prevLow = Math.Min(CloseIdentity[1].Value, CloseIdentity[2].Value);
                    if (Macd[1] != null && Macd[2] != null)
                        prevMacdLow = Math.Min(Macd[1].Value, Macd[2].Value);
                    if (CloseIdentity[1] != null && CloseIdentity[2] != null &&
                        Macd[1] != null && Macd[2] != null &&
                        prevLow.HasValue && prevMacdLow.HasValue &&
                        closePrice < prevLow && macdValue > prevMacdLow)
                    {
                        IsBullishDivergence = true;
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"MacdAnalysis.UpdateStatus方法中发生空引用异常: {ex.Message}");
            }
        }
    }
}
