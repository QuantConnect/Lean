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

using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Util;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Sets the alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha</param>
        [DocumentationAttribute(AlgorithmFramework)]
        public void SetAlpha(PyObject alpha)
        {
            // None is accepted as the null model, e.g. to disable a model a template set up
            if (alpha is null || alpha.IsNone())
            {
                SetAlpha(new NullAlphaModel());
                return;
            }

            Alpha = PythonUtil.CreateInstanceOrWrapper<IAlphaModel>(
                alpha,
                py => new AlphaModelPythonWrapper(py)
            );
        }

        /// <summary>
        /// Adds a new alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha to add</param>
        [DocumentationAttribute(AlgorithmFramework)]
        public void AddAlpha(PyObject alpha)
        {
            var model = PythonUtil.CreateInstanceOrWrapper<IAlphaModel>(
                alpha,
                py => new AlphaModelPythonWrapper(py)
            );
            AddAlpha(model);
        }

        /// <summary>
        /// Sets the execution model
        /// </summary>
        /// <param name="execution">Model defining how to execute trades to reach a portfolio target</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void SetExecution(PyObject execution)
        {
            // None is accepted as the null model, e.g. to disable a model a template set up
            if (execution is null || execution.IsNone())
            {
                SetExecution(new NullExecutionModel());
                return;
            }

            Execution = PythonUtil.CreateInstanceOrWrapper<IExecutionModel>(
                execution,
                py => new ExecutionModelPythonWrapper(py)
            );
        }

        /// <summary>
        /// Sets the portfolio construction model
        /// </summary>
        /// <param name="portfolioConstruction">Model defining how to build a portfolio from alphas</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void SetPortfolioConstruction(PyObject portfolioConstruction)
        {
            // None is accepted as the null model, e.g. to disable a model a template set up
            if (portfolioConstruction is null || portfolioConstruction.IsNone())
            {
                SetPortfolioConstruction(new NullPortfolioConstructionModel());
                return;
            }

            PortfolioConstruction = PythonUtil.CreateInstanceOrWrapper<IPortfolioConstructionModel>(
                portfolioConstruction,
                py => new PortfolioConstructionModelPythonWrapper(py)
            );
        }

        /// <summary>
        /// Sets the universe selection model
        /// </summary>
        /// <param name="universeSelection">Model defining universes for the algorithm</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(Universes)]
        public void SetUniverseSelection(PyObject universeSelection)
        {
            // None is accepted as the null model, e.g. to disable a model a template set up
            if (universeSelection is null || universeSelection.IsNone())
            {
                SetUniverseSelection(new NullUniverseSelectionModel());
                return;
            }

            UniverseSelection = PythonUtil.CreateInstanceOrWrapper<IUniverseSelectionModel>(
                universeSelection,
                py => new UniverseSelectionModelPythonWrapper(py)
            );
        }

        /// <summary>
        /// Adds a new universe selection model
        /// </summary>
        /// <param name="universeSelection">Model defining universes for the algorithm to add</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(Universes)]
        public void AddUniverseSelection(PyObject universeSelection)
        {
            var model = PythonUtil.CreateInstanceOrWrapper<IUniverseSelectionModel>(
                universeSelection,
                py => new UniverseSelectionModelPythonWrapper(py)
            );
            AddUniverseSelection(model);
        }

        /// <summary>
        /// Sets the risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining how risk is managed</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void SetRiskManagement(PyObject riskManagement)
        {
            // None is accepted as the null model, e.g. to disable a model a template set up.
            // Without this, set_risk_management(None) failed with
            // "IRiskManagementModel must be fully implemented. Please implement these missing methods on NoneType: ManageRisk"
            if (riskManagement is null || riskManagement.IsNone())
            {
                SetRiskManagement(new NullRiskManagementModel());
                return;
            }

            RiskManagement = PythonUtil.CreateInstanceOrWrapper<IRiskManagementModel>(
                riskManagement,
                py => new RiskManagementModelPythonWrapper(py)
            );
        }

        /// <summary>
        /// Adds a new risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining how risk is managed to add</param>
        [DocumentationAttribute(AlgorithmFramework)]
        [DocumentationAttribute(TradingAndOrders)]
        public void AddRiskManagement(PyObject riskManagement)
        {
            var model = PythonUtil.CreateInstanceOrWrapper<IRiskManagementModel>(
                riskManagement,
                py => new RiskManagementModelPythonWrapper(py)
            );
            AddRiskManagement(model);
        }
    }
}
