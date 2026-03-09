![alt tag](https://raw.githubusercontent.com/QuantConnect/Lean/master/Documentation/logo.white.small.png) 
## LEAN Data Formats / Futures

QuantConnect hosts futures data provided by [AlgoSeek](https://algoseek.com/). The data contains *quotes*, *trades*, and *open interest* data. You can explore futures data on our website at https://www.quantconnect.com/data/tree/future

The data are stored as compressed ZIP files, each capable of containing a single, or multiple CSV files, depending on the resolution requested.

Futures data can be used with the following Resolutions:

* Tick
* Second
* Minute

The markets we currently support are:

* CBOT
* CME 
* NYMEX
* COMEX
* CBOE
* ICE

`tickType` in this documentation can refer to one of the following:

* trade
* quote
* openinterest 

### Minute and Second File Format

Second/Minute files are located in the future / market / resolution / symbol folder. The zip file contains multiple csv entries, varying by the symbol's expiration date.

The zip files have the filename format: `YYYYMMDD_tickType.zip`. The CSV file contained within has the filename: `YYYYMMDD_symbol_resolution_tickType_symbolExpirationDate.csv`

Second/Minute trade schema and example data is as follows:

| Time | Open | High | Low | Close | Volume |
| ---- | ---- | ---- | --- | ----- | ------ |
| 63271000 | 85.22 | 85.24 | 85.21 | 85.24 | 126 |

* Time - Milliseconds since midnight
* Open - Opening price
* High - High price
* Low - Low price
* Close - Closing price
* Volume - Total contracts traded 

Second/Minute quote schema and example data is as follows:

| Time | Bid Open | Bid High | Bid Low | Bid Close | Last Bid Size | Ask Open | Ask High | Ask Low | Ask Close | Last Ask Size |
| ---- | -------- | -------- | ------- | --------- | ------------- | -------- | -------- | ------- | --------- | ------------- |
| 10920000 | 1666.5 | 1666.5 | 1666.25 | 1666.25 | 47 | 1666.75 |1666.75 | 1666.5 | 1666.5 | 37 |

* Time - Milliseconds since midnight
* Bid Open - Opening price for the best bid
* Bid High - Highest recorded bid price
* Bid Low - Lowest recorded bid price
* Bid Close - Closing price for the best bid
* Last Bid Size - Size of best bid at close
* Ask Open - Opening price for the best ask
* Ask High - Highest recorded ask price
* Ask Low - Lowest recorded ask price
* Ask Close - Closing price for the best ask
* Last Ask Size - Size of best ask at close

Second/Minute open interest schema and example data is as follows:

| Time | Open Interest |
| ---- | ------------- |
| 42660000 | 2693575 |

* Time - Milliseconds since midnight
* Open Interest - outstanding contracts

### Hour and Daily File Format
Hour/Daily files are located in the future / market / resolution folder. The zip file contains only a single entry.

The zip files have the filename format: `symbol_tickType.zip`. The CSV file contained within has the filename format: `symbol_tickType_symbolExpirationDate.csv`

Hour/Daily trades schema and example data is as follows:

| Time | Open | High | Low | Close | Volume |
| ---- | ---- | ---- | --- | ----- | ------ |
| 20160601 00:00 | 43.20 | 43.50 | 43.10 | 43.45 | 513 |

* Time - Formatted as `YYYYMMDD HH:mm`
* Open - Opening price
* High - High price
* Low - Low price
* Close - Closing price
* Volume - Total contracts traded 

Hour/Daily quote schema and example data is as follows:

| Time | Bid Open | Bid High | Bid Low | Bid Close | Last Bid Size | Ask Open | Ask High | Ask Low | Ask Close | Last Ask Size |
| ---- | -------- | -------- | ------- | --------- | ------------- | -------- | -------- | ------- | --------- | ------------- |
| 20170719 00:00 | 583.20 | 583.40 | 583.10 | 583.40 | 2932 | 583.21 | 583.50 | 583.11 | 583.44 | 392 |

Hour/Daily open interest schema and example data is as follows:

| Time | Open Interest |
| ---- | ------------- |
| 20190203 00:00 | 3902 |

* Time - Formatted as `YYYYMMDD HH:mm`
* Open Interest - outstanding contracts

### Tick File Format
Tick data is stored in the future / market / tick / symbol folder. The zip file contains multiple csv entries, varying by the symbol's expiration date.

The zip files have the filename format: `YYYYMMDD_tickType.zip`. The CSV files contained within have the filename format: `YYYYMMDD_symbol_tick_tickType_symbolExpirationDate.csv`

Tick trades schema and example data is as follows:

| Time | Last Price | Quantity | Exchange | Sale Condition | Suspicious |
| ---- | ---------- | -------- | -------- | -------------- | ---------- |
| 939243 | 402.01 | 203 | usa | null | 0 |

* Time - Milliseconds since midnight
* Last Price - Last traded price
* Quantity - Amount traded
* Exchange - Where transaction took place
* Sale Condition - always null, not used
* Suspicious - Not used, will always be "0"
