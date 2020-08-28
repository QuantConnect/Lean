from .____init___7 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Indicators
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect
import Python.Runtime
import datetime


class KaufmanAdaptiveMovingAverage(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Kaufman Adaptive Moving Average (KAMA).
                The Kaufman Adaptive Moving Average is calculated as explained here:
                http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:kaufman_s_adaptive_moving_average
    
    KaufmanAdaptiveMovingAverage(name: str, period: int, fastEmaPeriod: int, slowEmaPeriod: int)
    KaufmanAdaptiveMovingAverage(period: int, fastEmaPeriod: int, slowEmaPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, fastEmaPeriod: int, slowEmaPeriod: int) -> QuantConnect.Indicators.KaufmanAdaptiveMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int, fastEmaPeriod: int, slowEmaPeriod: int) -> QuantConnect.Indicators.KaufmanAdaptiveMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.KaufmanAdaptiveMovingAverage:
        pass

    IsReady: bool

    WarmUpPeriod: int



class KeltnerChannels(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator creates a moving average (middle band) with an upper band and lower band
                fixed at k average true range multiples away from the middle band.
    
    KeltnerChannels(period: int, k: Decimal, movingAverageType: MovingAverageType)
    KeltnerChannels(name: str, period: int, k: Decimal, movingAverageType: MovingAverageType)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int, k: float, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.KeltnerChannels:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, k: float, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.KeltnerChannels:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.KeltnerChannels:
        pass

    AverageTrueRange: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    IsReady: bool

    LowerBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    MiddleBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    UpperBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    WarmUpPeriod: int



class LeastSquaresMovingAverage(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    The Least Squares Moving Average (LSMA) first calculates a least squares regression line
                over the preceding time periods, and then projects it forward to the current period. In
                essence, it calculates what the value would be if the regression line continued.
                Source: https://rtmath.net/helpFinAnalysis/html/b3fab79c-f4b2-40fb-8709-fdba43cdb363.htm
    
    LeastSquaresMovingAverage(name: str, period: int)
    LeastSquaresMovingAverage(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.LeastSquaresMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.LeastSquaresMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.LeastSquaresMovingAverage:
        pass

    Intercept: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Slope: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class LinearWeightedMovingAverage(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents the traditional Weighted Moving Average indicator. The weight are linearly
                 distributed according to the number of periods in the indicator.
                
                 For example, a 4 period indicator will have a numerator of (4 * window[0]) + (3 * window[1]) + (2 * window[2]) + window[3]
                 and a denominator of 4 + 3 + 2 + 1 = 10
                
                 During the warm up period, IsReady will return false, but the LWMA will still be computed correctly because
                 the denominator will be the minimum of Samples factorial or Size factorial and
                 the computation iterates over that minimum value.
                
                 The RollingWindow of inputs is created when the indicator is created.
                 A RollingWindow of LWMAs is not saved.  That is up to the caller.
    
    LinearWeightedMovingAverage(name: str, period: int)
    LinearWeightedMovingAverage(period: int)
    """
    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.LinearWeightedMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.LinearWeightedMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.LinearWeightedMovingAverage:
        pass

    WarmUpPeriod: int



class LogReturn(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents the LogReturn indicator (LOGR)
                - log returns are useful for identifying price convergence/divergence in a given period
                - logr = log (current price / last price in period)
    
    LogReturn(name: str, period: int)
    LogReturn(period: int)
    """
    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.LogReturn:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.LogReturn:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.LogReturn:
        pass

    WarmUpPeriod: int



class MassIndex(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The Mass Index uses the high-low range to identify trend reversals based on range expansions.
                In this sense, the Mass Index is a volatility indicator that does not have a directional
                bias. Instead, the Mass Index identifies range bulges that can foreshadow a reversal of the
                current trend. Developed by Donald Dorsey.
    
    MassIndex(name: str, emaPeriod: int, sumPeriod: int)
    MassIndex(emaPeriod: int, sumPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, emaPeriod: int, sumPeriod: int) -> QuantConnect.Indicators.MassIndex:
        pass

    @typing.overload
    def __init__(self, emaPeriod: int, sumPeriod: int) -> QuantConnect.Indicators.MassIndex:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MassIndex:
        pass

    IsReady: bool

    WarmUpPeriod: int



class Maximum(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Represents an indicator capable of tracking the maximum value and how many periods ago it occurred
    
    Maximum(period: int)
    Maximum(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Maximum:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Maximum:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Maximum:
        pass

    IsReady: bool

    PeriodsSinceMaximum: int

    WarmUpPeriod: int



class MeanAbsoluteDeviation(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the n-period mean absolute deviation.
    
    MeanAbsoluteDeviation(period: int)
    MeanAbsoluteDeviation(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.MeanAbsoluteDeviation:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.MeanAbsoluteDeviation:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.MeanAbsoluteDeviation:
        pass

    IsReady: bool

    Mean: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int
