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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders.Slippage;
using QuantConnect.Python;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmModelsTests
    {
        private QCAlgorithm _algorithm;

        [SetUp]
        public void SetUp()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
        }

        [Test]
        public void NoneFrameworkModelsAreAcceptedAsNullModels()
        {
            // Reproduces 'self.set_risk_management(None)' raising
            // "IRiskManagementModel must be fully implemented. Please implement these missing methods on NoneType: ManageRisk"
            using (Py.GIL())
            {
                var module = PyModule.FromString(nameof(NoneFrameworkModelsAreAcceptedAsNullModels), @"
def set_none_models(algo):
    algo.set_alpha(None)
    algo.set_execution(None)
    algo.set_portfolio_construction(None)
    algo.set_risk_management(None)
    algo.set_universe_selection(None)
");
                Assert.DoesNotThrow(() => module.GetAttr("set_none_models").Invoke(_algorithm.ToPython()));
            }

            Assert.IsInstanceOf<NullAlphaModel>(_algorithm.Alpha);
            Assert.IsInstanceOf<NullExecutionModel>(_algorithm.Execution);
            Assert.IsInstanceOf<NullPortfolioConstructionModel>(_algorithm.PortfolioConstruction);
            Assert.IsInstanceOf<NullRiskManagementModel>(_algorithm.RiskManagement);
            Assert.IsInstanceOf<NullUniverseSelectionModel>(_algorithm.UniverseSelection);
        }

        [Test]
        public void AlgorithmSlippageModelIsAppliedToExistingAndFutureSecurities()
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);
            var model = new ConstantSlippageModel(0.5m);
            _algorithm.SetSlippageModel(model);
            Assert.AreSame(model, spy.SlippageModel);

            var ibm = _algorithm.AddEquity("IBM", Resolution.Daily);
            Assert.AreSame(model, ibm.SlippageModel);
        }

        [Test]
        public void PerSecuritySlippageModelOverridesAlgorithmLevelModel()
        {
            var model = new ConstantSlippageModel(0.5m);
            _algorithm.SetSlippageModel(model);
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);
            var ibm = _algorithm.AddEquity("IBM", Resolution.Daily);

            // per-security models set after the algorithm-level model take precedence for that security
            ibm.SetSlippageModel(NullSlippageModel.Instance);
            Assert.AreSame(model, spy.SlippageModel);
            Assert.AreSame(NullSlippageModel.Instance, ibm.SlippageModel);

            // a new algorithm-level model is applied to all securities again
            var newModel = new ConstantSlippageModel(0.1m);
            _algorithm.SetSlippageModel(newModel);
            Assert.AreSame(newModel, spy.SlippageModel);
            Assert.AreSame(newModel, ibm.SlippageModel);
        }

        [Test]
        public void AlgorithmSlippageModelSurvivesSetBrokerageModel()
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);
            var model = new ConstantSlippageModel(0.5m);
            _algorithm.SetSlippageModel(model);

            // SetBrokerageModel re-initializes existing securities with the brokerage default models,
            // the algorithm-level slippage model must survive it
            _algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            Assert.AreSame(model, spy.SlippageModel);
        }

        [Test]
        public void AlgorithmSlippageModelRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _algorithm.SetSlippageModel((ISlippageModel)null));
        }

        [Test]
        public void AlgorithmSlippageModelIsAppliedToAllSecurities_Python()
        {
            // Reproduces "'MyAlgorithm' object has no attribute 'set_slippage_model'"
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);

            using (Py.GIL())
            {
                var module = PyModule.FromString(nameof(AlgorithmSlippageModelIsAppliedToAllSecurities_Python), @"
from AlgorithmImports import *

def set_model(algo):
    algo.set_slippage_model(ConstantSlippageModel(0.5))
");
                Assert.DoesNotThrow(() => module.GetAttr("set_model").Invoke(_algorithm.ToPython()));
            }

            Assert.IsInstanceOf<ConstantSlippageModel>(spy.SlippageModel);

            // securities added after the algorithm-level model is set also get it
            var ibm = _algorithm.AddEquity("IBM", Resolution.Daily);
            Assert.IsInstanceOf<ConstantSlippageModel>(ibm.SlippageModel);
        }

        [Test]
        public void PythonCustomAlgorithmSlippageModelIsAppliedToAllSecurities()
        {
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);

            using (Py.GIL())
            {
                var module = PyModule.FromString(nameof(PythonCustomAlgorithmSlippageModelIsAppliedToAllSecurities), @"
from AlgorithmImports import *

class CustomSlippageModel:
    def get_slippage_approximation(self, asset, order):
        return 0.25

def set_model(algo):
    algo.set_slippage_model(CustomSlippageModel())
");
                Assert.DoesNotThrow(() => module.GetAttr("set_model").Invoke(_algorithm.ToPython()));
            }

            var ibm = _algorithm.AddEquity("IBM", Resolution.Daily);
            foreach (var security in new[] { spy, ibm })
            {
                Assert.IsInstanceOf<SlippageModelPythonWrapper>(security.SlippageModel);
            }
        }
    }
}
