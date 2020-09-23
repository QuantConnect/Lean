from .__Fundamental_38 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class LandAndImprovementsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fixed Assets that specifically deal with land a company owns. Includes the improvements associated with land. This excludes land
                held for sale.
    
    LandAndImprovementsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LandAndImprovementsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LeasesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Carrying amount at the balance sheet date of a long-lived, depreciable asset that is an addition or improvement to assets held
                under lease arrangement. This item is usually not available for the insurance industry.
    
    LeasesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LeasesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LiabilitiesHeldforSaleCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Liabilities due within the next 12 months related from an asset classified as Held for Sale.
    
    LiabilitiesHeldforSaleCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LiabilitiesHeldforSaleCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LiabilitiesHeldforSaleNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Liabilities related to an asset classified as held for sale excluding the portion due the next 12 months or operating cycle.
    
    LiabilitiesHeldforSaleNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LiabilitiesHeldforSaleNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LiabilitiesHeldforSaleTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Liabilities related to an asset classified as held for sale.
    
    LiabilitiesHeldforSaleTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LiabilitiesHeldforSaleTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LiabilitiesOfDiscontinuedOperationsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The obligations arising from the sale, disposal, or planned sale in the near future (generally within one year) of a disposal group,
                including a component of the entity (discontinued operation). This item is typically available for bank industry.
    
    LiabilitiesOfDiscontinuedOperationsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LiabilitiesOfDiscontinuedOperationsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LimitedPartnershipCapitalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    In a limited partnership or master limited partnership form of business, this represents the balance of capital held by the limited
                partners.
    
    LimitedPartnershipCapitalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LimitedPartnershipCapitalBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LineOfCreditBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying value as of the balance sheet date of obligations drawn from a line of credit, which is a bank's commitment to make
                loans up to a specific amount.
    
    LineOfCreditBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LineOfCreditBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LoansandAdvancestoBankBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of loans and advances made to a bank or financial institution.
    
    LoansandAdvancestoBankBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LoansandAdvancestoBankBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LoansandAdvancestoCustomerBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of loans and advances made to customers.
    
    LoansandAdvancestoCustomerBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LoansandAdvancestoCustomerBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LoansHeldForSaleBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    It means the aggregate amount of loans receivable that will be sold to other entities.  This item is typically available for bank
                industry.
    
    LoansHeldForSaleBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LoansHeldForSaleBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LoansReceivableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Reflects the carrying amount of unpaid loans issued to other institutions for cash needs or an asset purchase.
    
    LoansReceivableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LoansReceivableBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class LongTermCapitalLeaseObligationBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the total liability for long-term leases lasting over one year. Amount equal to the present value (the principal) at the
                beginning of the lease term less lease payments during the lease term.
    
    LongTermCapitalLeaseObligationBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.LongTermCapitalLeaseObligationBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
