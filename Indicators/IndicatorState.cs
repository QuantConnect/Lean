using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Indicators
{
    public enum IndicatorState
    {
        /// <summary>
        /// State of an indicator which haven't received any data (0)
        /// </summary>
        Cold,
        /// <summary>
        /// State of an indicator which have received at least one data (1)
        /// </summary>
        WarmingUp,
        /// <summary>
        /// State of an indicator which is ready (2)
        /// </summary>
        Ready,
    }
}
