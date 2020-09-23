import typing
import System.Linq.Expressions
import System.Dynamic
import System.Collections.Generic
import System.Collections.Concurrent
import System.Collections
import System
import QuantConnect.Securities.Interfaces
import QuantConnect.Securities
import QuantConnect.Orders.Slippage
import QuantConnect.Orders.Fills
import QuantConnect.Orders.Fees
import QuantConnect.Orders
import QuantConnect.Interfaces
import QuantConnect.Indicators
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Market
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect.Brokerages
import QuantConnect.Algorithm.Framework.Portfolio
import QuantConnect
import Python.Runtime
import NodaTime
import datetime


class UnsettledCashAmount(System.object):
    """
    Represents a pending cash amount waiting for settlement time
    
    UnsettledCashAmount(settlementTimeUtc: DateTime, currency: str, amount: Decimal)
    """
    def __init__(self, settlementTimeUtc: datetime.datetime, currency: str, amount: float) -> QuantConnect.Securities.UnsettledCashAmount:
        pass

    Amount: float

    Currency: str

    SettlementTimeUtc: datetime.datetime



class VolatilityModel(System.object):
    """ Provides access to a null implementation for QuantConnect.Securities.IVolatilityModel """
    Null: NullVolatilityModel
    __all__: list
