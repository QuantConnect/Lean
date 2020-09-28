from .__Fundamental_10 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class CashRatioGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's cash ratio on a percentage basis. Morningstar calculates the growth percentage based on the short
                term liquid investments (cash, cash equivalents, short term investments) divided by current liabilities reported in the Balance Sheet
                within the company filings or reports.
    
    CashRatioGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashRatioGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceiptsfromDepositsbyBanksandCustomersCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from banks and customer deposits in operating cash flow, using the direct method. This item is usually only available
                for bank industry
    
    CashReceiptsfromDepositsbyBanksandCustomersCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceiptsfromDepositsbyBanksandCustomersCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceiptsfromFeesandCommissionsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from agency fees and commissions in operating cash flow, using the direct method. This item is usually available for
                bank and insurance industries
    
    CashReceiptsfromFeesandCommissionsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceiptsfromFeesandCommissionsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceiptsfromLoansCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from loans in operating cash flow, using the direct method. This item is usually only available for bank industry
    
    CashReceiptsfromLoansCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceiptsfromLoansCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceiptsfromRepaymentofAdvancesandLoansMadetoOtherPartiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from the repayment of advances and loans made to other parties, in the Investing Cash Flow section.
    
    CashReceiptsfromRepaymentofAdvancesandLoansMadetoOtherPartiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceiptsfromRepaymentofAdvancesandLoansMadetoOtherPartiesCashFlowStatement:
        pass

    SixMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceiptsfromSecuritiesRelatedActivitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from the trading of securities in operating cash flow, using the direct method. This item is usually only available for
                bank and insurance industries
    
    CashReceiptsfromSecuritiesRelatedActivitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceiptsfromSecuritiesRelatedActivitiesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceiptsfromTaxRefundsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received as refunds from tax authorities in operating cash flow, using the direct method
    
    CashReceiptsfromTaxRefundsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceiptsfromTaxRefundsCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashReceivedfromInsuranceActivitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash received from insurance activities in operating cash flow, using the direct method. This item is usually only available for
                insurance industry
    
    CashReceivedfromInsuranceActivitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashReceivedfromInsuranceActivitiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashRestrictedOrPledgedBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash that the company can use only for specific purposes or cash deposit or placing of owned property by a debtor (the pledger) to
                a creditor (the pledgee) as a security for a loan or obligation.
    
    CashRestrictedOrPledgedBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashRestrictedOrPledgedBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashtoTotalAssets(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Represents the percentage of a company's total assets is in cash.
    
    CashtoTotalAssets(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashtoTotalAssets:
        pass

    OneYear: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CededPremiumsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of premiums paid and payable to another insurer as a result of reinsurance arrangements in order to exchange for that
                company accepting all or part of insurance on a risk or exposure. This item is usually only available for insurance industry.
    
    CededPremiumsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CededPremiumsIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CFOGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's cash flow from operations on a percentage basis. Morningstar calculates the growth percentage
                based on the underlying cash flow from operations data reported in the Cash Flow Statement within the company filings or reports.
    
    CFOGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CFOGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
