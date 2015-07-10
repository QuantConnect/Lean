using System;
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Defines a request to update an order's values
    /// </summary>
    public class UpdateOrderRequest : OrderRequest
    {
        /// <summary>
        /// Gets <see cref="Orders.OrderRequestType.Update"/>
        /// </summary>
        public override OrderRequestType OrderRequestType
        {
            get { return OrderRequestType.Update; }
        }

        /// <summary>
        /// Gets the new quantity of the order, null to not change the quantity
        /// </summary>
        public int? Quantity { get; private set; }

        /// <summary>
        /// Gets the new limit price of the order, null to not change the limit price
        /// </summary>
        public decimal? LimitPrice { get; private set; }

        /// <summary>
        /// Gets the new stop price of the order, null to not change the stop price
        /// </summary>
        public decimal? StopPrice { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOrderRequest"/> class
        /// </summary>
        /// <param name="time">The time the request was submitted</param>
        /// <param name="orderId">The order id to be updated</param>
        /// <param name="fields">The fields defining what should be updated</param>
        public UpdateOrderRequest(DateTime time, int orderId, UpdateOrderFields fields)
            : base(time, orderId, fields.Tag)
        {
            Quantity = fields.Quantity;
            LimitPrice = fields.LimitPrice;
            StopPrice = fields.StopPrice;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var updates = new List<string>();
            if (Quantity.HasValue)
            {
                updates.Add("Quantity: " + Quantity.Value);
            }
            if (LimitPrice.HasValue)
            {
                updates.Add("LimitPrice: " + LimitPrice.Value.SmartRounding());
            }
            if (StopPrice.HasValue)
            {
                updates.Add("StopPrice: " + StopPrice.Value.SmartRounding());
            }
            return string.Format("{0} UTC: Update Order: ({1}) - {2} {3} Status: {4}", Time, OrderId, string.Join(", ", updates), Tag, Status);
        }
    }
}