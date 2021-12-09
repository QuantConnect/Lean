using QuantConnect.Data;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class ConstantVolatilityModel : IVolatilityModel
    {
        private readonly decimal _volatility;

        public ConstantVolatilityModel(decimal volatility)
        {
            this._volatility = volatility;
        }

        public decimal Volatility => _volatility;

        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            return Array.Empty<HistoryRequest>();
        }

        public void Update(Security security, BaseData data)
        {
            
        }
    }
}
