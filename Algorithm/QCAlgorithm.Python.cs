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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, Resolution? resolution = null)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, resolution);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, Resolution? resolution = null)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, resolution);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, Resolution? resolution, Func<IBaseData, IBaseDataBar> selector)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, Resolution? resolution, Func<IBaseData, TradeBar> selector)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, TimeSpan? resolution, Func<IBaseData, IBaseDataBar> selector)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, TimeSpan? resolution, Func<IBaseData, TradeBar> selector)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, resolution, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IBaseDataBar> indicator, IDataConsolidator consolidator, Func<IBaseData, IBaseDataBar> selector)
        {
            RegisterIndicator<IBaseDataBar>(symbol, indicator, consolidator, selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<TradeBar> indicator, IDataConsolidator consolidator, Func<IBaseData, TradeBar> selector)
        {
            RegisterIndicator<TradeBar>(symbol, indicator, consolidator, selector);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, params IndicatorBase<IndicatorDataPoint>[] indicators)
        {
            Plot<IndicatorDataPoint>(chart, indicators);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, params IndicatorBase<IBaseDataBar>[] indicators)
        {
            Plot<IBaseDataBar>(chart, indicators);
        }

        /// <summary>
        /// Plots the value of each indicator on the chart
        /// </summary>
        /// <param name="chart">The chart's name</param>
        /// <param name="indicators">The indicatorsto plot</param>
        /// <seealso cref="Plot(string,string,decimal)"/>
        public void Plot(string chart, params IndicatorBase<TradeBar>[] indicators)
        {
            Plot<TradeBar>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, params IndicatorBase<IndicatorDataPoint>[] indicators)
        {
            PlotIndicator<IndicatorDataPoint>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, params IndicatorBase<IBaseDataBar>[] indicators)
        {
            PlotIndicator<IBaseDataBar>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available
        /// </summary>
        public void PlotIndicator(string chart, params IndicatorBase<TradeBar>[] indicators)
        {
            PlotIndicator<TradeBar>(chart, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase<IndicatorDataPoint>[] indicators)
        {
            PlotIndicator<IndicatorDataPoint>(chart, waitForReady, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase<IBaseDataBar>[] indicators)
        {
            PlotIndicator<IBaseDataBar>(chart, waitForReady, indicators);
        }

        /// <summary>
        /// Automatically plots each indicator when a new value is available, optionally waiting for indicator.IsReady to return true
        /// </summary>
        public void PlotIndicator(string chart, bool waitForReady, params IndicatorBase<TradeBar>[] indicators)
        {
            PlotIndicator<TradeBar>(chart, waitForReady, indicators);
        }
    }
}
