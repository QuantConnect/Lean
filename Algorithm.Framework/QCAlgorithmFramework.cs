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
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
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

        /// <summary>
        /// Gets or sets the execution model
        /// </summary>
        public IExecutionModel Execution { get; set; }

        /// <summary>
        /// Gets or sets the risk management model
        /// </summary>
        public IRiskManagementModel RiskManagement { get; set; }

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
            CheckModels();

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
            Execution.Execute(this, targets);
            RiskManagement.ManageRisk(this);
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Signal.OnSecuritiesChanged(this, changes);
            PortfolioConstruction.OnSecuritiesChanged(this, changes);
            Execution.OnSecuritiesChanged(this, changes);
            RiskManagement.OnSecuritiesChanged(this, changes);
        }

        private void CheckModels()
        {
            if (PortfolioSelection == null)
            {
                throw new Exception("Framework algorithms must specify a portfolio selection model using the 'PortfolioSelection' property.");
            }
            if (Signal == null)
            {
                throw new Exception("Framework algorithms must specify a signal model using the 'Signal' property.");
            }
            if (PortfolioConstruction == null)
            {
                throw new Exception("Framework algorithms must specify a portfolio construction model using the 'PortfolioConstruction' property");
            }
            if (Execution == null)
            {
                throw new Exception("Framework algorithms must specify an execution model using the 'Execution' property.");
            }
            if (RiskManagement == null)
            {
                throw new Exception("Framework algorithms must specify an risk management model using the 'RiskManagement' property.");
            }
        }
    }
}
