using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Conversion
{
    /// <summary>
    /// This is used inside Cash class
    /// </summary>
    public class ConversionRate
    {
        public IConversionRateProvider conversionRateProvider;
        public Cash cash;

        public decimal GetRate()
        {
            return conversionRateProvider.GetPrice(cash);
        }

        public override string ToString()
        {
            return "ConversionRate: " + cash.Symbol + conversionRateProvider.TargetCurrency;
        }
    }
}