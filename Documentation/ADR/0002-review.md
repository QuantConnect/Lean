# Review of ADR 0002 (OCO order group)

Date: 2026-07-17. Reviewed against the real code in `Lean`, `Lean.Brokerages.Alpaca` and `Lean.Brokerages.InteractiveBrokers`. Review criteria: (1) the design must be abstract enough that OTO and Bracket reuse it, (2) it must reuse existing Lean logic instead of adding parallel mechanisms.

**Overall:** the ADR is strong. Almost every file and line reference checks out. The reuse table is mostly honest. But the review found one confirmed engine-level design bug, two claims in the reuse story that are not true in the code, one mitigation plan that cannot work as written, and several smaller gaps.

## Must fix

### 1. Scan can process the same group twice in one bar

`Scan` loops over every pending order (`BacktestingBrokerage.cs:248`), and each leg resolves the same group again at `:272`. Combos survive this today only because all legs fill together and leave the pending set on the first visit. The v1 rule keeps every leg pending after a partial fill (lifecycle row 7). So the sibling's loop iteration runs the OCO processor again on the same bar, and `TryFillLeg` can fill the partially-filled leg a second time — duplicate fill events, broken volume-limited fill models. The five-touch invariant does not cover this; it only controls removal.

**Fix:** add a sixth rule — process each group at most once per Scan pass (a visited-group-id set, or only process a group from its lowest-id open leg). OTO and bracket need this rule even more, because their retained closed trigger legs add extra group visits.

Also: the processor sketch has no `Combo` case. Add one sentence saying combos stay on the existing path (they do not migrate), so the two-pipelines situation is a stated decision, not an accident.

### 2. "One submitter for every group type" is false — combos can never fold into `SubmitGroupOrder`

The combo path never builds `Order` objects at all. It goes `Leg` -> `SubmitOrderRequest` directly, and orders are created later by `Order.CreateOrder` in the handler (`Common/Orders/Order.cs:413-481`). Combo quantities are ratios that need the manager to resolve. So `SubmitComboOrder` cannot become a wrapper over `SubmitGroupOrder(ComboType, List<Order>, ...)`, and as written Lean gets a **second parallel submitter** with the sync-wait rule in two copies — the exact thing criterion (2) forbids.

**Fix:** extract the genuinely shared tail (pre-check all -> single invalid ticket -> AddOrder loop -> per-type sync wait, `QCAlgorithm.Trading.cs:979-1006`, ~30 lines) into a request-level helper both submitters call, and reword the "one submitter" claim to match.

### 3. The Python-wrapper fallback cannot work as described

The ADR says the wrapper "must fall back to the default answer when a user's Python model does not define the new method". But `BrokerageModelPythonWrapper` has zero per-method fallbacks, and the real failure point is construction: `PythonWrapper.ValidateImplementationOf` (`PythonWrapper.cs:39-71`) requires **every** interface member to exist on the Python object, or it throws — before any call ever happens. Python models that subclass `DefaultBrokerageModel` are fine (they inherit the method). Pure duck-typed Python models break at construction the moment the member is added to `IBrokerageModel`.

**Fix:** plan the real mechanism — exclude the member from validation plus a `HasAttr` call-time check (precedent: `VolatilityModelPythonWrapper.cs:82`), or accept the construction-time break and say so.

### 4. The buying-power `ComboType` branch must run *before* `TryCreatePositionGroup`

`HasSufficientBuyingPowerForOrder(List<Order>)` first tries `Positions.TryCreatePositionGroup(orders, ...)` (`SecurityPortfolioManager.cs:944`). Two option legs on different contracts can form a valid option strategy and get margined as if **both** execute — wrong for a one-winner group. The ADR only says where the branch lives, not that it must come before the position-group attempt.

Two smaller corrections in the same block:

- The same-symbol mechanism the ADR cites is wrong — `ToDictionary` at `PositionGroupBuyingPowerModel.cs:94-96` always **throws** on duplicates (never sums), and it is unreachable anyway because the resolvers reject earlier. The conclusion (per-leg path) survives; the explanation does not.
- "Most expensive leg" has no defined metric across symbols/currencies/margin models — already flagged in open questions, but v1 could simply restrict multi-symbol groups to the conservative per-leg sum and keep max-leg for the one-symbol case.

## Should fix

### 5. `ExpireGroup` already exists — reuse it, don't rebuild it

`TryOrderPreChecks` (`BacktestingBrokerage.cs:597-643`) already cancels the **whole resolved group** when any leg expires (`:626-629`, comment: "We remove all orders in the combo"). The only missing piece is the skip-closed guard in `RemoveOrders` (`:584-595`). The ADR presents `ExpireGroup` as a new named step — a reuse miss inside the reuse document itself.

### 6. `SecurityTransactionManager.CancelOpenOrders` has the same group-blind cancel loop as `Liquidate`

It is used by `SetHoldings` with `liquidateExistingHoldings`. Same fix needed. Also, the "fails loudly" claim is overstated: the second cancel returns `OrderResponse.InvalidStatus` quietly (`BrokerageTransactionHandler.cs:1078-1082`); the loud `_algorithm.Error` path only fires when the leg is still open but gone from pending. The fix is still right; the justification text is wrong.

### 7. Update semantics stop at price

`ticket.Update` also carries quantity and tag. What does a per-leg **quantity** update mean for a group whose `GroupOrderManager.Quantity` is the first leg's quantity? And the Alpaca section says nothing about updating a leg of a live OCO group. Define it: v1 allows price-only updates (reject the rest), and add the per-broker update mapping or an explicit rejection.

### 8. Live restart is under-defined, and the IB section oversells reuse

IB today: `OcaGroup` is completely unwired — the only mention is a "not yet supported" comment (`InteractiveBrokersBrokerage.cs:3062-3064`), `GetOpenOrders` rebuilds managers only from BAG contracts (`:3075-3078`), and `Transmit` is hardcoded `true` (`:2909`). So the IB plan is new build, not light reuse, and the restart mechanics (matching `lean-oco-{id}` strings back to Lean order ids and a manager id) are unstated. Alpaca: there is **no** restart/restore plan at all — after a restart with an open OCO, the parent+child from `GET /v2/orders` must be rebuilt into one Lean group.

### 9. "Count one leg per group" is hand-written at four unrelated sites

Group margin rule, `CashBuyingPowerModel` sibling exclusion, `GetProjectedHoldings`, `Shortable` — with no shared helper. ADR 0003 changes the rule at all four again (dormant children). Build one primitive (for example, an "effective exposure legs of a group" helper next to `GroupOrderExtensions`) so each future PR edits one place. This is the single best abstraction improvement for criterion (1).

### 10. The buffering row in the reuse table mixes two different mechanisms

Engine-side buffering is `_completeOrders` + `TryGetGroupOrders` (`BrokerageTransactionHandler.cs:877-893`); `GroupOrderCacheManager` is plugin-side only — zero references in `Engine/`. Split the row. It matters because OTO's dormancy work will touch the engine-side one.

## Worth a look

### 11. The same-batch cancel promise is backtest-only

Live streams deliver the winner's fill and the siblings' cancels as separate events (Alpaca even documents the both-fill race). The lifecycle table states the batch rule with no qualifier — add one sentence.

### 12. `SubmitGroupOrder` cannot say which leg is the trigger

PR 2 will need a signature change or a convention. Cheap fix now: document "first order in the list is the trigger leg" as the convention (the ADR already uses "first order provides quantity/direction" for OCO).

### 13. The reserved `TriggerOrderId` will hit the same erasure trap as `comboType`

`DeserializeGroupOrderManager` stays field-by-field, so the new field will be silently dropped on round trip unless ADR 0003 also edits the deserializer. One sentence in the ADR 0003 plan prevents a repeat.

### 14. Audit head start

The grep audit promised for PR 1 was effectively run during this review. Two consumers beyond the ones the ADR names assume manager==combo:

- `DefaultMarginCallModel` (`:152-159`, `:199-203`) — groups margin-call liquidations by manager id to "execute together".
- `GetOrdersByBrokerageId` (`BrokerageTransactionHandler.cs:644-651`) — assumes grouped legs share one brokerage id, which is false for the per-leg-id IB OCO mapping; it works via the `_completeOrders` fallback, but the assumption is baked in.

## Small facts to correct in the text

- `PaperBrokerage` does **not** reuse `Scan` unchanged — it overrides it (dividends) and then delegates to `base.Scan()`. The "paper comes free" conclusion still holds.
- The `CancelOrder` overwrite ("Filled leg overwritten with Canceled") is unreachable today — once a leg leaves pending, the group cannot resolve, so `CancelOrder` returns false. It becomes real only under the retain-closed-legs invariant — and then it **would** land, because the event pipeline lets a Canceled event overwrite a Filled order (`BrokerageTransactionHandler.cs:1204-1210`). So the guard is truly required; the "today" framing is wrong.

## What survived attack (good news)

Three concerns raised during review were refuted with evidence:

- The fill-batch group eviction at `BacktestingBrokerage.cs:417-423` is safely bypassed, because the processor branches right after group resolution at `:272` — OCO groups never reach that code.
- The per-leg rejection path really does invalidate the whole group, as lifecycle row 6 claims.
- The purge-branch change (`:258-263`) is combo-neutral, since combo legs always close together.

The five-touch pending-set list itself held up.
