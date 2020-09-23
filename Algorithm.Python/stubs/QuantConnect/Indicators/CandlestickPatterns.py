from .__CandlestickPatterns_1 import *
import typing
import QuantConnect.Indicators.CandlestickPatterns
import datetime

# no functions
# classes

class CandlestickPattern(QuantConnect.Indicators.WindowIndicator[IBaseDataBar], System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """ Abstract base class for a candlestick pattern indicator """
    def __init__(self, *args): #cannot find CLR constructor
        pass


class AbandonedBaby(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Abandoned Baby candlestick pattern
    
    AbandonedBaby(name: str, penetration: Decimal)
    AbandonedBaby(penetration: Decimal)
    AbandonedBaby()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.AbandonedBaby:
        pass

    @typing.overload
    def __init__(self, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.AbandonedBaby:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.AbandonedBaby:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.AbandonedBaby:
        pass

    IsReady: bool



class AdvanceBlock(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Advance Block candlestick pattern
    
    AdvanceBlock(name: str)
    AdvanceBlock()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.AdvanceBlock:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.AdvanceBlock:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.AdvanceBlock:
        pass

    IsReady: bool



class BeltHold(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Belt-hold candlestick pattern indicator
    
    BeltHold(name: str)
    BeltHold()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.BeltHold:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.BeltHold:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.BeltHold:
        pass

    IsReady: bool



class Breakaway(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Breakaway candlestick pattern indicator
    
    Breakaway(name: str)
    Breakaway()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Breakaway:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Breakaway:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Breakaway:
        pass

    IsReady: bool



class CandleColor(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Colors of a candle
    
    enum CandleColor, values: Black (-1), White (1)
    """
    value__: int
    Black: 'CandleColor'
    White: 'CandleColor'


class CandleRangeType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Types of candlestick ranges
    
    enum CandleRangeType, values: HighLow (1), RealBody (0), Shadows (2)
    """
    value__: int
    HighLow: 'CandleRangeType'
    RealBody: 'CandleRangeType'
    Shadows: 'CandleRangeType'


class CandleSetting(System.object):
    """
    Represents a candle setting
    
    CandleSetting(rangeType: CandleRangeType, averagePeriod: int, factor: Decimal)
    """
    def __init__(self, rangeType: QuantConnect.Indicators.CandlestickPatterns.CandleRangeType, averagePeriod: int, factor: float) -> QuantConnect.Indicators.CandlestickPatterns.CandleSetting:
        pass

    AveragePeriod: int

    Factor: float

    RangeType: QuantConnect.Indicators.CandlestickPatterns.CandleRangeType



class CandleSettings(System.object):
    """ Candle settings for all candlestick patterns """
    @staticmethod
    def Get(type: QuantConnect.Indicators.CandlestickPatterns.CandleSettingType) -> QuantConnect.Indicators.CandlestickPatterns.CandleSetting:
        pass

    @staticmethod
    def Set(type: QuantConnect.Indicators.CandlestickPatterns.CandleSettingType, setting: QuantConnect.Indicators.CandlestickPatterns.CandleSetting) -> None:
        pass

    __all__: list


class CandleSettingType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Types of candlestick settings
    
    enum CandleSettingType, values: BodyDoji (3), BodyLong (0), BodyShort (2), BodyVeryLong (1), Equal (10), Far (9), Near (8), ShadowLong (4), ShadowShort (6), ShadowVeryLong (5), ShadowVeryShort (7)
    """
    value__: int
    BodyDoji: 'CandleSettingType'
    BodyLong: 'CandleSettingType'
    BodyShort: 'CandleSettingType'
    BodyVeryLong: 'CandleSettingType'
    Equal: 'CandleSettingType'
    Far: 'CandleSettingType'
    Near: 'CandleSettingType'
    ShadowLong: 'CandleSettingType'
    ShadowShort: 'CandleSettingType'
    ShadowVeryLong: 'CandleSettingType'
    ShadowVeryShort: 'CandleSettingType'


class ClosingMarubozu(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Closing Marubozu candlestick pattern indicator
    
    ClosingMarubozu(name: str)
    ClosingMarubozu()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.ClosingMarubozu:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.ClosingMarubozu:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.ClosingMarubozu:
        pass

    IsReady: bool



class ConcealedBabySwallow(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Concealed Baby Swallow candlestick pattern
    
    ConcealedBabySwallow(name: str)
    ConcealedBabySwallow()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.ConcealedBabySwallow:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.ConcealedBabySwallow:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.ConcealedBabySwallow:
        pass

    IsReady: bool



class Counterattack(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Counterattack candlestick pattern
    
    Counterattack(name: str)
    Counterattack()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Counterattack:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Counterattack:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Counterattack:
        pass

    IsReady: bool



class DarkCloudCover(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Dark Cloud Cover candlestick pattern
    
    DarkCloudCover(name: str, penetration: Decimal)
    DarkCloudCover(penetration: Decimal)
    DarkCloudCover()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.DarkCloudCover:
        pass

    @typing.overload
    def __init__(self, penetration: float) -> QuantConnect.Indicators.CandlestickPatterns.DarkCloudCover:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.DarkCloudCover:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.DarkCloudCover:
        pass

    IsReady: bool



class Doji(QuantConnect.Indicators.CandlestickPatterns.CandlestickPattern, System.IComparable, QuantConnect.Indicators.IIndicator[IBaseDataBar], QuantConnect.Indicators.IIndicator, System.IComparable[IIndicator[IBaseDataBar]]):
    """
    Doji candlestick pattern indicator
    
    Doji(name: str)
    Doji()
    """
    def Reset(self) -> None:
        pass

    @typing.overload
    def __init__(self, name: str) -> QuantConnect.Indicators.CandlestickPatterns.Doji:
        pass

    @typing.overload
    def __init__(self) -> QuantConnect.Indicators.CandlestickPatterns.Doji:
        pass

    def __init__(self, *args) -> QuantConnect.Indicators.CandlestickPatterns.Doji:
        pass

    IsReady: bool
