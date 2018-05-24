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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.Framework
{
    /// <summary>
    /// Provides a base class for algorithms written against <see cref="QCAlgorithm"/>
    /// to be easily ported into the algorithm framework.
    /// </summary>
    public class QCAlgorithmFrameworkBridge : QCAlgorithmFramework
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QCAlgorithmFrameworkBridge"/> class
        /// </summary>
        public QCAlgorithmFrameworkBridge()
        {
            // default models for ported algorithms, universe selection set via PostInitialize
            SetAlpha(new NullAlphaModel());
            SetPortfolioConstruction(new NullPortfolioConstructionModel());
            SetExecution(new NullExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// Called by setup handlers after Initialize and allows the algorithm a chance to organize
        /// the data gather in the Initialize method
        /// </summary>
        public override void PostInitialize()
        {
            // set universe model if still null, needed to wait for AddSecurity calls
            if (UniverseSelection == null)
            {
                SetUniverseSelection(new ManualUniverseSelectionModel(Securities.Keys));
            }

            base.PostInitialize();
        }

        /// <summary>
        /// Manually emit insights from an algorithm.
        /// This is typically invoked before calls to submit orders in algorithms written against
        /// QCAlgorithm that have been ported into the algorithm framework.
        /// </summary>
        /// <param name="insights"></param>
        public void EmitInsights(params Insight[] insights)
        {
            OnInsightsGenerated(insights);
        }
    }
}