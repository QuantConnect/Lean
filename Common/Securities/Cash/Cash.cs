using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Cash
{
    public class Cash : Security
    {
        public Cash(string symbol, Resolution resolution)
            : base(symbol, SecurityType.Cash, resolution, true, 1m, false, false)
        {
        }
    }
}
