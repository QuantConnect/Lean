# Issue Selection and Execution Plan

## Open bug candidates (small-scoped)
As of **February 20, 2026**, these are open bug issues in `QuantConnect/Lean` that look suitable for contained fixes:

1. **#9287** - [BUG] InteractiveBrokersBrokerageModel allows combos with four legs with incompatibles settings (ComboLegLimit and NonGuaranteed = true)  
   Link: https://github.com/QuantConnect/Lean/issues/9287  
   Why small: concentrated in order-submission validation plus brokerage model tests.

2. **#9248** - [BUG] QuantConnect.Lean.Launcher fails without config.lean-manager.json due to warm-up timestamp inconsistency  
   Link: https://github.com/QuantConnect/Lean/issues/9248  
   Why small/medium: likely limited to launcher/config timestamp handling, but touches startup flow.

3. **#6930** - [BUG] Logging warnings does not throw in tests  
   Link: https://github.com/QuantConnect/Lean/issues/6930  
   Why small: test-behavior alignment around warning/error handling; narrow blast radius.

## Best issue to execute
**Selected: #9287**

### Why this one
- Clear expected vs actual behavior in the issue description.
- Narrow technical surface area (Interactive Brokers combo-order prechecks).
- Straightforward regression-test opportunity.

## Implementation plan (no code in this document)

### Scope
- Add/adjust Interactive Brokers combo-order validation so unsupported 4-leg setting combinations are rejected before submission.
- Keep behavior unchanged for valid 2-leg/4-leg combinations.

### Likely files to touch
- `Common/Brokerages/InteractiveBrokersBrokerageModel.cs`
- `Tests/Common/Brokerages/InteractiveBrokersBrokerageModelTests.cs`
- `Common/Messages/Messages.Brokerages.cs` (only if new user-facing rejection text is needed)

### Steps
1. Reproduce current behavior in a new unit test case using `InteractiveBrokersBrokerageModel.CanSubmitOrder(...)` with 4-leg combo scenarios.
2. Identify where combo-leg count and order type/properties are available during prechecks.
3. Add targeted rule checks for invalid 4-leg combinations described in the issue.
4. Return a deterministic `BrokerageMessageEvent` (warning + actionable message) when rejecting.
5. Add positive tests proving valid combinations still pass.
6. Run focused test filter for brokerage model tests, then run broader impacted tests.

### Verification
- `dotnet test Tests/QuantConnect.Tests.csproj --filter "FullyQualifiedName~InteractiveBrokersBrokerageModelTests"`
- Confirm new negative and positive cases pass and no regressions in adjacent combo-order tests.

### Out of scope
- Any unrelated brokerage behavior changes.
- Refactoring combo order APIs beyond what is needed for #9287.
