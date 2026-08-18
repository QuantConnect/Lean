# ADR 0001: Brokerage order polling service

## Status

Proposed - 2026-08-07

Pilot implemented - 2026-08-13: CharlesSchwab is the first plugin on the service
(`Lean.Brokerages.CharlesSchwab`, draft PR #107). Its own `OrderUpdatePollingService` and diff are deleted;
what remains in the plugin is what this document says remains - the read with its sweep window, the
model-to-state mapping, the leg id assignment shared with the stream's `OrderAccepted`, and the seeded
stream-to-polling handover through the pre-loading `Start` overload.

Replacement watch implemented - 2026-08-14: a polled replace goes through `WatchReplacement` and the diff
reports the update submit. The plugin's own replace reporting and its by-position leg id derivation are
deleted. Backed by the replace survey (see "A replace, across the brokers").

Pilot verified live - 2026-08-14: place, replace, cancel and fill were reported correctly by the polled
connection running next to a streaming one, on real CharlesSchwab accounts. The diff's documented edges
showed up as designed: a market order already filled on its first listing reports the submit and the fill
in one batch, and the executions of one sweep arrive as one event.

Second plugin adopted - 2026-08-15: Public.com runs on `PerOrderIdPollingService`
(`Lean.Brokerages.Public`, draft PR #6). Its own service class, diff and snapshot model are deleted; the
plugin keeps the get-order read and the model-to-state mapping. Its same-id replace stays plugin-side, so
`WatchReplacement` is not wired, and a get-order 404 maps to a null state - the contract's "the broker
does not know the id". One rollout intention changed: Public kept its change-of-average price recovery,
moved into its mapping (see "The diff", pricing).

Wiring moved into core - 2026-08-17: the root `Brokerage` class gained the seam the base-class section
describes - two protected `CreateOrderPollingService` overloads whose read-callback signature picks the
mode, the service as a protected `OrderPollingService` property with `IsOrderPolling`, a virtual
`OnOrderPollingNotAcknowledged` for the silence warning, and a `Dispose` that covers the service.
CharlesSchwab and Public.com were moved onto it: the creation, the three event forwards and the dispose
call left both plugins (see "Wiring, per plugin").

Third plugin adopted - 2026-08-17: Tradier runs on `PerOrderIdPollingService`
(`Lean.Brokerages.Tradier`, draft PR #54). Its fill timer, `CheckForFills`
diff, order cache and unknown-id verification are deleted (~230 lines); the plugin keeps the get-order
read and the mapping. Tradier is the first adopter that splits orders across zero: the legs chain from
the read through the base cross-zero helpers, and the second leg's watch seed carries what the first leg
filled (see "A cross-zero order, two ids"). Two behavior changes are intentional: orders placed outside
Lean are ignored - a per-id read never sees them, where the old code raised a fatal "UnknownOrderId"
error - and the fee attaches once per Lean order instead of once per broker leg.

Testing process run end to end - 2026-08-17: Public.com is the first plugin tested by this document's own
order - live capture first, offline replay second (`PublicBrokerageOrderPollingTests.cs`, in draft PR #6).
No recorded payloads existed, so two Explicit live tests ran by hand with debug logging on, and three
capture runs recorded the get-order bodies: an equity place-update-cancel and a market fill, an option
update whose fill beat the cancel, and a multi-leg cancel. Eleven offline tests replay those bodies. The
runs confirmed the per-id mode's edges on a real account: a market order filled on its first read reports
the submit and the fill in one batch, a `PENDING_CANCEL` read between the request and the cancel reports
nothing, and one canceled shared-id combo reports one `Canceled` per leg.

Seeded start folded into Start - 2026-08-18: `SeedAndStart(seed)` became the `Start(preLoadOpenOrders)`
overload, so starting is one method with two shapes: the plain `Start()` resumes the loop, and the overload
runs the handover first. Behavior unchanged; Schwab's call site renamed with it.

State constructors and wiring traces - 2026-08-18: `BrokerOrderState` gained two constructors - the
always-known facts positionally, and an overload taking the message without the fill numbers - and all
three adopters build through them. The wiring now traces the created mode class with its intervals, and
the pre-loading `Start` traces how many open orders it pre-loaded. Schwab moved its mapping into
`CharlesSchwabExtensions.ToLegOrderStates`, called as `brokerageOrder.ToLegOrderStates()` from the read.

## Purpose

Three brokerage plugins have already written the same thing, and a fourth one needs it and does not have it:

- **CharlesSchwab** wrote `Services/OrderUpdatePollingService.cs` so the algorithm keeps working when a second
  algorithm takes the single streaming connection away.
- **Public.com** wrote `OrderPollingService.cs` because Public.com has no order stream at all.
- **Tradier** has a smaller version of the same idea written inline in the brokerage.
- **InteractiveBrokers** has the problem and no answer: when the broker does not reply, the algorithm stops.

Underneath those four there is one problem, not four: **the real-time channel is the only thing telling Lean what
happened to an order, and it is not reliable.** It can be absent, it can be taken away, it can drop for 15 minutes,
and it can stay up while quietly missing an update. In all four shapes the broker still knows the answer over HTTP,
and nobody asks.

This document proposes one helper class in Lean core, `BrokerageOrderPollingService`, that any brokerage can create
and use. The brokerage picks one of two classes — read one order by its brokerage id, or read all orders — and
hands the service a read callback that converts each order from the broker's own model into one shared snapshot
shape. Every N seconds the service runs the read, and the snapshots travel through the brokerage's message handler
back into the service, which compares each one with the last state it has seen for that order and raises an event
with the order events that are new. The brokerage decides what to do with them.

This document covers the service only. It does not change any brokerage on its own — each plugin adopts it in its
own pull request.

## The problem

### 1. The same loop, written three times

| Plugin | Class | Size | Mode | Interval | How a result reaches Lean |
| --- | --- | --- | --- | --- | --- |
| CharlesSchwab | `Services/OrderUpdatePollingService.cs` | 245 lines | all orders | `charles-schwab-order-poll-interval-ms`, default `3000` | `_messageHandler.HandleNewMessage` |
| Public.com | `OrderPollingService.cs` | 268 lines | per order id | `OrderPollingInterval` ctor argument | `_messageHandler.HandleNewMessage` |
| Tradier | inline, `TradierBrokerage.cs:1240-1284` | ~45 lines | per order id, one shot | `Task.Delay(2s)` | direct |

All three copies are deleted by their adoptions; the table records what stood before. The two service
classes were near copies outside their two callbacks. Both had: a background task, a
`CancellationTokenSource` recreated by `Start` and cleared by `Stop`, an idempotent `Start`, a `Stop` that canceled and
waited up to 2 seconds before disposing the source, a `Dispose` that refused to start again, a loop that logged and
retried on a failed read instead of dying, and the same two trace lines. Even the comments matched, because the second
one was written from the first.

What actually differed was small and none of it was a design decision worth keeping twice: `Task.Run` against
`Task.Factory.StartNew(LongRunning)`, an async fetch against a sync one, `Task.Delay` against
`cancellationToken.WaitHandle.WaitOne`, and a failure counter that only Schwab had. Public had one real extra: a
registry of watched brokerage ids with the last state seen for each (`Models/OrderSnapshot.cs`: status, cumulative
filled quantity, average price).

Tradier's version was the same idea again in miniature. When a fill arrived for a brokerage id Lean did not know, it
waited 2 seconds, re-checked `_orderProvider.GetOrdersByBrokerageId`, and re-requested the orders from the API
(`TradierBrokerage.cs:1240-1284` before its adoption deleted the path).

### 2. Two brokerages block the order thread for minutes, then kill the run

Both CharlesSchwab and InteractiveBrokers place the order, then **block inside the order method** waiting for the
broker to confirm it on the real-time channel. Neither of them ever asked the broker over HTTP instead —
this service is that ask, and Schwab's polled mode now answers the same wait.

| Brokerage | Waits for | How long | Where the number comes from | When it expires |
| --- | --- | --- | --- | --- |
| CharlesSchwab | `OrderAccepted` on the account activity stream | **3 minutes** | hardcoded `TimeSpan.FromMinutes(3)`, `CharlesSchwabBrokerage.cs:486` | `Error` `MissingWebSocketResponse` (`:488`) |
| InteractiveBrokers | `openOrder` / `orderStatus` / `execDetails` callback | **5 minutes** | `ib-response-timeout`, default `300` seconds, `InteractiveBrokersBrokerage.cs:84` | `Error` `NoBrokerageResponse` (`:1659`) |
| InteractiveBrokers, `MarketOnOpen` / `ComboLegLimit` / `ComboMarket` / `ComboLimit` | same | 10 seconds | `ib-no-submission-orders-response-timeout`, `:90` | Lean **invents** a `Submitted` event (`:1649-1652`) |

Two costs, and both are paid on every occurrence.

**The wait itself.** Schwab's is in `PlaceOrder`; IB's is in `IBPlaceOrder` (`:1536`), which serves both `PlaceOrder`
(`:451`) and `UpdateOrder` (`:483`), and `CancelOrder` has its own copy at `:538`. So a single lost message parks an
order thread for three or five minutes while the market moves.

**The end of the run.** A `BrokerageMessageType.Error` is not a log line. `DefaultBrokerageMessageHandler` turns it
into `SetRuntimeError` (`Common/Brokerages/DefaultBrokerageMessageHandler.cs:92-96`), which ends the deployment.

The IB plugin already admits the cause in a comment:

```csharp
// tracks pending brokerage order responses. In some cases we've seen orders been placed and they never get through to IB
private readonly ConcurrentDictionary<int, ManualResetEventSlim> _pendingOrderResponse = new();
```
`InteractiveBrokersBrokerage.cs:160-161`

And the third row is the worst one, because it is not even a failure the operator can see. IB is known not to send a
submission event for those order types, so after 10 seconds Lean writes the event itself:

```csharp
Status = OrderStatus.Submitted,
Message = "Lean Generated Interactive Brokers Order Event"
```

That event is a guess. The broker is never asked whether the order is there.

So today the answer to "the broker did not reply" is: block for minutes, then stop the algorithm, or guess.

### 3. A real-time stream can be down, or just quiet

Section 2 was about one lost message: the order confirmation never arrives, the algorithm stops or Lean invents the
event itself. But the stream can also fail in two quieter ways: the connection drops, or the connection is fine and
a single update is just never sent. Today nothing recovers from either.

**The socket drops.** When a brokerage reports `Disconnect` and any exchange is open, Lean does not stop immediately:
it waits `DefaultInitialDelay`, **15 minutes**, for the connection to come back
(`Common/Brokerages/DefaultBrokerageMessageHandler.cs:38` and `:117-122`). That is 15 minutes in which the algorithm
is alive, orders may fill, and no order update can arrive. Worse, nothing re-reads the orders once the socket comes
back: a search across the CharlesSchwab, Tastytrade, Alpaca and TradeStation plugins finds no path that calls
`GetOpenOrders` or re-syncs order state on reconnect. Whatever happened during the gap is lost for the rest of the
run.

Schwab has the sharper version of this: the account activity stream can be taken away permanently, because Schwab
allows one streaming connection per user and a second algorithm on the same account takes the slot. That is the
reason its polling fallback exists at all.

**The socket is up but incomplete.** A live connection is not proof that every update arrived. IB
[documents this about its own API](https://interactivebrokers.github.io/tws-api/order_submission.html#order_status#:~:text=There%20are%20not%20guaranteed%20to%20be%20orderStatus%20callbacks%20for%20every%20change%20in%20order%20status.),
and the plugin copies the warning into the code:

```csharp
// There are not guaranteed to be orderStatus callbacks for every change in order status. For example with market orders
// when the order is accepted and executes immediately, there commonly will not be any corresponding orderStatus callbacks.
// For that reason it is recommended to monitor the IBApi.EWrapper.execDetails function in addition to
// IBApi.EWrapper.orderStatus. From IB API docs
```
`InteractiveBrokersBrokerage.cs:2553-2555`

A dropped or never-sent update leaves a Lean order open forever while the broker has closed it. The algorithm then
sizes its next trade against holdings that are wrong, and any cancel or update it sends on that order is rejected.

A poll answers all three cases with the same question — *does the broker still have this order, and at what
status?* — which is why they belong in one service and not three:

| Case | Mode that covers it |
| --- | --- |
| No order stream at all (Public.com) | all orders, running for the whole session |
| Stream lost or taken away (Schwab, any disconnect) | all orders, started when the stream goes down and stopped when it returns |
| Stream up but an update never arrived (IB, Schwab acks) | watched ids only, running while something is unconfirmed |

## The shared input: a snapshot, not a Lean order

The obvious shared input would be `IBrokerage.GetOpenOrders()` (`Common/Interfaces/IBrokerage.cs:94`) — the one
order read every brokerage already implements. It returns Lean `Order` objects, and that is exactly why it does not
work.

An `Order` carries `BrokerId`, `Symbol`, `Quantity`, `Price`, `Time`, `LastFillTime`, `LastUpdateTime`,
`CanceledTime`, `Type`, `Status` and `Tag` (`Common/Orders/Order.cs`). **There is no filled quantity and no fill
price on it.** Lean core already says this out loud in the startup path that adopts these orders:

> Beware that this order ticket may not accurately reflect the quantity of the order if the open order is partially
> filled.

`Engine/Setup/BrokerageSetupHandler.cs:504`

The plugins prove the gap themselves. Each one sets `Status` on the orders it returns, and each one does it
differently: IB maps the IB order state (`InteractiveBrokersBrokerage.cs:3318`), Alpaca writes `Submitted`, or
`PartiallyFilled` when `brokerageOrder.FilledQuantity` is between zero and the order quantity
(`AlpacaBrokerage.cs:386-390`), TradeStation does the same from `leg.ExecQuantity`
(`TradeStationExtensions.cs:378`). Two of them **read the broker's filled quantity and then throw the number
away**, because a Lean `Order` has nowhere to put it. All that survives is the word `PartiallyFilled`.

So the plugins already have these numbers. There is just no field to store them in. That is the whole
fix: instead of the service reading Lean `Order` objects, **the brokerage converts its
own order model into one small shared snapshot and passes that to the service.** The snapshot has fields for the
status, the total filled quantity and the fill price — the three numbers Public's replaced poller tracked
per order (its since-deleted `Models/OrderSnapshot.cs`) — so one compare in Lean core works for every brokerage and can report real
fills, not only that the order exists.

Two rules shape what the service does with a snapshot:

1. **The service acts only on what a snapshot says.** The plugin fills the snapshot's status by mapping its
   broker's own status, the same way it already does for stream messages. So a `Canceled` in a snapshot is a fact,
   not a guess.
2. **A missing order proves nothing.** The service never emits an event because an order stopped appearing in the
   reads. An order can be missing because it was filled, canceled, rejected, replaced, or never reached the broker
   at all — and each of those needs a different event. If the service guessed `Canceled`, a fill would be lost and
   the holdings would be wrong. So when an order stays missing, the service does not decide anything: after a
   timeout it tells the brokerage the order was never seen, and the brokerage checks with the broker what happened.

One more fact, this one about the modes: IB **cannot ask the broker about one order id**. Its API only returns all
open orders (`reqOpenOrders` / `reqAllOpenOrders`, with a 15 second wait — `InteractiveBrokersBrokerage.cs:626`).
So the service cannot require per-order reads from every brokerage. Which orders one read covers is the brokerage's
choice, made when it picks the class.

## Design

### Where it lives

`Lean/Brokerages/Services/BrokerageOrderPollingService.cs` — the base class — with
`PerOrderIdPollingService.cs`, `AllOrdersPollingService.cs` and `BrokerOrderState.cs` next to it, all in
namespace `QuantConnect.Brokerages.Services`.

`Services` is a new folder under `Brokerages`, and it follows the convention the other subfolders there already use:
`Authentication`, `CrossZero` and `LevelOneOrderBook` each take the matching namespace suffix. It also matches where
the two plugins kept this class before the move — CharlesSchwab had it in `QuantConnect.CharlesSchwabBrokerage/Services/`, so
the move up to core kept the same path.

Tests go to `Tests/Brokerages/Services/BrokerageOrderPollingServiceTests.cs`.

### How a poll flows

```
poll thread — one sweep every PollInterval
  service      calls the read: once per watched id, or once for all orders (by class)
  brokerage    the read asks the broker and converts each order to a BrokerOrderState
  service      sends each state to the brokerage's message handler
               (waits in the queue while an order request holds the lock)
  service      counts failed reads: three sweeps in a row -> one Warning
  service      checks the watched orders: one the broker never reported is flagged after a timeout

handler thread — when the message handler takes a state from the queue
  handler      dispatches it to what the brokerage registered: service.ProcessOrderState(orderState)
  service      compares it with the last state seen and keeps only what is new
  service      raises OrderEvents: the submit first, then fills, then a close
  brokerage    forwards them: OnOrderEvents(events) -> Lean applies them
```

The service does the start and the end: it runs the loop and it compares the states. The brokerage does the middle:
it reads the broker, converts the orders, and says where the results go. That split is what makes the class
generic: nothing broker-specific ever enters it, because the brokerage translated everything into the shared shape
before the service looks at it. And because the service is the one calling the read, a read that throws is caught
and counted by the service itself — the repeated-failure warning below works without any extra code in the plugin.

Both replaced pollers had exactly this flow: Schwab's loop handed each polled order to
`_messageHandler.HandleNewMessage` and the diff ran when the handler dequeued it, and Public wired its
poller the same way. The service's own loop does the enqueue now.

### The broker order state

```csharp
namespace QuantConnect.Brokerages.Services;

/// <summary>
/// One order, as the brokerage last saw it. The brokerage converts its own order model into this shape
/// and passes it to the service, which compares it with the last snapshot seen for the same order and
/// reports only what is new.
/// </summary>
public class BrokerOrderState
{
    /// <summary>The brokerage order id. Some brokers give every combo leg its own id, some give the
    /// whole combo one id; the snapshot carries whatever the broker uses.</summary>
    public string BrokerageOrderId { get; set; }

    /// <summary>The Lean status the brokerage maps its broker's own status to.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>The total absolute quantity filled so far. Null when the read does not carry it.</summary>
    public decimal? FilledQuantity { get; set; }

    /// <summary>The price the broker reports for the fills. Null when the read does not carry it.</summary>
    public decimal? FillPrice { get; set; }

    /// <summary>When the brokerage reported this state, in UTC.</summary>
    public DateTime TimeUtc { get; set; }

    /// <summary>The broker's own words for a closing status, e.g. the reject reason.</summary>
    public string Message { get; set; }

    /// <summary>The always-known facts positionally - id, status, time - with the fill numbers and the
    /// message defaulting to null. An empty constructor stays for filling through the properties, and an
    /// overload takes the message without the fill numbers.</summary>
    public BrokerOrderState(string brokerageOrderId, OrderStatus status, DateTime timeUtc,
        decimal? filledQuantity = null, decimal? fillPrice = null, string message = null);
    public BrokerOrderState(string brokerageOrderId, OrderStatus status, DateTime timeUtc, string message);
}
```

Every field except the id and the status is optional, and null means "my read does not know", never "zero". A
brokerage whose read is `GetOpenOrders()` fills only the id and the status, and the service will emit only what
those two can prove. A brokerage whose endpoint returns fill numbers fills `FilledQuantity` and `FillPrice` too and gets fill events.
A brokerage whose endpoint returns a full execution history, like Schwab, reduces it in the mapping: the quantities
sum into `FilledQuantity`, and the newest execution's price becomes `FillPrice`. The service never invents a number
to cover a null.

The state does not carry an average price or a list of executions, and the service does no price math.
`FillPrice` is used as the broker reported it, and Lean's portfolio averages the fills on its own, like it does
for every other fill event.

Fees never travel through a poll: both existing pollers report `OrderFee.Zero`, and the snapshot keeps that rule
instead of carrying a fee field nobody fills.

Combos come in two shapes, and the state carries both without any extra field. Schwab gives every leg its own
brokerage id (`mainId + legId - 1`), so the plugin passes one state per leg. Public.com gives the whole combo
**one** id (`PublicBrokerage.Brokerage.cs:430-441`), so the plugin passes one state and the service fans it out:
`GetOrdersByBrokerageId` returns every Lean leg order behind the id, and each leg's share of a new fill is

```
legFill = leanOrder.Quantity * newPart / abs(leanOrder.GroupOrderManager.Quantity)
```

The state has no quantity field because the service does not need one: it already knows the brokerage id, so it
reads the group quantity from the Lean orders themselves. That number equals the broker's own order quantity — the
group quantity is exactly what the combo was placed with. Public's replaced diff split fills this way, and
the rule moved into the service's fan-out (`Brokerages/Services/BrokerageOrderPollingService.cs`). One rule for the mapping follows:
`FilledQuantity` of a shared-id combo is in strategy units, the same units the group quantity counts in.

A worked example, from a real Public.com order — 5 AAPL strangles, one brokerage id, a put leg and a call leg with
ratio 1 each:

```
Lean orders behind the id:   put leg   Quantity 5    (ratio 1 x group quantity 5)
                             call leg  Quantity 5    (ratio 1 x group quantity 5)
group quantity:              5                       (abs(GroupOrderManager.Quantity))

broker reports FilledQuantity 2   ->   newPart = 2 - 0 = 2 strangles

put leg:   5 * 2 / 5 = 2 contracts filled
call leg:  5 * 2 / 5 = 2 contracts filled
```

The proportion is the point. If the call leg had ratio 2 (Lean quantity 10), the same order-level "2 filled" would
give it 10 * 2 / 5 = 4 contracts — each leg fills at its own ratio, all from one broker number.

And the other shape, from a real Schwab recording — one combo `OrderResponse`, main id `1002667707949`, three legs,
each leg with its own brokerage id counting up from the main one (`WS_ACCT_ACTIVITY_COMBO_MARKET_FILLED.json` in
the Schwab repo):

```
one OrderResponse   ->   three states, one per leg:
  leg 1  ->  BrokerageOrderId "1002667707949"   (main id + 1 - 1)
  leg 2  ->  BrokerageOrderId "1002667707950"   (main id + 2 - 1)
  leg 3  ->  BrokerageOrderId "1002667707951"   (main id + 3 - 1)

per state: FilledQuantity = the sum of that leg's execution legs,
           FillPrice      = that leg's newest execution price
```

Here every state finds exactly one Lean order, so there is no split at all — the fan-out and the group quantity
never come into play. The two shapes meet the same diff; only the mapping differs.

The shape is not guessed — it is what a survey of eight plugins' order reads actually returns. Each column maps to
one field or rule of the state: **filled qty** feeds `FilledQuantity`, **fill price** feeds `FillPrice`, **reason
text** feeds `Message`, and **one id, many Lean orders** is the case the fan-out exists for. A "no" in a cell is
what the nullable fields are for — that broker's state simply carries less, and the service emits less.

| Broker read | filled qty | fill price | reason text | one id, many Lean orders |
| --- | --- | --- | --- | --- |
| InteractiveBrokers (`reqAllOpenOrders`) | yes — captured but never read | on the paired `orderStatus` callback, not hooked today | no — error callback only | yes, combo legs share the id |
| CharlesSchwab (`GetAllOrders`) | yes | from its execution legs | yes | yes, `mainId + legId - 1` |
| Public.com (`GetOrderById`) | yes | yes | rejects only | yes, combo legs share the id |
| Webull (`GetOpenOrders`) | yes | yes | no | no |
| TradeStation (`GetOrders`) | per leg | yes, as a string | yes | yes, combo legs share the id |
| Alpaca (`ListOrdersAsync`) | yes | yes | no | no |
| Binance (`GetOpenOrders`) | yes | no | no | no |
| Tradier (`GetOrder`) | yes | yes | yes | no |

The IB row deserves its footnote: an open-orders request is answered with an `openOrder` **and** an `orderStatus`
callback per order
([TWS API docs](https://interactivebrokers.github.io/tws-api/open_orders.html)). The plugin already keeps the whole
`openOrder` payload — `orders.Add((args.Order, args.Contract, args.OrderState))`
(`InteractiveBrokersBrokerage.cs:598`) — and that `IBApi.Order` carries a `FilledQuantity` field. The paired
`orderStatus` callback adds the filled quantity and the average fill price (`Client/OrderStatusEventArgs.cs:38-50`).
Only the last step is missing today: the conversion never reads `FilledQuantity` (no reference anywhere in the
plugin), and `GetOpenOrdersInternal` hooks only `OpenOrder`/`OpenOrderEnd` (`:611-612`), not `OrderStatus`. So the
numbers are already in hand when IB's mapping wants them.

Three regularities fall out. Every read fills the id and a broker status — the two required fields. Almost every
read fills cumulative quantities, while a full execution list exists at exactly one broker (Schwab) — which is why
the state carries cumulative numbers only and Schwab reduces its legs to them in the mapping. And half the brokers
map one wire order to several Lean orders, so the fan-out is not an edge case.
Nothing common enough to add is missing; the nullable fields cover every "my read does not have it" hole in the
table. Even the ordered quantity needs no field — for the split, the service reads the group quantity from the
Lean orders it already looks up.

### The class

```csharp
namespace QuantConnect.Brokerages.Services;

/// <summary>
/// Reads orders from the brokerage on an interval and turns the returned snapshots into order events.
/// Used when a brokerage has no order stream, when the stream is unavailable, or to resolve an order
/// the broker never replied about. The base class owns everything both modes share — the loop, the
/// watch registry, the compare and the events. What one sweep reads is the subclass:
/// <see cref="PerOrderIdPollingService"/> or <see cref="AllOrdersPollingService"/>.
/// </summary>
public abstract class BrokerageOrderPollingService : IDisposable
{
    /// <summary>Initializes what both modes share: the message handler wiring, the order provider, and
    /// the two time settings with their defaults. The service wires the handler both ways itself: it
    /// registers <see cref="ProcessOrderState"/> and enqueues every polled snapshot, so one handler
    /// serializes polled snapshots with everything else the brokerage processes. A null handler routes
    /// each snapshot straight into <see cref="ProcessOrderState"/>.</summary>
    protected BrokerageOrderPollingService(BrokerageConcurrentMessageHandler messageHandler, IOrderProvider orderProvider,
        TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null);

    /// <summary>One read of the broker, giving the states the sweep saw. The loop calls it every
    /// poll interval, hands each state to the message handler, and counts a throw as one failed sweep.</summary>
    protected abstract IEnumerable<BrokerOrderState> Sweep();

    /// <summary>A copy of the ids a sweep still has to read: everything tracked whose end was not
    /// reported yet.</summary>
    protected List<string> GetWatchedBrokerageIds();

    /// <summary>The order events one snapshot produced. Raised inside <see cref="ProcessOrderState"/>, never empty.</summary>
    public event EventHandler<List<OrderEvent>> OrderEvents;

    /// <summary>A watched order that nothing reported for <c>watchTimeout</c> of polling.
    /// Raised once; the id is unwatched with it.</summary>
    public event EventHandler<OrderNotAcknowledgedEventArgs> OrderNotAcknowledged;

    /// <summary>Several reads in a row failed, so the run currently has no order updates.</summary>
    public event EventHandler<BrokerageMessageEvent> Message;

    /// <summary>True while the polling task is running.</summary>
    public bool IsPolling { get; }

    /// <summary>How long the loop sleeps between sweeps.</summary>
    public TimeSpan PollInterval { get; }

    /// <summary>How long a watched order may stay completely unreported, in polling time.</summary>
    public TimeSpan WatchTimeout { get; }

    /// <summary>Watches a brokerage order id, with nothing seen for it yet. Idempotent: watching an
    /// already-watched id never overwrites its state.</summary>
    public void Watch(string brokerageId);

    /// <summary>Watches a brokerage order id, seeded with what another path already reported, so the
    /// next poll does not repeat it. Used for orders adopted at startup, for a submit reported from
    /// the request path, and to move state onto the new id of a replace.</summary>
    public void Watch(string brokerageId, BrokerOrderState lastSeen);

    /// <summary>Watches the new brokerage order id of a replace and drops the replaced id in the same
    /// step, so the first state to carry the new id reports the update submit. The new id starts with
    /// no fill state; a broker that carries fills across a replace seeds with Watch instead.</summary>
    public void WatchReplacement(string brokerageId, string previousBrokerageId);

    /// <summary>Stops watching an order and drops its state.</summary>
    public void Unwatch(string brokerageId);

    /// <summary>Records what another path already reported for an order, so the next poll does not
    /// repeat it. Called by the streaming path while the stream lives.</summary>
    public void UpdateOrderState(string brokerageId, BrokerOrderState orderState);

    /// <summary>The last state seen for an order, from any path. The streaming path reads it for its
    /// own duplicate check, and a replace reads it to move the state to the new id.</summary>
    public bool TryGetLastOrderState(string brokerageId, out BrokerOrderState lastSeen);

    /// <summary>
    /// Compares a snapshot with the last state seen for the same order and raises
    /// <see cref="OrderEvents"/> with what is new. The constructor registers it on the message
    /// handler, so polled orders queue behind an order request that holds the stream lock.
    /// </summary>
    public void ProcessOrderState(BrokerOrderState orderState);

    /// <summary>The whole handover from a stream to polling, in the only safe order: process what the
    /// stream already delivered, pre-load one watch per open Lean order, then start the loop. A null
    /// callback pre-loads nothing. See "Seed before Start".</summary>
    public void Start(Func<Order, BrokerOrderState> preLoadOpenOrders);

    public void Start();
    public void Stop();
    public void Dispose();
}

/// <summary>
/// For a broker with a get-order endpoint. A sweep calls the read once per watched brokerage id —
/// no request when nothing is watched. A null return means the broker does not know the id, so the
/// watch timeout keeps counting. A read that throws is logged and skipped, so one bad id cannot
/// starve the others; the sweep only counts as failed when every read of the sweep failed.
/// </summary>
public class PerOrderIdPollingService : BrokerageOrderPollingService
{
    public PerOrderIdPollingService(Func<string, BrokerOrderState> readOrder, BrokerageConcurrentMessageHandler messageHandler,
        IOrderProvider orderProvider, TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null);
}

/// <summary>
/// For a broker with only a bulk endpoint. A sweep calls the read once, whatever is watched.
/// </summary>
public class AllOrdersPollingService : BrokerageOrderPollingService
{
    public AllOrdersPollingService(Func<IEnumerable<BrokerOrderState>> readAllOrders, BrokerageConcurrentMessageHandler messageHandler,
        IOrderProvider orderProvider, TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null);
}
```

The loop, `Start`, `Stop`, `Dispose` and the failure counter come from Schwab's service, the more complete one.
The watch registry, the compare with the last state seen and the seeding of orders already open at startup come from Public's.

### The two modes

The mode is the class. Both run the same diff — it lives in the base — and a subclass is only its `Sweep`:

- **`PerOrderIdPollingService`** — `Func<string, BrokerOrderState>`: the sweep loops the watched ids and calls the
  read once per id. Nothing watched, nothing requested. Public.com's replaced service had exactly this
  constructor — `new OrderPollingService(_apiClient.GetOrderById, _messageHandler.HandleNewMessage, interval)` —
  and its adoption passes the same read to `CreateOrderPollingService`.
- **`AllOrdersPollingService`** — `Func<IEnumerable<BrokerOrderState>>`: the sweep calls the read once, and the
  read returns everything the broker lists.

Two classes instead of one class with both reads, because a bulk read does not fit a per-id shape: called once
per watched id, it would repeat the full-account request for every id in the same sweep, and with nothing watched
it would never run at all (Schwab's fallback watches nothing — it is the whole order path). The split also keeps
each class clean: one class with both reads would hold a null field for the unused mode and a branch in the loop
to pick the right read; a subclass holds only its own read.

The watch registry serves both modes. In per-id mode it is also the read list. In all-orders mode it only feeds
the watch timeout: the service still checks that every watched id shows up in the snapshots sooner or later.

| Plugin | The sweep reads | Scope | Why |
| --- | --- | --- | --- |
| CharlesSchwab | bulk | all | one request returns the whole account, and Schwab is the order path for the run |
| Public.com | per id | watched | Public.com has a get-order endpoint and only cares about its own orders |
| InteractiveBrokers | bulk | watched | IB has **no** per-order request — `reqOpenOrders` always returns everything |
| Tradier | per id | watched | adopted: `GetOrder` asks about one id, so a sweep reads only the watched orders and an idle account sends no request at all (the plan here said bulk; the per-order endpoint made per id the better fit) |
| Webull | bulk | watched | its only order read is `GetOpenOrders` (`Api/ApiClient.cs:666`); order updates come over a gRPC stream, and the poll covers its drops |
| TradeStation | bulk | watched | one `GetOrders` request returns the account's orders (`Api/TradeStationApiClient.cs:185`); the stream stays the first path |
| Alpaca | per id | watched | the SDK has `GetOrderAsync` and the plugin already calls it (`AlpacaBrokerage.cs:546`), so a watch asks only about its own orders |
| Binance | bulk | watched | a single-order request needs the symbol next to the id and the plugin never built one; `GetOpenOrders` sits on the shared REST client base, so every market variant has it |

### The service never raises Lean order events itself

The service raises its own `OrderEvents` event, from inside `ProcessOrderState`. It never calls `OnOrderEvents`. The
brokerage routes snapshots through its `BrokerageConcurrentMessageHandler` and forwards the events.

This is not style. The poll runs on its own thread, so it can see an order already filled while `PlaceOrder` is
still reporting `Submitted` for the same order. If the fill goes out first, the late submit flips a filled order
back to open, and Lean then accepts a cancel or update on it that the broker rejects. Schwab hit exactly this and
fixed it by pushing polled orders through the same message handler as stream messages, so they wait in the queue
while an order request holds the stream lock (`WithLockedStream`,
`Brokerages/BrokerageConcurrentMessageHandler.cs:99`). That is why `ProcessOrderState` is a separate public method
instead of something the loop calls itself: the message handler sits between the read and the diff,
and the queue keeps the order right. The handler's message type also has to cover both the stream's model and the
snapshot — the next section is how it does.

One requirement travels with the queue: the order request, the record the sweep needs to assign the ids, and
the watch must all happen inside the same `WithLockedStream` block that the snapshots queue behind. The leg ids
themselves are assigned by the first sweep that sees the order, from the snapshot: only the broker says which
leg id belongs to which symbol, so an id derived from the request's leg order would be a guess. The
placement's assignment is the same code the stream's `OrderAccepted` runs, and it is what releases the
plugin's place wait. A replace has a mirror of it, fed from its own pending record: its assignment moves
each Lean order onto its new id and marks it through `WatchReplacement`, so the diff reports the update
submit instead of a plain submit. Nothing waits on a replacement - the replace reply already confirmed it,
and the watch raises `OrderNotAcknowledged` if the broker never lists it. Schwab's fallback path works this
way today.

### One lock for two message types

`BrokerageConcurrentMessageHandler<T>` knows exactly one message type, so a plugin whose stream model and snapshot
differ had no way to push both through one lock. Schwab works around it with a marker interface: its stream model
and its polled model both implement `IOrderUpdateMessage`, and the handler is typed to that. This works inside one
plugin that owns both models, and it cannot be the shared answer — every plugin that adopts the service would
have to add the interface to its own wire models, and the core `BrokerOrderState` cannot implement a per-plugin
interface.

So a second, non-generic `BrokerageConcurrentMessageHandler` ships with the service, in the same file
(`Brokerages/BrokerageConcurrentMessageHandler.cs`). It wraps a `BrokerageConcurrentMessageHandler<object>`
inside, so the lock, the buffer and the drain loop are the exact same code, not a copy. Any number of listeners
register, one per message type, and every source enqueues through one `HandleNewMessage(object)`:

```csharp
_messageHandler = new BrokerageConcurrentMessageHandler(concurrencyEnabled);
_messageHandler.Register<AccountContent>(OnAccountContent);   // the stream's own type
// the polling service registers its own BrokerOrderState listener itself, in its constructor

// one method for both sources
_messageHandler.HandleNewMessage(accountContent);
_messageHandler.HandleNewMessage(orderState);
```

`Register` subscribes a filter: a dequeued message runs one `is` check per registered type and lands in every
listener that matches, in registration order; a message no listener matches is dropped. A plugin that adopts the
service switches its handler field to the non-generic class and turns the constructor callback into one
`Register` call — the stream's hot path pays one type check per registered type, nanoseconds next to the lock the
path already takes. The shared lock across two types has its own tests
(`Tests/Brokerages/BrokerageConcurrentMessageHandlerMultiSourceTests.cs`).

One rule is absolute: **the generic `BrokerageConcurrentMessageHandler<T>` stays byte-identical to master.**
Thirteen live plugins hold a field of it today — CharlesSchwab, Public.com, Alpaca, Binance, TradeStation,
Tastytrade, Webull, ByBit, Eze, OANDA, IG, dYdX and TerminalLink — plus the Template scaffold. It is not edited,
not even additively: the non-generic class reuses it by composition, and its tests
(`Tests/Brokerages/BrokerageConcurrentMessageHandlerTests.cs`) stay untouched with it. Only a plugin that adopts
the polling service moves to the non-generic class, one plugin at a time.

### The diff

`ProcessOrderState` compares the snapshot with the last state it has seen for that brokerage id:

```
record the id as seen, for the watch timeout
find the Lean orders by brokerage id (IOrderProvider.GetOrdersByBrokerageId — a list,
because combo legs can share one id and each leg is its own Lean order)
    none found       -> skip and write nothing: not ours, or ours with the id not on the
                        Lean order yet — the next sweep sees it again
    closed in Lean   -> skip, unwatch, drop its state

the submit first, once: Submitted is emitted when nothing was emitted for the id yet,
the Lean order is still New, and the snapshot is not a reject. Lean requires it before
any fill, and a market order can already be Filled the first time a poll sees it. The
second gate matters in bulk mode beside a live stream: orders the stream already
confirmed are not New in Lean, so a sweep seeing them for the first time stays quiet.
The marked exception is the new id of a replace: WatchReplacement flagged it, its Lean
order is already past New, and the first state reports UpdateSubmitted instead.

then the fills, so a close can never outrun a fill of the same order:
    a fill needs both numbers: without a FillPrice nothing is emitted and
        alreadyReported does not move - the service never invents a price
    the new part is FilledQuantity - alreadyReported; nothing at zero or below
    one fill event, priced at the state's FillPrice
    alreadyReported never shrinks, and it moves only when a part was actually emitted
    legs sharing one id split the new part by each leg's share of the group quantity,
        read from the Lean order's GroupOrderManager
    fill quantities are signed by the Lean order's direction; the state stays absolute
    an order is Filled once its total covers abs(leanOrder.Quantity), else PartiallyFilled

the end of the order last:
    Canceled / Invalid -> emitted once, with the snapshot's Message; the id leaves the
                          read list, but its state stays until a compare sees the Lean
                          order closed
    anything already emitted -> skipped
```

The cumulative compare is the rule both existing pollers already share: everything at or below `alreadyReported`
was seen before, so a re-read of the same history reports nothing, and a fill the stream already delivered is not
repeated. It only works because `alreadyReported` never moves backwards.

A worked example — long 1000 AAPL, two 100-share fills at the same price:

| Broker reports (cumulative) | alreadyReported | New part | Event |
| --- | --- | --- | --- |
| FilledQuantity 100, FillPrice 310 | 0 | 100 | PartiallyFilled +100 at 310 |
| FilledQuantity 200, FillPrice 310 | 100 | 100 | PartiallyFilled +100 at 310 |
| FilledQuantity 200 again, next sweep | 200 | 0 | nothing |

Two fills at the same price never look alike to the service, because `FilledQuantity` is the running total and
totals only grow. This is also the field's contract for the mapping: the plugin fills in the broker's cumulative
number, never the size of the last fill — with the increment in that field, the second fill above would look
identical to the first and be lost.

Pricing is deliberately simple: the new part takes the state's `FillPrice`, as the broker reported it. When several
fills land inside one sweep, the quantity is still exact and the price is the broker's reported price at sweep
time, not each fill's own — Tradier's replaced poller shipped exactly this trade-off, and its adoption
keeps it, documented in the mapping (`TradierBrokerage.cs:1169-1171`). Public recovers the exact increment price from the change of the average;
the service refuses that arithmetic on purpose — it amplifies broker rounding and can even go negative on a
tiny part, while the simple price needs no guard at all. Public's adoption kept the recovery anyway, but inside
its own mapping: the state's `FillPrice` arrives already recovered, with a guard that falls back to the plain
average when the new part is not positive. The service still only copies the price it is given.

The recovery also cannot move into the service today. The state has no average field, so the service never
sees the broker's raw average — the mapping turns it into a fill price first. Keeping it in Public costs one
small map (the previous read's cumulative and average) and one fallback guard only the plugin can judge. It
moves into the service the day a second average-only broker adopts — IB's `orderStatus` reports an average
fill price — as one additive change: a nullable `AveragePrice` on the state and the arithmetic in the service.

One more detail is load-bearing. **State outlives the terminal event.** Forgetting an order the moment its
`Canceled` goes out re-reports every fill if the next sweep lands before Lean applies the event — Schwab's own ADR
documents exactly this race. So state is dropped only when a compare sees the order closed **in Lean**, never at
emission.

### Later: orders placed outside Lean

The "none found" branch skips today, but it is also an opening. An id the order provider keeps not knowing is most
likely an order the user placed outside Lean, in the broker's own app — and Lean already has a door for those:
`OnNewBrokerageOrderNotification` (`Brokerages/Brokerage.cs:257`). The transaction handler picks it up
(`Engine/TransactionHandlers/BrokerageTransactionHandler.cs:190`, `:1674`), asks the algorithm's brokerage message
handler whether to accept the order, and adopts it with `AddOpenOrder`. TradeStation already raises it from its
stream (`TradeStationBrokerage.cs:1088`); a poll can feed the same door.

Not in this PR, for one reason: telling "placed outside Lean" apart from "ours, id not assigned yet" needs care —
the 2-second recheck Tradier's replaced poll carried existed exactly because an unknown id can turn out to be
ours a moment later. The safe
shape is: only an id that stays unknown across several sweeps, and is not inside any order request, gets raised as
a new brokerage-side order. That rule can be added to the diff later without changing the snapshot or the API, so
it is future work, not part of this design.

This is the piece the snapshot buys. In the first draft of this document the diff could only ever emit `Submitted`,
because a Lean `Order` carries no fill numbers, and Schwab and Public had to subclass the service to keep their own
diffs. With the snapshot the diff is shared, whole, and the plugins keep only their mapping.

### Watch mode and the give-up rule

`Watch(brokerageId)` is called by `PlaceOrder` right after the request returns an id. From then on:

- A snapshot arrives for the id, or the stream records one through `UpdateOrderState` → the order is acknowledged,
  and the id stays watched until the order closes.
- The order is closed in Lean → `Unwatch`, and its state is dropped.
- `watchTimeout` of polling passes and nothing ever carried the id → `OrderNotAcknowledged` is raised once, with
  the id and how long it was watched, and the id is unwatched.

The timeout only counts while the service is polling, and only on sweeps whose read succeeded — a failed read
asked the broker nothing, so it proves no silence. A watch set while the service is stopped does not count —
otherwise every healthy order would hit the timeout the moment polling starts. So a plugin in watch mode calls
`Start` together with `Watch` (calling `Start` twice is fine) and may `Stop` once nothing is watched.

`OrderNotAcknowledged` is a question, not a verdict — this is rule 2 again, a missing order proves nothing. The service does
not know whether the order never arrived or filled instantly, so it does not decide. The brokerage handles it:
Public can call its get-order endpoint, Schwab can read the order with its executions, IB can use `reqExecutions`.
A brokerage that handles nothing raises a `Warning` and the run keeps going, which is already better than today's
`Error` that stops it.

### One registry for the stream and the poll

In watch mode the stream is alive **while** the service polls, so both paths see the same fills, and each must know
what the other already reported. The registry is that shared memory, in both directions:

- **The stream writes what it reports.** After reporting a stream fill, the plugin calls `UpdateOrderState` with the
  new cumulative state. The next poll compare starts from it and repeats nothing.
- **The stream reads before it reports.** The stream's own duplicate check is not enough in watch mode, because it
  does not know what the poll already reported. `TryGetLastOrderState` fills that gap: a stream update at or below
  the registry's quantity was already reported — by either path — and is dropped.

Without that write, watch mode reports a fill twice: the poll reports it, the stream reports the same fill a
moment later, and Lean applies both. This is the one wiring rule a plugin must follow when it polls while its
stream is alive.

A fallback-mode plugin needs none of that wiring, because its stream never comes back once polling starts. Its
stream handler makes no registry calls — Schwab's keeps only its own cumulative-quantity dictionary, untouched.
Everything it owes the service is one seed at the moment the stream dies (see "Seed before Start"). The registry
complements the plugin's existing logic, it never replaces it — and in fallback mode the stream and the service
never even touch while the stream lives.

A place and a replace are reported the same way: not by the plugin. For a place, Schwab watches the main id
and keeps the placement's Lean orders by symbol; the first sweep to see the order assigns each leg id from
the snapshot — one shared method with the stream's `OrderAccepted`, because only the broker says which leg id
belongs to which symbol — and reports the submit through the diff, one poll interval later at most. For a
replace, its own pending record goes in under the new main id, and a mirror of the assignment handles it: it
moves each Lean order onto its new leg id and marks it with `WatchReplacement`, which drops the replaced id
in the same call — so the dead order's last snapshot reports nothing — and makes the diff report the update
submit instead of a plain submit. The new id starts with no fill state, because a Charles Schwab replacement counts
its executions from zero; a broker that carries fills across a replace moves the state instead:
`TryGetLastOrderState(oldId, out var lastSeen)`, then `Watch(newId, lastSeen)`, then `Unwatch(oldId)`.

The seed rule stays for what a plugin still reports itself: it seeds in the same locked block, so the next
sweep does not repeat it. The assignment also releases the three minute place wait, so the same
`MissingWebSocketResponse` error guards a placement nothing ever confirms, on the stream and on the poll
alike. A replacement has no wait of its own - the replace reply already confirmed it. The watch doubles as
the alarm: an id the broker never lists raises `OrderNotAcknowledged` instead of staying silent.

### A replace, across the brokers

A replace is the one order action that can change the key the registry lives on. Whether it does is the
broker's design, not the plugin's choice — so before deciding what the service should own here, the update
path of nine sibling plugins was read, twice (a survey pass and a line-by-line check pass), for the four
facts that matter to a poll: does the id change, where does the new id come from, who reports
`UpdateSubmitted` today, and what happens to the fill count.

| Plugin | Update | Id after a replace | New id from | `UpdateSubmitted` reported by | Fills after |
| --- | --- | --- | --- | --- | --- |
| CharlesSchwab | yes, combos too | new — one per leg | REST reply, legs from snapshot/stream | plugin after REST (poll mode); stream `ChangeAccepted` | reset |
| Tastytrade | yes, combos too | new — one for all legs | REST reply | stream `Routed`/`Live`, waited 100 s | unknown |
| Alpaca | yes, no combos | new | REST reply | stream `Replaced` only | unknown |
| InteractiveBrokers | yes, combos too | same — modified in place | — | stream `orderStatus` + an updated flag | carry over |
| TradeStation | yes, combos too | same | — (the reply's `OrderID` is never read) | plugin after REST; the stream echo is swallowed | carry over |
| Tradier | price and type only | same | — | plugin after REST | carry over |
| Public.com | single-leg only | same | — (the reply echoes the id) | plugin after REST | carry over |
| Webull | single-leg only | same (`client_order_id` kept) | — | stream `MODIFY_SUCCESS` | carry over |
| ByBit | futures amend only | same | — | plugin after REST | carry over |
| Binance | no — cancel and re-create | — | — | never emitted | — |

The rows in code: IB re-sends the same broker id (`InteractiveBrokersBrokerage.cs:1599,1626`), TradeStation
PUTs to the existing id and never reads the reply's `OrderID` (`Api/TradeStationApiClient.cs:329-335`),
Tradier's PUT has no quantity parameter at all (`TradierBrokerage.cs:475-496`), Public replaces in place
with the id echoed back (`Api/ApiClient.cs:351-362`), Webull keeps the `client_order_id` that is the Lean
`BrokerId` (`Api/ApiClient.cs:595-605`), ByBit amends futures under the same id
(`Api/BybitTradeApiEndpoint.cs:94-100`) and cannot amend spot (`BybitBrokerage.Brokerage.cs:200-204`), and
Binance's `UpdateOrder` throws (`BinanceBrokerage.cs:346-349`).

**The same-id half needs nothing new from the service.** The watched id survives the replace, and the fill
count continues on it — all six carry-over cells are verified in code, e.g. TradeStation's cumulative
`ExecQuantity` delta is never reset by an update (`TradeStationBrokerage.cs:1187-1194`). And the report
itself can never come from a sweep: the state carries a status and two fill numbers, no price and no
quantity, so a modified order polls exactly like an unmodified one. The rule for a same-id plugin is
Tradier's and Public's, already shipping: report `UpdateSubmitted` right after the REST reply
(`TradierBrokerage.cs:947-948`, `PublicBrokerage.Brokerage.cs:692`) and leave the registry alone — Public
runs on this service today and its `UpdateOrder` makes no service call. The two that report from the stream
instead (IB `InteractiveBrokersBrokerage.cs:2359,2380`, Webull `WebullBrokerage.Brokerage.cs:448-453`)
simply have no update report while their channel is down; on adoption they move the report next to the REST
reply, like the other four.

**The cancel-replace half is where the report can move into the service.** All three learn the new id
synchronously, from the REST reply itself: Schwab's `UpdateOrder` result, Alpaca's `PatchOrderAsync`
response (`AlpacaBrokerage.cs:782-793`), Tastytrade's `ReplaceOrderById` return
(`Api/TastytradeApiClient.cs:172-186`). So the plugin can always watch the new id inside the same locked
block as the request — the same rule the place already follows. What no REST reply gives is the
confirmation that the replacement is live: Tastytrade holds its `UpdateSubmitted` until the stream says the
new order is `Routed`/`Live` and waits 100 seconds for that (`TastytradeBrokerage.Brokerage.cs:431,530-533`),
and Alpaca reports it only from the stream's `Replaced` event (`AlpacaBrokerage.cs:677,1058-1059`), so with
the stream down it is never reported. The first sweep that lists the new id is exactly that confirmation.
So the general shape is one addition, the mirror of the place rule: a **replacement watch** —
`WatchReplacement(newBrokerageId, previousBrokerageId)` marks the new id and drops the old one in one
locked step, and the diff's first state for a marked id whose Lean order is open but past `New` reports
`UpdateSubmitted`, "Update submitted by polling". Combos fit both shapes: Tastytrade's whole combo takes
one new id (`TastytradeBrokerage.Brokerage.cs:416`), and Schwab's per-leg ids go through the same by-symbol
assignment its place already runs.

**Dropping the old id is a correctness step, not housekeeping.** A cancel-replace ends the old order at the
broker, and the old order's last snapshot says so — Schwab lists it `Replaced`, Tastytrade sends
`Cancelled` for it, Alpaca `Replaced`. A registry still holding the old id could read that as the Lean
order's end. The streaming plugins prove the hazard is real: Tastytrade swallows exactly this `Cancelled`
today (`TastytradeBrokerage.Brokerage.cs:575-581`), and Schwab's status mapping keeps `Replaced`
non-terminal on purpose. The replacement watch removes the old entry in the same call, and once the plugin
re-keys the Lean order the old id no longer resolves — both guards, one step.

**The fill count restarts with the id.** Schwab's replacement counts its executions from zero, so the new
entry starts at zero reported. Alpaca and Tastytrade leave no evidence either way in code or tests —
"unknown" is the honest cell. A broker that turns out to carry fills across a replace seeds instead of
starting fresh: `TryGetLastOrderState(oldId)` then `Watch(newId, lastSeen)`, two calls that already exist.

**Telling Lean about the new id stays in the plugin.** Three plugins, three conventions: Schwab swaps the
whole `BrokerId` list through `OnOrderIdChangedEvent`, Tastytrade does the same right after the REST reply
(`TastytradeBrokerage.Brokerage.cs:416`), and Alpaca appends the new id and reads `BrokerId.Last()` from
then on (`AlpacaBrokerage.cs:789-793`). The service never touches `Order.BrokerId` — the plugin tells
Lean, the service only watches.

So the seed rule above stands for the same-id half — what a plugin reports itself, it seeds — and the
cancel-replace half gets the replacement watch instead: one additive method and one diff branch. Schwab,
the pilot, runs on it: its polled replace goes through a mirror of its place's by-symbol assignment and the
diff reports the update submit. The by-position leg id derivation its first pilot shipped with is deleted.

### A cross-zero order, two ids

A broker that cannot cross a position through zero gets two brokerage orders for one Lean order: a closing
leg, then an opening leg the base class places when the first one fills
(`TryHandleRemainingCrossZeroOrder`, `Brokerages/Brokerage.cs:892`). That breaks the diff's frame twice.
The diff calls a fill final only when the cumulative reaches the Lean order's whole quantity, and neither
leg reaches it alone. And the trigger for the second leg is the first leg's *broker-side* fill — a state
the diff never surfaces, because at that point the Lean order is only half done.

Tradier, the first adopter with this split, keeps both jobs in the read, on the base helpers that already
own the pending leg:

- The read sees the closing leg filled at the broker and hands `TryHandleRemainingCrossZeroOrder` a
  `Filled` event carrying the unreported part. The helper knows whether a remaining leg is pending — for
  every other order it declines and nothing happens. When it takes the event, it reports the fill as
  `PartiallyFilled` and places the second leg itself; the read then records the state through
  `UpdateOrderState`, so the sweep's own diff stays silent.
- The second leg's watch seed carries what the first leg filled, and its reads add the same offset to the
  leg's own cumulative. The diff's frame is whole again: the leg's last fill reaches the Lean quantity and
  reports `Filled`. A second leg the order provider has not indexed yet resolves through the base
  cross-zero map instead, and the hook reports its closing fill itself, marking it reported so the diff
  stays silent.

The service itself needs nothing new for this — the seed, `UpdateOrderState` and `TryGetLastOrderState`
were already there. What it costs the plugin is two small maps holding the first leg's filled quantity —
one keyed by the Lean order id between the second leg's request and its watch, one keyed by the second
leg's brokerage id for the reads — and one closed-order hook in the read.

### Seed before Start

Polling never starts first: the stream reported orders before it, and the registry must know what was already
reported before the first sweep runs. So every `Start` that follows stream time begins with a handover, and the
service owns its order — `Start(preLoadOpenOrders)` does nothing while polling already runs, and otherwise runs three
steps:

1. Drain the message handler — the service runs an empty `WithLockedStream` block on the handler it was built
   with, so every fill the stream already delivered is counted. Nothing slips in after it, because the switch
   runs on the stream's own thread — the only thread that delivers stream messages.
2. One `preLoadOpenOrders(openLeanOrder)` call per open Lean order, each becoming a `Watch(id, lastSeen)`: the
   plugin returns the brokerage id, the order's status and the cumulative filled quantity from its own
   bookkeeping. Orders the stream already closed need no seed — the diff skips every order Lean has closed,
   and a null return skips the order.
3. Start the loop.

A plugin with nothing to hand over — its stream reported nothing, or it has no stream — calls the plain
`Start()` instead, and a null callback pre-loads nothing. The first sweep then continues from what the
stream reported instead of repeating it. A fallback-mode plugin makes this one call, because its stream
never comes back — Schwab's `ToPreLoadState` is the working example of the callback. A gap-mode plugin repeats
the call on every drop, and `Stop`s when the stream returns.

The seed source already exists in most streaming plugins, because they keep the same bookkeeping Schwab does — the
cumulative quantity already reported, per Lean order: Webull's `_orderIdToPreviousCumulativeQuantity`
(`WebullBrokerage.cs:78`), ByBit's `_cumulativeFillQuantity` (`BybitBrokerage.Messaging.cs:42`), Alpaca's and
TradeStation's `_orderIdToFillQuantity` (`AlpacaBrokerage.cs:59`, `TradeStationBrokerage.cs:132` — both signed, so
their seed takes the absolute value). Webull is the clearest next adopter: a dropped stream is a blind gap today,
nothing replays the missed events, and its `StreamDisconnected`/`StreamReconnected` events are ready-made
`Start`/`Stop` triggers. TradeStation needs the seed only for the outage window itself, because its server replays
an order snapshot on every reconnect. Two would add the dictionary first: Tastytrade tracks processed fill ids
instead of quantities, and Binance keeps nothing — its stream reports the per-fill delta the wire sends.

### Start and Stop follow the stream

`Start` and `Stop` are the hook for the disconnect case. The brokerage calls `Start` when the real-time channel goes
down and `Stop` when it comes back, so polling covers exactly the window in which the stream cannot deliver. Both are
idempotent, and `Stop` does not dispose the service, so a run can switch back and forth as many times as the socket
does. `Dispose` is the one-way door.

Three rules for a plugin that uses this:

- **Seed before every `Start`.** While the socket was up the stream was reporting and the registry was not
  listening. The pre-loading `Start` overload hands over what was reported, in the right order — see "Seed before Start".
- **Sweep once after the stream returns, before stopping.** The socket coming back does not replay what it missed,
  and no plugin re-reads orders on reconnect today. One last sweep closes the gap.
- **Coming back is not always allowed.** Schwab must stay on polling for the rest of the run, because reconnecting
  takes the single streaming slot back from the other algorithm. That decision belongs to the plugin, not to this
  service, which is why the service only offers `Start` and `Stop` and never reconnects anything itself.

What that sweep recovers depends on what the plugin's read returns. A read that only lists open orders brings back
missed submissions. A read with fill numbers brings back the missed fills too — Schwab's does. The service reports
what the states can prove, nothing more.

### Repeated read failures

A single failed read is logged and retried on the next sweep. Three failures in a row raise one
`BrokerageMessageType.Warning` through the `Message` event, and a later successful read arms the warning again. This
is Schwab's rule, kept as is: while the sweeps are failing the run may have no order path at all, and a log line
alone leaves the algorithm looking idle for no visible reason.

Never an `Error`. An `Error` ends the run, which is the outcome this service exists to avoid.

### What the service keeps, and what it does not

The service owns the watch registry, the last snapshot seen and the already-reported quantity per order, the failure
counter, and the polling task with its cancellation source. The streaming path shares the snapshot registry through
`UpdateOrderState` and `TryGetLastOrderState`, so the poll and the stream never report the same fill twice. The service
does **not** own the Lean order state: that is read from `IOrderProvider` on every compare, so the service never
drifts from what Lean actually knows.

Four kinds of thread touch that state: the poll loop, the handler thread inside `ProcessOrderState`, the order
threads through `Watch`/`Unwatch`/`UpdateOrderState`, and the watch-timeout check. One internal lock protects the
registry from all of them. A per-id sweep copies the watched ids under that lock before it reads the broker, so
`PlaceOrder` can watch a new id while a sweep runs. `ProcessOrderState` must not run twice at the same time, and
the service does not guard that itself: the message handler already runs it one call at a time, and a plugin
without a handler must make its calls run one at a time too.

### Configuration

Both time settings are optional constructor arguments. A plugin that has its own configuration key — Schwab keeps
`charles-schwab-order-poll-interval-ms` — reads it and passes the value in, so no existing deployment changes. A
plugin that passes nothing gets the shared defaults, resolved once in the base class constructor both subclasses
chain to:

```csharp
PollInterval = pollInterval ?? TimeSpan.FromMilliseconds(Config.GetInt("brokerage-order-poll-interval-ms", 3000));
WatchTimeout = watchTimeout ?? TimeSpan.FromMinutes(1);
```

`brokerage-order-poll-interval-ms` is one new generic config entry, so the interval can be tuned once for every
brokerage that uses the default. Core helpers already work this way: the message handler reads
`brokerage-concurrent-message-handler-buffer-size` in its constructor
(`Brokerages/BrokerageConcurrentMessageHandler.cs:56`). The 3000 ms default is the value Schwab runs today.

## Wiring, per plugin

The general shape, for a streaming brokerage with a bulk endpoint. The root `Brokerage` class owns the
wiring: one protected create call builds the service, forwards its events onto the brokerage events,
stores it in the protected `OrderPollingService` property and hands it to `Dispose`. The read callback's
signature picks the mode — a bulk read can only build an `AllOrdersPollingService`:

```csharp
// bulk broker: one request per sweep reads the whole account. The service wires itself onto the
// handler: it registers its diff as a listener and enqueues every snapshot, so the plugin never
// touches that relationship again.
CreateOrderPollingService(
    () => _apiClient.GetAllOrders().Select(ToOrderState),   // read: model -> snapshot
    _messageHandler,
    _orderProvider, pollInterval: TimeSpan.FromSeconds(3), watchTimeout: TimeSpan.FromMinutes(1));
```

A per-id broker only changes the read — one call replaces Public.com's construction, its three event
forwards and its dispose line:

```csharp
CreateOrderPollingService(ReadOrderState, _messageHandler, _orderProvider, pollInterval: OrderPollingInterval);
// BrokerOrderState ReadOrderState(string brokerageId): get-order by id, a 404 maps to null.
// Public keeps its own key, public-order-poll-interval-ms, default 1000 ms. A plugin that passes no
// interval gets the shared brokerage-order-poll-interval-ms default, 3000 ms.
```

What silence means is one override per plugin: `OnOrderPollingNotAcknowledged` defaults to a single
warning built from the brokerage name, and Schwab overrides it to keep the wording its live pilot
verified.

InteractiveBrokers, the plugin with no answer today, adopts `AllOrdersPollingService` (it has no per-id request) with
the thinnest possible mapping — id and status from the orders `reqAllOpenOrders` returns, fill fields left null.
The fill numbers are not out of reach: the captured `IBApi.Order` already carries a `FilledQuantity` field, and the
paired `orderStatus` callback adds the average fill price, so the mapping can grow later without a new endpoint.
The first step stays thin:

```csharp
// IBPlaceOrder, instead of blocking up to 5 minutes and then killing the run
OrderPollingService.Watch(ibOrderId.ToStringInvariant());
OrderPollingService.Start();   // idempotent; Stop once nothing is watched
```

One honest cost first: IB has no `BrokerageConcurrentMessageHandler` today — only Schwab and Public do — so its
adoption starts by putting one, or an equivalent lock, between its order path and `ProcessOrderState`. Without that,
the fill-before-submit ordering this design depends on does not exist in IB.

The 5 minute block goes away: `IBPlaceOrder` returns as soon as the request is out, and the watch resolves the order
in the background. The `_noSubmissionOrderTypes` guess (`MarketOnOpen`, `ComboLegLimit`, `ComboMarket`, `ComboLimit`)
becomes a real check: the broker either lists the order, and `Submitted` is true, or it does not, and the brokerage
is asked instead of Lean inventing an event.

The same two lines cover a dropped socket, in any streaming plugin:

```csharp
// where the brokerage already handles the connection going down; see "Seed before Start"
OrderPollingService.Start(ToPreLoadState);

// on reconnect: one last sweep for the gap, then hand the job back to the stream
OrderPollingService.Stop();
```

CharlesSchwab and Public.com delete their own service class **and their diff**, and keep only the mapping from
their models to the snapshot. Schwab keeps its own rule of never going back to the stream. Tradier deletes its
fill timer and `CheckForFills` diff the same way; its cross-zero split is "A cross-zero order, two ids".

## What stays in the plugin

- Reading the broker: the read callback and the class choice between per-id and all orders, plus how far
  back a bulk read reaches (Schwab reads from its oldest open Lean order).
- Converting model to state: the status mapping, combo leg ids (Schwab's `mainId + legId - 1`), reducing an
  execution history to the two numbers (Schwab sums its execution legs and takes the newest leg's price). A plugin
  whose leg ids are derived rather than returned by the broker should verify they resolve and warn when they do
  not — the service skips silently.
- Routing: handing the create call its message handler, or null when the poll loop is the only caller of
  the diff. The event forwards and the dispose are the base class's job.
- Reporting without the stream: the plugin reports neither the place nor the replace itself. It watches the
  main id, the first sweep assigns the leg ids by symbol — a replacement's through `WatchReplacement` — and
  the diff reports the submit or the update submit.
- Deciding what an unacknowledged order means — the `OnOrderPollingNotAcknowledged` override; the default
  is one warning built from the brokerage name.

## Testing the polling in a plugin

The service's own behavior — the diff, the watch, the registry — is covered once, in Lean's
`Tests/Brokerages/Services/BrokerageOrderPollingServiceTests.cs`. A plugin does not test the service
again: it tests its read, its mapping and its wiring **through** the service. The offline tests carry
the everyday coverage; the live tests prove the whole path against the real broker — and for a plugin
starting from nothing they come first, because their runs record the data the offline tests replay. The reference is the Schwab pilot's fixture,
`Lean.Brokerages.CharlesSchwab/QuantConnect.CharlesSchwabBrokerage.Tests/CharlesSchwabBrokerageOrderUpdatePollingTests.cs`;
its mock classes sit next to it in `Tests/Models`. For a polling-primary, per-id plugin the reference is
Public.com's fixture, `Lean.Brokerages.Public/QuantConnect.PublicBrokerage.Tests/PublicBrokerageOrderPollingTests.cs` —
the first one built in this section's order, live capture first.

### The test doubles

Two subclasses, and no reflection:

- A mock brokerage deriving from the real one. It runs the normal initialization and overrides exactly
  the edges: the API client factory returns a mock, the socket is replaced with one a test can feed
  captured messages into, and the protected polling trigger gets a public wrapper (Schwab's
  `SwitchToOrderUpdatePolling` calls its stream-loss handler; a polling-primary plugin just calls
  `Connect`). It also plays the transaction handler's part where a test has none: its
  `OnOrderIdChangedEvent` override moves the new broker id onto the order, so a sweep can find a
  replacement.
- A mock API client deriving from the real one. The read's source is a settable property (Schwab: the
  order collection its bulk read returns; a per-id broker sets the get-order response), and an
  `AutoResetEvent` fires when the read runs, so a test waits for the next sweep instead of sleeping.

A test stages broker time by swapping that property between waits: set the working snapshot, wait for
the events, set the half-filled snapshot, wait, set the filled one. One config line per test shortens
the poll interval (Schwab sets `charles-schwab-order-poll-interval-ms` to 100 ms), and the fixture's
setup puts it back, so no test changes the pace of the next one.

And the test plays the transaction handler: every fixture subscribes to `OrdersStatusChanged` and
writes each reported status back onto the Lean order before anything else (Schwab's `ApplyOrderStatus`
helper) — the same closing of the loop Lean does live. The diff decides from the Lean order: the
submit goes out only while the order is still `New`, and an id leaves the registry only once its order
closed. A fixture that skips the write-back gets duplicate submits and orders that never stop being
read. The asserts then follow the live trading cycle, on the collected events in the order it produces
them: the submit, the fills, the close.
 
### The mock data is recorded, not invented

Every snapshot and stream message in the offline tests is a copy of something the broker really sent.
That sets the build order for a new plugin: one with no `TestData/` folder and no captured payloads
cannot start with the offline tests, because there is nothing to feed them. It starts with one live
test — write it, and run it by hand against a real account. The debug log of that run holds the
broker's real JSON responses and raw socket messages; copying them out is what creates the mock data,
and the offline tests come second, replaying it. Getting a payload is a manual, one-time step:

- REST order JSON comes from one live run: run the plugin (or one of the live tests below) with debug
  logging and copy the orders-endpoint response from the log, or save it to a `TestData/*.json` file
  (Schwab keeps `REST_GET_OPEN_ORDERS.json`). Paste it with its real ids, and write a comment naming
  the recorded order and its story — "order 1002449136610: two option legs, ended EXPIRED".
- Raw socket messages are copied from the same live log (Schwab's sits at `bin/Debug/log.txt` after one
  Explicit run) and fed through the mock socket, so the wrapper handles them exactly like live bytes.
- A staged sequence interpolates only what the stage changes into one recorded skeleton — Schwab's
  `BuildRigMarketOrderJson(status, filledQuantity, executionActivities)` replays a real market order's
  five executions one sweep at a time.

Copy the nulls too. A real Public.com order carries `"filledQuantity": null` until something fills,
`"legs": null` outside a combo, and `"limitPrice": null` on a market order; a builder that writes `"0"` or `[]`
instead feeds the code a shape the broker never sends, and the test passes on the wrong parse path. The
staged skeleton keeps every field exactly as recorded and changes only what the stage changes.

A note for the AI model driving an adoption: the log only records what the code writes to it. Before
the capture run, go through every request path the polling touches — the order read, place, replace,
cancel, and the socket's message handling — and check that each one traces the broker's raw response
(`Log.Trace`). Where a path is silent, add the trace and warn the developer which paths were silent:
the capture run only records payloads on a build that has the logging.

Short payloads stay inline in the test as verbatim strings; only large or shared ones go to
`TestData/*.json`.

### Offline tests

These run without credentials, through the real order methods of the mock brokerage, and no request
leaves the test. What they must show, per plugin — the list is the checklist for the next adopter:

- **Place while polling**: the sweep reports the submit; a recorded terminal snapshot reports the close
  once; a rejected snapshot reports `Invalid` carrying the broker's words; the fills of one sweep
  arrive as one event; a staged sequence reports every partial fill; a working order without fill data
  does not stop the sweep. A combo plugin adds the leg id assignment from the snapshot by symbol.
- **Cancel and its races**: the cancel request stays quiet and the poll reports the `Canceled` once; an
  intermediate `CancelPending` read reports nothing; a fill that beats the cancel ends the order `Filled`
  with no `Canceled` at all; a canceled shared-id combo reports one `Canceled` per leg.
- **An id the broker does not know yet**: the read returns null (Public.com's get-order 404) and the
  sweep asks again until the order appears — the submit is reported then, not before.
- **Stream-to-polling handover**, for a hybrid plugin: the stream reports part of a fill and the seeded
  poll reports only the rest; the same fill split differently by the two paths stays consistent; a leg
  the stream already closed is not repeated by the poll. A polling-primary plugin proves the seed with
  the orders adopted at startup: one adopted half-filled reports only the part that fills after.
- **Event ordering**: a fill arriving within the first sweep still reports `Submitted` before `Filled`,
  and `UpdateSubmitted` before `Filled` after a replace — the proof the message-handler lock holds.
- **The whole lifecycle**: place, update, cancel while polling, and the same on the stream, so the two
  paths can be compared event for event.
- **The plugin's own edges**: Schwab adds its sweep window (a read reaches back to the oldest open
  order) and its closed-socket subscription guards; Tradier adds its cross-zero legs.

### Live tests

The live half is `[Explicit]`, run by hand during market hours against a real account, with the cost
spelled out in the reason string — "takes the single streamer connection", "places real orders", "buys
real shares" — so nobody runs it by accident. Schwab's `WebSocketVsPolling` category runs two brokerage
connections side by side: the first loses the stream and falls back to polling, the second keeps
streaming, and every test asserts the same lifecycle on both. What proved worth copying:

- Place-update-cancel first: the full lifecycle on a limit order resting far from the market costs
  nothing and checks the submit, the update submit and the cancel on both connections.
- Fills on a cheap liquid stock next: market and limit orders sized to a few dollars, run until
  `Filled`.
- Account hygiene inside the test: a flat-account pre-check that sells leftovers before asserting, a
  sell-back after every real buy — seeding the holdings first, so the sell maps to the right
  instruction — and `cleanup:` log lines separating the hygiene from the behavior under test.
- Partial-fill hunting is its own test and it logs instead of asserting: a bid resting inside the
  spread for the whole balance may catch a partial fill, but nothing can force one, so a hard assert
  would only make the test flaky. The developer adjusts the price to the live quote before running.
- One live run doubles as the recorder: its debug log is where the offline tests' REST and socket
  payloads come from. A polling-primary plugin turns the logging on inside the test itself
  (`Log.DebuggingEnabled = true`), so every hand run records — and every extra asset class is one more
  capture run, the way Public.com recorded its option and its multi-leg bodies after the equity ones.

## Alternatives not taken

- **Hand the service Lean `Order` objects from `GetOpenOrders()` and let it diff those.** The first draft of this
  document. It dies at the boundary: a Lean `Order` has no filled quantity and no fill price, so the shared diff
  could only emit `Submitted`, and every brokerage with richer data had to subclass the service and override the
  diff. The snapshot carries the same numbers the plugins already read and today throw away, so the subclass and
  the override are gone.
- **A core interface the wire model implements** (`OrderResponse : IBrokerOrderState`), so the plugin passes its
  API model straight in with no conversion. Checked against all eight surveyed plugins and rejected on the
  evidence. Two cannot implement it at all: IB's order model is compiled into the vendor `CSharpAPI.dll`, and
  Alpaca's `JsonOrder` is `internal sealed` inside the SDK — both would need a wrapper class, which is the same
  work as filling the snapshot. Tradier's model exposes public **fields**, which cannot implement interface
  properties, so its whole serialization surface would have to change. TradeStation's model is a struct, boxed on
  every interface use. And for the four brokers where one wire order becomes several Lean orders, one object
  cannot be several states — the per-leg conversion survives anyway. The interface would also pull the
  broker-to-Lean status mapping inside the wire DTOs as computed properties, and the service registry would hold
  whole wire objects alive as stored state (Schwab's `OrderResponse` carries the full execution history). A plain
  class the plugin fills is one pattern that works for all eight.
- **A marker interface as the message handler's type** — Schwab's current answer to two message types in one
  handler (`IOrderUpdateMessage`). As the shared answer it fails the same way the core interface does: every
  plugin edits its wire models, and the core `BrokerOrderState` cannot implement a per-plugin interface.
  Replaced by the non-generic multi-source handler (see "One lock for two message types").
- **A dual-generic handler**, `BrokerageConcurrentMessageHandler<T, U>` with the stream type and the polled type.
  Rejected: every existing plugin migrates to the new shape even with no polling, a plugin with no stream (IB)
  has no honest `T`, and a third source would need `<T, U, V>`. The non-generic handler adds a `Register` call
  instead of a type parameter.
- **A handler base class that queues work items (`Action`) instead of messages.** The typed wrapper would then
  wrap every stream message in a new closure — one allocation per message on the hottest path a brokerage has.
  The non-generic handler keeps the message itself in the buffer and allocates only at registration.
- **Put it on the `Brokerage` base class, driven by the engine, like the cash sync.**
  `ShouldPerformCashSync` / `PerformCashSync` (`Brokerages/Brokerage.cs:577`, called from
  `Engine/TransactionHandlers/BrokerageTransactionHandler.cs:731`) is the existing shape for core-driven periodic
  work, and it was the obvious candidate. Rejected: cash sync runs once a day on a schedule core can decide, while
  the right poll interval here is a property of the broker's rate limits and of whether its stream is alive. It also
  has to be off by default — most plugins must not poll — and a base-class hook that nearly everyone turns off is
  worse than a class you create when you need it.
- **Let the service call `OnOrderEvents` directly.** Simpler to wire and reintroduces the fill-before-submit bug in
  every plugin that uses it. See "The service never raises Lean order events itself".
- **Emit `Canceled` when an order leaves the open list.** This is the tempting one, and it is wrong: an order that
  filled also leaves the open list, so this drops fills and desynchronizes holdings. It is why rule 2 exists — the
  service acts on what a snapshot says, never on an order going missing.
- **Carry the execution history in the state** — a per-execution list of quantity, price and time, so every
  execution becomes its own event at its own price. The survey killed it: exactly one of the eight brokers (Schwab)
  returns such a list, and the cumulative compare already keeps quantities exact. The list would buy per-execution
  price precision for one broker at the cost of a second diff branch every plugin has to reason about. If it is
  ever wanted, it comes back as one additive nullable field without breaking anyone — the same goes for a
  `GetOrderExecutions` API on `IBrokerage`.
- **Arm the replacement watch from the `OrderIdChanged` event instead of `WatchReplacement`.** The plugin
  already raises `OnOrderIdChangedEvent` when a replace re-keys a Lean order, so the service could subscribe
  to it instead of offering a method. Rejected three times over. The event does not mean "replace": Lean
  core raises it on a plain place — the cross-zero flow assigns the second part's id through it
  (`Brokerages/Brokerage.cs:939`) — and Binance assigns every initial id with it, so the service would
  report an update submit for a placement. The event does not carry the previous id, and the service cannot
  recover it: the transaction handler subscribes first and swaps `order.BrokerId` before a later subscriber
  runs (`Engine/TransactionHandlers/BrokerageTransactionHandler.cs:1497`), so the old registry entries could
  not be dropped. And a stream-alive plugin raises the event while reporting `UpdateSubmitted` itself, so an
  event-armed service would report it a second time. The reuse that works is locality, not wiring: the
  plugin calls `WatchReplacement` on the same line where it builds the id-changed event, with the old and
  new id it already holds.
- **Leave it in the plugins.** It is already written three times, and the fourth copy would be IB's.

### A base brokerage class that owns the wiring

Every adopter wires the service the same way: create the mode class with the read callback, forward
`OrderEvents` to `OnOrderEvents` and `Message` to `OnMessage`, decide what `OrderNotAcknowledged` means, and
dispose the service with the brokerage. So the tempting next step is to move that wiring into an inheritance
layer — one abstract brokerage class that owns the service and asks the plugin only for overrides:

```csharp
public abstract class BaseWebSocketsAndPollingServiceBrokerage : BaseWebsocketsBrokerage
{
    // owns the service, forwards its events, disposes it with the brokerage
    protected abstract IEnumerable<BrokerOrderState> ReadOrderStates();   // or a per-id read
    protected virtual BrokerOrderState ToSeedState(Order order) => null;
}
```

The attraction is real: a developer who picks the base class is told by the compiler what to implement,
instead of reading this document. Considered and not taken, on three facts.

**The class cannot reach the plugins that need it.** A C# class inherits one class, so a layer under
`BaseWebsocketsBrokerage` serves only the plugins that already sit there — and most do not:

| Plugin | Base class today | Order updates today | Could inherit it |
| --- | --- | --- | --- |
| CharlesSchwab | `BaseWebsocketsBrokerage` | account activity stream, this service as the fallback | yes |
| Tradier | `BaseWebsocketsBrokerage` | its own inline poll | yes |
| Binance (+ US and futures variants) | `BaseWebsocketsBrokerage` | user data stream | only through `BinanceBrokerage` — the variants subclass it |
| ByBit | `BaseWebsocketsBrokerage` | user data stream | yes, as a fallback only |
| dYdX | `BaseWebsocketsBrokerage` | subaccounts channel | yes, as a fallback only |
| Eze | `BaseWebsocketsBrokerage` | websocket protobuf push | yes, as a fallback only |
| Public.com | `Brokerage` | none — this service is the order path | **no** |
| InteractiveBrokers | `Brokerage`, `sealed` | vendor TCP callback SDK | **no** |
| TradeStation | `Brokerage` | HTTP stream, not a websocket | **no** |
| Alpaca | `Brokerage` | vendor SDK streaming client | **no** |
| Tastytrade | `Brokerage` | its own two-socket wrapper | no — a base class migration first |
| WeBull | `Brokerage` | its own websocket order events | no — a base class migration first |
| IG | `Brokerage` | Lightstreamer vendor SDK | no |
| OANDA | `Brokerage`, through its own `OandaRestApiBase` | HTTP transaction stream | no — its own hierarchy holds the slot |
| TerminalLink | `Brokerage` | Bloomberg session API | no |
| TradingTechnologies, Fix.Bloomberg, Fix.InteractiveBrokers | `Brokerage` / `FixBrokerage` | FIX execution reports | no |

Only the first six rows sit on `BaseWebsocketsBrokerage`, and only two of those poll. The two plugins this
document opens with as the ones that need polling most — Public.com, with no order stream at all
(`PublicBrokerage.cs:42` extends plain `Brokerage`), and InteractiveBrokers, whose lost reply ends the run
(`InteractiveBrokersBrokerage.cs:68`, a `sealed` class on the vendor SDK) — are exactly the two the class
can never serve.

**Serving both sides means writing the class twice.** The only way around the table is a second abstract
class with the same body under `Brokerage`, next to the websocket one. The bodies cannot be shared: a class
inherits one class, and a default interface method can neither hold the service field nor call the protected
`OnOrderEvents`. That is the same wiring twice in core, edited in pairs forever, and a fix applied to one
copy and not the other quietly makes the two halves behave differently.

**The overrides are not where the adoption work is.** Counted on the two adopters: the wiring a base class
could absorb is about 30 lines in Schwab and about 45 in Public — the creation, three event forwards, one
dispose call. What it cannot absorb is everything else the plugin writes: the read and the mapping (~200
lines in Schwab, ~45 in Public), the `Watch` calls inside the order methods, and the start trigger, which is
broker policy — Public starts polling in `Connect` because the service is its connection, Schwab starts it
when the stream is taken away. The class cannot even own the stop: `Disconnect` is abstract on `Brokerage`
(`Brokerages/Brokerage.cs:150`), so `Stop` stays a line the plugin writes either way. An abstract read would
guard the one step nobody gets wrong, and guard it for a third of the plugins.

The place that reaches every row of the table is the root `Brokerage` class, the way the cross-zero
helpers and `CreateOAuthTokenHandler` already sit there as protected members only some plugins use
(`Brokerages/Brokerage.cs:701`, `:343`): one protected creation helper per mode, whose read-callback
signature picks the class, the service as a protected property, and a `Dispose` that covers it. Every
brokerage derives from `Brokerage`, the sealed IB class included, and constructing the service directly
stays possible — the helper is additive. Implemented that way on 2026-08-17: the seam is the two
`CreateOrderPollingService` overloads, `OrderPollingService`, `IsOrderPolling` and the virtual
`OnOrderPollingNotAcknowledged`, and Schwab, Public.com and Tradier all create their service through it
(see "Wiring, per plugin"). The abstract class this section declines stays declined.

## Risks

| Risk | What we do about it |
| --- | --- |
| A plugin maps a broker status to the wrong Lean status | The mapping is the same one its streaming path already needs, written once per plugin and covered by its own tests. The service only emits transitions, so a wrong mapping surfaces once, not as a flood. |
| A state without fill numbers cannot produce fill events | By design: null means unknown, and the service never invents a number. The watch still confirms submission, and the watch timeout still fires. |
| Several fills inside one sweep share one price | Quantities stay exact; the price is the broker's reported price at sweep time. Tradier kept this trade-off through its adoption (`TradierBrokerage.cs:1169-1171`); a shorter poll interval narrows it. |
| Polling adds requests on brokers with tight rate limits | Watch mode reads only while an order is unacknowledged, and the interval is a constructor argument the plugin picks. |
| A bulk read is expensive on some plugins — `GetOpenOrders()` rebuilds Lean orders and maps symbols on every call | The action converts straight from the broker's wire model to the snapshot, skipping the Lean `Order` build entirely; and in watch mode a sweep only runs while something is pending. |
| A plugin wires the handler wrong and gets fill-before-submit | The misuse is gone: the service takes the handler in its constructor and wires both directions itself. A plugin either hands over its handler, or passes null and the poll loop is the only caller of the diff. Schwab and Public have a handler; Tradier adopted with null — its submit event goes out before the watch begins, so the poll cannot outrun it — and IB's rollout step still names adding one. |
| The stream and the poll both see the same fill in watch mode | The registry works in both directions: the stream writes what it reports (`UpdateOrderState`) and checks before reporting (`TryGetLastOrderState`). Named as the one non-optional wiring rule for polling beside a live stream. |
| The watch timeout cannot tell "never arrived" from "filled instantly" | It does not try. `OrderNotAcknowledged` hands the question to the brokerage, which has the endpoints to answer it. |
| Polling while the stream is down misses the fills that happened during the gap | Only when the read carries no fill data. A read with fill numbers recovers them — the state has the fields, so this is a property of the broker's endpoint, not of the service. |
| A plugin starts polling on disconnect and forgets to stop on reconnect | Both paths are one line and sit next to the connection handling the plugin already has. The poll side repeats nothing the registry already holds, so the cost of forgetting is extra requests, not extra events. |

## Rollout

1. This PR: the non-generic multi-source message handler, the service, the snapshot, their unit tests,
   and the protected create seam on `Brokerage`. The generic handler is untouched, so every plugin
   compiles as before, and no plugin is forced onto the seam.
2. InteractiveBrokers: add the message handler it does not have, then replace the `NoBrokerageResponse` error and
   the invented `Submitted` with a watch. This is the proof that the abstraction holds for a plugin that did not
   write it.
3. CharlesSchwab and Public.com (done): delete their service class and their fill/close diff. What stays is real
   and named: the read and its sweep window, the model-to-state mapping, Schwab's stream-unavailable switch and
   its by-symbol leg id assignment. One behavior change is intentional: a Public poll that shows a
   new fill and the cancel together now emits both — the old code dropped the cancel. Schwab's per-execution
   prices become per-sweep prices while the quantities stay exact; Public kept its change-of-average price
   recovery inside its mapping, so its part prices stay exact too.
4. Tradier (done, further than planned here): the step was a watch for submissions with fills staying on its own
   path. The adoption moved the fill path itself onto `PerOrderIdPollingService` and handles the cross-zero split
   as "A cross-zero order, two ids" describes. Two costs are accepted: a sweep is one gated request per watched
   order instead of one bulk request — the one-order-per-symbol rule keeps that count small, and an idle account
   now polls nothing at all — and orders placed outside Lean are ignored, where the old code raised a fatal
   "UnknownOrderId" error.
