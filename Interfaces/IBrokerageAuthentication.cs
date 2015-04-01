using System;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{

    public interface IBrokerageAuthentication
    {
        /// <summary>
        /// Validate Brokerage Authentication Parameters
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Validate(out string message);
    }
}
