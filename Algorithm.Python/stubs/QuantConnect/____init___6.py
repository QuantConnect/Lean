import typing
import System.Timers
import System.Threading.Tasks
import System.Threading
import System.Text
import System.IO
import System.Globalization
import System.Drawing
import System.Collections.Generic
import System.Collections.Concurrent
import System.Collections
import System
import QuantConnect.Util
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Packets
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Data.Market
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect.Algorithm.Framework.Alphas
import QuantConnect
import Python.Runtime
import NodaTime
import Newtonsoft.Json
import datetime


class TradingDay(System.object):
    """
    Class contains trading events associated with particular day in QuantConnect.TradingCalendar
    
    TradingDay()
    """
    BusinessDay: bool

    Date: datetime.datetime

    EquityDividends: typing.List[QuantConnect.Symbol]

    FutureExpirations: typing.List[QuantConnect.Symbol]

    FutureRolls: typing.List[QuantConnect.Symbol]

    OptionExpirations: typing.List[QuantConnect.Symbol]

    PublicHoliday: bool

    SymbolDelistings: typing.List[QuantConnect.Symbol]

    Weekend: bool



class TradingDayType(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    Enum lists available trading events
    
    enum TradingDayType, values: BusinessDay (0), EconomicEvent (8), EquityDividends (7), FutureExpiration (4), FutureRoll (5), OptionExpiration (3), PublicHoliday (1), SymbolDelisting (6), Weekend (2)
    """
    value__: int
    BusinessDay: 'TradingDayType'
    EconomicEvent: 'TradingDayType'
    EquityDividends: 'TradingDayType'
    FutureExpiration: 'TradingDayType'
    FutureRoll: 'TradingDayType'
    OptionExpiration: 'TradingDayType'
    PublicHoliday: 'TradingDayType'
    SymbolDelisting: 'TradingDayType'
    Weekend: 'TradingDayType'


class UserPlan(System.Enum, System.IConvertible, System.IFormattable, System.IComparable):
    """
    User / Algorithm Job Subscription Level
    
    enum UserPlan, values: Free (0), Hobbyist (1), Professional (2)
    """
    value__: int
    Free: 'UserPlan'
    Hobbyist: 'UserPlan'
    Professional: 'UserPlan'


class USHoliday(System.object):
    """ US Public Holidays - Not Tradeable: """
    Dates: HashSet[DateTime]
    __all__: list
