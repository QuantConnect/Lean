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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders.Fees;
using QuantConnect.Data.Custom;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
	/// <summary>
	/// This Alpha Model uses Wells Fargo 30-year Fixed Rate Mortgage data from Quandl to 
	/// generate Insights about the movement of Real Estate ETFs. Mortgage rates can provide information 
	/// regarding the general price trend of real estate, and ETFs provide good continuous-time instruments 
	/// to measure the impact against. Volatility in mortgage rates tends to put downward pressure on real 
	/// estate prices, whereas stable mortgage rates, regardless of true rate, lead to stable or higher real
	/// estate prices. This Alpha model seeks to take advantage of this correlation by emitting insights
	/// based on volatility and rate deviation from its historic mean.
	
	/// This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    /// sourced so the community and client funds can see an example of an alpha.
	/// <summary>
    public class MortgageRateVolatilityAlgorithm : QCAlgorithmFramework
    {
        public override void Initialize()
        {
            SetStartDate(2017, 1, 1);  //Set Start Date
            SetCash(100000);             //Set Strategy Cash

            UniverseSettings.Resolution = Resolution.Daily;
            SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));

            // Basket of 6 liquid real estate ETFs
            Func<string, Symbol> ToSymbol = x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA);
            var realEstateETFs = new[] { "VNQ", "REET", "TAO", "FREL", "SRET", "HIPS" }.Select(ToSymbol).ToArray();
            SetUniverseSelection(new ManualUniverseSelectionModel(realEstateETFs));

            SetAlpha(new MortgageRateVolatilityAlphaModel(this));

            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());

            SetExecution(new ImmediateExecutionModel());

            SetRiskManagement(new NullRiskManagementModel());

        }
        
        public void OnData(QuandlMortgagePriceColumns data) {  }

        private class MortgageRateVolatilityAlphaModel : AlphaModel
        {
            private readonly int _indicatorPeriod;
            private readonly Resolution _resolution;
            private readonly TimeSpan _insightDuration;
            private readonly int _deviations;
            private readonly double _insightMagnitude;
            private readonly Symbol _mortgageRate;
            private readonly SimpleMovingAverage _mortgageRateSma;
            private readonly StandardDeviation _mortgageRateStd;

            public MortgageRateVolatilityAlphaModel(
                QCAlgorithmFramework algorithm,
                int indicatorPeriod = 15,
                double insightMagnitude = 0.0005,
                int deviations = 2,
                Resolution resolution = Resolution.Daily
                )
            {
            	// Add Quandl data for a Well's Fargo 30-year Fixed Rate mortgage
                _mortgageRate = algorithm.AddData<QuandlMortgagePriceColumns>("WFC/PR_GOV_30YFIXEDVA_APR").Symbol;
                _indicatorPeriod = indicatorPeriod;
                _resolution = resolution;
                _insightDuration = resolution.ToTimeSpan().Multiply(indicatorPeriod);
                _insightMagnitude = insightMagnitude;
                _deviations = deviations;
                
                // Add indicators for the mortgage rate -- Standard Deviation and Simple Moving Average
                _mortgageRateStd = algorithm.STD(_mortgageRate, _indicatorPeriod, resolution);
                _mortgageRateSma = algorithm.SMA(_mortgageRate, _indicatorPeriod, resolution);
                
                // Use a history call to warm-up the indicators
                WarmUpIndicators(algorithm);
            }
            
            public override IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
            {
                var insights = new List<Insight>();
                
                // Return empty list if data slice doesn't contain monrtgage rate data
                if (!data.Keys.Contains(_mortgageRate))
                {
                    return insights;
                }
				// Extract current mortgage rate, the current STD indicator value, and current SMA value
                var rate = data[_mortgageRate].Value;
                var deviation = _deviations * _mortgageRateStd;
                var sma = _mortgageRateSma;

				// Loop through all Active Securities to emit insights
                foreach (var security in algorithm.ActiveSecurities.Keys)
                {
                	// Mortgage rate Symbol will be in the collection, so skip it
                    if (security == _mortgageRate)
                    {
                        return insights;
                    }

					// If volatility in mortgage rates is high, then we emit an Insight to sell
                    if ((rate < sma - deviation) || (rate > sma + deviation))
                    {
                        insights.Add(Insight.Price(security, _insightDuration, InsightDirection.Down, _insightMagnitude));
                    }
                    
                    // If volatility in mortgage rates is low, then we emit an Insight to buy
                    if ((rate < sma - (decimal)deviation/2) || (rate > sma + (decimal)deviation/2))
                    {
                        insights.Add(Insight.Price(security, _insightDuration, InsightDirection.Up, _insightMagnitude));
                    }
                }

                return insights;
            }

            private void WarmUpIndicators(QCAlgorithmFramework algorithm)
            {
            	// Make a history call and update the indicators
                algorithm.History(new[] { _mortgageRate }, _indicatorPeriod, _resolution).PushThrough(bar =>
                {
                    _mortgageRateSma.Update(bar.EndTime, bar.Value);
                    _mortgageRateStd.Update(bar.EndTime, bar.Value);
                });
            }
        }
        public class QuandlMortgagePriceColumns : Quandl
        {
            public QuandlMortgagePriceColumns()
            
            // Rename the Quandl object column to the data we want, which is the 'Value' column
	        // of the CSV that our API call returns
                : base(valueColumnName: "Value")
            {
            }
        }
    }
}