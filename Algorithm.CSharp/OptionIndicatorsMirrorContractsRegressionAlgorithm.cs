/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using MathNet.Numerics.RootFinding;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm illustrating the usage of the <see cref="OptionIndicatorBase"/> indicators with mirror-paired contracts
    /// </summary>
    public class OptionIndicatorsMirrorContractsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private ImpliedVolatility _impliedVolatility;
        private Delta _delta;
        private Gamma _gamma;
        private Vega _vega;
        private Theta _theta;
        private Rho _rho;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 7);
            SetCash(100000);

            var equity = AddEquity("AAPL", Resolution.Daily).Symbol;
            var option = QuantConnect.Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Put, 650m, new DateTime(2014, 6, 21));
            AddOptionContract(option, Resolution.Daily);
            // add the call counter side of the mirrored pair
            var mirrorOption = QuantConnect.Symbol.CreateOption("AAPL", Market.USA, OptionStyle.American, OptionRight.Call, 650m, new DateTime(2014, 6, 21));
            AddOptionContract(mirrorOption, Resolution.Daily);

            _delta = D(option, mirrorOption, optionModel: OptionPricingModelType.BinomialCoxRossRubinstein, ivModel: OptionPricingModelType.BlackScholes);
            _gamma = G(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            _vega = V(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            _theta = T(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);
            _rho = R(option, mirrorOption, optionModel: OptionPricingModelType.ForwardTree, ivModel: OptionPricingModelType.BlackScholes);

            // A custom IV indicator with custom calculation of IV
            var riskFreeRateModel = new InterestRateProvider();
            var dividendYieldModel = new DividendYieldProvider(equity);
            _impliedVolatility = new CustomImpliedVolatility(option, mirrorOption, riskFreeRateModel, dividendYieldModel);
            RegisterIndicator(option, _impliedVolatility, new QuoteBarConsolidator(TimeSpan.FromDays(1)));
            RegisterIndicator(mirrorOption, _impliedVolatility, new QuoteBarConsolidator(TimeSpan.FromDays(1)));
            RegisterIndicator(equity, _impliedVolatility, new TradeBarConsolidator(TimeSpan.FromDays(1)));

            // custom IV smoothing function: assume the lower IV is more "fair"
            Func<decimal, decimal, decimal> smoothingFunc = (iv, mirrorIv) => Math.Min(iv, mirrorIv);
            // set the smoothing function
            _delta.ImpliedVolatility.SetSmoothingFunction(smoothingFunc);
            _gamma.ImpliedVolatility.SetSmoothingFunction(smoothingFunc);
            _vega.ImpliedVolatility.SetSmoothingFunction(smoothingFunc);
            _theta.ImpliedVolatility.SetSmoothingFunction(smoothingFunc);
            _rho.ImpliedVolatility.SetSmoothingFunction(smoothingFunc);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_impliedVolatility == 0m || _delta == 0m || _gamma == 0m || _vega == 0m || _theta == 0m || _rho == 0m)
            {
                throw new Exception("Expected IV/greeks calculated");
            }
            Debug(@$"Implied Volatility: {_impliedVolatility},
Delta: {_delta},
Gamma: {_gamma},
Vega: {_vega},
Theta: {_theta},
Rho: {_rho}");
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
        public long DataPoints => 34;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }

    public class CustomImpliedVolatility : ImpliedVolatility
    {
        public CustomImpliedVolatility(Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            : base(option, riskFreeRateModel, dividendYieldModel, mirrorOption, period: 2)
        {
            SetSmoothingFunction((iv, mirrorIV) => iv);
        }

        protected override decimal CalculateIV(decimal timeTillExpiry)
        {
            // we demonstate put-call parity calculation here, but note that it is not suitable for American options
            try
            {
                Func<double, double> f = (vol) =>
                {
                    var callBlackPrice = OptionGreekIndicatorsHelper.BlackTheoreticalPrice(
                        Convert.ToDecimal(vol), UnderlyingPrice, Strike, timeTillExpiry, RiskFreeRate, DividendYield, OptionRight.Call);
                    var putBlackPrice = OptionGreekIndicatorsHelper.BlackTheoreticalPrice(
                        Convert.ToDecimal(vol), UnderlyingPrice, Strike, timeTillExpiry, RiskFreeRate, DividendYield, OptionRight.Put);
                    return (double)(Price + OppositePrice - callBlackPrice - putBlackPrice);
                };
                return Convert.ToDecimal(Brent.FindRoot(f, 1e-7d, 2.0d, 1e-4d, 100));
            }
            catch
            {
                Log.Error("ImpliedVolatility.CalculateIV(): Fail to converge, returning 0.");
                return 0m;
            }
        }
    }
}
