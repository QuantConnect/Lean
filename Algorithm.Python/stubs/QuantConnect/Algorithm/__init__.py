from .____init___1 import *
import typing
import System.Collections.Generic
import System.Collections.Concurrent
import System
import QuantConnect.Storage
import QuantConnect.Securities.Option
import QuantConnect.Securities.Future
import QuantConnect.Securities.Forex
import QuantConnect.Securities.Equity
import QuantConnect.Securities.Crypto
import QuantConnect.Securities.Cfd
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Python
import QuantConnect.Orders
import QuantConnect.Notifications
import QuantConnect.Interfaces
import QuantConnect.Indicators.CandlestickPatterns
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data.Consolidators
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Benchmarks
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm.Framework.Risk
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Execution
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import pandas
import NodaTime
import datetime

# no functions
# classes

class CandlestickPatterns(System.object):
    """
    Provides helpers for using candlestick patterns
    
    CandlestickPatterns(algorithm: QCAlgorithm)
    """
    def AbandonedBaby(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.AbandonedBaby:
        pass

    def AdvanceBlock(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.AdvanceBlock:
        pass

    def BeltHold(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.BeltHold:
        pass

    def Breakaway(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Breakaway:
        pass

    def ClosingMarubozu(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ClosingMarubozu:
        pass

    def ConcealedBabySwallow(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ConcealedBabySwallow:
        pass

    def Counterattack(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Counterattack:
        pass

    def DarkCloudCover(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.DarkCloudCover:
        pass

    def Doji(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Doji:
        pass

    def DojiStar(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.DojiStar:
        pass

    def DragonflyDoji(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.DragonflyDoji:
        pass

    def Engulfing(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Engulfing:
        pass

    def EveningDojiStar(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.EveningDojiStar:
        pass

    def EveningStar(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.EveningStar:
        pass

    def GapSideBySideWhite(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.GapSideBySideWhite:
        pass

    def GravestoneDoji(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.GravestoneDoji:
        pass

    def Hammer(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Hammer:
        pass

    def HangingMan(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.HangingMan:
        pass

    def Harami(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Harami:
        pass

    def HaramiCross(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.HaramiCross:
        pass

    def HighWaveCandle(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.HighWaveCandle:
        pass

    def Hikkake(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Hikkake:
        pass

    def HikkakeModified(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.HikkakeModified:
        pass

    def HomingPigeon(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.HomingPigeon:
        pass

    def IdenticalThreeCrows(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.IdenticalThreeCrows:
        pass

    def InNeck(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.InNeck:
        pass

    def InvertedHammer(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.InvertedHammer:
        pass

    def Kicking(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Kicking:
        pass

    def KickingByLength(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.KickingByLength:
        pass

    def LadderBottom(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.LadderBottom:
        pass

    def LongLeggedDoji(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.LongLeggedDoji:
        pass

    def LongLineCandle(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.LongLineCandle:
        pass

    def Marubozu(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Marubozu:
        pass

    def MatchingLow(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.MatchingLow:
        pass

    def MatHold(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.MatHold:
        pass

    def MorningDojiStar(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.MorningDojiStar:
        pass

    def MorningStar(self, symbol: QuantConnect.Symbol, penetration: float, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.MorningStar:
        pass

    def OnNeck(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.OnNeck:
        pass

    def Piercing(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Piercing:
        pass

    def RickshawMan(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.RickshawMan:
        pass

    def RiseFallThreeMethods(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.RiseFallThreeMethods:
        pass

    def SeparatingLines(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.SeparatingLines:
        pass

    def ShootingStar(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ShootingStar:
        pass

    def ShortLineCandle(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ShortLineCandle:
        pass

    def SpinningTop(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.SpinningTop:
        pass

    def StalledPattern(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.StalledPattern:
        pass

    def StickSandwich(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.StickSandwich:
        pass

    def Takuri(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Takuri:
        pass

    def TasukiGap(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.TasukiGap:
        pass

    def ThreeBlackCrows(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ThreeBlackCrows:
        pass

    def ThreeInside(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ThreeInside:
        pass

    def ThreeLineStrike(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ThreeLineStrike:
        pass

    def ThreeOutside(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ThreeOutside:
        pass

    def ThreeStarsInSouth(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ThreeStarsInSouth:
        pass

    def ThreeWhiteSoldiers(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.ThreeWhiteSoldiers:
        pass

    def Thrusting(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Thrusting:
        pass

    def Tristar(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.Tristar:
        pass

    def TwoCrows(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.TwoCrows:
        pass

    def UniqueThreeRiver(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.UniqueThreeRiver:
        pass

    def UpDownGapThreeMethods(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.UpDownGapThreeMethods:
        pass

    def UpsideGapTwoCrows(self, symbol: QuantConnect.Symbol, resolution: typing.Optional[QuantConnect.Resolution], selector: typing.Callable[[QuantConnect.Data.IBaseData], QuantConnect.Data.Market.IBaseDataBar]) -> QuantConnect.Indicators.CandlestickPatterns.UpsideGapTwoCrows:
        pass

    def __init__(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> QuantConnect.Algorithm.CandlestickPatterns:
        pass
