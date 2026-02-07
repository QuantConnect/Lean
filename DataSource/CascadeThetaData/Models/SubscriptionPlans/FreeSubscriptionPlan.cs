/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Util;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

public class FreeSubscriptionPlan : ISubscriptionPlan
{
    public HashSet<Resolution> AccessibleResolutions => new() { Resolution.Daily };

    public DateTime FirstAccessDate => new DateTime(2023, 06, 01);

    public uint MaxStreamingContracts => 0;

    public RateGate RateGate => new(30, TimeSpan.FromMinutes(1));
}
