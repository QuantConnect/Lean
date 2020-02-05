import datetime as dt
import pandas as pd
import pandas_datareader.data as web
from recieve import recieve
import urllib.request
from yahoo_finance import Share
import yfinance as yf
import csv
import zipfile
import time

def main():
	
    tickers = []
    tickers = recieve()
    timestr = time.strftime("%Y%m%d")

    for x in tickers:
        try:
	    data = yf.download(x, period = "1d")
	    z = zipfile.ZipFile('/Algo-Trader-Lean/Data/equity/usa/daily/'+x+'.zip', "a") 
	    z.write(data.to_csv(timestr+'.csv'))
	    z.close()
        except:
            continue





if __name__ == "__main__":
    main()
