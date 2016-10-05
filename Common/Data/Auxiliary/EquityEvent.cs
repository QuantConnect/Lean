using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Class that represents split or dividend from yahoo
    /// </summary>
    public class EquityEvent
    {
        public EquityEventType EventType { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Represents a split or dividend
    /// </summary>
    public enum EquityEventType
    {
        Dividend,
        Split
    }
}
