![alt tag](https://raw.githubusercontent.com/QuantConnect/Lean/master/Documentation/logo.white.small.png) 
## LEAN Data Formats / Equity

QuantConnect hosts US Equity Data (market 'usa') provided by QuantQuote. Ticks are stored unfiltered. TradeBars have suspicious ticks filtered out and the resulting ticks are consolidated and saved. QuantConnect has *trade* equity ticks only; so only saves `date_trade.zip` files.

The US equity data is in the New York timezone. Data timezones are found in the [MarketHoursDatabase.json](https://github.com/QuantConnect/Lean/blob/master/Data/market-hours/market-hours-database.json)

Equity data supports the following Resolutions:

* Tick
* Second
* Minute
* Hour
* Daily

### Minute, Second Data File Format ###

Minute, Second files are located in the equity / usa / resolution folders. The file name uses a 8-character length date.   `/data/equity/usa/minute/ticker/YYYYMMDD_trade.zip`.

The zip file contains 1 CSV file which repeats the information about the path in the file name. e.g. `20140605_aapl_minute_trade.csv`. Currently this filename is not used but for consistency it should follow this pattern.

The CSV contents are as follows:

| Time | Open | High | Low | Close | Volume
| ----------- | ---------- | --------- | ---------- | --------- | ---------
| 15300000 | 6448000  | 6448000 | 6448000 | 6448000 | 90

 - Time - Milliseconds since midnight in the timezone of the data format. 
 - Open - Deci-cents Open Price for TradeBar.
 - High - Deci-cents High Price for TradeBar.
 - Low - Deci-cents Low Price for TradeBar.
 - Close - Deci-cents Close Price for TradeBar.
 - Volume - Number of shares traded in this TradeBar.

### Hour and Daily File Format

Hour and Daily files are located in the `/equity/usa/{hour|daily}` folder. Each file contains all bars available for this ticker. e.g. `/data/equity/usa/hour/aapl.zip`. The zip file contains 1 CSV file named the same as the ticker (`aapl.csv`). Currently this filename is not used but for consistency it should follow this pattern.

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

Equity tick data is stored in files which are located in the `/equity/usa/tick` folder. The file name uses a 8-character length date.   `/data/equity/usa/tick/{ticker}/YYYYMMDD_tickType.zip`.  QuantConnect currently only provides *Trade* equity ticks.

Trade tick files are stored in files named `YYYYMMDD_trade.zip`. There is one file equity tick names named after the data: `20131008_bac_Trade_Tick.csv`. The CSV contains records of each trade:

| Time | TradeSale | Trade Volume | Exchange | Sale Condition | Suspicious
| ----------- | ---------- | --------- | ---------- | --------- | ---------
| 48754000 | 137450 | 100 | D | @ | 0

 - Time - Milliseconds since midnight in the timezone of the data format. 
 - TradeSale - Deci-cents price of the tick sale.
 - Volume - Number of shares in the sale.
 - Exchange - Location of the sale.
 - Sale Condition - Notes on the sale. 
 - Suspicious - Boolean indicating the tick is flagged as suspicious according to QuantQuote's algorithms. This generally indicates the trade is far from other market prices and may be reversed. TradeBar data excludes suspicious ticks.

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
Exchange | Sale Condition Code | Description |
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
