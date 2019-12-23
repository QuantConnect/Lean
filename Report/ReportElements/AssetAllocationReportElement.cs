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
using Deedle;
using Python.Runtime;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class AssetAllocationReportElement : ChartReportElement
    {
        private BacktestResult _backtest;
        private List<PointInTimePortfolio> _backtestPortfolios;
        private LiveResult _live;
        private List<PointInTimePortfolio> _livePortfolios;

        /// <summary>
        /// Create a new plot of the asset allocation over time
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="backtestPortfolios">Backtest point in time portfolios</param>
        /// <param name="livePortfolios">Live point in time portfolios</param>
        public AssetAllocationReportElement(
            string name,
            string key,
            BacktestResult backtest,
            LiveResult live,
            List<PointInTimePortfolio> backtestPortfolios,
            List<PointInTimePortfolio> livePortfolios)
        {
            _backtest = backtest;
            _backtestPortfolios = backtestPortfolios;
            _live = live;
            _livePortfolios = livePortfolios;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the asset allocation pie chart using the python libraries.
        /// </summary>
        public override string Render()
        {
            var backtestSeries = Metrics.AssetAllocations(_backtestPortfolios);
            var liveSeries = Metrics.AssetAllocations(_livePortfolios);

            PyObject result;

            using (Py.GIL())
            {
                var data = new PyList();
                var liveData = new PyList();

                data.Append(backtestSeries.SortBy(x => -x).Where(x => x.Value != 0).Keys.Select(x => x.Value).ToList().ToPython());
                data.Append(backtestSeries.SortBy(x => -x).Where(x => x.Value != 0).Values.ToList().ToPython());

                liveData.Append(liveSeries.SortBy(x => -x).Where(x => x.Value != 0).Keys.Select(x => x.Value).ToList().ToPython());
                liveData.Append(liveSeries.SortBy(x => -x).Where(x => x.Value != 0).Values.ToList().ToPython());

                result = Charting.GetAssetAllocation(data, liveData);
            }

            var base64 = result.ConvertToDictionary<string, string>();
            if (base64.ContainsKey("Live Asset Allocation"))
            {
                return base64["Live Asset Allocation"];
            }

            return base64["Backtest Asset Allocation"];
        }
    }
}