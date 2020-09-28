import typing
import System.Collections.Generic
import System
import QuantConnect.Securities
import QuantConnect.Scheduling
import QuantConnect.Interfaces
import QuantConnect.Data.UniverseSelection
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect.Algorithm.Framework.Selection
import QuantConnect.Algorithm
import QuantConnect
import Python.Runtime
import NodaTime
import datetime

class TechnologyETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Universe Selection Model that adds the following Technology ETFs at their inception date
                1998-12-22   XLK    Technology Select Sector SPDR Fund
                1999-03-10   QQQ    Invesco QQQ
                2001-07-13   SOXX   iShares PHLX Semiconductor ETF
                2001-07-13   IGV    iShares Expanded Tech-Software Sector ETF
                2004-01-30   VGT    Vanguard Information Technology ETF
                2006-04-25   QTEC   First Trust NASDAQ 100 Technology
                2006-06-23   FDN    First Trust Dow Jones Internet Index
                2007-05-10   FXL    First Trust Technology AlphaDEX Fund
                2008-12-17   TECL   Direxion Daily Technology Bull 3X Shares
                2008-12-17   TECS   Direxion Daily Technology Bear 3X Shares
                2010-03-11   SOXL   Direxion Daily Semiconductor Bull 3x Shares
                2010-03-11   SOXS   Direxion Daily Semiconductor Bear 3x Shares
                2011-07-06   SKYY   First Trust ISE Cloud Computing Index Fund
                2011-12-21   SMH    VanEck Vectors Semiconductor ETF
                2013-08-01   KWEB   KraneShares CSI China Internet ETF
                2013-10-24   FTEC   Fidelity MSCI Information Technology Index ETF
    
    TechnologyETFUniverse()
    """

class UniverseSelectionModelPythonWrapper(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that wraps a Python.Runtime.PyObject object
    
    UniverseSelectionModelPythonWrapper(model: PyObject)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModelPythonWrapper:
        pass


class USTreasuriesETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Universe Selection Model that adds the following US Treasuries ETFs at their inception date
                2002-07-26   IEF    iShares 7-10 Year Treasury Bond ETF
                2002-07-26   SHY    iShares 1-3 Year Treasury Bond ETF
                2002-07-26   TLT    iShares 20+ Year Treasury Bond ETF
                2007-01-11   SHV    iShares Short Treasury Bond ETF
                2007-01-11   IEI    iShares 3-7 Year Treasury Bond ETF
                2007-01-11   TLH    iShares 10-20 Year Treasury Bond ETF
                2007-12-10   EDV    Vanguard Ext Duration Treasury ETF
                2007-05-30   BIL    SPDR Barclays 1-3 Month T-Bill ETF
                2007-05-30   SPTL   SPDR Portfolio Long Term Treasury ETF
                2008-05-01   TBT    UltraShort Barclays 20+ Year Treasury
                2009-04-16   TMF    Direxion Daily 20-Year Treasury Bull 3X
                2009-04-16   TMV    Direxion Daily 20-Year Treasury Bear 3X
                2009-08-20   TBF    ProShares Short 20+ Year Treasury
                2009-11-23   VGSH   Vanguard Short-Term Treasury ETF
                2009-11-23   VGIT   Vanguard Intermediate-Term Treasury ETF
                2009-11-24   VGLT   Vanguard Long-Term Treasury ETF
                2010-08-06   SCHO   Schwab Short-Term U.S. Treasury ETF
                2010-08-06   SCHR   Schwab Intermediate-Term U.S. Treasury ETF
                2011-12-01   SPTS   SPDR Portfolio Short Term Treasury ETF
                2012-02-24   GOVT   iShares U.S. Treasury Bond ETF
    
    USTreasuriesETFUniverse()
    """

class VolatilityETFUniverse(QuantConnect.Algorithm.Framework.Selection.InceptionDateUniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """ VolatilityETFUniverse() """


class NullUniverseSelectionModel(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides a null implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel
    
    NullUniverseSelectionModel()
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass


class UniverseSelectionModelPythonWrapper(QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModel, QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel):
    """
    Provides an implementation of QuantConnect.Algorithm.Framework.Selection.IUniverseSelectionModel that wraps a Python.Runtime.PyObject object
    
    UniverseSelectionModelPythonWrapper(model: PyObject)
    """
    def CreateUniverses(self, algorithm: QuantConnect.Algorithm.QCAlgorithm) -> typing.List[QuantConnect.Data.UniverseSelection.Universe]:
        pass

    def GetNextRefreshTimeUtc(self) -> datetime.datetime:
        pass

    def __init__(self, model: Python.Runtime.PyObject) -> QuantConnect.Algorithm.Framework.Selection.UniverseSelectionModelPythonWrapper:
        pass
