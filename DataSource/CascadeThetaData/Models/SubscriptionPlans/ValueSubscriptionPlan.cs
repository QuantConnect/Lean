/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using QuantConnect.Util;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

public class ValueSubscriptionPlan : ISubscriptionPlan
{
    /// <inheritdoc />
    public HashSet<Resolution> AccessibleResolutions => new() { Resolution.Minute, Resolution.Hour, Resolution.Daily };

    public DateTime FirstAccessDate => new DateTime(2020, 01, 01);

    public uint MaxStreamingContracts => 0;

    public RateGate? RateGate => null;
}
