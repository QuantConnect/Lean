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

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Sets the alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha</param>
        public void SetAlpha(PyObject alpha)
        {
            IAlphaModel model;
            if (alpha.TryConvert(out model))
            {
                SetAlpha(model);
            }
            else
            {
                Alpha = new AlphaModelPythonWrapper(alpha);
            }
        }

        /// <summary>
        /// Adds a new alpha model
        /// </summary>
        /// <param name="alpha">Model that generates alpha to add</param>
        public void AddAlpha(PyObject alpha)
        {
            IAlphaModel model;
            if (alpha.TryConvert(out model))
            {
                AddAlpha(model);
            }
            else
            {
                AddAlpha(new AlphaModelPythonWrapper(alpha));
            }
        }

        /// <summary>
        /// Sets the execution model
        /// </summary>
        /// <param name="execution">Model defining how to execute trades to reach a portfolio target</param>
        public void SetExecution(PyObject execution)
        {
            IExecutionModel model;
            if (execution.TryConvert(out model))
            {
                SetExecution(model);
            }
            else
            {
                Execution = new ExecutionModelPythonWrapper(execution);
            }
        }

        /// <summary>
        /// Sets the portfolio construction model
        /// </summary>
        /// <param name="portfolioConstruction">Model defining how to build a portfolio from alphas</param>
        public void SetPortfolioConstruction(PyObject portfolioConstruction)
        {
            IPortfolioConstructionModel model;
            if (portfolioConstruction.TryConvert(out model))
            {
                SetPortfolioConstruction(model);
            }
            else
            {
                PortfolioConstruction = new PortfolioConstructionModelPythonWrapper(portfolioConstruction);
            }
        }

        /// <summary>
        /// Sets the universe selection model
        /// </summary>
        /// <param name="universeSelection">Model defining universes for the algorithm</param>
        public void SetUniverseSelection(PyObject universeSelection)
        {
            IUniverseSelectionModel model;
            if (!universeSelection.TryConvert(out model))
            {
                model = new UniverseSelectionModelPythonWrapper(universeSelection);
            }
            SetUniverseSelection(model);
        }

        /// <summary>
        /// Adds a new universe selection model
        /// </summary>
        /// <param name="universeSelection">Model defining universes for the algorithm to add</param>
        public void AddUniverseSelection(PyObject universeSelection)
        {
            IUniverseSelectionModel model;
            if (!universeSelection.TryConvert(out model))
            {
                model = new UniverseSelectionModelPythonWrapper(universeSelection);
            }
            AddUniverseSelection(model);
        }

        /// <summary>
        /// Sets the risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining how risk is managed</param>
        public void SetRiskManagement(PyObject riskManagement)
        {
            IRiskManagementModel model;
            if (riskManagement.TryConvert(out model))
            {
                SetRiskManagement(model);
            }
            else
            {
                RiskManagement = new RiskManagementModelPythonWrapper(riskManagement);
            }
        }

        /// <summary>
        /// Adds a new risk management model
        /// </summary>
        /// <param name="riskManagement">Model defining how risk is managed to add</param>
        public void AddRiskManagement(PyObject riskManagement)
        {
            IRiskManagementModel model;
            if (!riskManagement.TryConvert(out model))
            {
                model = new RiskManagementModelPythonWrapper(riskManagement);
            }
            AddRiskManagement(model);
        }
    }
}