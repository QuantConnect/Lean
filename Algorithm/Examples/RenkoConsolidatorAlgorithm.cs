using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.Examples
{
    public class RenkoConsolidatorAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2012, 01, 01);
            SetEndDate(2013, 01, 01);

            AddSecurity(SecurityType.Equity, "SPY");

            // break SPY into $10 renko bricks and send that data to our 'OnRenkoBar' method
            var renko = new RenkoConsolidator(10);
            renko.DataConsolidated += (sender, consolidated) =>
            {
                OnRenkoBar((RenkoBar) consolidated);
            };

            // register the consolidator for updates
            SubscriptionManager.AddConsolidator("SPY", renko);
        }

        public void OnData(TradeBars data)
        {
            
        }

        public void OnRenkoBar(RenkoBar data)
        {
            // plot both
            Plot("SPY_Renko", data.BarHigh);
            Plot("SPY_Renko", data.BarLow);
        }
    }
}
