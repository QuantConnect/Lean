![alt tag](https://raw.githubusercontent.com/QuantConnect/Lean/master/Documentation/logo.white.small.png) 
## LEAN Data Formats / Options 

QuantConnect hosts options data provided by [AlgoSeek](https://algoseek.com/). The data contains *quotes*, *trades*, and *open interest* data. You can explore options data on our website at https://www.quantconnect.com/data/tree/option/

The data are stored as compressed ZIP files, each containing multiple CSV entries, varying on the option style, e.g. call/put, strike price, and expiration date. 

Options data can be used with the following Resolutions:

* Minute

The markets we currently support are:

* USA

`tickType` in this documentation can refer to one of the following:

* trade
* quote
* openinterest 

### Minute File Format

Minute files are located in the option / market / resolution / symbol folder. 

The zip files have the filename format: `YYYYMMDD_tickType_optionType.zip`. The CSV file contained within has the filename format: `YYYYMMDD_symbol_resolution_tickType_optionType_optionStyle_decicentStrikePrice_symbolExpirationDate(YYYYMMDD).csv`

Minute trade schema and example data is as follows:

| Time | Open | High | Low | Close | Volume |
| ---- | ---- | ---- | --- | ----- | ------ |
| 63271000 | 120800 | 125600 | 120800 | 125000 | 404 |

* Time - Milliseconds since midnight
* Open - Opening price as deci-cents
* High - High price as deci-cents
* Low - Low price as deci-cents
* Close - Closing price as deci-cents
* Volume - Total contracts traded

Minute quote schema and example data is as follows:

| Time | Bid Open | Bid High | Bid Low | Bid Close | Last Bid Size | Ask Open | Ask High | Ask Low | Ask Close | Last Ask Size |
| ---- | -------- | -------- | ------- | --------- | ------------- | -------- | -------- | ------- | --------- | ------------- |
| 10920000 | 120800 | 125600 | 120800 | 125000 | 10 | 120900 | 126800 | 120900 | 137000 | 100 |

* Time - Milliseconds since midnight
* Bid Open - Opening price for the best bid as deci-cents
* Bid High - Highest recorded bid price as deci-cents
* Bid Low - Lowest recorded bid price as deci-cents
* Bid Close - Closing price for the best bid as deci-cents
* Last Bid Size - Size of best bid at close
* Ask Open - Opening price for the best ask as deci-cents
* Ask High - Highest recorded ask price as deci-cents
* Ask Low - Lowest recorded ask price as deci-cents
* Ask Close - Closing price for the best ask as deci-cents
* Last Ask Size - Size of best ask at close

Divide prices by 10,000 to convert deci-cents to dollars

Minute open interest schema and example data is as follows:

| Time | Open Interest |
| ---- | ------------- |
| 50280000 | 102 |

* Time - Milliseconds since midnight
* Open Interest - outstanding contracts
