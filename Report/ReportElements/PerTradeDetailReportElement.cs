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
using System.Globalization;
using System.Linq;
using System.Text;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class PerTradeDetailReportElement : ReportElement
    {
        private BacktestResult _backtest;
        private LiveResult _live;
        private readonly string _template;

        /// <summary>
        /// Create a new trade detail report element
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="template">Template for the trade details section</param>
        public PerTradeDetailReportElement(string name, string key, BacktestResult backtest, LiveResult live, string template)
        {
            _backtest = backtest;
            _live = live;
            _template = template;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// Generate the trade details HTML
        /// </summary>
        public override string Render()
        {
            if (Key == ReportKey.TradeDetailsPageStyle)
            {
                // Return CSS to hide the page if no trades
                var hasTrades = HasTradeData();
                return hasTrades ? "" : "display: none;";
            }

            // This handles the ReportKey.TradeDetails case - generate HTML content
            if (Key == ReportKey.TradeDetails)
            {
                if (!HasTradeData())
                {
                    return "<div class='col-xs-12'><h3>No trade data available</h3></div>";
                }

                var html = new StringBuilder();
                var tradeIndex = 1;

                // Process backtest trades using ClosedTrades
                if (_backtest?.TotalPerformance?.ClosedTrades != null && _backtest.TotalPerformance.ClosedTrades.Count > 0)
                {
                    var allTrades = _backtest.TotalPerformance.ClosedTrades.OrderBy(t => t.EntryTime).ToList();
                    const int tradesPerPage = 4;
                    var totalPages = (int)Math.Ceiling((double)allTrades.Count / tradesPerPage);
                    var currentPage = 1;
                    
                    for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                    {
                        var tradesOnThisPage = allTrades.Skip(pageIndex * tradesPerPage).Take(tradesPerPage);
                        
                        // Start new page container
                        if (pageIndex > 0)
                        {
                            html.AppendLine("</div>"); // Close previous page
                            html.AppendLine("</div>"); // Close previous container-row
                            html.AppendLine("</div>"); // Close previous content
                            html.AppendLine("</div>"); // Close previous page
                            
                            // Start new page
                            html.AppendLine("<div class='page' id='trade-details-page-" + (pageIndex + 1) + "'>");
                            html.AppendLine("    <div class='header'>");
                            html.AppendLine("        <div class='header-left'>");
                            html.AppendLine("            <img src='https://cdn.quantconnect.com/web/i/logo.png'>");
                            html.AppendLine("        </div>");
                            html.AppendLine("        <div class='header-right'>Trade Details Report: {{$TEXT-STRATEGY-NAME}} {{$TEXT-STRATEGY-VERSION}}</div>");
                            html.AppendLine("    </div>");
                            html.AppendLine("    <div class='content'>");
                            html.AppendLine("        <div class='container-row'>");
                        }
                        
                        if (pageIndex == 0)
                        {
                            html.AppendLine("<div class='col-xs-12'>");
                        }
                        else
                        {
                            html.AppendLine("            <div class='col-xs-12'>");
                        }
                        
                        html.AppendLine(CultureInfo.InvariantCulture, $"<h2>Backtest Trade Details - Page {currentPage} of {totalPages}</h2>");
                        
                        foreach (var trade in tradesOnThisPage)
                        {
                            if (trade.EntryPrice == 0m)
                            {
                                Log.Error($"PerTradeDetailReportElement.Render(): Encountered entry price of 0 in trade with entry time: {trade.EntryTime:yyyy-MM-dd HH:mm:ss} - Exit time: {trade.ExitTime:yyyy-MM-dd HH:mm:ss}");
                                continue;
                            }

                            // Calculate profit percentage
                            var sideMultiplier = trade.Direction == TradeDirection.Long ? 1 : -1;
                            var profitPercentage = sideMultiplier * (Convert.ToDouble(trade.ExitPrice) - Convert.ToDouble(trade.EntryPrice)) / Convert.ToDouble(trade.EntryPrice) * 100;
                            
                            // Get direction string
                            var direction = trade.Direction == TradeDirection.Long ? "Long" : "Short";
                            
                            html.AppendLine("<div class='trade-detail-container'>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<div class='trade-header'>Trade #{tradeIndex}</div>");
                            html.AppendLine("<div class='trade-content'>");
                            html.AppendLine("<table class='trade-table'>");
                            
                            // Row 1: Entry/Exit Times
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Entry Time:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>{trade.EntryTime:yyyy-MM-dd HH:mm:ss} UTC</td>");
                            html.AppendLine("<td class='trade-label'>Exit Time:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>{trade.ExitTime:yyyy-MM-dd HH:mm:ss} UTC</td>");
                            html.AppendLine("</tr>");
                            
                            // Row 2: Direction and P&L
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Direction:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value {(direction == "Long" ? "trade-long" : "trade-short")}'>{direction}</td>");
                            html.AppendLine("<td class='trade-label'>Profit/Loss (Gross):</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value {(trade.ProfitLoss >= 0 ? "trade-profit" : "trade-loss")}'>${trade.ProfitLoss:F2}</td>");
                            html.AppendLine("</tr>");
                            
                            // Row 3: Percentage and Duration
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Profit/Loss (%):</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value {(profitPercentage >= 0 ? "trade-profit" : "trade-loss")}'>{profitPercentage:F2}%</td>");
                            html.AppendLine("<td class='trade-label'>Duration:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>{trade.Duration.TotalHours:F1} hours</td>");
                            html.AppendLine("</tr>");
                            
                            // Row 4: Entry/Exit Prices
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Entry Price:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>${trade.EntryPrice:F2}</td>");
                            html.AppendLine("<td class='trade-label'>Exit Price:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>${trade.ExitPrice:F2}</td>");
                            html.AppendLine("</tr>");
                            
                            // Row 5: Quantity and Symbol
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Quantity:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>{trade.Quantity:F0}</td>");
                            html.AppendLine("<td class='trade-label'>Symbol:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>{trade.Symbol}</td>");
                            html.AppendLine("</tr>");
                            
                            // Row 6: MAE/MFE (drawdown info)
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Max Adverse Excursion:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value trade-loss'>${trade.MAE:F2}</td>");
                            html.AppendLine("<td class='trade-label'>Max Favorable Excursion:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value trade-profit'>${trade.MFE:F2}</td>");
                            html.AppendLine("</tr>");
                            
                            // Row 7: Fees and End Trade Drawdown
                            html.AppendLine("<tr>");
                            html.AppendLine("<td class='trade-label'>Total Fees:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value'>${trade.TotalFees:F2}</td>");
                            html.AppendLine("<td class='trade-label'>End Trade Drawdown:</td>");
                            html.AppendLine(CultureInfo.InvariantCulture, $"<td class='trade-value {(trade.EndTradeDrawdown >= 0 ? "trade-profit" : "trade-loss")}'>${trade.EndTradeDrawdown:F2}</td>");
                            html.AppendLine("</tr>");
                            
                            html.AppendLine("</table>");
                            html.AppendLine("</div>");
                            html.AppendLine("</div>");
                            
                            tradeIndex++;
                        }
                        
                        currentPage++;
                    }
                    
                    html.AppendLine("</div>"); // Close final col-xs-12
                }

                // Process live trades if available (Note: LiveResult doesn't have TotalPerformance.ClosedTrades)
                // We would need to implement a different approach for live trades if needed

                Result = html.ToString();
                return html.ToString();
            }

            // Default case - return empty string for unknown keys
            return "";
        }

        /// <summary>
        /// Check if there is trade data available
        /// </summary>
        private bool HasTradeData()
        {
            return (_backtest?.TotalPerformance?.ClosedTrades != null && _backtest.TotalPerformance.ClosedTrades.Count > 0);
        }
    }
}
