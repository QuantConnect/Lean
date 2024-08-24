using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Statistics
{
    internal class DrawdownPercentageDrawdownDatesHighValueDTO
    {
        public decimal DrawdownPercent { get; }
        public List<DateTime> MaxDrawdownEndDates { get; }
        public decimal HighPrice { get; }

        public DrawdownPercentageDrawdownDatesHighValueDTO(decimal drawdownPercent, List<DateTime> maxDrawdownEndDates, decimal recoveryThresholdPrice)
        {
            DrawdownPercent = drawdownPercent;
            MaxDrawdownEndDates = maxDrawdownEndDates;
            HighPrice = recoveryThresholdPrice;
        }
    }
}
