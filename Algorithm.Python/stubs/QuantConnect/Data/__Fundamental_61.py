from .__Fundamental_62 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class SaleOfInvestmentCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Proceeds received from selling all kind of investments, including both long term and short term.
    
    SaleOfInvestmentCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleOfInvestmentCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SaleOfInvestmentPropertiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash inflow from sale of investment properties during the accounting PeriodAsByte.
    
    SaleOfInvestmentPropertiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleOfInvestmentPropertiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SaleofJointVentureAssociateCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash inflow from the disposal of joint venture/associates (investment below 50%).
    
    SaleofJointVentureAssociateCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleofJointVentureAssociateCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SaleOfPPECashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Proceeds from selling any fixed assets such as property, plant and equipment, which also includes retirement of equipment.
    
    SaleOfPPECashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleOfPPECashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SaleofSubsidiariesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash inflow from the disposal of any subsidiaries.
    
    SaleofSubsidiariesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleofSubsidiariesCashFlowStatement:
        pass

    SixMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SalesPerEmployee(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of Revenue to Employees. Morningstar calculates the ratio by using the underlying data reported in the company
                filings or reports:     Revenue / Employee Number.
    
    SalesPerEmployee(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SalesPerEmployee:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SecuritiesActivitiesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income/Loss from Securities and Activities
    
    SecuritiesActivitiesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SecuritiesActivitiesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SecuritiesAmortizationIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The gradual elimination of a liability, such as a mortgage, in regular payments over a specified period of time. Such payments must
                be sufficient to cover both principal and interest.
    
    SecuritiesAmortizationIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SecuritiesAmortizationIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SecuritiesAndInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Asset, often applicable to Banks, which refers to the aggregate amount of all securities and investments.
    
    SecuritiesAndInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SecuritiesAndInvestmentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SecuritiesLendingCollateralBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying value as of the balance sheet date of the liabilities collateral securities loaned to other broker-dealers. Borrowers of
                securities generally are required to provide collateral to the lenders of securities, commonly cash but sometimes other securities or
                standby letters of credit, with a value slightly higher than that of the securities borrowed.
    
    SecuritiesLendingCollateralBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SecuritiesLendingCollateralBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SecuritiesLoanedBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying value as of the balance sheet date of securities loaned to other broker dealers, typically used by such parties to cover
                short sales, secured by cash or other securities furnished by such parties until the borrowing is closed.
    
    SecuritiesLoanedBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SecuritiesLoanedBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SecurityAgreeToBeResellBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying value of funds outstanding loaned in the form of security resale agreements if the agreement requires the purchaser to
                resell the identical security purchased or a security that meets the definition of "substantially the same" in the case of a dollar roll.
                Also includes purchases of participations in pools of securities that are subject to a resale agreement.
    
    SecurityAgreeToBeResellBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SecurityAgreeToBeResellBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
