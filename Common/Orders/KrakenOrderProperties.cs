using System;

namespace QuantConnect.Orders
{
    public class KrakenOrderProperties : OrderProperties
    {
        /// <summary>
        /// Comma delimited list of order flags. viqc = volume in quote currency (not currently available), fcib = prefer fee in base currency, fciq = prefer fee in quote currency,
        /// nompp = no market price protection, post = post only order (available when ordertype = limit)
        /// </summary>
        public string Oflags { get; set; }

        /// <summary>
        /// Conditional close orders are triggered by execution of the primary order in the same quantity and opposite direction. Ordertypes can be the same with primary order.
        /// </summary>
        public Order ConditionalOrder { get; set; } = null;
    }
}
