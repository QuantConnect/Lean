# ADR 0001: Brokerage order polling service

## Status

Proposed - 2026-08-07

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

The two service classes are near copies outside their two callbacks. Both have: a background task, a
`CancellationTokenSource` recreated by `Start` and cleared by `Stop`, an idempotent `Start`, a `Stop` that cancels and
waits up to 2 seconds before disposing the source, a `Dispose` that refuses to start again, a loop that logs and
retries on a failed read instead of dying, and the same two trace lines. Even the comments match, because the second
one was written from the first.

What actually differs is small and none of it is a design decision worth keeping twice: `Task.Run` against
`Task.Factory.StartNew(LongRunning)`, an async fetch against a sync one, `Task.Delay` against
`cancellationToken.WaitHandle.WaitOne`, and a failure counter that only Schwab has. Public has one real extra: a
registry of watched brokerage ids with the last state seen for each (`Models/OrderSnapshot.cs`: status, cumulative
filled quantity, average price).

Tradier's version is the same idea again in miniature. When a fill arrives for a brokerage id Lean does not know, it
waits 2 seconds, re-checks `_orderProvider.GetOrdersByBrokerageId`, and re-requests the orders from the API
(`TradierBrokerage.cs:1240-1284`).

### 2. Two brokerages block the order thread for minutes, then kill the run

Both CharlesSchwab and InteractiveBrokers place the order, then **block inside the order method** waiting for the
broker to confirm it on the real-time channel. Neither of them ever asks the broker over HTTP instead.

| Brokerage | Waits for | How long | Where the number comes from | When it expires |
| --- | --- | --- | --- | --- |
| CharlesSchwab | `OrderAccepted` on the account activity stream | **3 minutes** | hardcoded `TimeSpan.FromMinutes(3)`, `CharlesSchwabBrokerage.cs:478` | `Error` `MissingWebSocketResponse` (`:480`) |
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
status, the total filled quantity and the fill price — the three numbers Public's poller already tracks
per order (`Models/OrderSnapshot.cs`) — so one compare in Lean core works for every brokerage and can report real
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
the two plugins keep this class today — CharlesSchwab has it in `QuantConnect.CharlesSchwabBrokerage/Services/`, so
the move up to core keeps the same path.

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

Both existing pollers already have exactly this flow. Schwab's loop hands each polled order to
`_messageHandler.HandleNewMessage` and the diff runs when the handler dequeues it
(`CharlesSchwabBrokerage.OrderUpdatePolling.cs`), and Public wires its poller the same way
(`PublicBrokerage.cs:182`).

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
**one** id (`PublicBrokerage.Brokerage.cs:328`), so the plugin passes one state and the service fans it out:
`GetOrdersByBrokerageId` returns every Lean leg order behind the id, and each leg's share of a new fill is

```
legFill = leanOrder.Quantity * newPart / abs(leanOrder.GroupOrderManager.Quantity)
```

The state has no quantity field because the service does not need one: it already knows the brokerage id, so it
reads the group quantity from the Lean orders themselves. That number equals the broker's own order quantity — the
group quantity is exactly what the combo was placed with. Public's diff already splits fills this way today
(`PublicBrokerage.Brokerage.cs:700-711`). One rule for the mapping follows:
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
    /// <summary>Initializes what both modes share: the route, the order provider, and the two time
    /// settings with their defaults. Each snapshot the sweep returns is handed to
    /// <paramref name="route"/>, normally the brokerage's message handler.</summary>
    protected BrokerageOrderPollingService(Action<BrokerOrderState> route, IOrderProvider orderProvider,
        TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null);

    /// <summary>One read of the broker, giving the states the sweep saw. The loop calls it every
    /// poll interval, hands each state to the route, and counts a throw as one failed sweep.</summary>
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
    /// <see cref="OrderEvents"/> with what is new. Register it on the message handler for
    /// <see cref="BrokerOrderState"/>, so polled orders queue behind an order request that holds
    /// the stream lock.
    /// </summary>
    public void ProcessOrderState(BrokerOrderState orderState);

    /// <summary>The whole handover from a stream to polling, in the only safe order: process what the
    /// stream already delivered, seed one watch per open Lean order, then Start. Both callbacks are
    /// optional. See "Seed before Start".</summary>
    public void SeedAndStart(Action drainBufferedMessages = null, Func<Order, BrokerOrderState> seed = null);

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
    public PerOrderIdPollingService(Func<string, BrokerOrderState> readOrder, Action<BrokerOrderState> route,
        IOrderProvider orderProvider, TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null);
}

/// <summary>
/// For a broker with only a bulk endpoint. A sweep calls the read once, whatever is watched.
/// </summary>
public class AllOrdersPollingService : BrokerageOrderPollingService
{
    public AllOrdersPollingService(Func<IEnumerable<BrokerOrderState>> readAllOrders, Action<BrokerOrderState> route,
        IOrderProvider orderProvider, TimeSpan? pollInterval = null, TimeSpan? watchTimeout = null);
}
```

The loop, `Start`, `Stop`, `Dispose` and the failure counter come from Schwab's service, the more complete one.
The watch registry, the compare with the last state seen and the seeding of orders already open at startup come from Public's.

### The two modes

The mode is the class. Both run the same diff — it lives in the base — and a subclass is only its `Sweep`:

- **`PerOrderIdPollingService`** — `Func<string, BrokerOrderState>`: the sweep loops the watched ids and calls the
  read once per id. Nothing watched, nothing requested. Public.com's own service has exactly this constructor
  today — `new OrderPollingService(_apiClient.GetOrderById, _messageHandler.HandleNewMessage, interval)`.
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
| Tradier | bulk | watched | same as IB: the unknown ids are re-checked against a full read (`GetIntradayAndPendingOrders`, `TradierBrokerage.cs:1266`) |
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
`Brokerages/BrokerageMessageQueue.cs:98`). That is why `ProcessOrderState` is a separate public method
instead of something the loop calls itself: the brokerage puts its message handler between the read and the diff,
and the queue keeps the order right. The handler's message type also has to cover both the stream's model and the
snapshot — the next section is how it does.

One requirement travels with the queue: the place request, the `BrokerId` assignment and the `Submitted` report
must all happen inside the same `WithLockedStream` block that the snapshots queue behind. That is what guarantees
a queued snapshot always resolves the id and finds the status the request already reported. Schwab's fallback path
does all three inside the lock today.

### One lock for two message types

`BrokerageConcurrentMessageHandler<T>` knows exactly one message type, so a plugin whose stream model and snapshot
differ had no way to push both through one lock. Schwab works around it with a marker interface: its stream model
and its polled model both implement `IOrderUpdateMessage`, and the handler is typed to that. This works inside one
plugin that owns both models, and it cannot be the shared answer — every plugin that adopts the service would
have to add the interface to its own wire models, and the core `BrokerOrderState` cannot implement a per-plugin
interface.

So the handler is split instead, and the split ships with the service. The lock, the buffer and the drain loop
move unchanged into a new class, `BrokerageMessageQueue` (`Brokerages/BrokerageMessageQueue.cs`): the buffer holds
`object`, and every dequeued message is raised through a `MessageReceived` event, in arrival order.
`BrokerageConcurrentMessageHandler<T>` stays as a thin wrapper over one queue, with the same public surface every
plugin compiles against today, plus one method:

```csharp
// the stream type, exactly as today
_messageHandler = new BrokerageConcurrentMessageHandler<AccountContent>(OnAccountContent, concurrencyEnabled);

// one more line, and snapshots share the same lock — no marker interface, no second handler
_messageHandler.RegisterMessageType<BrokerOrderState>(_orderPollingService.ProcessOrderState);

// one method for both; the compiler picks the type from the argument
_messageHandler.HandleNewMessage(accountContent);
_messageHandler.HandleNewMessage(orderState);
```

`RegisterMessageType` subscribes a filter on the queue's event: a dequeued message runs one `is` check per
registered type and lands in the action that matches. `HandleNewMessage` becomes generic, with the type inferred
from the argument, so every existing call in every plugin compiles unchanged.

The stream's hot path pays nothing for this. The filter is created once at registration, never per message; the
buffer stored references before and stores references now (`T` was already constrained to `class`); the lock code
moved without an edit. Per message, the old direct delegate call becomes an event invoke plus one type check per
registered type — nanoseconds next to the lock the path already takes. The existing handler tests pass against
the split unchanged (`Tests/Brokerages/BrokerageConcurrentMessageHandlerTests.cs` — ordering, backpressure, single
drainer, exception recovery), and the shared lock across two types has its own
(`Tests/Brokerages/BrokerageMessageQueueTests.cs`).

One rule of the split is absolute: **the generic `BrokerageConcurrentMessageHandler<T>` stays.** Thirteen live
plugins hold a field of it today — CharlesSchwab, Public.com, Alpaca, Binance, TradeStation, Tastytrade, Webull,
ByBit, Eze, OANDA, IG, dYdX and TerminalLink — plus the Template scaffold. Removing or reshaping `<T>` breaks all
of them at once. So the class keeps its name, both constructors, `HandleNewMessage(T message)`,
`WithLockedStream` and `Dispose`, and every plugin recompiles with zero edits. The riskiest call shape was
checked one repo at a time: Schwab, Public and Eze pass `_messageHandler.HandleNewMessage` as a delegate
(`CharlesSchwabBrokerage.OrderUpdatePolling.cs:65`, `PublicBrokerage.cs:182`, `EzeBrokerage.cs:259`), and all
three still compile; the one shape that could not — a bare `null` argument, which type inference cannot resolve —
appears in no repo.

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
time, not each fill's own — Tradier's poller ships exactly this trade-off today and documents it
(`TradierBrokerage.cs:1469-1472`). Public today recovers the exact increment price from the change of the average
(`PublicBrokerage.Brokerage.cs:697`); the service drops that arithmetic on purpose — it amplifies broker rounding
and can even go negative on a tiny part, while the simple price needs no guard at all.

One more detail is load-bearing. **State outlives the terminal event.** Forgetting an order the moment its
`Canceled` goes out re-reports every fill if the next sweep lands before Lean applies the event — Schwab's own ADR
documents exactly this race. So state is dropped only when a compare sees the order closed **in Lean**, never at
emission.

### Later: orders placed outside Lean

The "none found" branch skips today, but it is also an opening. An id the order provider keeps not knowing is most
likely an order the user placed outside Lean, in the broker's own app — and Lean already has a door for those:
`OnNewBrokerageOrderNotification` (`Brokerages/Brokerage.cs:256`). The transaction handler picks it up
(`Engine/TransactionHandlers/BrokerageTransactionHandler.cs:190`, `:1674`), asks the algorithm's brokerage message
handler whether to accept the order, and adopts it with `AddOpenOrder`. TradeStation already raises it from its
stream (`TradeStationBrokerage.cs:1088`); a poll can feed the same door.

Not in this PR, for one reason: telling "placed outside Lean" apart from "ours, id not assigned yet" needs care —
Tradier's 2-second recheck exists exactly because an unknown id can turn out to be ours a moment later. The safe
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

A replace moves the state, because the registry is keyed by brokerage id and a replace gives the order a new one.
Inside the same locked block that reports `UpdateSubmitted`, the plugin moves it across:
`TryGetLastOrderState(oldId, out var lastSeen)`, then `Watch(newId, lastSeen)`, then `Unwatch(oldId)`. A broker
whose replacement counts its executions from zero seeds the new id with a fresh `Submitted` snapshot instead of
moving the old state — Schwab's replace path does exactly that. The same seed rule covers a plugin that reports
`Submitted` from the request path, as Schwab does without the stream: watch the new id with a `Submitted` snapshot
in that block, so the next sweep does not repeat the submit.

### Seed before Start

Polling never starts first: the stream reported orders before it, and the registry must know what was already
reported before the first sweep runs. So every `Start` that follows stream time begins with a handover, and the
service owns its order — `SeedAndStart(drainBufferedMessages, seed)` does nothing while polling already runs, and
otherwise runs three steps:

1. `drainBufferedMessages` — the plugin passes `() => _messageHandler.WithLockedStream(() => { })`, so every fill
   the stream already delivered is counted. Nothing slips in after it, because the switch runs on the stream's own
   thread — the only thread that delivers stream messages.
2. One `seed(openLeanOrder)` call per open Lean order, each becoming a `Watch(id, lastSeen)`: the plugin returns
   the brokerage id, the order's status and the cumulative filled quantity from its own bookkeeping. Orders the
   stream already closed need no seed — the diff skips every order Lean has closed, and a null return skips the
   order.
3. `Start`.

Both callbacks are optional: a plugin with no message handler passes no drain, a plugin whose stream reported
nothing passes no seed. The first sweep then continues from what the stream reported instead of repeating it. A
fallback-mode plugin makes this one call, because its stream never comes back — Schwab's `ToSeedState` is the
working example of the seed callback. A gap-mode plugin repeats the call on every drop, and `Stop`s when the
stream returns.

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
  listening. `SeedAndStart` hands over what was reported, in the right order — see "Seed before Start".
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
brokerage that uses the default. Core helpers already work this way: the message queue reads
`brokerage-concurrent-message-handler-buffer-size` in its constructor
(`Brokerages/BrokerageMessageQueue.cs:55`). The 3000 ms default is the value Schwab runs today.

## Wiring, per plugin

The general shape, for a streaming brokerage with a bulk endpoint — the class is the mode:

```csharp
// bulk broker: one request per sweep reads the whole account
_orderPollingService = new AllOrdersPollingService(
    () => _apiClient.GetAllOrders().Select(ToOrderState),   // read: model -> snapshot
    _messageHandler.HandleNewMessage,                     // route: through the message handler
    _orderProvider, pollInterval: TimeSpan.FromSeconds(3), watchTimeout: TimeSpan.FromMinutes(1));
_orderPollingService.OrderEvents += (_, orderEvents) => OnOrderEvents(orderEvents);

// the handler dequeues a snapshot and hands it to the diff
_messageHandler.RegisterMessageType<BrokerOrderState>(_orderPollingService.ProcessOrderState);
```

A per-id broker only changes the class and the read — this is Public.com's own constructor today, snapshot
instead of its DTO:

```csharp
_orderPollingService = new PerOrderIdPollingService(
    brokerageId => ToOrderState(_apiClient.GetOrderById(brokerageId)),
    _messageHandler.HandleNewMessage,
    _orderProvider);   // nothing passed: brokerage-order-poll-interval-ms decides, default 3000 ms
```

InteractiveBrokers, the plugin with no answer today, adopts `AllOrdersPollingService` (it has no per-id request) with
the thinnest possible mapping — id and status from the orders `reqAllOpenOrders` returns, fill fields left null.
The fill numbers are not out of reach: the captured `IBApi.Order` already carries a `FilledQuantity` field, and the
paired `orderStatus` callback adds the average fill price, so the mapping can grow later without a new endpoint.
The first step stays thin:

```csharp
// IBPlaceOrder, instead of blocking up to 5 minutes and then killing the run
_orderPollingService.Watch(ibOrderId.ToStringInvariant());
_orderPollingService.Start();   // idempotent; Stop once nothing is watched
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
_orderPollingService.SeedAndStart(() => _messageHandler.WithLockedStream(() => { }), ToSeedState);

// on reconnect: one last sweep for the gap, then hand the job back to the stream
_orderPollingService.Stop();
```

CharlesSchwab and Public.com delete their own service class **and their diff**, and keep only the mapping from
their models to the snapshot. Schwab keeps its own rule of never going back to the stream. Tradier replaces the
inline `Task.Delay` block with `Watch` on the unknown ids.

## What stays in the plugin

- Reading the broker: the read callback and the class choice between per-id and all orders, plus how far
  back a bulk read reaches (Schwab reads from its oldest open Lean order).
- Converting model to state: the status mapping, combo leg ids (Schwab's `mainId + legId - 1`), reducing an
  execution history to the two numbers (Schwab sums its execution legs and takes the newest leg's price). A plugin
  whose leg ids are derived rather than returned by the broker should verify they resolve and warn when they do
  not — the service skips silently.
- Routing: passing snapshots through its message handler and forwarding `OrderEvents` to `OnOrderEvents`.
- Reporting without the stream: Schwab's submit and replace events from the REST reply stay in the plugin; the
  service only asks that they seed the registry (see "One registry for the stream and the poll").
- Deciding what an unacknowledged order means.

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
  Replaced by the `BrokerageMessageQueue` split (see "One lock for two message types").
- **A dual-generic handler**, `BrokerageConcurrentMessageHandler<T, U>` with the stream type and the polled type.
  Rejected: every existing plugin migrates to the new shape even with no polling, a plugin with no stream (IB)
  has no honest `T`, and a third source would need `<T, U, V>`. The queue split adds a type with a registration
  call instead of a type parameter.
- **A handler base class that queues work items (`Action`) instead of messages.** The typed wrapper would then
  wrap every stream message in a new closure — one allocation per message on the hottest path a brokerage has.
  The queue split keeps the message itself in the buffer and allocates only at registration.
- **Put it on the `Brokerage` base class, driven by the engine, like the cash sync.**
  `ShouldPerformCashSync` / `PerformCashSync` (`Brokerages/Brokerage.cs:480`, called from
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
- **Leave it in the plugins.** It is already written three times, and the fourth copy would be IB's.

## Risks

| Risk | What we do about it |
| --- | --- |
| A plugin maps a broker status to the wrong Lean status | The mapping is the same one its streaming path already needs, written once per plugin and covered by its own tests. The service only emits transitions, so a wrong mapping surfaces once, not as a flood. |
| A state without fill numbers cannot produce fill events | By design: null means unknown, and the service never invents a number. The watch still confirms submission, and the watch timeout still fires. |
| Several fills inside one sweep share one price | Quantities stay exact; the price is the broker's reported price at sweep time. Tradier ships this trade-off today (`TradierBrokerage.cs:1469-1472`); a shorter poll interval narrows it. |
| Polling adds requests on brokers with tight rate limits | Watch mode reads only while an order is unacknowledged, and the interval is a constructor argument the plugin picks. |
| A bulk read is expensive on some plugins — `GetOpenOrders()` rebuilds Lean orders and maps symbols on every call | The action converts straight from the broker's wire model to the snapshot, skipping the Lean `Order` build entirely; and in watch mode a sweep only runs while something is pending. |
| A plugin passes `ProcessOrderState` as the route and gets fill-before-submit | The route is documented as "your message handler", and wiring it right is two lines: `RegisterMessageType<BrokerOrderState>(ProcessOrderState)` once, `HandleNewMessage` as the route. Schwab and Public have a handler; IB and Tradier do not, and adding one is named in their rollout steps. |
| The stream and the poll both see the same fill in watch mode | The registry works in both directions: the stream writes what it reports (`UpdateOrderState`) and checks before reporting (`TryGetLastOrderState`). Named as the one non-optional wiring rule for polling beside a live stream. |
| The watch timeout cannot tell "never arrived" from "filled instantly" | It does not try. `OrderNotAcknowledged` hands the question to the brokerage, which has the endpoints to answer it. |
| Polling while the stream is down misses the fills that happened during the gap | Only when the read carries no fill data. A read with fill numbers recovers them — the state has the fields, so this is a property of the broker's endpoint, not of the service. |
| A plugin starts polling on disconnect and forgets to stop on reconnect | Both paths are one line and sit next to the connection handling the plugin already has. The poll side repeats nothing the registry already holds, so the cost of forgetting is extra requests, not extra events. |

## Rollout

1. This PR: the `BrokerageMessageQueue` split of the message handler, the service, the snapshot, and their unit
   tests, no brokerage changes. The split keeps the handler's public surface, so every plugin compiles as before.
2. InteractiveBrokers: add the message handler it does not have, then replace the `NoBrokerageResponse` error and
   the invented `Submitted` with a watch. This is the proof that the abstraction holds for a plugin that did not
   write it.
3. CharlesSchwab and Public.com: delete their service class and their fill/close diff. What stays is real and
   named: the read and its sweep window, the model-to-state mapping, Schwab's stream-unavailable switch and its
   without-stream submit and replace reporting. Two behavior changes are intentional: a Public poll that shows a
   new fill and the cancel together now emits both — today's code drops the cancel
   (`PublicBrokerage.Brokerage.cs:629-638`) — and polled fills are priced at the broker's reported price of the
   sweep, so Public's change-of-average recovery and Schwab's per-execution prices become per-sweep prices while
   the quantities stay exact.
4. Tradier: replace the inline re-check block with a watch. Tradier splits orders across zero, so one brokerage
   order can cover only part of the Lean quantity — its watch resolves submissions only, and fills stay on its
   existing path.
