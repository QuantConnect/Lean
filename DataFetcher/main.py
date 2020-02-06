#!/usr/bin/env python
import datetime as dt
import urllib.request
import pandas as pd
import yfinance as yf
import zipfile
import time
import quandl
from recieve import recieve


def main():
	tickers = []
	tickers = recieve()
	timestr = time.strftime("%Y%m%d")

	print(tickers)

	# data = yf.download("SPY AAPL", start="2017-01-01", end="2017-04-30", period="1d")
	#
	# print(data)


    # for x in tickers:
    #     try:
	# 	    data = yf.download(x, period = "1d")
	# 	    z = zipfile.ZipFile('/Algo-Trader-Lean/Data/equity/usa/daily/'+x+'.zip', "a")
	# 	    z.write(data.to_csv(timestr+'.csv'))
	# 	    z.close()
    #     except:
    #         continue


if __name__ == "__main__":
    main()



# import datetime as dt
# import pandas as pd
# import pandas_datareader.data as web
# import urllib.request
#
# def main():
#     url='https://s3.amazonaws.com/rawstore.datahub.io/b8d80d0c578007c9e0d199a6cd2625f1.csv'
#
#     with urllib.request.urlopen(url) as testfile, open('dataset.csv', 'w') as f:
#         f.write(testfile.read().decode())
#
#
#     start = dt.datetime(2018, 1, 1)
#     end = dt.datetime(2018, 1, 2)
#
#     marketData = []
#
#     df = pd.read_csv('dataset.csv')
#     nasdaqSymbols = list(df['Symbol'])
#
#     for symbolCounter, symbol in enumerate(nasdaqSymbols):
#         try:
#             stock = web.DataReader(nasdaqSymbols[symbolCounter], 'yahoo', start, end)
#             marketData.append(stock)
#             print(marketData)
#         except:
#             continue
#
# if __name__ == "__main__":
#     main()
