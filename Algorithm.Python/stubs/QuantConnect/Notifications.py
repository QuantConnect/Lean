# encoding: utf-8
# module QuantConnect.Notifications calls itself Notifications
# from QuantConnect.Common, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
# by generator 1.145
# no doc

# imports
import datetime
import QuantConnect.Notifications
import System.Collections.Concurrent
import typing

# no functions
# classes

class Notification(System.object):
    """ Local/desktop implementation of messaging system for Lean Engine. """
    def Send(self) -> None:
        pass


class NotificationEmail(QuantConnect.Notifications.Notification):
    """
    Email notification data.
    
    NotificationEmail(address: str, subject: str, message: str, data: str)
    """
    def __init__(self, address: str, subject: str, message: str, data: str) -> QuantConnect.Notifications.NotificationEmail:
        pass

    Address: str
    Data: str
    Message: str
    Subject: str

class NotificationManager(System.object):
    """
    Local/desktop implementation of messaging system for Lean Engine.
    
    NotificationManager(liveMode: bool)
    """
    def Email(self, address: str, subject: str, message: str, data: str) -> bool:
        pass

    def Sms(self, phoneNumber: str, message: str) -> bool:
        pass

    def Web(self, address: str, data: object) -> bool:
        pass

    def __init__(self, liveMode: bool) -> QuantConnect.Notifications.NotificationManager:
        pass

    Messages: System.Collections.Concurrent.ConcurrentQueue[QuantConnect.Notifications.Notification]



class NotificationSms(QuantConnect.Notifications.Notification):
    """
    Sms Notification Class
    
    NotificationSms(number: str, message: str)
    """
    def __init__(self, number: str, message: str) -> QuantConnect.Notifications.NotificationSms:
        pass

    Message: str
    PhoneNumber: str

class NotificationWeb(QuantConnect.Notifications.Notification):
    """
    Web Notification Class
    
    NotificationWeb(address: str, data: object)
    """
    def __init__(self, address: str, data: object) -> QuantConnect.Notifications.NotificationWeb:
        pass

    Address: str
    Data: object

