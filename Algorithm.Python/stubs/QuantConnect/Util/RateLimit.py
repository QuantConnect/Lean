# encoding: utf-8
# module QuantConnect.Util.RateLimit calls itself RateLimit
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect
import QuantConnect.Util.RateLimit
import System
import typing

# no functions
# classes

class BusyWaitSleepStrategy(System.object, QuantConnect.Util.RateLimit.ISleepStrategy):
    """
    Provides a CPU intensive means of waiting for more tokens to be available in QuantConnect.Util.RateLimit.ITokenBucket.
                This strategy is only viable when the requested number of tokens is expected to become available in an
                extremely short period of time. This implementation aims to keep the current thread executing to prevent
                potential content switches arising from a thread yielding or sleeping strategy.
    
    BusyWaitSleepStrategy()
    """
    def Sleep(self) -> None:
        pass


class FixedIntervalRefillStrategy(System.object, QuantConnect.Util.RateLimit.IRefillStrategy):
    """
    Provides a refill strategy that has a constant, quantized refill rate.
                For example, after 1 minute passes add 5 units. If 59 seconds has passed, it will add zero unit,
                but if 2 minutes have passed, then 10 units would be added.
    
    FixedIntervalRefillStrategy(timeProvider: ITimeProvider, refillAmount: Int64, refillInterval: TimeSpan)
    """
    def Refill(self) -> int:
        pass

    def __init__(self, timeProvider: QuantConnect.ITimeProvider, refillAmount: int, refillInterval: datetime.timedelta) -> QuantConnect.Util.RateLimit.FixedIntervalRefillStrategy:
        pass


class IRefillStrategy:
    """ Provides a strategy for making tokens available for consumption in the QuantConnect.Util.RateLimit.ITokenBucket """
    def Refill(self) -> int:
        pass


class ISleepStrategy:
    """
    Defines a strategy for sleeping the current thread of execution. This is currently used via the
                QuantConnect.Util.RateLimit.ITokenBucket.Consume(System.Int64,System.Int64) in order to wait for new tokens to become available for consumption.
    """
    def Sleep(self) -> None:
        pass


class ITokenBucket:
    """
    Defines a token bucket for rate limiting
                See: https://en.wikipedia.org/wiki/Token_bucket
    """
    def Consume(self, tokens: int, timeout: int) -> None:
        pass

    def TryConsume(self, tokens: int) -> bool:
        pass

    AvailableTokens: int

    Capacity: int



class LeakyBucket(System.object, QuantConnect.Util.RateLimit.ITokenBucket):
    """
    Provides an implementation of QuantConnect.Util.RateLimit.ITokenBucket that implements the leaky bucket algorithm
                See: https://en.wikipedia.org/wiki/Leaky_bucket
    
    LeakyBucket(capacity: Int64, refillAmount: Int64, refillInterval: TimeSpan)
    LeakyBucket(capacity: Int64, sleep: ISleepStrategy, refill: IRefillStrategy, timeProvider: ITimeProvider)
    """
    def Consume(self, tokens: int, timeout: int) -> None:
        pass

    def TryConsume(self, tokens: int) -> bool:
        pass

    @typing.overload
    def __init__(self, capacity: int, refillAmount: int, refillInterval: datetime.timedelta) -> QuantConnect.Util.RateLimit.LeakyBucket:
        pass

    @typing.overload
    def __init__(self, capacity: int, sleep: QuantConnect.Util.RateLimit.ISleepStrategy, refill: QuantConnect.Util.RateLimit.IRefillStrategy, timeProvider: QuantConnect.ITimeProvider) -> QuantConnect.Util.RateLimit.LeakyBucket:
        pass

    def __init__(self, *args) -> QuantConnect.Util.RateLimit.LeakyBucket:
        pass

    AvailableTokens: int

    Capacity: int



class ThreadSleepStrategy(System.object, QuantConnect.Util.RateLimit.ISleepStrategy):
    """
    Provides a CPU non-intensive means of waiting for more tokens to be available in QuantConnect.Util.RateLimit.ITokenBucket.
                This strategy should be the most commonly used as it either sleeps or yields the currently executing thread,
                allowing for other threads to execute while the current thread is blocked and waiting for new tokens to become
                available in the bucket for consumption.
    
    ThreadSleepStrategy(milliseconds: int)
    """
    def Sleep(self) -> None:
        pass

    @staticmethod
    def Sleeping(milliseconds: int) -> QuantConnect.Util.RateLimit.ISleepStrategy:
        pass

    def __init__(self, milliseconds: int) -> QuantConnect.Util.RateLimit.ThreadSleepStrategy:
        pass

    Yielding: 'ThreadSleepStrategy'


class TokenBucket(System.object):
    """
    Provides extension methods for interacting with QuantConnect.Util.RateLimit.ITokenBucket instances as well
                as access to the QuantConnect.Util.RateLimit.TokenBucket.NullTokenBucket via QuantConnect.Util.RateLimit.TokenBucket.Null
    """
    @staticmethod
    def Consume(bucket: QuantConnect.Util.RateLimit.ITokenBucket, tokens: int, timeout: datetime.timedelta) -> None:
        pass

    Null: NullTokenBucket
    __all__: list


