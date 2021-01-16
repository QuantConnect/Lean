Use these parameters for running the polygon downloader.   You can add these when calling the executable and also can place this code into the Program.cs file right after the Main method


            // TODO: FIX --> Remove these parameters ... just here for debugging
            args = new string[11];
            args[0] = "--app=PolygonDownloader";
            args[1] = "--security-type=Equity";
            args[2] = "--market=usa";
            args[3] = "--resolution=Tick";
            args[4] = "--from-date=20201001-00:00:00";    // 00:00:00 is beginning of the day
            args[5] = "--to-date=20210101-00:00:00";
            args[6] = "--polygon-api-name=HistoricTrades";    // --api-name=HistoricQuotes   --api-name=HistoricTrades
            args[7] = "--tickers=AAPL, SIX, TWTR";
            args[8] = "--polygon-api-key=xxxxxxxxxxxxxxxxxxxxxxxx";
            args[9] = "--polygon-api-results-limit=10000";
            args[10] = "--polygon-download-threads=64";
