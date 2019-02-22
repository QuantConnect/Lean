''' Contingent Claim Analysis is put forth by Robert Merton, recepient of the Noble Prize in Economics in 1997 for his work in contributing to
    Black-Scholes option pricing theory, which says that the equity market value of stockholders’ equity is given by the Black-Scholes solution
    for a European call option. This equation takes into account Debt, which in CCA is the equivalent to a strike price in the BS solution. The probability
    of default on corporate debt can be calculated as the N(-d2) term, where d2 is a function of the interest rate on debt(µ), face value of the debt (B), value of the firm's assets (V),
    standard deviation of the change in a firm's asset value (σ), the dividend and interest payouts due (D), and the time to maturity of the firm's debt(τ). N(*) is the cumulative
    distribution function of a standard normal distribution, and calculating N(-d2) gives us the probability of the firm's assets being worth less
    than the debt of the company at the time that the debt reaches maturity -- that is, the firm doesn't have enough in assets to pay off its debt and defaults.

    We use a Fine/Coarse Universe Selection model to select small cap stocks, who we postulate are more likely to default
    on debt in general than blue-chip companies, and extract Fundamental data to plug into the CCA formula.
    This Alpha emits insights based on whether or not a company is likely to default given its probability of default vs a default probability threshold that we set arbitrarily.


    Prob. default (on principal B at maturity T) = Prob(VT < B) = 1 - N(d2) = N(-d2) where -d2(µ) = -{ln(V/B) + [(µ - D) - ½σ2]τ}/ σ √τ.
        N(d) = (univariate) cumulative standard normal distribution function (from -inf to d)
        B = face value (principal) of the debt
        D = dividend + interest payout
        V = value of firm’s assets
        σ (sigma) = standard deviation of firm value changes (returns in V)
        τ (tau)  = time to debt’s maturity
        µ (mu) = interest rate
        
        
    
    This alpha is part of the Benchmark Alpha Series created by QuantConnect which are open
    sourced so the community and client funds can see an example of an alpha.
'''



import scipy.stats as sp
import pandas as pd
import numpy as np
from datetime import datetime, timedelta

from Risk.NullRiskManagementModel import NullRiskManagementModel
from Portfolio.EqualWeightingPortfolioConstructionModel import EqualWeightingPortfolioConstructionModel
from Execution.ImmediateExecutionModel import ImmediateExecutionModel


class ContingentClaimAnalysisDefaultPredictionAlgorithm(QCAlgorithmFramework):

    def Initialize(self):

        ## Set requested data resolution and variables to help with Universe Selection control
        self.UniverseSettings.Resolution = Resolution.Daily
        self.month = None
        self.symbols = None
        
        ## Declare single variable to be passed in multiple places -- prevents issue with conflicting start dates declared in different places
        self.SetStartDate(2018,1,1)
        self.SetCash(100000)
        
        ## SPDR Small Cap ETF is a better benchmark than the default SP500
        self.SetBenchmark('IJR')

        ## Set Universe Selection Model
        self.SetUniverseSelection(FineFundamentalUniverseSelectionModel(self.CoarseSelectionFunction, self.FineSelectionFunction, None, None))
        
        ## Set CCA Alpha Model
        self.SetAlpha(ContingentClaimsAnalysisAlphaModel())
        
        ## Set Portfolio Construction Model
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        ## Set Execution Model
        self.SetExecution(ImmediateExecutionModel())
        
        ## Set Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())


    def CoarseSelectionFunction(self, coarse):
        ## Boolean controls so that our symbol universe is only updated once per month
        if self.Time.month == self.month:
            return self.symbols
        else:
            self.month = self.Time.month

            ## Sort by dollar volume, lowest to highest
            sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)

            ## Filter for assets with fundamental data
            filtered = [ x.Symbol for x in sortedByDollarVolume ]
        
            ## Return smallest 750 -- idea is that smaller companies are most likely to go bankrupt than blue-chip companies
            self.symbols = filtered[:750]

            return self.symbols
    
    
    def FineSelectionFunction(self, fine):
        ## Boolean controls so that our symbol universe is updated only at the beginning of each month
        if self.Time.month == self.month:
            return self.symbols
        else:
            self.month = self.Time.month
            ## Select symbols with data necessary for our pricing model
            fineFilter = sorted(fine, key=lambda x: (x.FinancialStatements.BalanceSheet.TotalAssets.OneMonth > 0) and
                                                    (x.FinancialStatements.BalanceSheet.TotalAssets.ThreeMonths > 0) and
                                                    (x.FinancialStatements.BalanceSheet.TotalAssets.SixMonths > 0) and
                                                    (x.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths > 0) and
                                                    (x.FinancialStatements.BalanceSheet.CurrentLiabilities.TwelveMonths > 0) and
                                                    (x.FinancialStatements.BalanceSheet.InterestPayable.TwelveMonths > 0) and
                                                    (x.OperationRatios.TotalAssetsGrowth.OneYear > 0) and
                                                    (x.FinancialStatements.IncomeStatement.GrossDividendPayment.TwelveMonths > 0) and
                                                    (x.OperationRatios.ROA.OneYear > 0), reverse=True)

            self.symbols = [ x.Symbol for x in fineFilter ]

            return self.symbols


class ContingentClaimsAnalysisAlphaModel:
    
    def __init__(self, *args, **kwargs):
        self.symbolDataBySymbol = {}
        self.month = None
        self.default_threshold = kwargs['default_threshold'] if 'default_threshold' in kwargs else 0.25
        self.expiry = None
        self.epsilon = kwargs['epsilon'] if 'epsilon' in kwargs else 0.00001     ## This serves as a check to filter out symbols with a default probability of, e.g., 2.89e-20
        

    ## This model applies options pricing theory, Black-Scholes specifically, to fundamental data
    ## to give the probability of a default
    def CCADefaultProbability(self, algorithm, symbolData):
        
        B = symbolData.DebtValue
        V = symbolData.AssetValue
        D = symbolData.DividendAndInterest
        sigma = symbolData.ValuationChangeVolatility
        tau = symbolData.TimeToMaturity
        mu = symbolData.ROA
        
        d2 = ((np.log(V) - np.log(B)) + ((mu - D) - 0.5*sigma**2.0)*tau)/ (sigma*np.sqrt(tau))
        probability_of_default = sp.norm.cdf(-d2)
            
        return probability_of_default


    def Update(self, algorithm, data):
        
        '''Updates this alpha model with the latest data from the algorithm.
        This is called each time the algorithm receives data for subscribed securities
        Args:
            algorithm: The algorithm instance
            data: The new data available
        Returns:
            The new insights generated'''
            
        ## Build a list to hold our insights
        insights = []
        
        for symbol, symbolData in self.symbolDataBySymbol.items():
            pod = self.CCADefaultProbability(algorithm, symbolData)
            algorithm.Log('P.O.D. for ' + str(symbol) + ': ' + str(pod))
            ## If Prob. of Default is greater than our set threshold, then emit an insight indicating that this asset is trending downward
            if (pod >= self.default_threshold) and (pod != 1.0):
                insights.append(Insight(symbol, timedelta(days = 30), InsightType.Price, InsightDirection.Down, pod, None))

        return insights
    
    def OnSecuritiesChanged(self, algorithm, changes): 
        
        for removed in changes.RemovedSecurities:
            algorithm.Log('Removed: ' + str(removed.Symbol))
            symbolData = self.symbolDataBySymbol.pop(removed.Symbol, None)
            
        # initialize data for added securities
        symbols = [ x.Symbol for x in changes.AddedSecurities ]
        
        for symbol in symbols:
            if symbol not in self.symbolDataBySymbol:
                
                ## Retrieve fundamentals data necessary for our CCA valuation
                if (algorithm.Securities[symbol].Fundamentals.FinancialStatements is not None) and (algorithm.Securities[symbol].Fundamentals.OperationRatios is not None):
                    x = algorithm.Securities[symbol].Fundamentals
                    fundamentals = [x.FinancialStatements.BalanceSheet.TotalAssets.OneMonth,
                                    x.FinancialStatements.BalanceSheet.TotalAssets.ThreeMonths,
                                    x.FinancialStatements.BalanceSheet.TotalAssets.SixMonths,
                                    x.FinancialStatements.BalanceSheet.TotalAssets.TwelveMonths,
                                    x.FinancialStatements.BalanceSheet.CurrentLiabilities.TwelveMonths,
                                    x.FinancialStatements.BalanceSheet.InterestPayable.TwelveMonths,
                                    x.OperationRatios.TotalAssetsGrowth.OneYear,
                                    x.FinancialStatements.IncomeStatement.GrossDividendPayment.TwelveMonths,
                                    x.OperationRatios.ROA.OneYear]
                    
                    symbolData = SymbolData(symbol)
                    symbolData.PopulateData(algorithm, fundamentals)
                    self.symbolDataBySymbol[symbol] = symbolData

class SymbolData:
    
    def __init__(self, symbol):
        self.Symbol = symbol
        self.V = 0
        self.B = 0
        self.sigma = 0
        self.D = 0
        self.mu = 0
        self.tau = 360                      ## Days
        
    def PopulateData(self, algorithm, fundamentals):
        self.V = fundamentals[3]
        self.B = fundamentals[4]
        self.D = fundamentals[5] + fundamentals[7]
        self.mu = fundamentals[8]
        series = pd.Series(fundamentals[0:4])
        sigma = series.iloc[series.nonzero()[0]]
        self.sigma = np.std(sigma.pct_change()[1:len(sigma)])
        
    @property
    def AssetValue(self):
        return float(self.V)
    @property
    def DebtValue(self):
        return float(self.B)
    @property
    def ValuationChangeVolatility(self):
        return float(self.sigma)
    @property
    def DividendAndInterest(self):
        return float(self.D)
    @property
    def ROA(self):
        return float(self.mu)
    @property
    def TimeToMaturity(self):
        return float(self.tau)