using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Data.Custom
{
    public class ForexVolume : BaseData
    {
        public int Transanctions { get; set; }

        public ForexVolume()
        {
            DataType = MarketDataType.ForexVolume;
        }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode) throw new NotImplementedException("FOREX Volume data is not available in live mode, yet.");
            return base.GetSource(config, date, isLiveMode);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new ForexVolume
            {
                
            };
        }
    }
}
