import typing
import System.Collections.Generic
import QuantConnect.Securities
import QuantConnect.Data.UniverseSelection
import QuantConnect.Algorithm.Framework
import QuantConnect.Algorithm
import QuantConnect
import datetime

# no functions
# classes

class INotifiedSecurityChanges:
    """ Types implementing this interface will be called when the algorithm's set of securities changes """
    def OnSecuritiesChanged(self, algorithm: QuantConnect.Algorithm.QCAlgorithm, changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass


class NotifiedSecurityChanges(System.object):
    """ Provides convenience methods for updating collections in responses to securities changed events """
    @staticmethod
    def Update(changes: QuantConnect.Data.UniverseSelection.SecurityChanges, add: typing.Callable[[QuantConnect.Securities.Security], None], remove: typing.Callable[[QuantConnect.Securities.Security], None]) -> None:
        pass

    @staticmethod
    @typing.overload
    def UpdateCollection(securities: typing.List[QuantConnect.Securities.Security], changes: QuantConnect.Data.UniverseSelection.SecurityChanges) -> None:
        pass

    @staticmethod
    @typing.overload
    def UpdateCollection(securities: typing.List[QuantConnect.Algorithm.Framework.TValue], changes: QuantConnect.Data.UniverseSelection.SecurityChanges, valueFactory: typing.Callable[[QuantConnect.Securities.Security], QuantConnect.Algorithm.Framework.TValue]) -> None:
        pass

    def UpdateCollection(self, *args) -> None:
        pass

    @staticmethod
    @typing.overload
    def UpdateDictionary(dictionary: System.Collections.Generic.IDictionary[QuantConnect.Securities.Security, QuantConnect.Algorithm.Framework.TValue], changes: QuantConnect.Data.UniverseSelection.SecurityChanges, valueFactory: typing.Callable[[QuantConnect.Securities.Security], QuantConnect.Algorithm.Framework.TValue]) -> None:
        pass

    @staticmethod
    @typing.overload
    def UpdateDictionary(dictionary: System.Collections.Generic.IDictionary[QuantConnect.Symbol, QuantConnect.Algorithm.Framework.TValue], changes: QuantConnect.Data.UniverseSelection.SecurityChanges, valueFactory: typing.Callable[[QuantConnect.Securities.Security], QuantConnect.Algorithm.Framework.TValue]) -> None:
        pass

    @staticmethod
    @typing.overload
    def UpdateDictionary(dictionary: System.Collections.Generic.IDictionary[QuantConnect.Algorithm.Framework.TKey, QuantConnect.Algorithm.Framework.TValue], changes: QuantConnect.Data.UniverseSelection.SecurityChanges, keyFactory: typing.Callable[[QuantConnect.Securities.Security], QuantConnect.Algorithm.Framework.TKey], valueFactory: typing.Callable[[QuantConnect.Securities.Security], QuantConnect.Algorithm.Framework.TValue]) -> None:
        pass

    def UpdateDictionary(self, *args) -> None:
        pass

    __all__: list
