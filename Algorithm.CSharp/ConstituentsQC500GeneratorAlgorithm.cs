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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
	/// <summary>
	/// Demonstration of how to estimate constituents of QC500 index based on the company fundamentals
	/// The algorithm creates a default tradable and liquid universe containing 500 US equities 
	/// which are chosen at the first trading day of each month.   
	/// </summary>
	/// <meta name="tag" content="using data" />
	/// <meta name="tag" content="universes" />
	/// <meta name="tag" content="coarse universes" />
	/// <meta name="tag" content="fine universes" />
	public class ConstituentsQC500GeneratorAlgorithm : QCAlgorithm
	{
		private const int NumberOfSymbolsCoarse = 1000;
		private const int NumberOfSymbolsFine = 500;

		public readonly Dictionary<string, decimal> DollarVolumeData = new Dictionary<string, decimal>();
		private bool _rebalance = true;
		
        public override void Initialize()
		{
			UniverseSettings.Resolution = Resolution.Daily;

			SetStartDate(2018, 1, 1); //Set Start Date
			SetEndDate(2018, 1, 3); //Set End Date
			SetCash(50000); //Set Strategy Cash
			var spy = AddEquity("SPY", Resolution.Daily);

			// this add universe method accepts two parameters:
			// - coarse selection function: accepts an IEnumerable<CoarseFundamental> and returns an IEnumerable<Symbol>
			// - fine selection function: accepts an IEnumerable<FineFundamental> and returns an IEnumerable<Symbol>
			AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
			Schedule.On(DateRules.MonthStart(spy.Symbol), TimeRules.At(0, 0),
						() => { _rebalance = true; });
		}

		public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
		{
			if (!_rebalance)
				return new List<Symbol>();

			// The stocks must have fundamental data 
			// The stock must have positive previous-day close price
			// The stock must have positive volume on the previous trading day
			var sortedByDollarVolume = from x in coarse
									   where x.HasFundamentalData && x.Volume > 0 && x.Price > 0
									   orderby x.DollarVolume descending
									   select x;

			var top = sortedByDollarVolume.Take(NumberOfSymbolsCoarse).ToList();
			foreach (var i in top)
			{
				if (!DollarVolumeData.ContainsKey(i.Symbol.Value))
				{
					DollarVolumeData.Add(i.Symbol.Value, i.DollarVolume);
				}
			}
			return top.Select(x => x.Symbol);
		}

		public IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
		{
			if (!_rebalance)
				return new List<Symbol>();
			_rebalance = false;

			// The company's headquarter must in the U.S.
			// The stock must be traded on either the NYSE or NASDAQ 
			// At least half a year since its initial public offering
			// The stock's market cap must be greater than 500 million
			var filteredFine = (from x in fine
								where x.CompanyReference.CountryId == "USA" &&
									  (x.CompanyReference.PrimaryExchangeID == "NYS" || x.CompanyReference.PrimaryExchangeID == "NAS") &&
									  (Time - x.SecurityReference.IPODate).Days > 180 &&
									  x.EarningReports.BasicAverageShares.ThreeMonths *
									  x.EarningReports.BasicEPS.TwelveMonths * x.ValuationRatios.PERatio > 500000000m
								select x).ToList();

			// select stocks with top dollar volume in every single sector 
			var myDict = new[] { "B", "I", "M", "N", "T", "U" }.ToDictionary(x => x, y => new List<FineFundamental>());
			var percent = (float)NumberOfSymbolsFine / filteredFine.Count;
			foreach (var key in myDict.Keys.ToList())
			{
				var value = (from x in filteredFine
							 where x.CompanyReference.IndustryTemplateCode == key
							 orderby DollarVolumeData[x.Symbol.Value] descending
							 select x).ToList();
				myDict[key] = value.Take((int)Math.Ceiling(value.Count * percent)).ToList();
			}
			var topFine = myDict.Values.ToList().SelectMany(x => x).Take(NumberOfSymbolsFine).ToList();

			Log(string.Join(",", topFine.Select(x => x.Symbol.Value).OrderBy(x => x)));
			return topFine.Select(x => x.Symbol);
		}


		public void OnData(TradeBars data) { }
	}
}