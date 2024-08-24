using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Statistics
{
    internal class DrawdownDrawdownDateHighValueDTO
    {
        public decimal DrawdownPercent { get; }
        public List<DateTime> MaxDrawdownEndDates { get; }
        public decimal HighPrice { get; }

        public DrawdownDrawdownDateHighValueDTO(decimal drawdownPercent, List<DateTime> maxDrawdownEndDates, decimal recoveryThresholdPrice)
        {
            DrawdownPercent = drawdownPercent;
            MaxDrawdownEndDates = maxDrawdownEndDates;
            HighPrice = recoveryThresholdPrice;
        }
    }
}
