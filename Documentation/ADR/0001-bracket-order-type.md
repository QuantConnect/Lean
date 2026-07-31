# ADR 0001: Bracket order type

## Status

Proposed - 2026-07-09

## Purpose

Implement a new `OrderType.Bracket` in Lean core, plus the brokerage-side mapping in the plugins whose API supports it. This file is the working document for that implementation: it holds the broker research and the reference PRs to follow.

## Context

A bracket order is an entry (parent) order with two attached exit orders: a take-profit limit and a stop-loss. The two exits are OCO (one cancels the other). Lean users ask for this feature in live trading, but each brokerage API supports it differently: some have a native bracket/OTOCO order type, some only allow one attached exit, some only support standalone TP/SL orders, and some do not support it at all.

Before designing the Lean-side API, we researched every brokerage that has a `Lean.Brokerages.*` plugin and checked whether the broker's own trading API natively supports bracket orders. Sources are official broker/exchange documentation pages, fetched and verified.

Classification used in the table:

- **yes** - native API support for parent + take-profit + stop-loss
- **no** - no bracket support in the API

## Broker API support matrix

| brokerage | bracket | source (official doc link) | notes |
| --------- | ------- | -------------------------- | ----- |
| InteractiveBrokers | yes | https://interactivebrokers.github.io/tws-api/bracket_order.html | Parent + TP limit + SL via `parentId` |
| Alpaca | yes | https://docs.alpaca.markets/us/docs/orders-at-alpaca#bracket-orders | `order_class=bracket`, exits are OCO; no extended hours, TIF must be day/gtc |
| Tradier | yes | https://docs.tradier.com/reference/advanced-orders | Native OTOCO endpoint; equities and options; legs sent in one request |
| Tastytrade | yes | https://developer.tastytrade.com/open-api-spec/orders/, https://developer.tastytrade.com/order-management/#submit-complex-order | `POST /accounts/{n}/complex-orders`, type OTOCO; docs call it a bracket |
| TradeStation | yes | https://api.tradestation.com/docs/specification#tag/Order-Execution/operation/PlaceGroupOrder | OSO + OCO order types; stocks, options, futures; endpoint schema is deeper in the API spec |
| WeBull | yes | https://developer.webull.com/apis/docs/trade-api/stock/ | `combo_type` enum includes OTO, OCO, OTOCO and MASTER (entry + TP/SL legs) |
| Coinbase | yes | https://docs.cdp.coinbase.com/coinbase-app/advanced-trade-apis/guides/orders | `attached_order_configuration` with `trigger_bracket_gtc/gtd`; one exit fills, the other cancels |
| Binance | yes (spot) | https://developers.binance.com/docs/binance-spot-api-docs/rest-api/trading-endpoints | Spot OTOCO order list `POST /api/v3/orderList/otoco`; entry must be LIMIT/LIMIT_MAKER (no market-entry bracket). Futures API not verified yet - TP/SL there are separate conditional orders |
| TradingTechnologies | yes | https://library.tradingtechnologies.com/trade/tto-bracket-order.html | TT Bracket: parent limit/stop + OCO child pair with fill-size matching; page is platform help - TT REST/FIX reference still to confirm API-level |
| CharlesSchwab | yes (docs gated) | https://developer.schwab.com/ | Order schema (`orderStrategyType` TRIGGER/OCO, i.e. 1st-triggers-OCO) is behind developer login; public portal does not show it |
| Bybit | no | https://bybit-exchange.github.io/docs/v5/order/create-order | [Need deeply read doc] `takeProfit`/`stopLoss` attached on create (spot + derivatives); `tpslMode` Full/Partial; options get market-only exits; docs never say "OCO" explicitly |
| Kraken | no | https://docs.kraken.com/api/docs/rest-api/add-order/ | Conditional close attaches only ONE exit (`close[ordertype]`) - cannot attach both TP and SL, no OCO link |
| Bitfinex | no | https://docs.bitfinex.com/reference/ws-auth-input-order-new | `price_oco_stop` gives a limit+stop OCO pair, but no parent entry that triggers exits (no OTOCO) |
| dYdX | no | https://docs.dydx.xyz/concepts/trading/orders | Standalone TP/SL conditional orders for closing positions; no OCO/bracket linkage documented |
| Public.com | no | https://public.com/api/docs | Any mentions in doc |
| Zerodha | no | https://support.zerodha.com/category/trading-and-markets/trading-faqs/general/articles/why-bo-stopped | Bracket orders discontinued March 2020, confirmed still gone by Kite staff (2021); no API bracket |
| RBI (Raiffeisen) | no | - | Institutional FIX connection; nothing publicly documented to cite |
| Wolverine (WEX) | no | - | Institutional FIX; only an algo help page found so far |
| OANDA | no | https://developer.oanda.com/rest-live-v20/order-df/ | `takeProfitOnFill` + `stopLossOnFill`; exits are bound to the trade, so OCO effect comes from trade close |
| Eze (SS&C) | no | https://emsportal.ezesoft.com/pdf/EMS%20xAPI%20Technical%20Documentation.pdf (candidate) | EMS xAPI PDF found by search, not verified yet |
| TerminalLink (Bloomberg EMSX) | no | https://emsx-api-doc.readthedocs.io/ (candidate) | EMSX API docs found, not verified yet |
| Samco | no | - | Likely same SEBI-driven removal as Zerodha; needs confirmation |
| Rithmic | no | https://www.rithmic.com/apis | Server-side brackets and OCOs in R\|API+ and R\|Protocol; exits fire even if the client disconnects; field-level docs are gated |

## OCO / OTO primitive support matrix

A bracket is not a broker primitive on its own; it decomposes into two generic mechanisms that most brokers already expose separately or in combination:

- **OTO** (One-Triggers-Other / conditional): an order that, once filled (or once a price triggers), submits/activates another order.
- **OCO** (One-Cancels-Other): a linked pair/group where filling one leg cancels the rest.
- A **bracket** is the composition **OTO → OCO** (entry triggers an OCO exit pair); brokers usually call this **OTOCO** / **1st-Trigger-OCO** / **Bracket**.

This table records, per brokerage, whether OCO and OTO/conditional are available **standalone** (their own order class) and whether a **combined** OTOCO/bracket exists. Legend: **yes** / **no** / **partial** (only in a limited or attached form) / **—** (unknown / not publicly documented).

| brokerage | OCO standalone | OTO / conditional standalone | combined (OTOCO / bracket) | native name(s) | source (official doc link) |
| --------- | -------------- | ---------------------------- | -------------------------- | -------------- | -------------------------- |
| InteractiveBrokers | yes | yes | yes | OCA group, `parentId`, Conditional Orders, Bracket | https://interactivebrokers.github.io/tws-api/oca.html |
| Alpaca | yes | yes | yes | `order_class` = `oco` / `oto` / `bracket` (equities only) | https://alpaca.markets/blog/oco-oto/ |
| Tradier | yes | yes | yes | `class` = `oco` / `oto` / `otoco` | https://documentation.tradier.com/brokerage-api/trading/place-otoco-order |
| Tastytrade | yes | no (only inside OTOCO) | yes | complex-order type `OCO` / `OTOCO` | https://developer.tastytrade.com/order-management/#submit-complex-order |
| TradeStation | yes | yes (OSO) | yes (BRK) | group type `OCO` / `OSO` / `BRK` | https://help.tradestation.com/09_01/tradestationhelp/ob/about_oco_oso_orders.htm |
| WeBull | yes | yes | yes | `combo_type` = `OTO` / `OCO` / `OTOCO` / `MASTER` (US equities) | https://developer.webull.com/apis/docs/trade-api/stock/ |
| Coinbase | no | no | yes (attached only) | `attached_order_configuration` `trigger_bracket_gtc/gtd` | https://docs.cdp.coinbase.com/coinbase-app/advanced-trade-apis/guides/orders |
| Binance (spot) | yes | yes | yes | order list `oco` / `oto` / `otoco` (spot only) | https://developers.binance.com/docs/binance-spot-api-docs/rest-api/trading-endpoints |
| TradingTechnologies | yes | yes (OSO) | yes | TT OCO / OSO / Bracket (platform; REST/FIX exposure TBC) | https://library.tradingtechnologies.com/trade/tto-oco-order.html |
| CharlesSchwab | yes | yes (TRIGGER) | yes (TRIGGER→OCO) | `orderStrategyType` = `OCO` / `TRIGGER` (docs gated) | https://developer.schwab.com/ |
| Bybit | partial (attached TP/SL on position) | partial (conditional/trigger orders) | yes (attached TP/SL) | attached `takeProfit`/`stopLoss` (`tpslMode`), conditional order | https://bybit-exchange.github.io/docs/v5/order/create-order |
| Kraken | derivatives only | yes (Conditional Close, spot) | derivatives TP/SL bracket | Conditional Close (OTO); futures TP/SL (OCO) | https://support.kraken.com/articles/360038640052-conditional-close |
| Bitfinex | yes | no | no | OCO (limit + stop pair) | https://support.bitfinex.com/hc/en-us/articles/115003507305-What-is-a-One-Cancels-Other-OCO-order-option-on-Bitfinex |
| dYdX | no | partial (price-triggered conditional only, no order→order link) | no | conditional stop / take-profit orders | https://help.dydx.trade/en/articles/166981-perpetual-order-types-on-dydx-chain |
| Public.com | no | no | no | — | https://public.com/api/docs |
| Zerodha | yes (GTT two-leg) | partial (GTT single-leg conditional; intraday BO discontinued 2020) | no | GTT OCO (`two-leg`) | https://kite.trade/docs/connect/v3/gtt/ |
| RBI (Raiffeisen) | — | — | — | institutional FIX (undocumented) | - |
| Wolverine (WEX) | — | — | — | institutional FIX (undocumented) | - |
| OANDA | trade-level only | yes (`takeProfitOnFill` / `stopLossOnFill`) | yes (entry + TP/SL on fill) | `takeProfitOnFill` / `stopLossOnFill` | https://developer.oanda.com/rest-live-v20/order-df/ |
| Eze (SS&C) | — | — | — | EMS xAPI (unverified) | - |
| TerminalLink (Bloomberg EMSX) | — | — | — | EMSX API (unverified) | - |
| Samco | — | — | — | SEBI-driven removal like Zerodha BO; unverified | - |
| Rithmic | yes (server-side) | via bracket | yes (server-side bracket) | server-side OCO / Bracket (R\|API+) | https://www.rithmic.com/apis |

**Takeaway:** the composable **OCO + OTO** model is the norm, not the exception. Seven brokers expose OCO and OTO/OSO/TRIGGER as distinct order classes with OTOCO/bracket as the explicit combination (InteractiveBrokers, Alpaca, Tradier, Binance spot, WeBull, TradeStation, CharlesSchwab). Standalone **OCO** is broadly available beyond those (Bitfinex, Zerodha GTT, TradingTechnologies, Rithmic, Tastytrade). Only a minority are bracket-/attach-only (Coinbase, OANDA, Bybit) or single-conditional-only (dYdX, Kraken spot). This supports implementing **OCO** and **conditional (OTO)** as independent Lean primitives, with **bracket** as a thin OTOCO composition on top.

## Reference implementations

Earlier PRs that added a new order type to Lean. They show every place a new order type must touch: `OrderType` enum, order class in `Common/Orders`, serialization, brokerage models, fill models, Python bindings, and tests.

- TrailingStop - https://github.com/QuantConnect/Lean/pull/7402
- ComboMarketOrder, ComboLimitOrder, ComboLegLimitOrder - https://github.com/QuantConnect/Lean/pull/6813 - introduced `GroupOrderManager` for orders that belong to one group; closest pattern for a parent order with attached exits
- LimitIfTouched - https://github.com/QuantConnect/Lean/pull/5164

