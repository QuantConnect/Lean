from .__Fundamental_27 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class FeesandCommissionExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cost incurred by bank and insurance companies for fees and commission income.
    
    FeesandCommissionExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FeesandCommissionExpenseIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FeesandCommissionIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fees and commission income earned by bank and insurance companies on the rendering services.
    
    FeesandCommissionIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FeesandCommissionIncomeIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FeesAndCommissionsIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total fees and commissions earned from providing services such as leasing of space or maintaining: (1) depositor accounts; (2)
                transfer agent; (3) fiduciary and trust; (4) brokerage and underwriting; (5) mortgage; (6) credit cards; (7) correspondent clearing;
                and (8) other such services and activities performed for others. This item is usually available for bank and insurance industries.
    
    FeesAndCommissionsIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FeesAndCommissionsIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinanceLeaseReceivablesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Accounts owed to the bank in relation to capital leases. Capital/ finance lease obligation are contractual obligations that arise from
                obtaining the use of property or equipment via a capital lease contract.
    
    FinanceLeaseReceivablesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinanceLeaseReceivablesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinanceLeaseReceivablesCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Accounts owed to the bank in relation to capital leases to be received within the next accounting PeriodAsByte. Capital/ finance lease
                obligations are contractual obligations that arise from obtaining the use of property or equipment via a capital lease contract.
    
    FinanceLeaseReceivablesCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinanceLeaseReceivablesCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinanceLeaseReceivablesNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Accounts owed to the bank in relation to capital leases to be received beyond the next accounting PeriodAsByte. Capital/ finance lease
                obligations are contractual obligations that arise from obtaining the use of property or equipment via a capital lease contract.
    
    FinanceLeaseReceivablesNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinanceLeaseReceivablesNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fair values as of the balance sheet date of all assets resulting from contracts that meet the criteria of being accounted for as
                derivative instruments, net of the effects of master netting arrangements.
    
    FinancialAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialAssetsBalanceSheet:
        pass

    NineMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialAssetsDesignatedasFairValueThroughProfitorLossTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Financial assets that are held at fair value through profit or loss comprise assets held for trading and those financial assets
                designated as being held at fair value through profit or loss.
    
    FinancialAssetsDesignatedasFairValueThroughProfitorLossTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialAssetsDesignatedasFairValueThroughProfitorLossTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialInstrumentsSoldUnderAgreementsToRepurchaseBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying value as of the balance sheet date of securities that an institution sells and agrees to repurchase (the identical or
                substantially the same securities) as a seller-borrower at a specified date for a specified price, also known as a repurchase
                agreement.  This item is typically available for bank industry.
    
    FinancialInstrumentsSoldUnderAgreementsToRepurchaseBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialInstrumentsSoldUnderAgreementsToRepurchaseBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialLeverage(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of Total Assets to Common Equity. Morningstar calculates the ratio by using the underlying data reported in the
                Balance Sheet within the company filings or reports:    Total Assets / Common Equity.   [Note: Common Equity = Total
                Shareholder's Equity - Preferred Stock]
    
    FinancialLeverage(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialLeverage:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialLiabilitiesCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Financial related liabilities due within one year, including short term and current portions of long-term debt, capital leases and
                derivative liabilities.
    
    FinancialLiabilitiesCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialLiabilitiesCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
