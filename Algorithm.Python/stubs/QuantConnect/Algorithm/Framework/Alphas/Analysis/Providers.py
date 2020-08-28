# encoding: utf-8
# module QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers calls itself Providers
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm.Framework.Alphas.Analysis
import QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers
import QuantConnect.Interfaces
import typing

# no functions
# classes

class AlgorithmSecurityValuesProvider(System.object, QuantConnect.Algorithm.Framework.Alphas.Analysis.ISecurityValuesProvider):
    """
    Provides an implementation of QuantConnect.Securities.ISecurityProvider that uses the QuantConnect.Securities.SecurityManager
                to get the price for the specified symbols
    
    AlgorithmSecurityValuesProvider(algorithm: IAlgorithm)
    """
    def GetAllValues(self) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection:
        pass

    def GetValues(self, symbol: QuantConnect.Symbol) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.SecurityValues:
        pass

    def __init__(self, algorithm: QuantConnect.Interfaces.IAlgorithm) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.Providers.AlgorithmSecurityValuesProvider:
        pass


class DefaultInsightScoreFunctionProvider(System.object, QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunctionProvider):
    """
    Default implementation of QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunctionProvider always returns the QuantConnect.Algorithm.Framework.Alphas.Analysis.Functions.BinaryInsightScoreFunction
    
    DefaultInsightScoreFunctionProvider()
    """
    def GetScoreFunction(self, insightType: QuantConnect.Algorithm.Framework.Alphas.InsightType, scoreType: QuantConnect.Algorithm.Framework.Alphas.InsightScoreType) -> QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunction:
        pass


