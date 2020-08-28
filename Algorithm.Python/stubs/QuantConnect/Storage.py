# encoding: utf-8
# module QuantConnect.Storage calls itself Storage
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import Newtonsoft.Json
import QuantConnect.Interfaces
import QuantConnect.Packets
import QuantConnect.Storage
import System.Collections.Generic
import System.Text
import typing

# no functions
# classes

class ObjectStore(System.object, System.IDisposable, System.Collections.IEnumerable, QuantConnect.Interfaces.IObjectStore, System.Collections.Generic.IEnumerable[KeyValuePair[str, Array[Byte]]]):
    """
    Helper class for easier access to QuantConnect.Interfaces.IObjectStore methods
    
    ObjectStore(store: IObjectStore)
    """
    def ContainsKey(self, key: str) -> bool:
        pass

    def Delete(self, key: str) -> bool:
        pass

    def Dispose(self) -> None:
        pass

    def GetEnumerator(self) -> System.Collections.Generic.IEnumerator[System.Collections.Generic.KeyValuePair[str, typing.List[bytes]]]:
        pass

    def GetFilePath(self, key: str) -> str:
        pass

    def Initialize(self, algorithmName: str, userId: int, projectId: int, userToken: str, controls: QuantConnect.Packets.Controls) -> None:
        pass

    def Read(self, key: str, encoding: System.Text.Encoding) -> str:
        pass

    def ReadBytes(self, key: str) -> typing.List[bytes]:
        pass

    def ReadJson(self, key: str, encoding: System.Text.Encoding, settings: Newtonsoft.Json.JsonSerializerSettings) -> QuantConnect.Storage.T:
        pass

    def ReadString(self, key: str, encoding: System.Text.Encoding) -> str:
        pass

    def ReadXml(self, key: str, encoding: System.Text.Encoding) -> QuantConnect.Storage.T:
        pass

    def Save(self, key: str, text: str, encoding: System.Text.Encoding) -> bool:
        pass

    def SaveBytes(self, key: str, contents: typing.List[bytes]) -> bool:
        pass

    def SaveJson(self, key: str, obj: QuantConnect.Storage.T, encoding: System.Text.Encoding, settings: Newtonsoft.Json.JsonSerializerSettings) -> bool:
        pass

    def SaveString(self, key: str, text: str, encoding: System.Text.Encoding) -> bool:
        pass

    def SaveXml(self, key: str, obj: QuantConnect.Storage.T, encoding: System.Text.Encoding) -> bool:
        pass

    def __init__(self, store: QuantConnect.Interfaces.IObjectStore) -> QuantConnect.Storage.ObjectStore:
        pass

    ErrorRaised: BoundEvent


