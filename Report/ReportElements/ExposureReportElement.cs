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
    internal sealed class ExposureReportElement : ChartReportElement
    {
        private LiveResult _live;
        private BacktestResult _backtest;
        private List<PointInTimePortfolio> _backtestPortfolios;
        private List<PointInTimePortfolio> _livePortfolios;

        /// <summary>
        /// Create a new plot of the exposure
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="backtestPortfolios">Backtest point in time portfolios</param>
        /// <param name="livePortfolios">Live point in time portfolios</param>
        public ExposureReportElement(
            string name,
            string key,
            BacktestResult backtest,
            LiveResult live,
            List<PointInTimePortfolio> backtestPortfolios,
            List<PointInTimePortfolio> livePortfolios
        )
        {
            _backtest = backtest;
            _backtestPortfolios = backtestPortfolios;
            _live = live;
            _livePortfolios = livePortfolios;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the exposure plot using the python libraries.
        /// </summary>
        public override string Render()
        {
            var longBacktestFrame = Metrics.Exposure(_backtestPortfolios, OrderDirection.Buy);
            var shortBacktestFrame = Metrics.Exposure(_backtestPortfolios, OrderDirection.Sell);
            var longLiveFrame = Metrics.Exposure(_livePortfolios, OrderDirection.Buy);
            var shortLiveFrame = Metrics.Exposure(_livePortfolios, OrderDirection.Sell);

            var backtestFrame = longBacktestFrame
                .Join(shortBacktestFrame)
                .FillMissing(Direction.Forward)
                .FillMissing(0.0);

            var liveFrame = longLiveFrame
                .Join(shortLiveFrame)
                .FillMissing(Direction.Forward)
                .FillMissing(0.0);

            longBacktestFrame = Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();
            shortBacktestFrame = Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();
            longLiveFrame = Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();
            shortLiveFrame = Frame.CreateEmpty<DateTime, Tuple<SecurityType, OrderDirection>>();

            foreach (var key in backtestFrame.ColumnKeys)
            {
                longBacktestFrame[key] = backtestFrame[key].SelectValues(x => x < 0 ? 0 : x);
                shortBacktestFrame[key] = backtestFrame[key].SelectValues(x => x > 0 ? 0 : x);
            }

            foreach (var key in liveFrame.ColumnKeys)
            {
                longLiveFrame[key] = liveFrame[key].SelectValues(x => x < 0 ? 0 : x);
                shortLiveFrame[key] = liveFrame[key].SelectValues(x => x > 0 ? 0 : x);
            }

            longBacktestFrame = longBacktestFrame.DropSparseColumnsAll();
            shortBacktestFrame = shortBacktestFrame.DropSparseColumnsAll();
            longLiveFrame = longLiveFrame.DropSparseColumnsAll();
            shortLiveFrame = shortLiveFrame.DropSparseColumnsAll();

            var base64 = "";
            using (Py.GIL())
            {
                var time = backtestFrame.RowKeys.ToList().ToPython();
                var longSecurities = longBacktestFrame
                    .ColumnKeys.Select(x => x.Item1.ToStringInvariant())
                    .ToList()
                    .ToPython();
                var shortSecurities = shortBacktestFrame
                    .ColumnKeys.Select(x => x.Item1.ToStringInvariant())
                    .ToList()
                    .ToPython();
                var longData = longBacktestFrame
                    .ColumnKeys.Select(x => longBacktestFrame[x].Values.ToList().ToPython())
                    .ToPython();
                var shortData = shortBacktestFrame
                    .ColumnKeys.Select(x => shortBacktestFrame[x].Values.ToList().ToPython())
                    .ToPython();
                var liveTime = liveFrame.RowKeys.ToList().ToPython();
                var liveLongSecurities = longLiveFrame
                    .ColumnKeys.Select(x => x.Item1.ToStringInvariant())
                    .ToList()
                    .ToPython();
                var liveShortSecurities = shortLiveFrame
                    .ColumnKeys.Select(x => x.Item1.ToStringInvariant())
                    .ToList()
                    .ToPython();
                var liveLongData = longLiveFrame
                    .ColumnKeys.Select(x => longLiveFrame[x].Values.ToList().ToPython())
                    .ToPython();
                var liveShortData = shortLiveFrame
                    .ColumnKeys.Select(x => shortLiveFrame[x].Values.ToList().ToPython())
                    .ToPython();

                base64 = Charting.GetExposure(
                    time,
                    longSecurities,
                    shortSecurities,
                    longData,
                    shortData,
                    liveTime,
                    liveLongSecurities,
                    liveShortSecurities,
                    liveLongData,
                    liveShortData
                );
            }

            return base64;
        }
    }
}
