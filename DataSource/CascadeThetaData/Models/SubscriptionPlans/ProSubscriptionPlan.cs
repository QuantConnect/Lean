/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Util;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

public class ProSubscriptionPlan : ISubscriptionPlan
{
    public HashSet<Resolution> AccessibleResolutions => new() { Resolution.Tick, Resolution.Second, Resolution.Minute, Resolution.Hour, Resolution.Daily };

    public DateTime FirstAccessDate => new DateTime(2012, 06, 01);

    public uint MaxStreamingContracts => 15_000;

    public RateGate? RateGate => null;
}
