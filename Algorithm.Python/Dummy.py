from AlgorithmImports import *

class ExampleCustomData(PythonData):

    def GetSource(self, config, date, isLive):
        source = os.path.join(Globals.DataFolder, "path_to_my_csv_data.csv")
        if not os.path.isfile(source):
            raise FileNotFoundError(source + " not found")
        return SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile)

    def Reader(self, config, line, date, isLive):
        data = line.split(',')
        obj_data = ExampleCustomData()
        obj_data.Symbol = config.Symbol
        obj_data.Time = datetime.strptime(data[0], '%Y-%m-%d %H:%M:%S') + timedelta(hours=20)
        obj_data.Value = float(data[4])
        obj_data["Open"] = float(data[1])
        obj_data["High"] = float(data[2])
        obj_data["Low"] = float(data[3])
        obj_data["Close"] = float(data[4])
        return obj_data

class Dummy(QCAlgorithm):
    def Initialize(self):

        self.SetStartDate(2017, 8, 18)  # Set Start Date
        self.SetEndDate(2017, 8, 21)  # Set End Date
        self.SetCash(100000)  # Set Strategy Cash

        self.SetBrokerageModel(BrokerageName.Default, AccountType.Margin)

        self.AddEquity("SPY", Resolution.Hour)
        # Load benchmark data
        self.customSymbol = self.AddData(ExampleCustomData, "ExampleCustomData", Resolution.Hour).Symbol
        self.SetBenchmark(self.customSymbol)

    def OnData(self, data):
        if not self.Portfolio.Invested:
            self.SetHoldings("SPY", 1)
