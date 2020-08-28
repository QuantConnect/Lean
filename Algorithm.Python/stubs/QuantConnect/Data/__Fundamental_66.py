from .__Fundamental_67 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class TotalDividendPerShare(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total Dividend Per Share is cash dividends and special cash dividends paid per share over a certain period of time.
    
    TotalDividendPerShare(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalDividendPerShare:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalEquityAsReportedBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total Equity as reported by the company, may be the same or not the same as Morningstar's standardized definition.
    
    TotalEquityAsReportedBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalEquityAsReportedBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalEquityBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total Equity equals Preferred Stock Equity + Common Stock Equity.
    
    TotalEquityBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalEquityBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalEquityGrossMinorityInterestBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Residual interest, including minority interest, that remains in the assets of the enterprise after deducting its liabilities. Equity is
                increased by owners' investments and by comprehensive income, and it is reduced by distributions to the owners.
    
    TotalEquityGrossMinorityInterestBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalEquityGrossMinorityInterestBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The sum of operating expense and cost of revenue. If the company does not give the reported number, it will be calculated by
                adding operating expense and cost of revenue.
    
    TotalExpensesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalExpensesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalFinancialLeaseObligationsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the total amount of long-term capital leases that must be paid within the next accounting period for a Non-
                Differentiated Balance Sheet. Capital lease obligations are contractual obligations that arise from obtaining the use of property or
                equipment via a capital lease contract.
    
    TotalFinancialLeaseObligationsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalFinancialLeaseObligationsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Asset that refers to the sum of all available for sale securities and other investments often reported on the balance sheet of
                insurance firms.
    
    TotalInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalInvestmentsBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalLiabilitiesAsReportedBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total liabilities as reported by the company, may be the same or not the same as Morningstar's standardized definition.
    
    TotalLiabilitiesAsReportedBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalLiabilitiesAsReportedBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalLiabilitiesGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the total liabilities on a percentage basis. Morningstar calculates the growth percentage based on the total liabilities
                reported in the Balance Sheet within the company filings or reports.
    
    TotalLiabilitiesGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalLiabilitiesGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalLiabilitiesNetMinorityInterestBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Probable future sacrifices of economic benefits arising from present obligations of an enterprise to transfer assets or provide
                services to others in the future as a result of past transactions or events, excluding minority interest.
    
    TotalLiabilitiesNetMinorityInterestBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalLiabilitiesNetMinorityInterestBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalMoneyMarketInvestmentsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The sum of the money market investments held by a bank's depositors, which are FDIC insured.
    
    TotalMoneyMarketInvestmentsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalMoneyMarketInvestmentsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class TotalNonCurrentAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of the carrying amounts as of the balance sheet date of all assets that are expected to be realized in cash, sold or consumed
                after one year or beyond the normal operating cycle, if longer.
    
    TotalNonCurrentAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.TotalNonCurrentAssetsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
