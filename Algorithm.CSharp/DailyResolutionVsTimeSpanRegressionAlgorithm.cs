using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests the behavior of indicators with different update mechanisms based on resolution and time span.
    /// </summary>
    public class DailyResolutionVsTimeSpanRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        protected RelativeStrengthIndex RelativeStrengthIndex1;
        protected RelativeStrengthIndex RelativeStrengthIndex2;
        private bool _dataPointsReceived;

        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);
            SetEndDate(2013, 01, 5);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;

            SetDailyPreciseEndTime();

            // First RSI: Updates at market close (4 PM) using daily resolution (9:30 AM to 4 PM)
            RelativeStrengthIndex1 = new RelativeStrengthIndex(14, MovingAverageType.Wilders);
            RegisterIndicator(_spy, RelativeStrengthIndex1, Resolution.Daily);

            // Second RSI: Updates every 24 hours (from 12:00 AM to 12:00 AM) using a time span
            RelativeStrengthIndex2 = new RelativeStrengthIndex(14, MovingAverageType.Wilders);
            RegisterIndicator(_spy, RelativeStrengthIndex2, TimeSpan.FromDays(1));

            // Warm up indicators with historical data
            var history = History<TradeBar>(_spy, 20, Resolution.Daily).ToList();
            foreach (var bar in history)
            {
                RelativeStrengthIndex1.Update(bar.EndTime, bar.Close);
                RelativeStrengthIndex2.Update(bar.EndTime, bar.Close);
            }
            if (!RelativeStrengthIndex1.IsReady || !RelativeStrengthIndex2.IsReady)
            {
                throw new RegressionTestException("Indicators not ready.");
            }
            // During market hours, both RSI values should be equal because neither has been updated 
            Schedule.On(DateRules.EveryDay(), TimeRules.At(12, 0, 0), CompareValuesDuringMarketHours);

            // After market hours, the first RSI should have updated at 4 PM, so the values should differ.
            Schedule.On(DateRules.EveryDay(), TimeRules.At(17, 0, 0), CompareValuesAfterMarketHours);
        }

        protected virtual void SetDailyPreciseEndTime()
        {
            Settings.DailyPreciseEndTime = true;
        }

        protected virtual void CompareValuesDuringMarketHours()
        {
            var value1 = RelativeStrengthIndex1.Current.Value;
            var value2 = RelativeStrengthIndex2.Current.Value;
            if (value1 != value2)
            {
                throw new RegressionTestException("The values must be equal during market hours");
            }
        }

        protected virtual void CompareValuesAfterMarketHours()
        {
            var value1 = RelativeStrengthIndex1.Current.Value;
            var value2 = RelativeStrengthIndex2.Current.Value;

            if (value1 == value2 && _dataPointsReceived == true)
            {
                throw new RegressionTestException("The values must be different after market hours");
            }
        }

        public override void OnData(Slice slice)
        {
            if (slice.ContainsKey(_spy))
            {
                _dataPointsReceived = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_dataPointsReceived)
            {
                throw new RegressionTestException("No data points received");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 50;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 20;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-38.725"},
            {"Tracking Error", "0.232"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
