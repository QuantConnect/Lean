from .____init___3 import *
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



class BollingerBands(QuantConnect.Indicators.Indicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator creates a moving average (middle band) with an upper band and lower band
                fixed at k standard deviations above and below the moving average.
    
    BollingerBands(period: int, k: Decimal, movingAverageType: MovingAverageType)
    BollingerBands(name: str, period: int, k: Decimal, movingAverageType: MovingAverageType)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int, k: float, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.BollingerBands:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, k: float, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.BollingerBands:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.BollingerBands:
        pass

    BandWidth: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    IsReady: bool

    LowerBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    MiddleBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    MovingAverageType: QuantConnect.Indicators.MovingAverageType

    PercentB: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    Price: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    StandardDeviation: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    UpperBand: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class ChandeMomentumOscillator(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    This indicator computes the Chande Momentum Oscillator (CMO).
                CMO calculation is mostly identical to RSI.
                The only difference is in the last step of calculation:
                RSI = gain / (gain+loss)
                CMO = (gain-loss) / (gain+loss)
    
    ChandeMomentumOscillator(period: int)
    ChandeMomentumOscillator(name: str, period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.ChandeMomentumOscillator:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.ChandeMomentumOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.ChandeMomentumOscillator:
        pass

    IsReady: bool

    WarmUpPeriod: int



class CommodityChannelIndex(QuantConnect.Indicators.BarIndicator, QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Represents the traditional commodity channel index (CCI)
                
                 CCI = (Typical Price - 20-period SMA of TP) / (.015 * Mean Deviation)
                 Typical Price (TP) = (High + Low + Close)/3
                 Constant = 0.015
                
                 There are four steps to calculating the Mean Deviation, first, subtract
                 the most recent 20-period average of the typical price from each period's
                 typical price. Second, take the absolute values of these numbers. Third,
                 sum the absolute values. Fourth, divide by the total number of periods (20).
    
    CommodityChannelIndex(period: int, movingAverageType: MovingAverageType)
    CommodityChannelIndex(name: str, period: int, movingAverageType: MovingAverageType)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.CommodityChannelIndex:
        pass

    @typing.overload
    def __init__(self, name: str, period: int, movingAverageType: QuantConnect.Indicators.MovingAverageType) -> QuantConnect.Indicators.CommodityChannelIndex:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CommodityChannelIndex:
        pass

    IsReady: bool

    MovingAverageType: QuantConnect.Indicators.MovingAverageType

    TypicalPriceAverage: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    TypicalPriceMeanDeviation: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.IndicatorDataPoint]

    WarmUpPeriod: int



class CompositeIndicator(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    CompositeIndicator[T](name: str, left: IndicatorBase[T], right: IndicatorBase[T], composer: IndicatorComposer)
    CompositeIndicator[T](left: IndicatorBase[T], right: IndicatorBase[T], composer: IndicatorComposer)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], composer: QuantConnect.Indicators.IndicatorComposer[QuantConnect.Indicators.T]) -> QuantConnect.Indicators.CompositeIndicator:
        pass

    @typing.overload
    def __init__(self, left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T], composer: QuantConnect.Indicators.IndicatorComposer[QuantConnect.Indicators.T]) -> QuantConnect.Indicators.CompositeIndicator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CompositeIndicator:
        pass

    IsReady: bool

    Left: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]

    Right: QuantConnect.Indicators.IndicatorBase[QuantConnect.Indicators.T]


    IndicatorComposer: type


class ConstantIndicator(QuantConnect.Indicators.IndicatorBase[T], System.IComparable, QuantConnect.Indicators.IIndicator[T], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[T]]):
    """ ConstantIndicator[T](name: str, value: Decimal) """
    def Reset(self) -> None:
        pass

    def __init__(self, name: str, value: float) -> QuantConnect.Indicators.ConstantIndicator:
        pass

    IsReady: bool



class CoppockCurve(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    A momentum indicator developed by Edwin “Sedge” Coppock in October 1965.
                The goal of this indicator is to identify long-term buying opportunities in the S&P500 and Dow Industrials.
                Source: http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:coppock_curve
    
    CoppockCurve()
    CoppockCurve(shortRocPeriod: int, longRocPeriod: int, lwmaPeriod: int)
    CoppockCurve(name: str, shortRocPeriod: int, longRocPeriod: int, lwmaPeriod: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CoppockCurve:
        pass

    @typing.overload
    def __init__(self, shortRocPeriod: int, longRocPeriod: int, lwmaPeriod: int) -> QuantConnect.Indicators.CoppockCurve:
        pass

    @typing.overload
    def __init__(self, name: str, shortRocPeriod: int, longRocPeriod: int, lwmaPeriod: int) -> QuantConnect.Indicators.CoppockCurve:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CoppockCurve:
        pass

    IsReady: bool

    WarmUpPeriod: int



class Delay(QuantConnect.Indicators.WindowIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    An indicator that delays its input for a certain period
    
    Delay(period: int)
    Delay(name: str, period: int)
    """
    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.Delay:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.Delay:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.Delay:
        pass

    IsReady: bool

    WarmUpPeriod: int



class DetrendedPriceOscillator(QuantConnect.Indicators.IndicatorBase[IndicatorDataPoint], QuantConnect.Indicators.IIndicatorWarmUpPeriodProvider, System.IComparable, QuantConnect.Indicators.IIndicator[IndicatorDataPoint], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IndicatorDataPoint]]):
    """
    The Detrended Price Oscillator is an indicator designed to remove trend from price
                and make it easier to identify cycles.
                DPO does not extend to the last date because it is based on a displaced moving average.
                Is estimated as Price {X/2 + 1} periods ago less the X-period simple moving average.
                E.g.DPO(20) equals price 11 days ago less the 20-day SMA.
    
    DetrendedPriceOscillator(name: str, period: int)
    DetrendedPriceOscillator(period: int)
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, period: int) -> QuantConnect.Indicators.DetrendedPriceOscillator:
        pass

    @typing.overload
    def __init__(self, period: int) -> QuantConnect.Indicators.DetrendedPriceOscillator:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.DetrendedPriceOscillator:
        pass

    IsReady: bool

    WarmUpPeriod: int
