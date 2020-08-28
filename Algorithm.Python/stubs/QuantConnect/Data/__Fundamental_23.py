from .__Fundamental_24 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime



class EarningReports(System.object):
    """
    Definition of the EarningReports class
    
    EarningReports()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.EarningReports) -> None:
        pass

    AccessionNumber: str

    BasicAccountingChange: QuantConnect.Data.Fundamental.BasicAccountingChange

    BasicAverageShares: QuantConnect.Data.Fundamental.BasicAverageShares

    BasicContinuousOperations: QuantConnect.Data.Fundamental.BasicContinuousOperations

    BasicDiscontinuousOperations: QuantConnect.Data.Fundamental.BasicDiscontinuousOperations

    BasicEPS: QuantConnect.Data.Fundamental.BasicEPS

    BasicEPSOtherGainsLosses: QuantConnect.Data.Fundamental.BasicEPSOtherGainsLosses

    BasicExtraordinary: QuantConnect.Data.Fundamental.BasicExtraordinary

    ContinuingAndDiscontinuedBasicEPS: QuantConnect.Data.Fundamental.ContinuingAndDiscontinuedBasicEPS

    ContinuingAndDiscontinuedDilutedEPS: QuantConnect.Data.Fundamental.ContinuingAndDiscontinuedDilutedEPS

    DilutedAccountingChange: QuantConnect.Data.Fundamental.DilutedAccountingChange

    DilutedAverageShares: QuantConnect.Data.Fundamental.DilutedAverageShares

    DilutedContinuousOperations: QuantConnect.Data.Fundamental.DilutedContinuousOperations

    DilutedDiscontinuousOperations: QuantConnect.Data.Fundamental.DilutedDiscontinuousOperations

    DilutedEPS: QuantConnect.Data.Fundamental.DilutedEPS

    DilutedEPSOtherGainsLosses: QuantConnect.Data.Fundamental.DilutedEPSOtherGainsLosses

    DilutedExtraordinary: QuantConnect.Data.Fundamental.DilutedExtraordinary

    DividendCoverageRatio: QuantConnect.Data.Fundamental.DividendCoverageRatio

    DividendPerShare: QuantConnect.Data.Fundamental.DividendPerShare

    FileDate: datetime.datetime

    FormType: str

    NormalizedBasicEPS: QuantConnect.Data.Fundamental.NormalizedBasicEPS

    NormalizedDilutedEPS: QuantConnect.Data.Fundamental.NormalizedDilutedEPS

    PeriodEndingDate: datetime.datetime

    PeriodType: str

    ReportedNormalizedBasicEPS: QuantConnect.Data.Fundamental.ReportedNormalizedBasicEPS

    ReportedNormalizedDilutedEPS: QuantConnect.Data.Fundamental.ReportedNormalizedDilutedEPS

    TaxLossCarryforwardBasicEPS: QuantConnect.Data.Fundamental.TaxLossCarryforwardBasicEPS

    TaxLossCarryforwardDilutedEPS: QuantConnect.Data.Fundamental.TaxLossCarryforwardDilutedEPS

    TotalDividendPerShare: QuantConnect.Data.Fundamental.TotalDividendPerShare



class EarningsFromEquityInterestIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The earnings from equity interest can be a result of any of the following: Income from earnings distribution of the business, either
                as dividends paid to corporate shareholders or as drawings in a partnership; Capital gain realized upon sale of the business; Capital
                gain realized from selling his or her interest to other partners. This item is usually not available for bank and insurance industries.
    
    EarningsFromEquityInterestIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EarningsFromEquityInterestIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EarningsfromEquityInterestNetOfTaxIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Income from other equity interest reported after Provision of Tax. This applies to all industries.
    
    EarningsfromEquityInterestNetOfTaxIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EarningsfromEquityInterestNetOfTaxIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EarningsLossesFromEquityInvestmentsCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This item represents the entity's proportionate share for the period of the net income (loss) of its investee (such as unconsolidated
                subsidiaries and joint ventures) to which the equity method of accounting is applied. The amount typically reflects adjustments.
    
    EarningsLossesFromEquityInvestmentsCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EarningsLossesFromEquityInvestmentsCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EBITDAGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's EBITDA on a percentage basis. Morningstar calculates the growth percentage based on the earnings
                minus expenses (excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the
                company filings or reports.
    
    EBITDAGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EBITDAGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EBITDAIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Earnings minus expenses (excluding interest, tax, depreciation, and amortization expenses).
    
    EBITDAIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EBITDAIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EBITDAMargin(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of earnings before interest, taxes and depreciation and amortization to revenue. Morningstar calculates the ratio
                by using the underlying data reported in the company filings or reports:   EBITDA / Revenue.
    
    EBITDAMargin(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EBITDAMargin:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EBITIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Earnings minus expenses (excluding interest and tax expenses).
    
    EBITIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EBITIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EBITMargin(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of earnings before interest and taxes to revenue. Morningstar calculates the ratio by using the underlying data
                reported in the company filings or reports:   EBIT / Revenue.
    
    EBITMargin(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EBITMargin:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class EffectiveTaxRateAsReportedIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The average tax rate for the period as reported by the company, may be the same or not the same as Morningstar's standardized
                definition.
    
    EffectiveTaxRateAsReportedIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.EffectiveTaxRateAsReportedIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
