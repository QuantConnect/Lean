![alt tag](https://cdn.quantconnect.com/web/i/20180601-1615-lean-logo-small.png) Lean Data ToolBox
=========
[![Slack Chat](https://img.shields.io/badge/chat-Slack-53c82b.svg)](https://www.quantconnect.com/slack)

[Lean Home][1] | [Documentation][2] | [Download Lean][3]
----------

## Introduction ##

Lean Engine is an open-source algorithmic trading engine built for easy strategy research, backtesting and live trading. We integrate with common data providers and brokerages so you can quickly deploy algorithmic trading strategies.

The ToolBox project is a command line program which wraps over 15 tools.

## Usage

Each tool requires a different set of parameters, the only **required argument is '--app='**, which defines the target tool and is case insensitive.

Help information is available using the '--help' parameter.

Example: --app=RandomDataGenerator --tickers=SPY,AAPL --resolution=Daily --from-date=yyyyMMdd-HH:mm:ss --to-date=yyyyMMdd-HH:mm:ss

#### Available downloaders

- **'--app='**
	- GDAXDownloader or GDAXDL
	- IBDownloader or IBDL
	- BitfinexDownloader or BFXDL
- **'--from-date=yyyyMMdd-HH:mm:ss'** required
- **'--tickers=SPY,AAPL,etc'** required
- **'--resolution=Tick/Second/Minute/Hour/Daily/All'** required. **Case sensitive. Not all downloaders support all resolutions**, send empty for more information.
- **'--to-date=yyyyMMdd-HH:mm:ss'** optional. If not provided 'DateTime.UtcNow' will be used

#### Available Converters

- **'--app='**
	- AlgoSeekFuturesConverter or ASFC
		- **'--date=yyyyMMdd'** reference date.
	- AlgoSeekOptionsConverter or ASOC
		- **'--date=yyyyMMdd'** reference date.
	- KaikoDataConverter or KDC
		- **'--market='** the exchange the data represents.
		- **'--tick-type=Quote/Trade'** the tick type being processed. Case insensitive.
		- **'--source-dir='** path to the raw Kaiko data.
	- QuantQuoteConverter or QQC
		- **'--source-dir='** directory where your QuantQuote order is extracted.
		- **'--destination-dir='** directory where Lean Data is located "Lean/Data".
		- **'--resolution='** resolution of the QuantQuote data.

#### Other tools
- **'--app='**
	- CoarseUniverseGenerator or CUG

[1]: https://lean.quantconnect.com "Lean Open Source Home Page"
[2]: https://lean.quantconnect.com/docs "Lean Documentation"
[3]: https://github.com/QuantConnect/Lean/archive/master.zip
