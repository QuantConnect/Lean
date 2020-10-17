using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// ZerodhaBrokerage Class: IDataQueueHandler implementation
    /// </summary>
    public partial class ZerodhaBrokerage : IDataQueueHandler
    {
        #region IDataQueueHandler implementation

        public void SetJob(LiveNodePacket job)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
