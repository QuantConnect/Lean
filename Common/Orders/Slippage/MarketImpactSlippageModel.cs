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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Indicators;
using MathNet.Numerics.Statistics;

namespace QuantConnect.Orders.Slippage
{
    /// <summary>
    /// Slippage model that mimic the effect brought by market impact,
    /// i.e. consume the volume listed in the order book
    /// </summary>
    /// <remark>Almgren, R., Thum, C., Hauptmann, E., & Li, H. (2005). 
    /// Direct estimation of equity market impact. Risk, 18(7), 58-62.
    /// Available from: https://www.ram-ai.com/sites/default/files/2022-06/costestim.pdf</remark>
    /// <remark>The default parameters are calibrated around 2 decades ago,
    /// the trading time effect is not accounted (volume near market open/close is larger),
    /// the market regime is not taken into account,
    /// and the market environment does not have many market makers at that time,
    /// so it is recommend to recalibrate with reference to the original paper.</remark>
    public class MarketImpactSlippageModel : ISlippageModel
    {
        private readonly IAlgorithm _algorithm;
        private readonly bool _nonNegative;
        private readonly double _latency;
        private readonly double _impactTime;
        private readonly double _alpha;
        private readonly double _beta;
        private readonly double _gamma;
        private readonly double _eta;
        private readonly double _delta;
        private readonly Random _random;
        private SymbolData _symbolData;

        /// <summary>
        /// Instantiate a new instance of MarketImpactSlippageModel
        /// </summary>
        /// <param name="algorithm">IAlgorithm instance</param>
        /// <param name="nonNegative">Indicator whether only non-negative slippage allowed</param>
        /// <param name="latency">Time between order submitted and filled, in seconds(s)</param>
        /// <param name="impactTime">Time between order filled and new equilibrium established, in second(s)</param>
        /// <param name="alpha">Exponent of the permanent impact function</param>
        /// <param name="beta">Exponent of the temporary impact function</param>
        /// <param name="gamma">Coefficient of the permanent impact function</param>
        /// <param name="eta">Coefficient of the temporary impact function</param>
        /// <param name="delta">Liquidity scaling factor for permanent impact</param>
        /// <param name="randomSeed">Random seed for generating gaussian noise</param>
        public MarketImpactSlippageModel(IAlgorithm algorithm, bool nonNegative = true, double latency = 0.075d,
                                         double impactTime = 1800d, double alpha = 0.891d, double beta = 0.600d, 
                                         double gamma = 0.314d, double eta = 0.142d, double delta = 0.267d, 
                                         int randomSeed = 50)
        {
            if (latency <= 0)
            {
                throw new Exception("Latency cannot be less than or equal to 0.");
            }
            if (impactTime <= 0)
            {
                throw new Exception("impactTime cannot be less than or equal to 0.");
            }

            _algorithm = algorithm;
            _nonNegative = nonNegative;
            _latency = latency;
            _impactTime = impactTime;
            _alpha = alpha;
            _beta = beta;
            _gamma = gamma;
            _eta = eta;
            _delta = delta;
            _random = new(randomSeed);
        }

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        public decimal GetSlippageApproximation(Security asset, Order order)
        {
            if (asset.Type == SecurityType.Forex || asset.Type == SecurityType.Cfd)
            {
                throw new Exception($"Asset of {asset.Type} is not supported as MarketImpactSlippageModel requires volume data");
            }

            if (_symbolData == null)
            {
                _symbolData = new SymbolData(_algorithm, asset, _latency, _impactTime);
            }

            if (_symbolData.AverageVolume == 0d)
            {
                return 0m;
            }
            
            // normalized volume of execution
            var nu = (double)order.AbsoluteQuantity / _symbolData.ExecutionTime / _symbolData.AverageVolume;
            // liquidity adjustment for temporary market impact, if any
            var liquidityAdjustment = asset.Fundamentals.HasFundamentalData && asset.Fundamentals.CompanyProfile.SharesOutstanding != default ?
                                      Math.Pow(asset.Fundamentals.CompanyProfile.SharesOutstanding / _symbolData.AverageVolume, _delta) :
                                      1d;
            // noise adjustment factor
            var noise = _symbolData.Sigma * Math.Sqrt(_symbolData.ImpactTime);

            // permanent market impact
            var permanentImpact = _symbolData.Sigma * _symbolData.ExecutionTime * G(nu) * liquidityAdjustment + SampleGaussian() * noise;
            // temporary market impact
            var temporaryImpact = _symbolData.Sigma * H(nu) + SampleGaussian() * noise;
            // realized market impact
            var realizedImpact = temporaryImpact + permanentImpact * 0.5d;

            // estimate the slippage by temporary impact
            return SlippageFromImpactEstimation(realizedImpact) * asset.Price;
        }

        /// <summary>
        /// The permanent market impact function
        /// </summary>
        /// <param name="absoluteOrderQuantity">The absolute, normalized order quantity</param>
        /// <return>Unadjusted permanent market impact factor</return>
        private double G(double absoluteOrderQuantity)
        {
            return _gamma * Math.Pow(absoluteOrderQuantity, _alpha);
        }

        /// <summary>
        /// The temporary market impact function
        /// </summary>
        /// <param name="absoluteOrderQuantity">The absolute, normalized order quantity</param>
        /// <return>Unadjusted temporary market impact factor</return>
        private double H(double absoluteOrderQuantity)
        {
            return _eta * Math.Pow(absoluteOrderQuantity, _beta);
        }

        /// <summary>
        /// Estimate the slippage size from impact
        /// </summary>
        /// <param name="impact">The market impact of the order</param>
        /// <return>Slippage estimation</return>
        private decimal SlippageFromImpactEstimation(double impact)
        {
            // The percentage of impact that an order is averagely being affected is random from 0.0 to 1.0
            var ultimateSlippage = (impact * _random.NextDouble()).SafeDecimalCast();
            // Impact at max can be the asset's price
            ultimateSlippage = Math.Min(ultimateSlippage, 1m);

            if (_nonNegative)
            {
                return Math.Max(0m, ultimateSlippage);
            }

            return ultimateSlippage;
        }

        private double SampleGaussian(double location = 0d, double scale = 1d)
        {
            var randomVariable1 = 1 - _random.NextDouble();
            var randomVariable2 = 1 - _random.NextDouble();

            var deviation = Math.Sqrt(-2.0 * Math.Log(randomVariable1)) * Math.Cos(2.0 * Math.PI * randomVariable2);
            return deviation * scale + location;
        }
    }

    internal class SymbolData
    {
        private readonly IAlgorithm _algorithm;
        private readonly Symbol _symbol;
        private readonly TradeBarConsolidator _consolidator;
        private readonly RollingWindow<decimal> _volumes = new(10);
        private readonly RollingWindow<decimal> _prices = new(252);

        public double Sigma { get; internal set; }

        public double AverageVolume { get; internal set; }

        public double ExecutionTime { get; internal set; }

        public double ImpactTime { get; internal set; }

        public SymbolData(IAlgorithm algorithm, Security asset, double latency, double impactTime)
        {
            _algorithm = algorithm;
            _symbol = asset.Symbol;

            _consolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += OnDataConsolidated;
            algorithm.SubscriptionManager.AddConsolidator(_symbol, _consolidator);

            var configs = algorithm
                .SubscriptionManager
                .SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(_symbol, includeInternalConfigs: true);
            var configToUse = configs.Where(x => x.TickType == TickType.Trade).First();

            var historyRequestFactory = new HistoryRequestFactory(algorithm);
            var historyRequest = historyRequestFactory.CreateHistoryRequest(configToUse,
                                                                            algorithm.Time - TimeSpan.FromDays(370),
                                                                            algorithm.Time,
                                                                            algorithm.Securities[_symbol].Exchange.Hours,
                                                                            Resolution.Daily);
            foreach (var bar in algorithm.HistoryProvider.GetHistory(new List<HistoryRequest> { historyRequest }, algorithm.TimeZone))
            {
                _consolidator.Update(bar.Bars[_symbol]);
            }

            // execution time is defined as time difference between order submission and filling here, 
            // default with 75ms latency (https://www.interactivebrokers.com/download/salesPDFs/10-PDF0513.pdf)
            // it should be in unit of "trading days", so we need to divide by normal trade day's length
            var normalTradeDayLength = asset.Exchange.Hours.RegularMarketDuration.TotalDays;
            ExecutionTime = TimeSpan.FromSeconds(latency).TotalDays / normalTradeDayLength;
            // expected valid time for impact
            var adjustedImpactTime = TimeSpan.FromSeconds(impactTime).TotalDays / normalTradeDayLength;
            ImpactTime = ExecutionTime + adjustedImpactTime;
        }

        public void OnDataConsolidated(object _, TradeBar bar)
        {
            _prices.Add(bar.Close);
            _volumes.Add(bar.Volume);

            if (_prices.Samples < 2)
            {
                return;
            }

            var rocp = new double[_prices.Samples - 1];
            for (var i = 0; i < _prices.Samples - 1; i++)
            {
                if (_prices[i + 1] == 0) continue;

                var roc = (_prices[i] - _prices[i + 1]) / _prices[i + 1];
                rocp[i] = (double)roc;
            }

            var variance = rocp.Variance();
            Sigma = Math.Sqrt(variance);
            AverageVolume = (double)_volumes.Average();
        }

        public void Dispose()
        {
            _prices.Reset();
            _volumes.Reset();

            _consolidator.DataConsolidated -= OnDataConsolidated;
            _algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
        }
    }
}
