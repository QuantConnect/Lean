# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *

### <summary>
### Hourly regression algorithm trading ADAUSDT binance futures long and short asserting the behavior
### </summary>
class BasicTemplateCryptoFutureHourlyAlgorithm(QCAlgorithm):
    # <summary>
    # Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
    # </summary>

    def Initialize(self):
        self.SetStartDate(2022, 12, 13)
        self.SetEndDate(2022, 12, 13)

        self.SetTimeZone(TimeZones.Utc)

        try:
            self.SetBrokerageModel(BrokerageName.BinanceCoinFutures, AccountType.Cash)
        except:
            # expected, we don't allow cash account type
            pass

        self.SetBrokerageModel(BrokerageName.BinanceCoinFutures, AccountType.Margin)

        self.adaUsdt = self.AddCryptoFuture("ADAUSDT", Resolution.Hour)

        self.fast = self.EMA(self.adaUsdt.Symbol, 3, Resolution.Hour)
        self.slow = self.EMA(self.adaUsdt.Symbol, 6, Resolution.Hour)

        self.interestPerSymbol = {self.adaUsdt.Symbol: 0}

        # Default USD cash, set 1M but it wont be used
        self.SetCash(1000000)

        # the amount of USDT we need to hold to trade 'ADAUSDT'
        self.adaUsdt.QuoteCurrency.SetAmount(200)

    # <summary>
    # OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    # </summary>
    # <param name="data">Slice object keyed by symbol containing the stock data</param>
    def OnData(self, slice):
        interestRates = slice.Get(MarginInterestRate);
        for interestRate in interestRates:
            self.interestPerSymbol[interestRate.Key] += 1
            self.cachedInterestRate = self.Securities[interestRate.Key].Cache.GetData[MarginInterestRate]()
            if self.cachedInterestRate != interestRate.Value:
                raise Exception(f"Unexpected cached margin interest rate for {interestRate.Key}!")
            
        if self.fast > self.slow:
            if self.Portfolio.Invested == False and self.Transactions.OrdersCount == 0:
                self.ticket = self.Buy(self.adaUsdt.Symbol, 100000)
                if self.ticket.Status != OrderStatus.Invalid:
                    raise Exception(f"Unexpected valid order {self.ticket}, should fail due to margin not sufficient")
                
                self.Buy(self.adaUsdt.Symbol, 1000)

                self.marginUsed = self.Portfolio.TotalMarginUsed

                self.adaUsdtHoldings = self.adaUsdt.Holdings

                # USDT/BUSD futures value is based on it's price
                self.holdingsValueUsdt = self.adaUsdt.Price * self.adaUsdt.SymbolProperties.ContractMultiplier * 1000

                if abs(self.adaUsdtHoldings.TotalSaleVolume - self.holdingsValueUsdt) > 1:
                    raise Exception(f"Unexpected TotalSaleVolume {self.adaUsdtHoldings.TotalSaleVolume}")
                
                if abs(self.adaUsdtHoldings.AbsoluteHoldingsCost - self.holdingsValueUsdt) > 1:
                    raise Exception(f"Unexpected holdings cost {self.adaUsdtHoldings.HoldingsCost}")
                
                if (abs(self.adaUsdtHoldings.AbsoluteHoldingsCost * 0.05 - self.marginUsed) > 1) or (BuyingPowerModelExtensions.GetMaintenanceMargin(self.adaUsdt.BuyingPowerModel, self.adaUsdt) != self.marginUsed):
                    raise Exception(f"Unexpected margin used {self.marginUsed}")
                
                # position just opened should be just spread here
                self.profit = self.Portfolio.TotalUnrealizedProfit
                
                if (5 - abs(self.profit)) < 0:
                    raise Exception(f"Unexpected TotalUnrealizedProfit {self.Portfolio.TotalUnrealizedProfit}")

                if (self.Portfolio.TotalProfit != 0):
                    raise Exception(f"Unexpected TotalProfit {self.Portfolio.TotalProfit}")
                
        else:
            # let's revert our position and double
            if self.Time.hour > 10 and self.Transactions.OrdersCount == 2:
                self.Sell(self.adaUsdt.Symbol, 3000)

                self.adaUsdtHoldings = self.adaUsdt.Holdings

                # USDT/BUSD futures value is based on it's price
                self.holdingsValueUsdt = self.adaUsdt.Price * self.adaUsdt.SymbolProperties.ContractMultiplier * 2000

                if abs(self.adaUsdtHoldings.AbsoluteHoldingsCost - self.holdingsValueUsdt) > 1:
                    raise Exception(f"Unexpected holdings cost {self.adaUsdtHoldings.HoldingsCost}")

                # position just opened should be just spread here
                self.profit = self.Portfolio.TotalUnrealizedProfit
                if (5 - abs(self.profit)) < 0:
                    raise Exception(f"Unexpected TotalUnrealizedProfit {self.Portfolio.TotalUnrealizedProfit}")
                                        
                # we barely did any difference on the previous trade
                if (5 - abs(self.Portfolio.TotalProfit)) < 0:
                    raise Exception(f"Unexpected TotalProfit {self.Portfolio.TotalProfit}")
                
            if self.Time.hour >= 22 and self.Transactions.OrdersCount == 3:
                self.Liquidate()
                                            
    def OnEndOfAlgorithm(self):
        if self.interestPerSymbol[self.adaUsdt.Symbol] != 1:
                raise Exception(f"Unexpected interest rate count {self.interestPerSymbol[self.adaUsdt.Symbol]}")

    def OnOrderEvent(self, orderEvent):
        self.Debug("{0} {1}".format(self.Time, orderEvent))
