using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public class AddAndRemoveOptionContractRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contract;
        private bool hasRemoved = false;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Second;
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            AddNextFridaySpyOptionContract();
        }

        private void AddNextFridaySpyOptionContract()
        {
            DateTime today = DateTime.Today;
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
            DateTime nextFriday = today.AddDays(daysUntilFriday);

            _contract = QuantConnect.Symbol.CreateOption("SPY", Market.USA.ToString(), OptionStyle.American, OptionRight.Call, 450, nextFriday);

            AddOptionContract(_contract, Resolution.Second);
        }

        public override void OnData(Slice slice)
        {
            if (slice.HasData)
            {
                if (!hasRemoved)
                {
                    RemoveOptionContract(_contract);
                    hasRemoved = true;
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "2.73%"},
            {"Average Loss", "-2.98%"},
            {"Compounding Annual Return", "-4.619%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-0.042"},
            {"Net Profit", "-0.332%"},
            {"Sharpe Ratio", "-3.7"},
            {"Probabilistic Sharpe Ratio", "0.563%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.92"},
            {"Alpha", "-0.021"},
            {"Beta", "-0.011"},
            {"Annual Standard Deviation", "0.006"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.385"},
            {"Tracking Error", "0.058"},
            {"Treynor Ratio", "2.117"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$45000000.00"},
            {"Lowest Capacity Asset", "AOL R735QTJ8XC9X"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-43.418"},
            {"Return Over Maximum Drawdown", "-14.274"},
            {"Portfolio Turnover", "0.007"},
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
            {"OrderListHash", "486118a60d78f74811fe8d927c2c6b43"}
        };
    }
}
