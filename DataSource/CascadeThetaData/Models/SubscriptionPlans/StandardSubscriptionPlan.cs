/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Util;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

public class StandardSubscriptionPlan : ISubscriptionPlan
{
    public HashSet<Resolution> AccessibleResolutions => new() { Resolution.Tick, Resolution.Second, Resolution.Minute, Resolution.Hour, Resolution.Daily };

    public DateTime FirstAccessDate => new DateTime(2016, 01, 01);

    public uint MaxStreamingContracts => 10_000;

    public RateGate? RateGate => null;
}
