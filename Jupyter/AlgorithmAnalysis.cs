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
using QuantConnect.Api;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Jupyter
{
    /// <summary>
    /// Fetches information on projects in user's account
    /// </summary>
    public class AlgorithmAnalysis
    {
        private dynamic _pandas;
        private IApi _api;

        /// <summary>
        /// <see cref = "AlgorithmAnalysis" /> constructor.
        /// Fetches information on projects in user's account.
        /// </summary>
        public AlgorithmAnalysis(int userId, string apiAcessToken)
        {
            try
            {
                using (Py.GIL())
                {
                    _pandas = Py.Import("pandas");
                }

                _api = new Api.Api();
                _api.Initialize(userId, apiAcessToken, Globals.DataFolder);
            }
            catch (Exception exception)
            {
                throw new Exception("AlgorithmAnalysis.Main(): " + exception);
            }
        }

        /// <summary>
        /// Read back a list of all projects on the account for a user.
        /// </summary>
        /// <returns>pandas.DataFrame for list of projects</returns>
        public PyObject ListProjects()
        {
            var listProject = _api.ListProjects();
            if (listProject.Success)
            {
                using (Py.GIL())
                {
                    var pyDict = new PyDict();
                    var index = listProject.Projects.Select(x => x.ProjectId);
                    pyDict.SetItem("Name", _pandas.Series(listProject.Projects.Select(x => x.Name).ToList(), index));
                    pyDict.SetItem("Created", _pandas.Series(listProject.Projects.Select(x => x.Created).ToList(), index));
                    pyDict.SetItem("Modified", _pandas.Series(listProject.Projects.Select(x => x.Modified).ToList(), index));
                    return _pandas.DataFrame(pyDict, columns: new[] { "Name", "Created", "Modified" }.ToList());
                }
            }

            return null;
        }

        /// <summary>
        /// Get a list of backtests for a specific project id
        /// </summary>
        /// <param name="projectId">Project id to search</param>
        /// <returns>pandas.DataFrame for list of backtests</returns>
        public PyObject ListBacktests(int projectId)
        {
            var listBacktests = _api.ListBacktests(projectId);
            if (listBacktests.Success)
            {
                try
                {
                    using (Py.GIL())
                    {
                        var pyDict = new PyDict();
                        var backtests = listBacktests.Backtests
                            .Select(x => ReadBacktest(projectId, x.BacktestId))
                            .Where(x => x.Result != null).ToList();

                        var index = backtests.Select(x => x.BacktestId);

                        var columns = new List<string>() { "Name" };
                        pyDict.SetItem("Name", _pandas.Series(backtests.Select(x => x.Name).ToList(), index));

                        if (backtests.Count() > 0)
                        {
                            var CsDict = backtests
                                .SelectMany(x => x.Result.Statistics)
                                .GroupBy(k => k.Key)
                                .ToDictionary(k => k.Key, v => v.Select(i => i.Value).ToList());

                            foreach (var kvp in CsDict)
                            {
                                pyDict.SetItem(kvp.Key, _pandas.Series(kvp.Value, index));
                            }
                            columns.AddRange(CsDict.Keys);
                        }

                        return _pandas.DataFrame(pyDict, columns: columns);
                    }
                }
                catch (Exception exception)
                {
                    return exception.Message.ToPython();
                }
            }
            return string.Join(Environment.NewLine, listBacktests.Errors).ToPython();
        }

        /// <summary>
        /// Read out the full result of a specific backtest
        /// </summary>
        /// <param name="projectId">Project id for the backtest we'd like to read</param>
        /// <param name="backtestId">Backtest id for the backtest we'd like to read</param>
        /// <returns>Backtest result object</returns>
        public Backtest ReadBacktest(int projectId, string backtestId)
        {
            return _api.ReadBacktest(projectId, backtestId);
        }

        /// <summary>
        /// Read out the orders of a specific backtest
        /// </summary>
        /// <param name="projectId">Project id for the backtest we'd like to read</param>
        /// <param name="backtestId">Backtest id for the backtest we'd like to read</param>
        /// <returns>andas.DataFrame for list of orders</returns>
        public PyObject GetOrders(int projectId, string backtestId)
        {
            var backtest = ReadBacktest(projectId, backtestId);
            if (backtest.Success)
            {
                using (Py.GIL())
                {
                    var pyDict = new PyDict();
                    var orders = backtest.Result.Orders;
                    var index = orders.Select(x => x.Key);
                    pyDict.SetItem("Time", _pandas.Series(orders.Values.Select(x => x.Time).ToList(), index));
                    pyDict.SetItem("Symbol", _pandas.Series(orders.Values.Select(x => x.Symbol).ToList(), index));
                    pyDict.SetItem("Quantity", _pandas.Series(orders.Values.Select(x => (double)x.Quantity).ToList(), index));
                    pyDict.SetItem("Price", _pandas.Series(orders.Values.Select(x => (double)x.Price).ToList(), index));
                    pyDict.SetItem("Type", _pandas.Series(orders.Values.Select(x => x.Type.ToString()).ToList(), index));
                    pyDict.SetItem("Status", _pandas.Series(orders.Values.Select(x => x.Status.ToString()).ToList(), index));
                    pyDict.SetItem("Tag", _pandas.Series(orders.Values.Select(x => x.Tag).ToList(), index));
                    return _pandas.DataFrame(pyDict, columns: new[] { "Time", "Symbol", "Quantity", "Price", "Type", "Status", "Tag" }.ToList());
                }
            }
            return string.Join(Environment.NewLine, backtest.Errors).ToPython();
        }

        /// <summary>
        /// Read out the P/L of a specific backtest
        /// </summary>
        /// <param name="projectId">Project id for the backtest we'd like to read</param>
        /// <param name="backtestId">Backtest id for the backtest we'd like to read</param>
        /// <returns>andas.DataFrame for list of P/L</returns>
        public PyObject GetProfitLoss(int projectId, string backtestId)
        {
            var backtest = ReadBacktest(projectId, backtestId);
            if (backtest.Success)
            {
                using (Py.GIL())
                {
                    var pyDict = new PyDict();
                    pyDict.SetItem("ProfitLoss", _pandas.Series(
                        backtest.Result.ProfitLoss.Values.Select(x => (double)x).ToList(),
                        backtest.Result.ProfitLoss.Keys.ToList()));
                    return _pandas.DataFrame(pyDict);
                }
            }
            return string.Join(Environment.NewLine, backtest.Errors).ToPython();
        }
    }
}