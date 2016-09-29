using System;
using IBApi;

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    public sealed class CommissionReportEventArgs : EventArgs
    {
        public CommissionReport CommissionReport { get; private set; }
        public CommissionReportEventArgs(CommissionReport commissionReport)
        {
            CommissionReport = commissionReport;
        }
    }
}