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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Orders;
using QuantConnect.Lean.Engine.Results.Analysis.Utils;

namespace QuantConnect.Lean.Engine.Results.Analysis.Analyses
{
    // ── Sub-test 1: both TP and SL filled ────────────────────────────────────────

    internal class TakeProfitAndStopLossBothFilledAnalysis : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(
            List<List<Order>> combos, Language language)
        {
            var result = combos
                .Where(orders => orders.Count(o => o.Status == OrderStatus.Filled) > 1)
                .Select(orders => orders.Select(OrdersReader.ParseOrder).ToList())
                .ToList<object>();

            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(result.Count > 0 ? (object)result : null, potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "There are some cases where both TP and SL orders filled, which can lead to an unintended position. " +
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

    // ── Sub-test 2: filled order's counterpart was not (or too late) cancelled ───

    internal class TakeProfitOrStopLossNotCanceledAnalysis : BaseBacktestAnalysis
    {
        public IReadOnlyList<BacktestAnalysisResult> Run(
            List<List<Order>> combos, Language language)
        {
            var result = new List<object>();

            foreach (var orders in combos)
            {
                var filledOrders    = orders.Where(o => o.Status == OrderStatus.Filled).ToList();
                if (filledOrders.Count != 1) continue; // both-filled case handled separately

                var filledOrder     = filledOrders[0];
                var cancelledOrders = orders.Where(o => o.Status == OrderStatus.Canceled).ToList();

                if (cancelledOrders.Count == 0)
                {
                    result.Add(orders.Select(OrdersReader.ParseOrder).ToList());
                    continue;
                }

                var cancelledOrder = cancelledOrders[0];
                if (cancelledOrder.CanceledTime != filledOrder.LastFillTime)
                    result.Add(orders.Select(OrdersReader.ParseOrder).ToList());
            }

            var potentialSolutions = result.Count > 0 ? PotentialSolutions(language) : [];
            return SingleResponse(result.Count > 0 ? (object)result : null, potentialSolutions);
        }

        private static List<string> PotentialSolutions(Language language) =>
        [
            "There are some cases where one of the TP/SL orders fills and the other one is left idle in the market. " +
            "To avoid dangling orders that can lead to unindended positions, immediately cancel one of the orders when the other one fills.\n" +
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

    // ── Orchestrator ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects TP/SL order pairs where both filled, or where the surviving leg
    /// was not cancelled when the other filled.
    /// </summary>
    public class TakeProfitAndStopLossOrdersAnalysis : BaseBacktestAnalysis
    {
        private static readonly OrderType[] TpTypes =
            [OrderType.Limit, OrderType.LimitIfTouched];

        private static readonly OrderType[] SlTypes =
            [OrderType.StopMarket, OrderType.TrailingStop, OrderType.StopLimit];

        private static readonly BaseBacktestAnalysis[] SubTests =
        [
            new TakeProfitAndStopLossBothFilledAnalysis(),
            new TakeProfitOrStopLossNotCanceledAnalysis(),
        ];

        public IReadOnlyList<BacktestAnalysisResult> Run(List<Order> orders, Language language)
        {
            // Group orders by (symbol, quantity, created_time) – the TP/SL fingerprint.
            var combos = orders
                .GroupBy(o => (o.Symbol, o.Quantity, o.CreatedTime))
                .Select(g => g.ToList())
                .Where(g => g.Count == 2)
                .Where(g =>
                    g.Any(o => TpTypes.Contains(o.Type)) &&
                    g.Any(o => SlTypes.Contains(o.Type)))
                .ToList();

            return CreateAggregatedResponse(
                SubTests.SelectMany(t =>
                    t is TakeProfitAndStopLossBothFilledAnalysis bt
                        ? bt.Run(combos, language)
                        : ((TakeProfitOrStopLossNotCanceledAnalysis)t).Run(combos, language)));
        }
    }
}
