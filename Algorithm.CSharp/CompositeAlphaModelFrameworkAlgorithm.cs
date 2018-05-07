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

using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Show cases how to use the <see cref="CompositeAlphaModel"/> to define
    /// </summary>
    public class CompositeAlphaModelFrameworkAlgorithm : QCAlgorithmFramework
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            // even though we're using a framework algorithm, we can still add our securities
            // using the AddEquity/Forex/Crypto/ect methods and then pass them into a manual
            // universe selection model using Securities.Keys
            AddEquity("SPY");
            AddEquity("IBM");
            AddEquity("BAC");
            AddEquity("AIG");

            // define a manual universe of all the securities we manually registered
            SetUniverseSelection(new ManualUniverseSelectionModel(Securities.Keys));

            // define alpha model as a composite of the rsi and ema cross models
            SetAlpha(new CompositeAlphaModel(
                new RsiAlphaModel(),
                new EmaCrossAlphaModel()
            ));

            // default models for the rest
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }
    }
}
