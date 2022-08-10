![alt tag](https://raw.githubusercontent.com/QuantConnect/Lean/master/Documentation/logo.white.small.png) 
## LEAN Data Formats / Equity

QuantConnect hosts US Equity Data (market 'USA') provided by [AlgoSeek](https://www.algoseek.com/) (trade and quote data from post-2007) and QuantQuote (trade data from pre-2007). Ticks are stored unfiltered. TradeBars and QuoteBars have suspicious ticks filtered out and the resulting ticks are consolidated and saved. QuantConnect has *trade* equity ticks as `{YYYYMMDD}_trade.zip` files, and *quote* equity ticks as `{YYYYMMDD}_quote.zip`.

The US equity data is in the New York timezone. Data timezones are found in the [MarketHoursDatabase.json](https://github.com/QuantConnect/Lean/blob/master/Data/market-hours/market-hours-database.json)

Equity data supports the following Resolutions:

* Tick
* Second
* Minute
* Hour
* Daily

### Minute, Second Data File Format ###

Minute, Second files are located in the equity / usa / resolution folders. The file name uses a 8-character length date: `/data/equity/usa/minute/ticker/{YYYYMMDD}_{trade|quote}.zip`. Note that only post-2007 period has quote data.

The zip file contains 1 CSV file which repeats the information about the path in the file name. e.g. `20140605_aapl_minute_trade.csv`.

The trade CSV contents are as follows:

| Time | Open | High | Low | Close | Volume
| ----------- | ---------- | --------- | ---------- | --------- | ---------
| 15300000 | 6448000  | 6448000 | 6448000 | 6448000 | 90

 - Time - Milliseconds since midnight in the timezone of the data format. 
 - Open - Deci-cents Open Price for TradeBar.
 - High - Deci-cents High Price for TradeBar.
 - Low - Deci-cents Low Price for TradeBar.
 - Close - Deci-cents Close Price for TradeBar.
 - Volume - Number of shares traded in this TradeBar.

The quote CSV contents are as follows:

| Time | Bid Open | Bid High | Bid Low | Bid Close | Bid Size | Ask Open | Ask High | Ask Low | Ask Close | Ask Size
| ----------- | ---------- | --------- | ---------- | --------- | --------- | ---------- | --------- | ---------- | --------- | ---------
| 15300000 | 6448000  | 6448000 | 6448000 | 6448000 | 90 | 6500000  | 6500000 | 6500000 | 6500000 | 100

 - Time - Milliseconds since midnight in the timezone of the data format. 
 - Bid Open - Deci-cents Bid Open Price for QuoteBar.
 - Bid High - Deci-cents Bid High Price for QuoteBar.
 - Bid Low - Deci-cents Bid Low Price for QuoteBar.
 - Bid Close - Deci-cents Bid Close Price for QuoteBar.
 - Bid Size - Number of shares being bid that quoted in this QuoteBar.
 - Ask Open - Deci-cents Ask Open Price for QuoteBar.
 - Ask High - Deci-cents Ask High Price for QuoteBar.
 - Ask Low - Deci-cents Ask Low Price for QuoteBar.
 - Ask Close - Deci-cents Ask Close Price for QuoteBar.
 - Ask Size - Number of shares being ask for that quoted in this QuoteBar.

### Hour and Daily File Format

Hour and Daily files are located in the `/equity/usa/{hour|daily}` folder. Each file contains all bars available for this ticker. e.g. `/data/equity/usa/hour/aapl.zip`. The zip file contains 1 CSV file named the same as the ticker (`aapl.csv`). Only trade bar data is available in Hour and Daily resolution.

The CSV contents are as follows:

| DateTime | Open | High | Low | Close | Volume
| ----------- | ---------- | --------- | ---------- | --------- | ---------
| 20131001 09:00 | 6448000  | 6448000 | 6448000 | 6448000 | 90

 - DateTime - String date "YYYYMMDD HH:MM" in the timezone of the data format.
 - Open - Deci-cents Open Price for TradeBar.
 - High - Deci-cents High Price for TradeBar.
 - Low - Deci-cents Low Price for TradeBar.
 - Close - Deci-cents Close Price for TradeBar.
 - Volume - Number of shares traded in this TradeBar.

Divide prices by 10,000 to convert deci-cents to dollars.

### Tick File Format

Equity tick data is stored in files which are located in the `/equity/usa/tick` folder. The file name uses a 8-character length date: `/data/equity/usa/tick/{ticker}/{YYYYMMDD}_{trade|quote}.zip`.  QuantConnect currently provides *Quote* equity ticks (post-2007) and *Trade* equity ticks.

Trade tick files are stored in files named `{YYYYMMDD}_trade.zip`. There is one file equity tick names named after the data: `20131008_bac_Trade_Tick.csv`. The CSV contains records of each trade:

| Time | TradeSale | Trade Volume | Exchange | Trade Sale Condition | Suspicious
| ----------- | ---------- | --------- | ---------- | --------- | ---------
| 14400009.367 | 137450 | 100 | D | 1 | 0

 - Time - Milliseconds since midnight in the timezone of the data format. 
 - TradeSale - Deci-cents price of the tick sale.
 - Volume - Number of shares in the sale.
 - Exchange - Location of the sale.
 - Trade Sale Condition - Notes on the sale. 
 - Suspicious - Boolean indicating the tick is flagged as suspicious according to AlgoSeek's algorithms. This generally indicates the trade is far from other market prices and may be reversed.
TradeBar data excludes suspicious ticks.

Quote tick files are stored in files named `{YYYYMMDD}_quote.zip`. There is one file equity tick names named after the data: `20131008_bac_Quote_Tick.csv`. The CSV contains records of each trade:

| Time | Bid Sale | Bid Size | Ask Sale | Ask Size | Exchange | Quote Sale Condition | Suspicious
| ----------- | ---------- | --------- | ---------- | --------- | ---------- | --------- | ---------
| 14400009.367 | 137450 | 100 | 0 | 0 | D | 1 | 0

 - Time - Milliseconds since midnight in the timezone of the data format. 
 - Bid Sale - Deci-cents bid price of the bid quote tick.
 - Bid Size - Number of shares in the bid quote tick.
 - Ask Sale - Deci-cents ask price of the ask quote tick.
 - Ask Size - Number of shares in the ask quote tick.
 - Exchange - Location of the sale.
 - Quote Sale Condition - Notes on the sale. 
 - Suspicious - Boolean indicating the tick is flagged as suspicious according to AlgoSeek's algorithms. This generally indicates the quote is far from other market prices and may be reversed. 
Each quote tick contains either bid or ask data only. QuoteBar data excludes suspicious ticks.
 
#### Tick Exchange Codes

Exchange Letter | Exchange Name
--- | ---
A | NYSE MKT Stock Exchange
B | NASDAQ OMX BX Stock Exchange
C | National Stock Exchange
D | FINRA
I | International Securities Exchange
J | Direct Edge A Stock Exchange
K | Direct Edge X Stock Exchange
M | Chicago Stock Exchange
N | New York Stock Exchange
T | NASDAQ OMX Stock Exchange
P | NYSE Arca SM
S | Consolidated Tape System
T/Q | NASDAQ Stock Exchange
W | CBOE Stock Exchange
X | NASDAQ OMX PSX Stock Exchange
Y | BATS Y-Exchange
Z | BATS Exchange 

#### Tick Sale Conditions
##### [Trade Ticks](https://github.com/QuantConnect/Lean/blob/master/Common/Data/Auxiliary/TradeConditionFlags.cs)
Sale Condition Code | Description 
--- | ---
0 | No Condition
1 | A trade made without stated conditions is deemed regular way for settlement on the third business day following the transaction date.
2 | A transaction which requires delivery of securities and payment on the same day the trade takes place.
4 | A transaction that requires the delivery of securities on the first business day following the trade date.
8 | A Seller’s Option transaction gives the seller the right to deliver the security at any time within a specific period, ranging from not less than two calendar days, to not more than sixty calendar days.
10 | Market Centers will have the ability to identify regular trades being reported during specific events as out of the ordinary by appending a new sale condition code Yellow Flag (Y) on each transaction reported to the UTP SIP. The new sale condition will be eligible to update all market center and consolidated statistics.
20 | The transaction that constituted the trade-through was the execution of an order identified as an Intermarket Sweep Order.
40 | The trade that constituted the trade-through was a single priced opening transaction by the Market Center.
80 | The transaction that constituted the trade-through was a single priced closing transaction by the Market Center.
100 | The trade that constituted the trade-through was a single priced reopening transaction by the Market Center.
200 | The transaction that constituted the trade-through was the execution of an order at a price that was not based, directly or indirectly, on the quoted price of the security at the time of execution and for which the material terms were not reasonably determinable at the time the commitment to execute the order was made.
400 | Trading in extended hours enables investors to react quickly to events that typically occur outside regular market hours, such as earnings reports. However, liquidity may be constrained during such Form T trading, resulting in wide bid-ask spreads.
800 | Sold Last is used when a trade prints in sequence but is reported late or printed in conformance to the One or Two Point Rule.
1000 | The transaction that constituted the trade-through was the execution by a trading center of an order for which, at the time of receipt of the order, the execution at no worse than a specified price a 'stopped order'
2000 | Identifies a trade that was executed outside of regular primary market hours and is reported as an extended hours trade.
4000 | Identifies a trade that takes place outside of regular market hours.
8000 | An execution in two markets when the specialist or Market Maker in the market first receiving the order agrees to execute a portion of it at whatever price is realized in another market to which the balance of the order is forwarded for execution.
10000 | A transaction made on the Exchange as a result of an Exchange acquisition.
20000 | A trade representing an aggregate of two or more regular trades in a security occurring at the same price either simultaneously or within the same 60-second period, with no individual trade exceeding 10,000 shares.
40000 | Stock-Option Trade is used to identify cash equity transactions which are related to options transactions and therefore potentially subject to cancellation if market conditions of the options leg(s) prevent the execution of the stock-option order at the price agreed upon.
80000 | Sale of a large block of stock in such a manner that the price is not adversely affected.
100000 | A trade where the price reported is based upon an average of the prices for transactions in a security during all or any portion of the trading day.
200000 | Indicates that the trade resulted from a Market Center’s crossing session.
400000 | Indicates a regular market session trade transaction that carries a price that is significantly away from the prevailing consolidated or primary market value at the time of the transaction.
800000 | To qualify as a NYSE AMEX Rule 155
1000000 | Indicates the ‘Official’ closing value as determined by a Market Center. This transaction report will contain the market center generated closing price.
2000000 | A sale condition that identifies a trade based on a price at a prior point in time i.e. more than 90 seconds prior to the time of the trade report. The execution time of the trade will be the time of the prior reference price.
4000000 | Indicates the ‘Official’ open value as determined by a Market Center. This transaction report will contain the market
8000000 | The CAP Election Trade highlights sales as a result of a sweep execution on the NYSE, whereby CAP orders have been elected and executed outside the best price bid or offer and the orders appear as repeat trades at subsequent execution prices. This indicator provides additional information to market participants that an automatic sweep transaction has occurred with repeat trades as one continuous electronic transaction.
10000000 | A sale condition code that identifies a NYSE trade that has been automatically executed without the potential benefit of price improvement.
20000000 | Denotes whether or not a trade is exempt (Rule 611) and when used jointly with certain Sale Conditions, will more fully describe the characteristics of a particular trade.
40000000 | This flag is present in raw data, but AlgoSeek document does not describe it.
80000000 | Denotes the trade is an odd lot less than a 100 shares.

##### [Quote Ticks](https://github.com/QuantConnect/Lean/blob/master/Common/Data/Auxiliary/QuoteConditionFlags.cs)
Sale Condition Code | Description 
--- | ---
0 | No Condition
1 | This condition is used for the majority of quotes to indicate a normal trading environment.
2 | This condition is used to indicate that the quote is a Slow Quote on both the Bid and Offer sides due to a Set Slow List that includes High Price securities.
4 | While in this mode, auto-execution is not eligible, the quote is then considered manual and non-firm in the Bid and Offer and either or both sides can be traded through as per Regulation NMS.
8 | This condition can be disseminated to indicate that this quote was the last quote for a security for that Participant.
10 | This regulatory Opening Delay or Trading Halt is used when relevant news influencing the security is being disseminated. Trading is suspended until the primary market determines that an adequate publication or disclosure of information has occurred.transaction reported to the UTP SIP. The new sale condition will be eligible to update all market center and consolidated statistics.
20 | This condition is used to indicate a regulatory Opening Delay or Trading Halt due to an expected news announcement, which may influence the security. An Opening Delay or Trading Halt may be continued once the news has been disseminated.
40 | The condition is used to denote the probable trading range (bid and offer prices, no sizes) of a security that is not Opening Delayed or Trading Halted. The Trading Range Indication is used prior to or after the opening of a security.
80 | This non-regulatory Opening Delay or Trading Halt is used when there is a significant imbalance of buy or sell orders.
100 | This condition is disseminated by each individual FINRA Market Maker to signify either the last quote of the day or the premature close of an individual Market Maker for the day.
200 | This quote condition indicates a regulatory Opening Delay or Trading Halt due to conditions in which a security experiences a 10 % or more change in price over a five minute period.
400 | This quote condition suspends a Participant's firm quote obligation for a quote for a security.
800 | This condition can be disseminated to indicate that this quote was the opening quote for a security for that Participant.
1000 | This non-regulatory Opening Delay or Trading Halt is used when events relating to one security will affect the price and performance of another related security. This non-regulatory Opening Delay or Trading Halt is also used when non-regulatory halt reasons such as Order Imbalance, Order Influx and Equipment Changeover are combined with Due to Related Security on CTS.
2000 | This quote condition along with zero-filled bid, offer and size fields is used to indicate that trading for a Participant is no longer suspended in a security which had been Opening Delayed or Trading Halted.
4000 | This quote condition is used when matters affecting the common stock of a company affect the performance of the non-common associated securities, e.g., warrants, rights, preferred, classes, etc.
8000 | This non-regulatory Opening Delay or Trading Halt is used when the ability to trade a security by a Participant is temporarily inhibited due to a systems, equipment or communications facility problem or for other technical reasons.
10000 | This non-regulatory Opening Delay or Trading Halt is used to indicate an Opening Delay or Trading Halt for a security whose price may fall below $1.05, possibly leading to a sub-penny execution.
20000 | This quote condition is used to indicate that an Opening Delay or a Trading Halt is to be in effect for the rest of the trading day in a security for a Participant.
40000 | This quote condition is used to indicate that a Limit Up-Limit Down Price Band is applicable for a security.
80000 | This quote condition is used to indicate that a Limit Up-Limit Down Price Band that is being disseminated is a ‘republication’ of the latest Price Band for a security.
100000 | This indicates that the market participant is in a manual mode on both the Bid and Ask. While in this mode, automated execution is not eligible on the Bid and Ask side and can be traded through pursuant to Regulation NMS requirements.
200000 | For extremely active periods of short duration. While in this mode, the UTP participant will enter quotations on a “best efforts” basis.
400000 | A halt condition used when there is a sudden order influx. To prevent a disorderly market, trading is temporarily suspended by the UTP participant.

See more information in the AlgoSeek [whitepaper](https://us-equity-market-data-docs.s3.amazonaws.com/algoseek.US.Equity.TAQ.pdf).

##### Trade Ticks (Pre-2007)
Exchange | Sale Condition Code | Description 
---- | --- | ---
CTS | Blank or ‘@’ | Regular Sale (no condition)
CTS | ‘B’ | Average Price Trade
CTS | ‘C’ | Cash Trade (same day clearing)
CTS | ‘E’ | Automatic Execution
CTS | ‘F’ | Intermarket Sweep Order
CTS | ‘G’ | Opening/Reopening Trade Detail
CTS | ‘H’ | Intraday Trade Detail
CTS | ‘I’ | CAP Election Trade
CTS | ‘J’ | Rule 127 Trade
CTS | ‘K’ | Rule 127 trade (NYSE only) or Rule 155 trade
NYSE | ‘L’ | Sold Last (late reporting)
NYSE | ‘N’ | Next Day Trade (next day clearing)
NYSE | ‘O’ | Market Center Opening Trade
NYSE | ‘R’ | Seller
NYSE | ‘S’ | Reserved
NYSE | ‘T’ | Extended Hours Trade
NYSE | ‘U’ | Extended Hours (Sold Out of Sequence)
NYSE | ‘Z’ | Sold (out of sequence)
NYSE | ‘4’ | Derivatively Priced
NYSE | ‘5’ | Market Center Re-opening Prints
NYSE | ‘6’ | Market Center Closing Prints 
NASD | ‘@’ | Regular Trade
NASD | ‘A’ | Acquisition
NASD | ‘B’ | Bunched Trade
NASD | ‘C’ | Cash Trade
NASD | ‘D’ | Distribution
NASD | ‘F’ | Intermarket Sweep
NASD | ‘G’ | Bunched Sold Trade
NASD | ‘K’ | Rule 155 Trade (NYSE MKT Only)
NASD | ‘L’ | Sold Last
NASD | ‘M’ | Market Center Close Price
NASD | ‘N’ | Next Day
NASD | ‘O’ | Opening Prints
NASD | ‘P’ | Prior Reference Price
NASD | ‘Q’ | Market Center Open Price
NASD | ‘R’ | Seller (Long-Form Message Formats Only)
NASD | ‘S’ | Split Trade
NASD | ‘T’ | Form - T Trade
NASD | ‘U’ | Extended Hours (Sold Out of Sequence)
NASD | ‘W’ | Average Price Trade
NASD | ‘Y’ | Yellow Flag
NASD | ‘Z’ | Sold (Out of Sequence)
NASD | ‘1’ | Stopped Stock - Regular Trade
NASD | ‘2’ | Stopped Stock - Sold Last
NASD | ‘3’ | Stopped Stock - Sold Last 3 | Stopped Stock - Sold
NASD | ‘4’ | Derivatively Priced
NASD | ‘5’ | Re-opening Prints
NASD | ‘6’ | Closing Prints
NASD | ‘7’ | Placeholder for 611 Exempt
NASD | ‘8’ | Placeholder for 611 Exempt
NASD | ‘9’ | Placeholder for 611 Exempt

See more information in the QuantQuote [whitepaper](https://quantquote.com/docs/TickView_Historical_Trades.pdf).