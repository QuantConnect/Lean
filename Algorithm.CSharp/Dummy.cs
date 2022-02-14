using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect.Algorithm.CSharp
{
    class Dummy: QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2022,1,1);
            var gdp = AddFuture("ES").Symbol;

            Schedule.On(DateRules.EveryDay(gdp),
                TimeRules.AfterMarketOpen(gdp),
                EveryDayAfterMarketOpen);
        }

        public void EveryDayAfterMarketOpen()
        {
            Log($"{Time}");
        }
    }
}
