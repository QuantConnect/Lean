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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Signals;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework
{
    public class QCAlgorithmFramework : QCAlgorithm
    {
        /// <summary>
        /// Gets or sets the portfolio selection model.
        /// </summary>
        public IPortfolioSelectionModel PortfolioSelection { get; set; }

        /// <summary>
        /// Gets or sets the signal model
        /// </summary>
        public ISignalModel Signal { get; set; }

        /// <summary>
        /// Gets or sets the portoflio construction model
        /// </summary>
        public IPortfolioConstructionModel PortfolioConstruction { get; set; }

        public QCAlgorithmFramework()
        {
            var type = GetType();
            var onDataSlice = type.GetMethod("OnData", new[] { typeof(Slice) });
            if (onDataSlice.DeclaringType != typeof(QCAlgorithmFramework))
            {
                throw new Exception("Framework algorithms can not override OnData(Slice)");
            }
            var onSecuritiesChanged = type.GetMethod("OnSecuritiesChanged", new[] { typeof(SecurityChanges) });
            if (onSecuritiesChanged.DeclaringType != typeof(QCAlgorithmFramework))
            {
                throw new Exception("Framework algorithms can not override OnSecuritiesChanged(SecurityChanges)");
            }
        }

        public override void PostInitialize()
        {
            foreach (var universe in PortfolioSelection.CreateUniverses(this))
            {
                AddUniverse(universe);
            }

            base.PostInitialize();
        }

        public override void OnData(Slice slice)
        {
            var signals = Signal.Update(this, slice);
            var targets = PortfolioConstruction.CreateTargets(this, signals);
            foreach (var target in targets)
            {
                var existing = Securities[target.Symbol].Holdings.Quantity
                    + Transactions.GetOpenOrders(target.Symbol).Sum(o => o.Quantity);
                var quantity = target.GetTargetQuantity(this) - existing;
                if (quantity != 0)
                {
                    MarketOrder(target.Symbol, quantity);
                }
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Signal.OnSecuritiesChanged(this, changes);
            PortfolioConstruction.OnSecuritiesChanged(this, changes);
        }
    }
}
