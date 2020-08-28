# encoding: utf-8
# module QuantConnect.Algorithm.Framework.Alphas.Analysis.Functions calls itself Functions
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm.Framework.Alphas.Analysis
import typing

# no functions
# classes

class BinaryInsightScoreFunction(System.object, QuantConnect.Algorithm.Framework.Alphas.Analysis.IInsightScoreFunction):
    """
    Defines a scoring function that always returns 1 or 0.
                You're either right or you're wrong with this one :)
    
    BinaryInsightScoreFunction()
    """
    def Evaluate(self, context: QuantConnect.Algorithm.Framework.Alphas.Analysis.InsightAnalysisContext, scoreType: QuantConnect.Algorithm.Framework.Alphas.InsightScoreType) -> float:
        pass


