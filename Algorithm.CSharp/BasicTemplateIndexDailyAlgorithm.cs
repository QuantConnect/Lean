using System;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression for running an Index algorithm with Daily data
    /// </summary>
    public class BasicTemplateIndexDailyAlgorithm : BasicTemplateIndexAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;
        protected override int StartDay => 1;

        // two complete weeks starting from the 5th. The 18th bar is not included since it is a holiday
        protected virtual int ExpectedBarCount => 2 * 5;
        protected int BarCounter = 0;

        /// <summary>
        /// Purchase a contract when we are not invested, liquidate otherwise
        /// </summary>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // SPX Index is not tradable, but we can trade an option
                MarketOrder(SpxOption, 1);
            }
            else
            {
                Liquidate();
            }

            // Count how many slices we receive with SPX data in it to assert later
            if (slice.ContainsKey(Spx))
            {
                BarCounter++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (BarCounter != ExpectedBarCount)
            {
                throw new ArgumentException($"Bar Count {BarCounter} is not expected count of {ExpectedBarCount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 121;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "8"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "617.546%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "8.460%"},
            {"Sharpe Ratio", "9.015"},
            {"Probabilistic Sharpe Ratio", "93.474%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "3.218"},
            {"Beta", "-1.013"},
            {"Annual Standard Deviation", "0.347"},
            {"Annual Variance", "0.12"},
            {"Information Ratio", "7.709"},
            {"Tracking Error", "0.394"},
            {"Treynor Ratio", "-3.087"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Fitness Score", "0.03"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.03"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "dfb226b675ed498ecb67a89483a70da2"}
        };
    }
}
