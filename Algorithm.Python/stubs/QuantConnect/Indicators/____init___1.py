from .____init___2 import *
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


class ArmsIndex(QuantConnect.Indicators.TradeBarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[TradeBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[TradeBar]]):
    """
    The Arms Index, also called the Short-Term Trading Index (TRIN) 
                is a technical analysis indicator that compares the number of advancing 
                and declining stocks (AD Ratio) to advancing and declining volume (AD volume).
    
    ArmsIndex(name: str)
    """
    def AddStock(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def RemoveStock(self, symbol: QuantConnect.Symbol) -> None:
        pass

    def Reset(self) -> None:
        pass

    def __init__(self, name: str) -> QuantConnect.Indicators.ArmsIndex:
        pass

    ADRatio: QuantConnect.Indicators.AdvanceDeclineRatio

    ADVRatio: QuantConnect.Indicators.AdvanceDeclineVolumeRatio

    IsReady: bool

    WarmUpPeriod: int



class ArnaudLegouxMovingAverage(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    Smooth and high sensitive moving Average. This moving average reduce lag of the information
                but still being smooth to reduce noises.
                Is a weighted moving average, which weights have a Normal shape;
                the parameters Sigma and Offset affect the kurtosis and skewness of the weights respectively.
                Source: http://www.arnaudlegoux.com/index.html
    
    ArnaudLegouxMovingAverage(name: str, period: int, sigma: int, offset: Decimal)
    ArnaudLegouxMovingAverage(name: str, period: int)
    ArnaudLegouxMovingAverage(period: int, sigma: int, offset: Decimal)
    ArnaudLegouxMovingAverage(period: int)
    """
    @typing.overload
    def __init__(self, name: str, period: int, sigma: int, offset: float) -> QuantConnect.Indicators.ArnaudLegouxMovingAverage:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.ArnaudLegouxMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int, sigma: int, offset: float) -> QuantConnect.Indicators.ArnaudLegouxMovingAverage:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.ArnaudLegouxMovingAverage:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.ArnaudLegouxMovingAverage:
        pass

    WarmUpPeriod: int



class BarIndicator(QuantConnect.Indicators.IndicatorBase[IBaseDataBar], System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    The BarIndicator is an indicator that accepts IBaseDataBar data as its input.
                
                This type is more of a shim/typedef to reduce the need to refer to things as IndicatorBase<IBaseDataBar>
    """
    def __init__(self, *args): #cannot find CLR constructor
        pass


class AroonOscillator(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    The Aroon Oscillator is the difference between AroonUp and AroonDown. The value of this
                indicator fluctuates between -100 and +100. An upward trend bias is present when the oscillator
                is positive, and a negative trend bias is present when the oscillator is negative. AroonUp/Down
                values over 75 identify strong trends in their respective direction.
    
    AroonOscillator(upPeriod: int, downPeriod: int)
    AroonOscillator(name: str, upPeriod: int, downPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, upPeriod: int, downPeriod: int) -> QuantConnect.Indicators.AroonOscillator:
        pass

    @typing.overload
    def __init__(self, name: str, upPeriod: int, downPeriod: int) -> QuantConnect.Indicators.AroonOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AroonOscillator:
        pass

    AroonDown: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    AroonUp: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    WarmUpPeriod: int



class AverageDirectionalIndex(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes Average Directional Index which measures trend strength without regard to trend direction.
                Firstly, it calculates the Directional Movement and the True Range value, and then the values are accumulated and smoothed
                using a custom smoothing method proposed by Wilder. For an n period smoothing, 1/n of each period's value is added to the total period.
                From these accumulated values we are therefore able to derived the 'Positive Directional Index' (+DI) and 'Negative Directional Index' (-DI)
                which is used to calculate the Average Directional Index.
                Computation source:
                https://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:average_directional_index_adx
    
    AverageDirectionalIndex(period: int)
    AverageDirectionalIndex(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.AverageDirectionalIndex:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.AverageDirectionalIndex:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AverageDirectionalIndex:
        pass

    IsReady: bool

    NegativeDirectionalIndex: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    PositiveDirectionalIndex: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class AverageDirectionalMovementIndexRating(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Average Directional Movement Index Rating (ADXR). 
                The Average Directional Movement Index Rating is calculated with the following formula:
                ADXR[i] = (ADX[i] + ADX[i - period + 1]) / 2
    
    AverageDirectionalMovementIndexRating(name: str, period: int)
    AverageDirectionalMovementIndexRating(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.AverageDirectionalMovementIndexRating:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.AverageDirectionalMovementIndexRating:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AverageDirectionalMovementIndexRating:
        pass

    ADX: QuantConnect.Indicators.AverageDirectionalIndex

    IsReady: bool

    WarmUpPeriod: int



class AverageTrueRange(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    The AverageTrueRange indicator is a measure of volatility introduced by Welles Wilder in his
                 book: New Concepts in Technical Trading Systems. This indicator computes the TrueRange and then
                 smoothes the TrueRange over a given period.
                
                 TrueRange is defined as the maximum of the following:
                   High - Low
                   ABS(High - PreviousClose)
                   ABS(Low - PreviousClose)
    
    AverageTrueRange(name: str, period: int, movingAverageType: MovingAverageType)
    AverageTrueRange(period: int, movingAverageType: MovingAverageType)
    """
    @staticmethod
    def ComputeTrueRange(previous: QuantConnect.Data.Market.IBaseDataBar, current: QuantConnect.Data.Market.IBaseDataBar) -> float:
        pass

    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.AverageTrueRange:
        pass

    @typing.overload
    def __init__(self, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.AverageTrueRange:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.AverageTrueRange:
        pass

    IsReady: bool

    TrueRange: QuantConnect.Indicators.IndicatorBase[QuantConnect.Data.Market.IBaseDataBar]

    WarmUpPeriod: int



class BalanceOfPower(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    This indicator computes the Balance Of Power (BOP).
                The Balance Of Power is calculated with the following formula:
                BOP = (Close - Open) / (High - Low)
    
    BalanceOfPower()
    BalanceOfPower(name: str)
    """
    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.BalanceOfPower:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.BalanceOfPower:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.BalanceOfPower:
        pass

    IsReady: bool

    WarmUpPeriod: int
