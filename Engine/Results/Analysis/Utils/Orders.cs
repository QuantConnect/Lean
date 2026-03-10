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
using QuantConnect.Packets;

namespace QuantConnect.Lean.Engine.Results.Analysis.Utils
{
    /// <summary>
    /// Reads orders and order events directly from a <see cref="BacktestResult"/>.
    /// Mirrors <c>utils/orders.py</c>.
    /// </summary>
    public static class OrdersReader
    {
        public static (List<Order> Orders, List<OrderEvent> OrderEvents) Read(BacktestResult result)
        {
            var orders = result.Orders?.Values.ToList() ?? [];
            var orderEvents = result.OrderEvents ?? [];

            if (orderEvents.Count == 0)
                throw new Exception("Error loading order events. Try again.");

            return (orders, orderEvents);
        }

        public static Dictionary<string, object?> ParseOrder(Order order) => new()
        {
            ["symbol"] = order.Symbol.ToString(),
            ["quantity"] = order.Quantity,
            ["created_time"] = order.CreatedTime.ToString(),
            ["canceled_time"] = order.CanceledTime?.ToString(),
            ["last_fill_time"] = order.LastFillTime?.ToString(),
            ["status"] = OrderStatusHelper.Parse(order.Status),
        };

        public static Dictionary<string, object?> ParseOrderEvent(OrderEvent e) => new()
        {
            ["order_id"] = e.OrderId,
            ["order_event_id"] = e.Id,
            ["symbol"] = e.Symbol.ToString(),
            ["time"] = e.UtcTime.ToString(),
            ["fill_price"] = e.FillPrice,
            ["fill_quantity"] = e.FillQuantity,
            ["is_assignment"] = e.IsAssignment,
            ["message"] = e.Message,
        };
    }

    public static class OrderStatusHelper
    {
        private static readonly string[] Names =
        [
            "NEW", "SUBMITTED", "PARTIALLY_FILLED",
            "FILLED", "CANCELED", "NONE", "INVALID",
            "CANCEL_PENDING", "UPDATE_SUBMITTED",
        ];

        public static string Parse(OrderStatus status) => Names[(int)status];
    }
}
