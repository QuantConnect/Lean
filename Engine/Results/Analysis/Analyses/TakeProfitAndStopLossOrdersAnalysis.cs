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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    /// <summary>
    /// Detects TP/SL order pairs where both filled, or where the surviving leg
    /// was not cancelled when the other filled.
    /// </summary>
    public class TakeProfitAndStopLossOrdersAnalysis : BaseResultsAnalysis
    {
        public override string Issue { get; } = "The algorithm isn't correctly handling take-profit and stop-loss order pairs.";

        public override int Weight { get; } = 68;
        public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => Run(parameters.Result.Orders.Values, parameters.Language);

        private static readonly OrderType[] TpTypes = [OrderType.Limit, OrderType.LimitIfTouched];

        private static readonly OrderType[] SlTypes = [OrderType.StopMarket, OrderType.TrailingStop, OrderType.StopLimit];

        private static readonly ISubAnalysis[] SubAnalyses =
        [
            new TakeProfitAndStopLossBothFilledAnalysis(),
            new TakeProfitOrStopLossNotCanceledAnalysis(),
        ];

        /// <summary>
        /// Groups orders into TP/SL pairs by symbol, quantity, and creation time, then
        /// delegates to sub-analyses that check for both-filled and dangling-order scenarios.
        /// </summary>
        /// <param name="orders">All orders from the backtest result.</param>
        /// <param name="language">The programming language the algorithm is written in.</param>
        /// <returns>Aggregated analysis results from all sub-analyses that detected issues.</returns>
        public IReadOnlyList<AnalysisResult> Run(ICollection<Order> orders, Language language)
        {
            // Group orders by (symbol, quantity, created_time) – the TP/SL fingerprint.
            var combos = orders
                .GroupBy(o => (o.Symbol, o.Quantity, o.CreatedTime))
                .Select(g => g.Take(2).ToList())
                .Where(g => g.Count == 2)
                .Where(g =>
                    g.Any(o => TpTypes.Contains(o.Type)) &&
                    g.Any(o => SlTypes.Contains(o.Type)))
                .ToList();

            return CreateAggregatedResponse(SubAnalyses.SelectMany(x => x.Run(combos, language)));
        }

        private interface ISubAnalysis
        {
            public IReadOnlyList<AnalysisResult> Run(List<List<Order>> combos, Language language);
        }

        // ── Sub-analysis 1: both TP and SL filled ────────────────────────────────────────

        private class TakeProfitAndStopLossBothFilledAnalysis : BaseResultsAnalysis, ISubAnalysis
        {
            public override string Issue { get; } = "There are some cases where both of the TP and SL orders filled, which can lead to an unintended position.";

            public override int Weight { get; } = 68;
            public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => throw new NotSupportedException("Use TakeProfitAndStopLossOrdersAnalysis.");

            public IReadOnlyList<AnalysisResult> Run(List<List<Order>> combos, Language language)
            {
                var result = combos
                    .Where(orders => orders.All(o => o.Status == OrderStatus.Filled))
                    .ToList();

                var potentialSolutions = result.Count > 0 ? Solutions(language) : [];
                return SingleResponse(new ResultsAnalysisRepeatedContext(result), potentialSolutions);
            }

            private static List<string> Solutions(Language language) =>
            [
                "To avoid this issue, try increasing the data resolution.",

            "Set the take profit and stop loss orders further away from the current market price.",

            "Simulate OCO orders instead of placing idle orders that wait for the market price to trigger them.\n" +
            (language == Language.Python
                ? """
                  ```
                  def on_data(self, slice: Slice) -> None:
                      if not self.portfolio.invested:
                          ticket = self.market_order("SPY", 1)
                          self.entry_price = ticket.average_fill_price

                      bar = slice.get("SPY")
                      if bar:
                          if bar.price >= self.entry_price * 1.10:
                              self.liquidate(symbol="SPY", -1, tag="take profit")

                          elif bar.price <= self.entry_price * 0.95:
                              self.liquidate(symbol="SPY", -1, tag="stop loss")
                  ```
                  """
                : """
                  ```
                  decimal _entryPrice;

                  public override void OnData(Slice slice)
                  {
                      if (!Portfolio.Invested)
                      {
                          var ticket = MarketOrder("SPY", 1);
                          _entryPrice = ticket.AverageFillPrice;
                      }

                      if (!slice.Bars.ContainsKey("SPY")) return;

                      if (slice.Bars["SPY"].Price >= _entryPrice * 1.10m)
                      {
                          Liquidate(symbol: "SPY", -1, tag: "take profit");
                      }
                      else if (slice.Bars["SPY"].Price <= _entryPrice * 0.95m)
                      {
                          Liquidate(symbol: "SPY", -1, tag: "stop loss");
                      }
                  }
                  ```
                  """),
        ];
        }

        // ── Sub-analysis 2: filled order's counterpart was not (or too late) cancelled ───

        private class TakeProfitOrStopLossNotCanceledAnalysis : BaseResultsAnalysis, ISubAnalysis
        {
            public override string Issue { get; } = "There are some cases where one of the TP/SL orders filled and the other one was left idle in the market.";

            public override int Weight { get; } = 68;
            public override IReadOnlyList<AnalysisResult> Run(ResultsAnalysisRunParameters parameters) => throw new NotSupportedException("Use TakeProfitAndStopLossOrdersAnalysis.");

            public IReadOnlyList<AnalysisResult> Run(List<List<Order>> combos, Language language)
            {
                var result = new List<object>();

                foreach (var orders in combos)
                {
                    var filledOrders = orders.Where(o => o.Status == OrderStatus.Filled).ToList();
                    // both-filled case handled separately
                    if (filledOrders.Count != 1)
                    {
                        continue;
                    }

                    var filledOrder = filledOrders[0];
                    var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Canceled).ToList();

                    if (cancelledOrders.Count == 0 || cancelledOrders[0].CanceledTime != filledOrder.LastFillTime)
                    {
                        result.Add(orders);
                    }
                }

                var potentialSolutions = result.Count > 0 ? Solutions(language) : [];
                return SingleResponse(new ResultsAnalysisRepeatedContext(result), potentialSolutions);
            }

            private static List<string> Solutions(Language language) =>
            [
                "To avoid dangling orders that can lead to unintended positions, immediately cancel one of the orders when the other one fills.\n" +
            (language == Language.Python
                ? """
                  ```
                  _stop_loss = None
                  _take_profit = None

                  def on_order_event(self, order_event: OrderEvent) -> None:
                      if order_event.status != OrderStatus.FILLED:
                          return

                      match order_event.ticket.order_type:
                          case OrderType.MARKET:
                              self._stop_loss = self.stop_market_order(order_event.symbol, -order_event.fill_quantity, order_event.fill_price*0.95)
                              self._take_profit = self.limit_order(order_event.symbol, -order_event.fill_quantity, order_event.fill_price*1.10)
                          case OrderType.STOP_MARKET:
                              self._take_profit.cancel()
                          case OrderType.LIMIT:
                              self._stop_loss.cancel()
                  ```
                  """
                : """
                  ```
                  private OrderTicket _stopLoss;
                  private OrderTicket _takeProfit;

                  public override void OnOrderEvent(OrderEvent orderEvent)
                  {
                      if (orderEvent.Status != OrderStatus.Filled) return;

                      switch (orderEvent.Ticket.OrderType)
                      {
                          case OrderType.Market:
                              _stopLoss = StopMarketOrder(orderEvent.Symbol, -orderEvent.FillQuantity, orderEvent.FillPrice*0.95m);
                              _takeProfit = LimitOrder(orderEvent.Symbol, -orderEvent.FillQuantity, orderEvent.FillPrice*1.10m);
                              return;
                          case OrderType.StopMarket:
                              _takeProfit?.Cancel();
                              return;
                          case OrderType.Limit:
                              _stopLoss?.Cancel();
                              return;
                      }
                  }
                  ```
                  """),
        ];
        }
    }
}
