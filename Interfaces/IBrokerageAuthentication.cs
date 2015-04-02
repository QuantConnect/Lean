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
        /// <returns>rue for OK (ie. no error)</returns>
        bool Validate();
    }
}
