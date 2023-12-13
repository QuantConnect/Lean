using QuantConnect.Data;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Asserts we can use a C# function as a FuncRiskFreeRateInterestRateModel
    /// </summary>
    public class FuncRiskFreeRateInterestRateModelWithPythonLambda: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        FuncRiskFreeRateInterestRateModel _model;

        public override void Initialize()
        {
            SetStartDate(2020, 5, 28);
            SetEndDate(2020, 6, 28);

            AddEquity("SPY", Resolution.Daily);
            _model = new FuncRiskFreeRateInterestRateModel(dt => dt.Date == new DateTime(2020, 5, 28) ? 0 : 1);
        }

        public override void OnData(Slice slice)
        {
            if (Time.Date == (new DateTime(2020, 5, 28)) && _model.GetInterestRate(Time) != 0)
            {
                throw new Exception($"Risk Free interest rate should be 0, but was {_model.GetInterestRate(Time)}");
            }else if (Time.Date != (new DateTime(2020, 5, 28)) && _model.GetInterestRate(Time) != 1)
            {
                throw new Exception($"Risk Free interest rate should be 1, but was {_model.GetInterestRate(Time)}");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 186;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0.069"},
            {"Tracking Error", "0.243"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
