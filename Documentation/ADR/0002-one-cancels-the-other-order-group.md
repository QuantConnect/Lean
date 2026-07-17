# ADR 0002: One-Cancels-the-Other (OCO) order group

## Status

Proposed - 2026-07-16

## Purpose

ADR 0001 research showed that a bracket order is not one thing. It is two smaller, generic pieces:

- **OCO** (One-Cancels-the-Other): linked orders where one fill cancels the rest.
- **OTO** (One-Triggers-the-Other / conditional): an order that submits another order when it fills.

The team decision is to build the two pieces separately and then build the bracket as a thin layer on top: **PR 1 = OCO (this document) -> PR 2 = conditional (OTO) -> PR 3 = bracket (sugar + small wiring)**. This document describes only the first PR: OCO support in Lean core, backtesting and paper trading, plus the live mapping plan for Alpaca and InteractiveBrokers.

One extra goal shapes every choice below: **the OCO code must be built from pieces that OTO and the bracket can reuse.** So this PR does not just add an OCO feature — it adds the group-order building blocks (one enum, one gate, one submitter, one set of engine steps), and OCO is only the first consumer. The section "Built to be reused by OTO and the bracket" lists each block and who reuses it.

## What is an OCO order group

The simple picture:

1. You place two or three orders at the same time.
2. The orders are linked together in one group.
3. All orders in the group are live in the market at the same time. Nobody waits for anybody.
4. When one order fills, the broker cancels every other order in the group.
5. So exactly one order from the group can win. The rest disappear.

A story example. You own 100 AAPL bought at $200. You want to take profit at $220, but you also want protection if the price falls to $190. You place two sell orders in one OCO group:

- sell 100 AAPL, limit $220 (take profit)
- sell 100 AAPL, stop $190 (stop loss)

If the price reaches $220 first, the limit order fills and the stop order is canceled. If the price falls to $190 first, the stop order fills and the limit order is canceled. You can never sell 200 shares by mistake, because the group allows only one winner.

Important difference from the next PR: in an OCO group no order waits for another order to fill. The "wait until my parent fills" behavior is the **conditional (OTO)** order — that is PR 2. A bracket is the combination: OTO entry that triggers an OCO exit pair.

## What brokers allow

Group size at the two brokers we checked in their trading platforms:

- **InteractiveBrokers** (TWS "One Cancel Another" panel): 1 main order + 4 attached orders. The TWS API itself is even more general: every order that carries the same `ocaGroup` string belongs to one group, the docs put no size limit on it, and the legs can be "multiple and possibly unrelated orders" — different symbols and even different asset classes (the TWS panel screenshot shows AAPL stock, NOK stock and an AAPL option in one group; the API docs use an S&P e-mini / treasury futures pair).
- **Webull** (desktop app, OCO strategy): 1 main order + 5 child orders on one symbol. In the OpenAPI the group is placed through the normal order endpoint with `combo_type = "OCO"` on **every** leg — the official example is three limit orders on AAPL in one group, and no maximum is documented.

One useful thing the APIs show: an OCO group is really **flat**. Every leg is equal; nobody is the boss. The "main order + children" look is only how the platforms draw it on screen. Lean keeps the flat model too — the first order in the list only provides the group's quantity and direction, nothing more.

Other brokers with a native OCO order class: Alpaca (`order_class=oco`), Tradier (`class=oco`), Binance spot (`POST /api/v3/orderList/oco`), TradeStation (group type `OCO`), CharlesSchwab (`orderStrategyType=OCO`), Tastytrade, Bitfinex, Zerodha (GTT two-leg), Rithmic (server side). The full per-broker matrix with sources is in ADR 0001.

## Scope for v1

- One OCO group = **2 or 3 orders: one main order and up to 2 more**. Why 3: the final goal (bracket) needs an OCO pair plus an entry, so 1 main + 2 child keeps the same shape and keeps testing small. Brokers allow more (IB 1+4, Webull 1+5), and the API takes a list, so raising the limit later does not break anything.
- Leg order types in v1: **Limit** and **StopMarket**. These are the two types a bracket needs. More types can come later.
- Legs may use different symbols (IB allows it; useful for breakout strategies that watch two assets). The common case is one symbol.
- Backtesting and paper trading work first, with zero brokerage changes. Live support is opt-in per brokerage model; Alpaca and InteractiveBrokers ship first.

## Algorithm API

One new method on `QCAlgorithm`:

```csharp
public List<OrderTicket> OneCancelsTheOtherOrder(List<Order> orders,
    bool asynchronous = false, string tag = "", IOrderProperties orderProperties = null)
```

The user builds the order objects with the public constructors and does not submit them; the method does the linking and the submitting. On success the result is one ticket per order, in the same position as the input list; when a pre-order check fails, nothing is placed and the list contains a single invalid ticket (the same failure shape combo orders have today).

**Decision: the input is `List<Order>`.** A `List<Leg>` alternative was considered (the code review flagged that order objects carry engine-managed fields, a time argument, and their own properties). We keep `List<Order>` and neutralize each risk with explicit rules instead:

- **The passed orders are specs, nothing more.** The method reads only symbol, quantity and the leg prices, builds fresh `SubmitOrderRequest`s, and never registers the user's instances. The user's objects stay untouched; the tickets are the live handles.
- **One source of truth for everything else.** The method-level `tag` and `orderProperties` apply to all legs; the per-order constructor `tag`/`properties` are ignored. Time in force comes from that single `orderProperties` (or `DefaultOrderProperties`), so all legs share one TIF **by construction** — no cross-leg TIF comparison is needed or performed.
- **One clock.** The submitter stamps the algorithm's UtcTime on every leg's request; the constructor's `time` argument is ignored (a stale user time would corrupt Day-TIF expiry, and legs must share one clock or one could fill a bar early).
- **Guard rails.** The method throws `ArgumentException` when a passed order already carries a `GroupOrderManager`, has a non-empty `BrokerId`, or has a status other than `None` — that catches accidental reuse of a live engine order (for example something returned by `Transactions.GetOrderById`). Passing the same instance twice is rejected too.

Starting example:

```csharp
public override void OnData(Slice slice)
{
    if (!Portfolio.Invested)
    {
        MarketOrder(_symbol, 100);

        // close the position either with profit at 220 or with protection at 190
        var tickets = OneCancelsTheOtherOrder(new List<Order>
        {
            new LimitOrder(_symbol, -100, limitPrice: 220m, UtcTime),      // take profit
            new StopMarketOrder(_symbol, -100, stopPrice: 190m, UtcTime)   // stop loss
        });
    }
}
```

Python gets `self.one_cancels_the_other_order([...])` through the snake-case binding. No Python algorithm in the repo constructs order objects today, so the Python regression twin is the acceptance test for the binding (a plain Python list of `LimitOrder`/`StopMarketOrder` into `List<Order>`).

Validation at submit time, throwing `ArgumentException`:

- the list has 2 or 3 orders;
- every order type is Limit or StopMarket (v1);
- every quantity is non-zero;
- every order is a fresh spec (see the guard rails above: no group manager, no broker ids, status `None`, no duplicate instances).

Note there is no "same time in force" check: the group's TIF comes from the single `orderProperties`, so mixed TIF cannot happen.

All-or-nothing submit: the method runs the pre-order checks for every leg before submitting any leg (same pattern as `SubmitComboOrder`, `Algorithm/QCAlgorithm.Trading.cs:944`). If one check fails, nothing is placed and one invalid ticket is returned.

Internally the public method is a thin wrapper. The real work happens in one shared private submitter that the next PRs reuse:

```csharp
// one submitter for every group type; OCO is only the first caller
private List<OrderTicket> SubmitGroupOrder(ComboType comboType, List<Order> orders,
    bool asynchronous, string tag, IOrderProperties orderProperties)
```

`SubmitGroupOrder` owns the shared steps: the common validation (leg count, non-zero quantities, one TIF), building the `GroupOrderManager`, turning each leg into a `SubmitOrderRequest`, running all pre-order checks before the first submit, submitting in list order, and the synchronous-wait rule. That rule needs a precise meaning per type: the combo code waits for fills only on `ComboMarket`; an OCO group has nothing that fills at submit, so for `ComboType != Combo` a synchronous call waits for **submission** only (never for fills — resting legs stay open by design). When the bracket arrives, the wait becomes trigger-aware: wait on the market-type trigger leg only. The per-type rules (for example the bracket's "take profit above entry above stop loss" price checks) stay in each public wrapper. Planned wrappers over the same submitter:

```csharp
// ADR 0003 (conditional):
public List<OrderTicket> OneTriggersTheOtherOrder(Order trigger, List<Order> children, ...)

// final PR (bracket sugar): builds entry + take profit + stop loss and calls SubmitGroupOrder(ComboType.Bracket, ...)
public List<OrderTicket> BracketOrder(Symbol symbol, decimal quantity,
    decimal takeProfitLimitPrice, decimal stopLossPrice, ...)
```

## Design

### GroupOrderManager + new enum ComboType

We reuse `GroupOrderManager` — the class that already links combo order legs together (`Common/Orders/GroupOrderManager.cs`). We add one enum that says how the group executes:

```csharp
public enum ComboType
{
    /// <summary>All legs are placed and filled together as one unit (today's combo behavior) (0)</summary>
    Combo = 0,

    /// <summary>One leg fills -> every other leg in the group is canceled (1)</summary>
    OneCancelsTheOther = 1
}
```

The enum is designed to grow. The next PRs append (never renumber — enum ints are persisted):

- `OneTriggersTheOther = 2` (ADR 0003): the group has a trigger leg; the other legs wait until it fills.
- `Bracket = 3` (final PR): trigger leg + children that are OCO between themselves. One enum value, because every dispatch point then stays a simple switch on `ComboType` — the bracket behavior itself is just the OTO step plus the OCO step composed (see the reuse section).

New serialized property on `GroupOrderManager`:

```csharp
[JsonProperty(PropertyName = "comboType", DefaultValueHandling = DefaultValueHandling.Ignore)]
public ComboType ComboType { get; set; }
```

`DefaultValueHandling.Ignore` keeps old serialized groups unchanged: they load as `Combo`, so nothing breaks on restart.

Why the flag is needed: combo code assumes all legs share one fate (all-or-nothing fills, group expiry, group price). Every one of those rules is wrong for OCO, where exactly one leg wins. The engine and the plugins need a cheap way to ask "is this group a combo or an OCO?" — this flag is that check.

Group values for an OCO group: `Quantity` = the main (first) order's signed quantity, `Count` = number of legs, `LimitPrice` unused (0). The legs keep their own real quantities. There is **no ratio math**: OCO legs are plain `LimitOrder` / `StopMarketOrder` objects, not `ComboOrder` subclasses.

### How the orders flow through the engine

1. `OneCancelsTheOtherOrder` creates one `GroupOrderManager` (`Transactions.GetIncrementGroupOrderManagerId()`, `ComboType = OneCancelsTheOther`) and builds one `SubmitOrderRequest` per leg, all sharing the manager. `SubmitOrderRequest` already carries a `GroupOrderManager` — no change there.
2. The transaction handler already buffers group legs until the whole group has arrived (`Engine/TransactionHandlers/BrokerageTransactionHandler.cs:877-889`) and invalidates all legs together when one fails (`:900-955`). The buffering and the invalidation are reused unchanged; the buying-power call inside that range (`:914`) is the one part that changes — see the buying-power building block below.
3. Three serialization fixes are needed, because today only the combo order **types** carry the manager through the factory and JSON paths, and OCO legs are plain order types:
   - `Order.CreateOrder` (`Common/Orders/Order.cs:420`): after the type switch, attach the request's manager to any order that has none — and do it **before** the Id is set, because the Id setter registers the leg into `GroupOrderManager.OrderIds` only when the manager is already there (`Order.cs:42-56`).
   - `OrderJsonConverter` (`Common/Orders/OrderJsonConverter.cs:307-317`): today only the three combo cases restore `groupOrderManager` from JSON. Add a generic restore for any order type whose JSON carries the token, placed **before** the Id assignment (`:90`). Without this, a live restart loses the OCO link on the legs.
   - `OrderJsonConverter.DeserializeGroupOrderManager` (`:362-385`): this method rebuilds the manager from exactly five fields (id, count, quantity, limit price, order ids) and drops everything else. It must also read `comboType`. Without this fix the flag is silently erased on the first restart: the group loads as `Combo`, the live gate stops protecting it, and — because the property serializes with `DefaultValueHandling.Ignore` — the next save writes no `comboType` at all, so the OCO marker is gone forever.
4. Cancel is already group-wide: `BacktestingBrokerage.CancelOrder` resolves the group and cancels every leg (`Brokerages/Backtesting/BacktestingBrokerage.cs:199`). It needs one guard: skip legs that are already closed, so a filled leg is not overwritten with Canceled. `QCAlgorithm.Liquidate` needs the same group awareness (`Algorithm/QCAlgorithm.Trading.cs:1371-1415`): it cancels open orders one by one, so with a group it would cancel leg 1 (which cancels the whole group) and then fail loudly trying to cancel the already-gone leg 2 — it should issue one cancel per group and skip the siblings.

### Buying power (its own building block)

The review of the current code showed buying power cannot be a side note — it is a shared building block with a **per-type rule**, and it must live where both backtest and live pass through:

- The group check runs at submit time in `SecurityPortfolioManager.HasSufficientBuyingPowerForOrder(List<Order>)` (`Common/Securities/SecurityPortfolioManager.cs:942-961`), called from the transaction handler (`:914`) in **both** backtest and live, and again from `BacktestingBrokerage.Scan`. The per-type rule branches there, keyed on `ComboType` — not inside the backtest processor.
- The rule per type: **OCO** = check the most expensive leg only (exactly one can execute). **OTO** (later) = trigger only while the trigger is open, then the sum of the children (they run independently). **Bracket** (later) = trigger only while dormant, then the most expensive exit. This table is the reuse contract for the margin axis.
- Same-symbol legs cannot go through the position-group path at all: duplicate symbols are summed or throw in the option position-group machinery (`PositionGroupBuyingPowerModel.cs:94-96` keys a dictionary by symbol), so the group check must fall to the per-leg path deliberately, not by accident.
- `CashBuyingPowerModel.GetOpenOrdersReservedQuantity` (`Common/Securities/CashBuyingPowerModel.cs:400-461`) sums the open same-direction Limit/StopMarket orders — exactly an OCO pair's shape. Holding 1 BTC and placing the sell-TP + sell-SL story trade, each leg sees the sibling already reserving the full 1 BTC and the group is rejected. PR 1 must exclude same-group siblings from that reservation sum, otherwise the flagship use case fails on every cash account.
- The same "one winner, don't count both" idea applies to the open-order aggregations: `GetProjectedHoldings` (`Engine/TransactionHandlers/BrokerageTransactionHandler.cs:815-824`) and the shortable check (`QCAlgorithm.Shortable`) sum every open ticket, so an OCO exit pair projects double the real exposure and can trigger false shortable rejections or oversized execution-model orders. PR 1 counts only the max-exposure open leg per group in these aggregations; ADR 0003 extends the same hook with the dormant-children rule.

### Update path and existing "manager means combo" assumptions

Two places assume today that "has a `GroupOrderManager`" means "is a combo", and both need the `ComboType` check in PR 1:

- `HandleUpdateOrderRequest` skips buying-power validation for any grouped order (`BrokerageTransactionHandler.cs:1009-1018` — written for combo ratio legs). OCO allows per-leg updates, so the skip must narrow to `ComboType == Combo`, or OCO leg updates run unvalidated.
- A grep audit of every `GroupOrderManager` consumer (Common + plugins) is part of PR 1: for example `PublicBrokerageModel.CanUpdateOrder` rejects any grouped order update with a combo-specific message, and `InteractiveBrokersFixModel` keys its combo handling off the manager. Each such branch must decide explicitly what it means for a non-combo group.

### Live safety gate

This is the one dangerous spot of the design. OCO legs are plain Limit/StopMarket orders, so every brokerage model whitelist accepts them one by one. A live brokerage that knows nothing about OCO would receive 2-3 normal orders and place them **without the cancel link**. The user thinks they have "one winner" protection; really both orders can fill. That must never happen silently.

Guard: **one** new virtual on `DefaultBrokerageModel`, keyed by `ComboType` so it serves OCO now and OTO/bracket later without any new members:

```csharp
/// <summary>True when this brokerage can place order groups with the given execution type</summary>
public virtual bool SupportsGroupExecution(ComboType comboType) => comboType == ComboType.Combo;
```

The transaction handler checks the group's `ComboType` against the model once per group **in live mode only** and invalidates the whole group with a clear message when the model says no. Backtesting and paper do not need the gate, because the engine simulates the group behavior itself (next section). The default answer keeps today's behavior (combos pass); each brokerage opts in per type: `AlpacaBrokerageModel` and `InteractiveBrokersBrokerageModel` return `true` for `OneCancelsTheOther` in their enablement PRs, and later add `OneTriggersTheOther` / `Bracket` in the same method — no second gate mechanism is ever needed.

Scope note: the handler only sees the model through the `IBrokerageModel` interface, so the gate is really three touches, not one: the member on `IBrokerageModel` (`Common/Brokerages/IBrokerageModel.cs`), the default implementation on `DefaultBrokerageModel`, and the forwarding in `BrokerageModelPythonWrapper` (`Common/Python/BrokerageModelPythonWrapper.cs`). The Python wrapper must fall back to the default answer when a user's Python model does not define the new method, so existing Python algorithms keep working.

### Lifecycle

State walk-through for a 3-leg group (A = main, B, C):

| # | Step | A | B | C | Events |
|---|------|---|---|---|--------|
| 0 | `OneCancelsTheOtherOrder()` returns 3 tickets | New | New | New | none (requests buffered until the last leg) |
| 1 | Group validated and placed | Submitted | Submitted | Submitted | 3x Submitted |
| 2 | Leg B fully fills | Canceled | Filled | Canceled | one batch: Filled(B), Canceled(A, "OCO"), Canceled(C, "OCO") |
| 3 | User cancels any one leg | Canceled | Canceled | Canceled | 3x Canceled (cancel one = cancel all) |
| 4 | User updates a leg price | unchanged | unchanged | unchanged | UpdateSubmitted on that leg only |
| 5 | Time in force expires | Canceled | Canceled | Canceled | 3x Canceled ("expired") |
| 6 | Submit-time failure on any leg | Invalid | Invalid | Invalid | 3x Invalid via `InvalidateOrders` |
| 7 | Leg B partially fills | Submitted | PartiallyFilled | Submitted | PartiallyFilled(B); siblings stay open (v1 rule, see open questions) |

The rules, short version:

- One group, one winner.
- The first leg that **fully** fills wins; every other open leg is canceled in the same event batch.
- Cancel one leg = cancel the whole group.
- Update one leg = only that leg changes.
- A partial fill does not cancel the siblings (v1 engine rule). Careful: live brokers can be stricter — IB's `OcaType` field is documented as acting "when one order **or part of an order** executes", so at IB a partial execution can already cancel the siblings. The backtest keeps the simple rule; the live behavior belongs to the broker. See open questions.

### Backtesting and paper

Home: `BacktestingBrokerage.Scan` (`Brokerages/Backtesting/BacktestingBrokerage.cs:233`) gets a new branch after group resolution (`:272`): a group processor keyed on the manager's `ComboType`.

Why not in `FillModel`: fill models are user-replaceable per security and overridable from Python. The OCO promise ("only one winner") must not depend on user code. The fill models keep doing what they do today — decide the fill price of a single leg. The group logic lives one level up.

Why not in `BrokerageTransactionHandler`: it is shared with real live trading, where the broker cancels the losing legs itself. Engine-side cancel there would double-cancel.

The processor is deliberately not one big OCO method. It is a small set of **named steps** (private helpers in `BacktestingBrokerage`), because OTO and the bracket run the same steps in a different order:

- `TryFillLeg(order, security, securities)` — evaluate one leg with its security's `FillModel`, honor its time in force, compute the fee. Knows nothing about groups.
- `CancelOpenSiblings(orders, winner, events, reason)` — append a `Canceled` event for every still-open leg except the winner, into the **same** event batch. This *is* the OCO promise, as one function.
- `EmitAndRemoveIfClosed(orders, events)` — send the batch, and drop the group from the pending set only when **every** leg is closed.
- A fixed leg-evaluation order: stop-type legs first, then limit legs, then by Lean order Id. Deterministic, and pessimistic when one bar could fill two legs at once — the tie rule the bracket will inherit for its exits.
- `ExpireGroup(orders, events)` — time in force expired on any leg: cancel every open leg of the group.

The OCO processor is then just a composition:

1. Check expiry (`ExpireGroup`) and `CanExecuteOrder` per open leg.
2. Buying power: only one leg can ever execute, so check the **most expensive leg**, not the sum. (Also practical: same-symbol duplicate legs break the group margin path — `PositionCollection` keys positions by symbol.)
3. Evaluate open legs in the fixed order via `TryFillLeg`; the first leg that reaches `Filled` wins -> `CancelOpenSiblings`.
4. `EmitAndRemoveIfClosed`.

The pending-set invariant ships with this. Groups whose legs close at different times need one consistent rule — a closed leg stays in the pending set while any sibling is open, and leaves only when the whole group is closed — and that rule touches **five** places, not one (if any is missed, `TryGetGroupOrders` cannot resolve the group anymore and the surviving leg becomes uncancelable):

- the `Scan` purge branch that today removes any closed order on sight (`BacktestingBrokerage.cs:258-263`);
- `UpdateOrder`, which today accepts any order found in the pending set — it must refuse retained closed legs;
- `RemoveOrders` (used by the expiry and insufficient-buying-power paths), which today stamps its status on every leg unconditionally — it needs the skip-closed guard so a Filled leg is never overwritten with Canceled/Invalid;
- `CancelOrder`'s cascade (same guard);
- `EmitAndRemoveIfClosed`, which is where the group finally leaves the pending set.

Group removal keys on order statuses, never on the current fill batch. All five touches are group-generic — OTO and the bracket run the same invariant unchanged.

Paper trading comes free: `PaperBrokerage` extends `BacktestingBrokerage` and reuses `Scan`.

## Built to be reused by OTO and the bracket

This is the map of every building block PR 1 creates, and who consumes it later. The honest claim after the code review: **OTO adds the dormancy/activation step plus a handful of trigger-aware branches; the bracket adds no new mechanism — it composes the existing steps.**

| Building block | Lives in | OCO (PR 1) | OTO (ADR 0003) | Bracket (final PR) |
| -------------- | -------- | ---------- | -------------- | ------------------ |
| `ComboType` on `GroupOrderManager` | Common | value `OneCancelsTheOther = 1` | appends `OneTriggersTheOther = 2` | appends `Bracket = 3` |
| Three serialization fixes (plain legs carry the group; `comboType` survives the round trip) | Common | ships | reused as is | reused as is |
| `SubmitGroupOrder` shared submitter (fresh time stamps, all-or-nothing pre-checks, submission-only sync wait) | QCAlgorithm | ships; OCO wrapper | new thin wrapper | new thin wrapper (the "sugar") + trigger-aware sync wait |
| `SupportsGroupExecution(ComboType)` live gate (interface + default model + Python wrapper) | Common | ships; models opt in per type | same method, new enum value | same method, new enum value |
| Group leg buffering (`BrokerageTransactionHandler` + `GroupOrderCacheManager`) | existing | reused as is | reused as is | reused as is |
| Group buying power, keyed by `ComboType` in `SecurityPortfolioManager` + sibling exclusion in `CashBuyingPowerModel` | Common | ships (rule: max leg) | new rule: trigger, then sum of children | new rule: trigger, then max exit |
| Group-aware open-order aggregations (`GetProjectedHoldings`, shortable) | handler + QCAlgorithm | ships (count max-exposure leg per group) | extends with dormant-children rule | reused as is |
| Cancel one leg = cancel the group + skip-closed guard; group-aware `Liquidate` | BacktestingBrokerage + QCAlgorithm | ships | reused as is | reused as is |
| Update path: combo-only skips narrowed to `ComboType == Combo`; `GroupOrderManager` consumer audit | Common + handler | ships | reused as is | reused as is |
| `TryFillLeg` single-leg evaluation | BacktestingBrokerage | ships | reused as is | reused as is |
| `CancelOpenSiblings` (the OCO step) | BacktestingBrokerage | ships | not used | reused on the two exits |
| Stops-first deterministic leg order | BacktestingBrokerage | ships | reused | reused (SL before TP on a tie) |
| `EmitAndRemoveIfClosed` + the pending-set invariant (five touch points) | BacktestingBrokerage | ships | reused as is | reused as is |
| Dormancy + activation step (children wait for the trigger; second message-bearing `Submitted` on activation; `OrderSubmissionData` re-stamped at activation) | BacktestingBrokerage | not needed | **ships in ADR 0003** | reused on the entry->exits switch |
| Trigger-aware engine branches (`Scan` same-bar rule for a market trigger; submitter sync wait) | BacktestingBrokerage + QCAlgorithm | not needed | **ships in ADR 0003** | reused as is |
| Plugin fill-buffer bypass for `ComboType != Combo` (IB `EmitOrderFill`) | plugins | ships (generic check) | reused as is | reused as is |

How the three processors read once everything exists (sketch, not code):

```
switch (group.ComboType)
{
    OneCancelsTheOther:  evaluate legs -> first Filled wins -> CancelOpenSiblings
    OneTriggersTheOther: trigger open?  -> TryFillLeg(trigger); on fill -> activate children
                         trigger filled? -> evaluate children independently (no OCO link)
    Bracket:             trigger open?  -> TryFillLeg(entry);  on fill -> activate children
                         trigger filled? -> evaluate children -> first Filled wins -> CancelOpenSiblings
}
```

Reserved extension point (documented now, shipped in ADR 0003): OTO and the bracket must know **which leg is the trigger**. The plan is one more serialized field on `GroupOrderManager` — `TriggerOrderId` (int, `0` = no trigger, ignored when default) — instead of new order types for the trigger leg. ADR 0001 used new order types (`BracketMarket`/`BracketLimit`) mainly to get live gating for free from the model whitelists; the `SupportsGroupExecution` gate now covers that job explicitly, so plain legs + `TriggerOrderId` keep every leg reusing the existing order classes end to end. PR 1 does **not** add the field — it only keeps every design choice compatible with it.

## Live mapping: Alpaca

Repo: `Lean.Brokerages.Alpaca`. Alpaca has a native OCO order class: **one** `POST /v2/orders` with `order_class=oco` and nested price objects. The request shape (verified in the Alpaca orders guide):

- top-level `type` must be `"limit"` — that top-level order **is** the take-profit leg;
- the take-profit price goes in `take_profit.limit_price` (not in a top-level `limit_price`);
- the stop loss goes in `stop_loss.stop_price` (plain stop) or `stop_loss.stop_price` + `stop_loss.limit_price` (stop-limit);
- when you read the orders back, the take-profit limit order is the **parent** and the stop loss is its child leg.

Alpaca constraints the model must enforce (they are stricter than Lean's generic OCO):

- exactly **2 legs**, both on the **same symbol**, both the **same side** — Alpaca's OCO is exit-only: "the second part of the bracket orders where the entry order is already filled", so an existing position is required;
- the take profit is a limit order; the stop loss is a stop (v1 maps Lean's `StopMarketOrder` to it);
- US equities only; time in force `day` or `gtc` (the same rule Alpaca applies to its other advanced order classes).

Plugin work:

- `PlaceOrder` (`QuantConnect.AlpacaBrokerage/AlpacaBrokerage.cs:457`) gets the same buffering gate the IB plugin uses (`GroupOrderCacheManager.TryGetGroupCachedOrders`, `Common/Orders/GroupOrderCacheManager.cs`): the first leg buffers, the last leg builds the single OCO POST via the SDK (`Alpaca.Markets` fork, aliased `AlpacaMarket`). The Lean limit leg becomes the parent order; the Lean stop leg becomes the `stop_loss` object.
- Register the parent id and the child leg id into the Lean legs' `BrokerId` right away, so the external-order import path does not adopt the server-created leg as a foreign order. Caution: the child id in the **POST response** is commonly observed but not clearly documented (the documented `nested=true` roll-up belongs to `GET /v2/orders`). Verify once on paper trading; the fallback is one nested GET right after placement — same caution ADR 0001 recorded for brackets.
- Stream: the winning leg's `fill` event and the losing leg's `canceled` event arrive per leg and route by broker id; the OCO cancel is fully server-side, so the engine just applies the broker's `Canceled` event. Two cautions: (a) Alpaca documents a race — "in extremely volatile and fast market conditions, both orders may fill before the cancellation occurs" — emit a `BrokerageMessageEvent` warning when two fills arrive for one group; (b) the passive leg is reported in a non-routed `held` status in community reports, but `held` is **not** in Alpaca's official order-status enum — handle it defensively in the status mapping and verify on paper trading.
- Cancel: DELETE on any leg cancels the group server-side ("If any one of the orders is canceled, any remaining open order in the group is canceled").
- Model: `AlpacaBrokerageModel.SupportsGroupExecution` returns `true` for `OneCancelsTheOther`, plus the constraint checks above with clear messages.

## Live mapping: InteractiveBrokers

Repo: `Lean.Brokerages.InteractiveBrokers`. IB has no single "OCO request". Instead, every leg is a normal order that carries two extra fields from the TWS API (`IBApi.Order` in `CSharpAPI.dll`):

- `OcaGroup` — a free string; all orders with the same string form one group;
- `OcaType` — what happens when a leg fills: `1` = cancel all remaining legs (with overfill block), `2` = reduce remaining legs' quantity with block, `3` = reduce without block.

Plugin work:

- Place: reuse the `_groupOrderCacheManager` gate (`QuantConnect.InteractiveBrokersBrokerage/InteractiveBrokersBrokerage.cs:1520`). When the whole group is buffered: give every leg its **own** IB order id (per-leg event routing needs distinct ids), set `OcaGroup = "lean-oco-{groupOrderManagerId}"` and `OcaType = 1` on each leg, then send the legs with `Transmit = false` on all but the last one. The official OCA sample uses exactly this technique ("to prevent accidental executions... transmitting the last order in the OCA will also cause the transmission of its predecessors") — the group goes live as one unit, but there is no `ParentId` (that is a bracket/OTO tool, not an OCO tool).
- v1 uses `OcaType = 1`: "cancel all remaining orders with block", where "block" means overfill protection — IB routes only one order of the group to an exchange at a time, so two legs can never execute together. Types 2/3 instead *reduce* the remaining legs' quantity on fills (with/without block). One careful detail from the field docs: `OcaType` handling applies "when one order **or part of an order** executes" — so at IB even a partial execution can trigger the cancel of the siblings. The Lean backtest simulates the simpler full-fill rule; verify the exact partial behavior on paper TWS (open question).
- Events: the losing legs come back as `Cancelled`/`ApiCancelled` and map to Lean `Canceled`. Per-leg fills must **bypass** the combo fill buffering (`EmitOrderFill` holds fills of any order with a `GroupOrderManager` until all legs fill). The bypass condition is written once and generically — `ComboType != Combo` — because in *every* non-combo group some legs never fill (OCO losers now; OTO/bracket losers later), so the buffer would always run into its 30-second timeout.
- Cancel: cancel one leg's id; TWS cancels the rest of the group.
- Restart: `GetOpenOrders` must group open orders that share an `OcaGroup` string back into one Lean group with a rebuilt `GroupOrderManager` (`ComboType = OneCancelsTheOther`).
- Model: `InteractiveBrokersBrokerageModel.SupportsGroupExecution` returns `true` for `OneCancelsTheOther`; the FIX model keeps the default (combos only).

## Unit tests

- `OrderJsonConverterTests`: round trip a plain `LimitOrder` and `StopMarketOrder` that carry an OCO `GroupOrderManager` — `ComboType`, `Count` and `OrderIds` survive **two** consecutive round trips (the second one catches the `DeserializeGroupOrderManager` drop combined with `DefaultValueHandling.Ignore`); old JSON without `comboType` loads as `Combo`.
- `OrderTests` / factory: `Order.CreateOrder` attaches the manager to plain legs and the Id setter registers the leg into `OrderIds`.
- `AlgorithmTradingTests`: `OneCancelsTheOtherOrder` returns one ticket per leg, all sharing one manager with `ComboType.OneCancelsTheOther`; validation rejects 1 leg, 4 legs, zero quantity, an unsupported order type, a reused live order (status/broker id/manager already set), and a duplicated instance; the user's passed orders stay untouched (status `None`, no ids) after submit; the group's TIF equals the passed `orderProperties` regardless of what the leg constructors carried.
- `BacktestingBrokerageTests` (new): leg fills -> siblings canceled in one batch; stop-before-limit tie rule; cancel one leg -> whole group canceled, closed legs untouched; TIF expiry cancels the group; group leaves the pending set only when all legs are closed; partial fill leaves siblings open. Test the shared steps (`TryFillLeg`, `CancelOpenSiblings`, `EmitAndRemoveIfClosed`) through these group scenarios — they are the same code paths OTO and the bracket will run, so this suite is the safety net for the next two PRs too.
- `BrokerageTransactionHandlerTests`: legs buffer until the group is complete; submit-time failure invalidates all legs; live-mode gate rejects the group when `SupportsGroupExecution(ComboType.OneCancelsTheOther)` is false and passes when true.
- Brokerage model tests: the default model supports only `Combo`; Alpaca/IB overrides accept `OneCancelsTheOther` and enforce their constraints (Alpaca: 2 legs / same symbol / same side); `BrokerageModelPythonWrapper` forwards the gate and falls back to the default for Python models without the method.
- Buying power tests: a cash account holding exactly the position can place the sell-TP + sell-SL pair (sibling exclusion in `CashBuyingPowerModel`); the group margin check uses the most expensive leg; `GetProjectedHoldings` counts one leg per OCO group; the shortable gate does not reject the second sell leg.
- `Liquidate` with an open OCO group cancels the group once, with no failed-cancel errors.

## Regression algorithms (Algorithm.CSharp)

Two self-verifying algorithms on SPY hourly, January 2019 (same data window the repo already uses for order-type regressions), C# + Python twins:

`OneCancelsTheOtherOrderRegressionAlgorithm` — the winner path:

```csharp
public override void OnData(Slice slice)
{
    if (!Portfolio.Invested)
    {
        MarketOrder(_spy, 100);

        // take profit +1% is reached by the January rally; the stop -30% can never fill
        _tickets = OneCancelsTheOtherOrder(new List<Order>
        {
            new LimitOrder(_spy, -100, Math.Round(Securities[_spy].Price * 1.01m, 2), UtcTime),
            new StopMarketOrder(_spy, -100, Math.Round(Securities[_spy].Price * 0.70m, 2), UtcTime)
        });
    }
}

public override void OnEndOfAlgorithm()
{
    // limit leg won, stop leg was canceled by the group
    if (_tickets[0].Status != OrderStatus.Filled) throw new RegressionTestException(...);
    if (_tickets[1].Status != OrderStatus.Canceled) throw new RegressionTestException(...);
    if (Portfolio.Invested) throw new RegressionTestException(...);
}
```

`OneCancelsTheOtherOrderCancelRegressionAlgorithm` — the cancel path: place the group with far-away prices (limit +30%, stop -30%), cancel one ticket a few days later, assert **both** legs end `Canceled`.

Both algorithms also assert in `OnOrderEvent` that a `Canceled` sibling event arrives in the same batch as the winning `Filled` event, and that no leg ever fills after a sibling filled.

## Rollout plan

- **PR 1 — Lean core (this ADR):** `ComboType` enum + property, `SubmitGroupOrder` + `OneCancelsTheOtherOrder` API, factory/JSON fixes, `SupportsGroupExecution` live gate, `BacktestingBrokerage` shared steps + OCO processor + cancel guard, unit tests, regression algorithms. No live enablement.
- **PR 2 — Alpaca:** model opt-in + constraints, plugin OCO mapping, live lifecycle test on paper trading.
- **PR 3 — InteractiveBrokers:** model opt-in, plugin OCA mapping, paper-TWS lifecycle test.
- After that: **ADR 0003 conditional (OTO)** — appends the enum value, the `TriggerOrderId` field and the dormancy/activation step; everything else in the table above is reused. Then the **bracket PR** — a `BracketOrder` wrapper over `SubmitGroupOrder` plus the `Bracket` switch case that composes the dormancy step with `CancelOpenSiblings`. If the reuse plan holds, the bracket PR touches no serialization, no gate, no buffering and no new engine mechanics.

## Open questions

- **Partial fills.** v1's engine rule is: siblings are canceled only when a leg **fully** fills. Live brokers differ: IB's `OcaType` docs say the group handling fires "when one order or part of an order executes", so `OcaType 1` can cancel siblings already on a partial execution; Bitfinex cancels the sibling on a partial fill too; IB `OcaType` 2/3 instead *reduce* sibling quantities proportionally. So the backtest is an approximation. Options: (a) keep the simple v1 rule and document the gap, (b) cancel siblings on the first fill event including partials (closer to IB/Bitfinex, but can leave a position half unprotected in the TP/SL use case), (c) per-model simulation flavor. Needs a team decision; verify IB's real partial behavior on paper TWS either way.
- **Max legs.** v1 caps at 3. Lift to a per-model limit later (IB 1+4 in TWS UI, Webull 1+5)?
- **Different symbols in one group.** The engine allows it (IB does); the Alpaca model restricts to one symbol. Is max-leg buying power the right group check for the multi-symbol case?
- **Live gate shape.** One virtual `SupportsGroupExecution(ComboType)` checked only in live mode is the proposed gate. Alternative: models reject in `CanSubmitOrder` by checking `order.GroupOrderManager?.ComboType` — but that needs an audit of every model, and it would need repeating for OTO and bracket. Confirm the single-method approach.
- **Trigger leg marker for OTO/bracket.** The reuse plan reserves `GroupOrderManager.TriggerOrderId` (plain legs, no new order types) instead of ADR 0001's `BracketMarket`/`BracketLimit` types. Trade-off to confirm in ADR 0003: plain legs reuse every existing code path, but new types make the entry leg visible in `OrderType` dispatches and reports.
- **Alpaca semantics check.** Alpaca's OCO is exit-only (it protects an existing position). Verify on paper trading that a fresh OCO with no position is rejected, and mirror that rejection message in the model.
