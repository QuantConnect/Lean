![alt tag](https://raw.githubusercontent.com/QuantConnect/Lean/master/Documentation/logo.white.small.png) 
## LEAN Data Formats / Cryptocurrency (crypto)

QuantConnect hosts crypto data provided by [Kaiko](https://www.kaiko.com).
The data contains both *Trade* and *Quote* data. Using the ToolBox applications `GDAXDownloader` and `BitfinexDownloader`, you can obtain historical *trade* data for free, but not *quote* data with this method. 
You can also download crypto data (trades and quotes starting with Tick and ending with Minute resolution) for a fee on our website. You can explore the data and purchase it at https://www.quantconnect.com/data/tree/crypto

CSV files are stored in compressed zip files, each containing a single CSV file.

Crypto data supports the following Resolutions:

* Tick
* Second
* Minute
* Hour
* Daily

The markets we currently support are: 

* GDAX/Coinbase Pro
* Bitfinex (Beta)

`tickType` in this documentation can refer to one of the following:

* trade
* quote

All times are in UTC unless noted otherwise.

### Minute and Second File Format
Second/Minute files are located in the crypto / market / resolution / symbol folder. 

The zip files have the filename: `YYYYMMDD_tickType.zip`. The CSV file contained within has the filename: `YYYYMMDD_symbol_resolution_tickType.csv`

Second/Minute trade format and example data is as follows:

| Time | Open | High | Low | Close | Volume |
| ---- | ---- | ---- | --- | ----- | ------ |
| 92000 | 132.01 | 132.05 | 131.95 | 132.03 | 49320 |

* Time - Milliseconds since midnight
* Open - Opening price
* High - High price
* Low - Low price
* Close - Closing price
* Volume - Total quantity trade 

Second/Minute quote format and example data is as follows:

| Time | Bid Open | Bid High | Bid Low | Bid Close | Last Bid Size | Ask Open | Ask High | Ask Low | Ask Close | Last Ask Size |
| ---- | -------- | -------- | ------- | --------- | ------------- | -------- | -------- | ------- | --------- | ------------- |
| 92000 | 132.01 | 132.05 | 132.00 | 132.03 | 24932.5 | 132.02 | 132.07 | 132.01 | 132.04 | 1200 |

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

### Hour and Daily File Format
Hour/Daily files are located in the crypto / market / resolution folder. 

The zip files have the filename: `symbol_tickType.zip`. The CSV file contained within has the filename: `symbol.csv`

Hour/Daily trade format and example data is as follows:

| Time | Open | High | Low | Close | Volume |
| ---- | ---- | ---- | --- | ----- | ------ |
| 20180101 08:00 | 40.10 | 45.99 | 40.05 | 45.50 | 209342 |

* Time - Formatted as `YYYYMMDD HH:mm`
* Open - Opening price
* High - High price
* Low - Low price
* Close - Closing price
* Volume - Total quantity traded

Hour/Daily quote format and example data is as follows:

| Time | Bid Open | Bid High | Bid Low | Bid Close | Last Bid Size | Ask Open | Ask High | Ask Low | Ask Close | Last Ask Size |
| ---- | -------- | -------- | ------- | --------- | ------------- | -------- | -------- | ------- | --------- | ------------- |
| 20190224 00:00 | 10.10 | 10.12 | 10.10 | 10.11 | 209324.91 | 10.11 | 10.13 | 10.11 | 10.12 | 290253 |

* Time - Formatted as `YYYYMMDD HH:mm`
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

### Tick File Format
Ticks files are located in the data / crypto / market / tick folder. 

The zip files have the filename format: `YYYYMMDD_tickType.zip`. The CSV file contained within has the filename format: `YYYYMMDD_symbol_resolution_tickType.csv`

Tick trade format and example data is as follows:

| Time | Last Price | Quantity |
| ---- | ---------- | -------- |
| 86400 | 232.40 | 93.1 |

* Time - Milliseconds passed since midnight
* Last Price - Most recent trade price
* Quantity - Amount of asset purchased or sold

Tick quote format and example data is as follows:

| Time | Bid Price | Bid Size | Ask Price | Ask Size |
| ---- | --------- | -------- | --------- | -------- |
| 86400 | 232.40 | 20392.0 | 232.42 | 8059.5 |

* Time - Milliseconds passed since midnight
* Bid Price - Best bid price
* Bid Size - Best bid price's size/quantity
* Ask Price - Best ask price
* Ask Size - Best ask price's size/quantity
