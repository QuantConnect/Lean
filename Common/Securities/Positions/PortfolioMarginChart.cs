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
 *
*/

using System;
using System.Linq;
using System.Drawing;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Securities.Positions
{
    /// <summary>
    /// Helper method to sample portfolio margin chart
    /// </summary>
    public static class PortfolioMarginChart
    {
        private static string PortfolioMarginTooltip = "{SERIES_NAME}: {VALUE}%";
        private static string PortfolioMarginIndexName = "Margin Used (%)";
        private static readonly int _portfolioMarginSeriesCount = Configuration.Config.GetInt("portfolio-margin-series-count", 5);

        /// <summary>
        /// Helper method to add the portfolio margin series into the given chart
        /// </summary>
        public static void AddSample(Chart portfolioChart, PortfolioState portfolioState, IMapFileProvider mapFileProvider, DateTime currentTime)
        {
            if (portfolioState == null || portfolioState.PositionGroups == null)
            {
                return;
            }

            var topSeries = new HashSet<string>(_portfolioMarginSeriesCount);
            foreach (var positionGroup in portfolioState.PositionGroups
                .OrderByDescending(x => x.PortfolioValuePercentage)
                .DistinctBy(x => GetPositionGroupName(x, mapFileProvider, currentTime))
                .Take(_portfolioMarginSeriesCount))
            {
                topSeries.Add(positionGroup.Name);
            }

            Series others = null;
            ChartPoint currentOthers = null;
            foreach (var positionGroup in portfolioState.PositionGroups)
            {
                var name = GetPositionGroupName(positionGroup, mapFileProvider, currentTime);
                if (topSeries.Contains(name))
                {
                    var series = GetOrAddSeries(portfolioChart, name, Color.Empty);
                    series.AddPoint(new ChartPoint(portfolioState.Time, positionGroup.PortfolioValuePercentage * 100));
                    continue;
                }

                others ??= GetOrAddSeries(portfolioChart, "OTHERS", Color.Gray);
                var value = positionGroup.PortfolioValuePercentage * 100;
                if (currentOthers != null && currentOthers.Time == portfolioState.Time)
                {
                    // we aggregate
                    currentOthers.y += value;
                }
                else
                {
                    currentOthers = new ChartPoint(portfolioState.Time, value);
                    others.AddPoint(currentOthers);
                }
            }

            foreach (var series in portfolioChart.Series.Values)
            {
                // let's add a null point for the series which have no value for this time
                var lastPoint = series.Values.LastOrDefault() as ChartPoint;
                if (lastPoint == null || lastPoint.Time != portfolioState.Time && lastPoint.Y.HasValue)
                {
                    series.AddPoint(new ChartPoint(portfolioState.Time, null));
                }
            }
        }

        private static string GetPositionGroupName(PositionGroupState positionGroup, IMapFileProvider mapFileProvider, DateTime currentTime)
        {
            if (positionGroup.Positions.Count == 0)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(positionGroup.Name))
            {
                positionGroup.Name = string.Join(", ", positionGroup.Positions.Select(x =>
                {
                    if (mapFileProvider == null)
                    {
                        return x.Symbol.Value;
                    }
                    return GetMappedSymbol(mapFileProvider, x.Symbol, currentTime).Value;
                }));
            }

            return positionGroup.Name;
        }

        private static Series GetOrAddSeries(Chart portfolioChart, string seriesName, Color color)
        {
            if (!portfolioChart.Series.TryGetValue(seriesName, out var series))
            {
                series = portfolioChart.Series[seriesName] = new Series(seriesName, SeriesType.StackedArea, 0, "%")
                {
                    Color = color,
                    Tooltip = PortfolioMarginTooltip,
                    IndexName = PortfolioMarginIndexName,
                    ScatterMarkerSymbol = ScatterMarkerSymbol.None
                };
            }
            return (Series)series;
        }

        private static Symbol GetMappedSymbol(IMapFileProvider mapFileProvider, Symbol symbol, DateTime referenceTime)
        {
            if (symbol.RequiresMapping())
            {
                var mapFileResolver = mapFileProvider.Get(AuxiliaryDataKey.Create(symbol));
                if (mapFileResolver.Any())
                {
                    var mapFile = mapFileResolver.ResolveMapFile(symbol);
                    if (mapFile.Any())
                    {
                        symbol = symbol.UpdateMappedSymbol(mapFile.GetMappedSymbol(referenceTime.Date, symbol.Value));
                    }
                }
            }
            return symbol;
        }

        /// <summary>
        /// Helper method to set the tooltip values after we've sampled and filter series with a single value
        /// </summary>
        public static void RemoveSinglePointSeries(Chart portfolioChart)
        {
            // let's remove series which have a single value, since it's a area chart they can't be drawn
            portfolioChart.Series = portfolioChart.Series.Values
                .Where(x =>
                {
                    var notNullPointsCount = 0;
                    foreach (var point in x.Values.OfType<ChartPoint>())
                    {
                        if (point != null && point.Y.HasValue)
                        {
                            notNullPointsCount++;
                            if (notNullPointsCount > 1)
                            {
                                return true;
                            }
                        }
                    }
                    return notNullPointsCount > 1;
                })
                .ToDictionary(x => x.Name, x => x);
        }
    }
}
