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

            // First RSI: Updates at market close (4 PM) by default  
            // If DailyPreciseEndTime is false, updates at midnight (12:00 AM)
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

            SetupFirstIndicatorUpdatedHandler();
            SetupSecondIndicatorUpdatedHandler();
        }


        /// <summary>
        /// Event handler for the first RSI indicator
        /// Validates update timing and sample consistency
        /// </summary>
        protected virtual void SetupFirstIndicatorUpdatedHandler()
        {
            RelativeStrengthIndex1.Updated += (sender, data) =>
            {
                var updatedTime = Time;

                // Ensure RSI1 updates exactly at market close (4 PM)
                if (!(updatedTime.Hour == 16 && updatedTime.Minute == 0 && updatedTime.Second == 0))
                {
                    throw new RegressionTestException($"RSI1 must have updated at 4 PM, but it updated at {updatedTime}.");
                }

                // Since RSI1 updates before RSI2, it should have one extra sample
                if (RelativeStrengthIndex1.Samples - 1 != RelativeStrengthIndex2.Samples)
                {
                    throw new RegressionTestException("First RSI indicator should have exactly one more sample than the second indicator.");
                }

                // RSI1's previous value should match RSI2's current value, ensuring consistency
                if (RelativeStrengthIndex1.Previous.Value != RelativeStrengthIndex2.Current.Value)
                {
                    throw new RegressionTestException("RSI1 and RSI2 must have same value");
                }

                // RSI1's and RSI2's current values should be different
                if (RelativeStrengthIndex1.Current.Value == RelativeStrengthIndex2.Current.Value)
                {
                    throw new RegressionTestException("RSI1 and RSI2 must have different values");
                }
            };
        }

        /// <summary>
        /// Event handler for the second RSI indicator
        /// Validates update timing and sample consistency
        /// </summary>
        protected virtual void SetupSecondIndicatorUpdatedHandler()
        {
            RelativeStrengthIndex2.Updated += (sender, data) =>
            {
                var updatedTime = Time;

                // RSI2 updates at midnight, ensure the update time is correct
                if (!(updatedTime.Hour == 0 && updatedTime.Minute == 0 && updatedTime.Second == 0))
                {
                    throw new RegressionTestException($"RSI2 must have updated at midnight, but it was updated at {updatedTime}");
                }

                // Since RSI2 updates later, it must now have the same number of samples as RSI1
                if (RelativeStrengthIndex1.Samples != RelativeStrengthIndex2.Samples)
                {
                    throw new RegressionTestException("RSI1 must have same number of samples as RSI2");
                }

                // At this point, RSI1 and RSI2 should have the same value
                if (RelativeStrengthIndex1.Current.Value != RelativeStrengthIndex2.Current.Value)
                {
                    throw new RegressionTestException("RSI1 and RSI2 must have same value");
                }
            };
        }

        protected virtual void SetDailyPreciseEndTime()
        {
            Settings.DailyPreciseEndTime = true;
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
