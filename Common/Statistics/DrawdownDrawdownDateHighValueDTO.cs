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
        public DateTime MaxDrawdownEndDate { get; }
        public decimal HighPrice { get; }

        public DrawdownDrawdownDateHighValueDTO(decimal drawdownPercent, DateTime maxDrawdownEndDate, decimal recoveryThresholdPrice)
        {
            DrawdownPercent = drawdownPercent;
            MaxDrawdownEndDate = maxDrawdownEndDate;
            HighPrice = recoveryThresholdPrice;
        }
    }
}
