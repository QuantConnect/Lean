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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class TachyonDynamicGearbox : QCAlgorithm
    {
        private int numberOfSymbols;
        private int numberOfSymbolsFine;
        private Queue<Symbol> queue;
        private int dequeueSize;
        

        public override void Initialize()
        {
            SetStartDate(2020, 9, 1);
            SetEndDate(2020, 9, 2);
            SetCash(100000);
            
            numberOfSymbols = 2000;
            numberOfSymbolsFine = 1000;
            SetUniverseSelection(new FineFundamentalUniverseSelectionModel(CoarseSelectionFunction, FineSelectionFunction));
            
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            
            SetExecution(new ImmediateExecutionModel());
            
            queue = new Queue<Symbol>();
            dequeueSize = 100;
            
            AddEquity("SPY", Resolution.Minute);
            Schedule.On(DateRules.EveryDay("SPY"), TimeRules.At(0, 0), FillQueue);
            Schedule.On(DateRules.EveryDay("SPY"), TimeRules.Every(TimeSpan.FromMinutes(60)), TakeFromQueue);
        }

        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            var sortedByDollarVolume = coarse
                .Where(x => x.HasFundamentalData)
                .OrderByDescending(x => x.DollarVolume);
            return sortedByDollarVolume.Take(numberOfSymbols).Select(x => x.Symbol);
        }
        
        public IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            
            var sortedByPeRatio = fine.OrderByDescending(x => x.ValuationRatios.PERatio);
            var topFine = sortedByPeRatio.Take(numberOfSymbolsFine);
            return topFine.Select(x => x.Symbol);
        }
        
        private void FillQueue() {
            var securities = ActiveSecurities.Values.Where(x => x.Fundamentals != null);
            
            // Fill queue with symbols sorted by PE ratio (decreasing order)
            queue.Clear();
            var sortedByPERatio = securities.OrderByDescending(x => x.Fundamentals.ValuationRatios.PERatio);
            foreach (Security security in sortedByPERatio)
                queue.Enqueue(security.Symbol);
        }
        
        private void TakeFromQueue() {
            List<Symbol> symbols = new List<Symbol>();
            for (int i = 0; i < Math.Min(dequeueSize, queue.Count); i++)
                symbols.Add(queue.Dequeue());
            History(symbols, 10, Resolution.Daily);
            
            Log("Symbols at " + Time + ": " + string.Join(", ", symbols.Select(x => x.ToString())));
        }
    }
}