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
using NodaTime;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Algorithm.CSharp.Alphas
{
	// <summary>
	// This demonstration alpha reads the DailyFx calendar and provides insights based upon
	// the news outlook for the country associated currency pairs
	// </summary>
	public class ForexCalendarAlpha : QCAlgorithmFramework, IRegressionAlgorithmDefinition
    {

		public override void Initialize()
		{
			SetStartDate(2015, 7, 12);
			SetEndDate(2018, 7, 27);
			SetCash(100000);

			// Selects a universe of popular currency pairs with USD
			var symbols = new[] { QuantConnect.Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda),
								QuantConnect.Symbol.Create("USDJPY", SecurityType.Forex, Market.Oanda),
								QuantConnect.Symbol.Create("USDCHF", SecurityType.Forex, Market.Oanda),
								QuantConnect.Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda),
								QuantConnect.Symbol.Create("USDCAD", SecurityType.Forex, Market.Oanda),
								QuantConnect.Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda)};

			// Initializes the class that provides DailyFx news
			AddData<DailyFx>("DFX", Resolution.Minute, DateTimeZone.Utc);

			// Add a Manually Set Universe
			UniverseSettings.Resolution = Resolution.Minute;
			SetUniverseSelection(new ManualUniverseSelectionModel(symbols));

			// Define the FX Alpha Model.
			SetAlpha(new FxCalendarTrigger());

			// Default Models For Other Framework Settings:
			SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
			SetExecution(new ImmediateExecutionModel());
			SetRiskManagement(new NullRiskManagementModel());
		}

		//we create a DailyFx event handler but insights will be produced in the Alpha Model
		public void OnData(DailyFx data) { }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "647"},
            {"Average Win", "0.07%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "2.958%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "1.008"},
            {"Net Profit", "9.285%"},
            {"Sharpe Ratio", "1.572"},
            {"Loss Rate", "43%"},
            {"Win Rate", "57%"},
            {"Profit-Loss Ratio", "2.50"},
            {"Alpha", "0.051"},
            {"Beta", "-1.878"},
            {"Annual Standard Deviation", "0.014"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0.5"},
            {"Tracking Error", "0.014"},
            {"Treynor Ratio", "-0.012"},
            {"Total Fees", "$0.00"}
        };
    }

	/// <summary>
	/// Generate Forex Insights for High Impact Calendar Events.
	/// </summary>
	public class FxCalendarTrigger : AlphaModel
	{
		public FxCalendarTrigger()
		{
			Name = "FxCalendarTrigger";
		}

   		public override IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
		{
			var insights = new List<Insight>();
			var period = TimeSpan.FromMinutes(5);
			var magnitude = 0.0005;

	   		// We will create our insights when we recieve news
			if (data.ContainsKey("DFX"))
			{
	   			var calendar = data.Get<DailyFx>("DFX");

	   			// Only act if this is important news.
	   			if (calendar.Importance != FxDailyImportance.High) return insights;
				if (calendar.Meaning == FxDailyMeaning.None) return insights;

				// Create insights for all active currencies in our universe when country matches currency
				foreach (var kvp in algorithm.ActiveSecurities.Where(kvp => kvp.Value.Symbol.SecurityType == SecurityType.Forex))
				{
					var symbol = kvp.Key;
					var pair = (Forex)kvp.Value;
					var direction = InsightDirection.Flat;

					if (pair.BaseCurrencySymbol == calendar.Currency.ToUpper())
					{
						direction = (calendar.Meaning == FxDailyMeaning.Better) ? InsightDirection.Up : InsightDirection.Down;
					}
					else if (pair.QuoteCurrency.Symbol == calendar.Currency.ToUpper())
					{
						direction = (calendar.Meaning == FxDailyMeaning.Better) ? InsightDirection.Down : InsightDirection.Up;
					}

					if (direction != InsightDirection.Flat)
					{
						insights.Add(Insight.Price(symbol, period, direction, magnitude));
					}
				}
			}

			return insights;
		}

		public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes) {	 }
	}
}