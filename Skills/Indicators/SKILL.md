---
name: lean-indicator
description: >
  Create QuantConnect/LEAN indicators following the official contribution guidelines.
  Use when a user wants to implement a new technical indicator for the LEAN engine,
  including the C# indicator class, QCAlgorithm helper method, unit tests, test data,
  and documentation registration. Covers data-point, bar, TradeBar, and window-based
  indicators. Triggers on phrases like "create a LEAN indicator", "contribute an
  indicator to QuantConnect", "implement [indicator name] for Lean", "new technical
  indicator C#".
---

# LEAN Indicator Contribution Skill

This skill generates all the files required to contribute a new indicator to the
[QuantConnect/Lean](https://github.com/QuantConnect/Lean) open-source engine,
following the official documentation at
https://www.quantconnect.com/docs/v2/lean-engine/contributions/indicators.

## When to Use

- User wants to **create a new technical indicator** for LEAN
- User wants to **contribute an indicator** to the QuantConnect open-source repo
- User needs help implementing an indicator class in C# for LEAN
- User wants the full set of contribution files: class, helper, tests, docs

## Workflow Overview

1. **Gather requirements** – indicator name, type, formula, parameters
2. **Classify the indicator** – data-point, bar, or TradeBar
3. **Generate the indicator class** – `Indicators/<Name>.cs`
4. **Generate the helper method** – addition to `QCAlgorithm.Indicators.cs`
5. **Generate unit tests** – `Tests/Indicators/<Name>Tests.cs`
6. **Generate test data & generation script** – `Tests/TestData/<n>.csv` + `Scripts/generate_test_data.py` (TA-Lib → internet → custom script)
7. **Output all files** to `/mnt/user-data/outputs/`

---

## Step 1: Gather Requirements

Ask the user (or infer from context) the following:

| Field | Description | Example |
|---|---|---|
| **Full name** | Human-readable name | "Chande Momentum Oscillator" |
| **Abbreviation** | Short acronym for helper method | "CMO" |
| **Parameters** | Constructor args beyond `name` | `period` (int) |
| **Formula** | Mathematical definition | CMO = 100 × (sumUp − sumDown) / (sumUp + sumDown) |
| **Indicator type** | One of: `DataPoint`, `Bar`, `TradeBar` | DataPoint |
| **Warm-up period** | Bars needed before `IsReady` | `period` |
| **Uses window?** | Whether it needs a rolling window of past values | yes/no |
| **Sub-indicators** | Any internal indicators used | e.g., an internal EMA |
| **Third-party source** | Where reference values come from (see test data 3-tier approach) | TA-Lib `talib.RSI`, or URL |

If the user only says something like "implement VWAP", infer sensible defaults
and confirm before generating.

---

## Step 2: Classify the Indicator

LEAN indicators are classified by the data they consume:

### Data-Point Indicators
- Inherit `IndicatorBase<IndicatorDataPoint>` (simple) or `WindowIndicator<IndicatorDataPoint>` (windowed)
- `ComputeNextValue` receives `IndicatorDataPoint` (a timestamp + decimal value)
- Examples: SMA, EMA, RSI, Momentum, BollingerBands

### Bar Indicators
- Inherit `BarIndicator`
- `ComputeNextValue` receives `IBaseDataBar` (Open/High/Low/Close but no Volume)
- Use for indicators that need OHLC but NOT volume
- Examples: ATR, Stochastics, Aroon, CandlestickPatterns

### TradeBar Indicators
- Inherit `TradeBarIndicator`
- `ComputeNextValue` receives `TradeBar` (OHLCV – includes Volume)
- Use for indicators requiring volume data
- Examples: VWAP, OnBalanceVolume, MoneyFlowIndex

### Decision Guide
```
Does the indicator need Volume?
  YES → TradeBarIndicator
  NO → Does it need OHLC?
    YES → BarIndicator
    NO → Does it need a rolling window of past values?
      YES → WindowIndicator<IndicatorDataPoint>
      NO  → IndicatorBase<IndicatorDataPoint>
```

---

## Step 3: Generate the Indicator Class

Create the file at: `Indicators/<IndicatorName>.cs`

Read the appropriate template from `templates/` based on indicator type:
- `templates/datapoint_indicator.cs.template`
- `templates/window_indicator.cs.template`
- `templates/bar_indicator.cs.template`
- `templates/tradebar_indicator.cs.template`

### Mandatory Structure (all types)

Every indicator class MUST:

1. **Namespace**: `QuantConnect.Indicators`
2. **XML doc comment** with `<summary>` describing the indicator and its formula
3. **Implement `IIndicatorWarmUpPeriodProvider`**
4. **Define properties**:
   - `WarmUpPeriod` (int) – minimum data points for accuracy
   - `IsReady` (bool) – whether enough data has been received
5. **Define constructors**:
   - Full constructor: `(string name, int period, ...)` calling `base(name)`
   - Convenience constructor: `(int period, ...)` calling `this($"ABBR({period})", period, ...)`
6. **Implement `ComputeNextValue`** – the core computation
7. **Override `Reset()`** – reset all internal state + call `base.Reset()`
8. Optionally override `ValidateAndComputeNextValue` for edge-case handling

### Code Style Requirements (LEAN conventions)

- Use `decimal` for all price/indicator values (NOT `double` or `float`)
- Use C# XML documentation comments on all public members
- Use `readonly` for fields set only in constructor
- Follow LEAN license header (Apache 2.0)
- PascalCase for public members, _camelCase for private fields
- Use expression-bodied members where appropriate (`=>`)

---

## Step 4: Generate the Helper Method

Add to `Algorithm/QCAlgorithm.Indicators.cs`:

```csharp
/// <summary>
/// Creates a new <see cref="IndicatorName"/> indicator.
/// </summary>
/// <param name="symbol">The symbol whose data feeds the indicator</param>
/// <param name="period">The period of the indicator</param>
/// <param name="resolution">The resolution</param>
/// <param name="selector">Selects a value from the BaseData to send to the indicator</param>
/// <returns>The IndicatorName for the given parameters</returns>
[DocumentationAttribute(Indicators)]
public IndicatorName ABBR(Symbol symbol, int period, Resolution? resolution = null,
    Func<IBaseData, TradeBar> selector = null)  // adjust selector type for indicator type
{
    var name = CreateIndicatorName(symbol, $"ABBR({period})", resolution);
    var indicator = new IndicatorName(name, period);
    InitializeIndicator(symbol, indicator, resolution, selector);
    return indicator;
}
```

**Selector type mapping:**
- DataPoint indicators: `Func<IBaseData, decimal>` (or omit, defaults to `Value`)
- Bar indicators: `Func<IBaseData, IBaseDataBar>`
- TradeBar indicators: `Func<IBaseData, TradeBar>`

---

## Step 5: Generate Unit Tests

Create at: `Tests/Indicators/<IndicatorName>Tests.cs`

The test class MUST:

1. Inherit `CommonIndicatorTests<T>` where T matches the indicator input type
   - `CommonIndicatorTests<IndicatorDataPoint>` for data-point indicators
   - `CommonIndicatorTests<IBaseDataBar>` for bar indicators
   - `CommonIndicatorTests<TradeBar>` for TradeBar indicators
2. Set `TestFileName` → path to CSV in TestData
3. Set `TestColumnName` → column header of expected values
4. Set `Assertion` → comparison lambda (typically `1e-4` tolerance)
5. Override `CreateIndicator()` → instantiate the indicator
6. Include tests for:
   - `IndicatorValueTest` (inherited – compares against CSV)
   - `ResetsProperly` (inherited)
   - `WarmsUpProperly` (inherited)
   - Constructor overloads
   - `IsReady` transitions
   - Edge cases specific to the indicator

### Test Data CSV Format

Save at: `Tests/TestData/<indicator_name>.csv`

For data-point indicators:
```
Date,Open,High,Low,Close,Volume,<IndicatorAbbreviation>
2024-01-02,100.00,102.00,99.00,101.50,1000000,
2024-01-03,101.50,103.00,100.50,102.80,1200000,
...
2024-01-20,105.00,107.00,104.00,106.50,900000,52.35
```

- Empty values before warm-up period
- Values after warm-up must match third-party source within tolerance

For bar/TradeBar indicators, same format but the engine reads OHLCV columns.

### Test Data Generation (3-tier approach)

The reference values in the CSV **must** come from a verifiable, independent source.
Follow this priority order to generate the test data:

#### Tier 1: TA-Lib (preferred)

If the indicator has an equivalent function in [TA-Lib](https://ta-lib.org/)
(the Technical Analysis Library), generate the test CSV using a Python script
with `ta-lib` (the [python wrapper](https://github.com/TA-Lib/ta-lib-python)):

```python
# Example: generate_test_data.py for SMA(14) using TA-Lib
import talib
import pandas as pd

# Use SPY daily data from Lean's public data repository
filepath = "https://github.com/QuantConnect/Lean/raw/refs/heads/master/Data/equity/usa/daily/spy.zip"
df = pd.read_csv(filepath, names=['Date', 'Open', 'High', 'Low', 'Close', 'Volume']) 
for col in ['Open', 'High', 'Low', 'Close']:
    df[col] = pd.to_numeric(df[col], errors='coerce') / 10000

df['SMA'] = talib.SMA(df['Close'], timeperiod=14)

df.to_csv("spy_sma.csv", index=False)
```

Common TA-Lib function mappings (non-exhaustive):
| LEAN Indicator | TA-Lib function |
|---|---|
| SimpleMovingAverage | `talib.SMA` |
| ExponentialMovingAverage | `talib.EMA` |
| RelativeStrengthIndex | `talib.RSI` |
| BollingerBands | `talib.BBANDS` |
| MovingAverageConvergenceDivergence | `talib.MACD` |
| AverageTrueRange | `talib.ATR` |
| Stochastic | `talib.STOCH` |
| CommodityChannelIndex | `talib.CCI` |
| WilliamsPercentR | `talib.WILLR` |
| OnBalanceVolume | `talib.OBV` |
| AverageDirectionalIndex | `talib.ADX` |
| MoneyFlowIndex | `talib.MFI` |
| RateOfChange | `talib.ROC` |
| Aroon | `talib.AROON` |
| ParabolicSAR | `talib.SAR` |

If the indicator exists in TA-Lib, **always** use Tier 1.

#### Tier 2: Reliable internet source

If there is **no** TA-Lib equivalent, search for reference values from a
reliable source. Acceptable sources (in order of preference):

1. **Original academic paper** – the seminal publication with worked examples
2. **TradingView** – export chart data with indicator overlay
3. **Investopedia / StockCharts** – educational examples with worked calculations
4. **GitHub reference implementations** – well-tested open-source libraries
   (e.g., [pandas-ta](https://github.com/twopirllc/pandas-ta),
   [tulipindicators](https://github.com/TulipCharts/tulipindicators))

When using an internet source:
- Record the exact URL and access date
- Document which values were extracted and how
- Write a Python script that fetches/reproduces the data and generates the CSV

#### Tier 3: Custom calculation script (last resort)

If neither TA-Lib nor a reliable internet source provides reference values,
implement the indicator formula from scratch in a Python generation script:

- Use [SPY daily data](https://github.com/QuantConnect/Lean/raw/refs/heads/master/Data/equity/usa/daily/spy.zip) from Lean's public data repository as input
- Implement the formula step-by-step in Python using only `numpy`/`pandas`
- Add inline comments referencing the mathematical definition
- Include assertions or spot-checks where possible
- Clearly document that values are self-computed (not independently verified)

```python
# Example: generate_test_data.py for SMA(14)
import talib
import pandas as pd
import numpy as np
from collections import deque 

# Use SPY daily data from Lean's public data repository
filepath = "https://github.com/QuantConnect/Lean/raw/refs/heads/master/Data/equity/usa/daily/spy.zip"
df = pd.read_csv(filepath, names=['Date', 'Open', 'High', 'Low', 'Close', 'Volume']) 
for col in ['Open', 'High', 'Low', 'Close']:
    df[col] = pd.to_numeric(df[col], errors='coerce') / 10000

df['SMA'] = np.nan  # Initialize SMA column with NaN values
period = 14
queue = deque(maxlen=period)

for i, row in df.iterrows():
    queue.append(row['Close'])
    # Compute SMA(14)
    if i >= period:  # need period changes = period+1 data points
        df.at[i, 'SMA'] = np.mean(queue)

df.to_csv("spy_sma.csv", index=False)
```

**Important**: Tier 3 data is less trustworthy. Flag it in the README and PR
description so reviewers can cross-verify.

#### Script requirements (all tiers)

Every generated test CSV **must** be accompanied by a generation script:

- Save at: `Scripts/generate_test_data.py`
- The script must be **self-contained** and **reproducible**
  - Use [SPY daily data](https://github.com/QuantConnect/Lean/raw/refs/heads/master/Data/equity/usa/daily/spy.zip) from Lean's public data repository as input
  - Pin any external data source (URL, date range, ticker)
- The script must produce the exact CSV file when run:
  `python generate_test_data.py` → outputs `spy_<indicator_name>.csv`
- Include a header comment block:
  ```python
  """
  Test data generator for <IndicatorName>
  Source: <Tier used> – <specific source, e.g. "TA-Lib talib.RSI" or URL>
  Generated: <date>
  
  Usage: python generate_test_data.py
  Output: spy_<indicator_name>.csv
  
  Dependencies: <list pip packages needed, e.g. "ta-lib, numpy, pandas">
  """
  ```

---

## Step 6: Documentation Entry

Add to `Documentation/Resources/indicators/IndicatorImageGenerator.py`:

For simple (single-symbol, non-composite) indicators:
```python
'<Hyphenated-Title-Case>': IndicatorInfo(
    <PythonConstructor>(<args>),
    <CSharpConstructor>(<args>),
    '<CSharpHelper>(<args>)',
    'self.<python_helper>(<args>)'
),
```

For composite/multi-symbol indicators, add to `special_indicators` dict.
For option indicators, add to `option_indicators` dict.

---

## File Output Structure

When generating, create all files under `/home/claude/lean-indicator/` then copy
to `/mnt/user-data/outputs/lean-indicator/`:

```
lean-indicator/
├── Indicators/
│   └── <IndicatorName>.cs              # The indicator class
├── Algorithm/
│   └── QCAlgorithm.Indicators.ABBR.cs  # Helper method (snippet)
├── Tests/
│   ├── Indicators/
│   │   └── <IndicatorName>Tests.cs     # Unit tests
│   └── TestData/
│       └── <indicator_name>.csv        # Test reference data
├── Scripts/
│   └── generate_test_data.py           # Reproducible test data generator
├── Documentation/
│   └── indicator_image_generator_entry.py  # Doc registration snippet
└── README.md                           # Summary, integration steps & PR description
```

---

## Generation Checklist

Before outputting files, verify:

- [ ] License header present on all `.cs` files
- [ ] `namespace QuantConnect.Indicators` used
- [ ] `IIndicatorWarmUpPeriodProvider` implemented
- [ ] `WarmUpPeriod` property defined
- [ ] `IsReady` property defined
- [ ] `ComputeNextValue` implemented with correct signature
- [ ] `Reset()` resets ALL internal state and calls `base.Reset()`
- [ ] Convenience constructor delegates to full constructor
- [ ] Helper method uses correct selector type
- [ ] Helper method calls `InitializeIndicator`
- [ ] Test class inherits correct `CommonIndicatorTests<T>`
- [ ] Test CSV has correct column headers
- [ ] Test data generated via 3-tier approach (TA-Lib → internet → custom)
- [ ] `Scripts/generate_test_data.py` included and reproducible
- [ ] Test data source documented in README
- [ ] README explains integration steps
- [ ] All `decimal` (not `double`) for financial values
- [ ] XML docs on all public members

---

## Common Patterns & Reference

### Internal Sub-Indicators

Many indicators compose other indicators internally:

```csharp
private readonly IndicatorBase<IndicatorDataPoint> _smoother;

public MyIndicator(string name, int period, MovingAverageType maType)
    : base(name)
{
    _smoother = maType.AsIndicator($"{name}_Smoother", period);
    WarmUpPeriod = period;
}

protected override decimal ComputeNextValue(IndicatorDataPoint input)
{
    _smoother.Update(input);
    return _smoother.Current.Value;
}

public override void Reset()
{
    _smoother.Reset();
    base.Reset();
}
```

### Exposing Sub-Indicator Properties

For indicators with multiple output lines (e.g., MACD has Signal, Histogram):

```csharp
/// <summary>Gets the signal line</summary>
public IndicatorBase<IndicatorDataPoint> Signal { get; }

/// <summary>Gets the histogram (MACD - Signal)</summary>
public IndicatorBase<IndicatorDataPoint> Histogram { get; }
```

### Using RollingWindow

```csharp
private readonly RollingWindow<decimal> _window;

public MyIndicator(string name, int period) : base(name)
{
    _window = new RollingWindow<decimal>(period);
    WarmUpPeriod = period;
}

public override bool IsReady => _window.IsReady;

protected override decimal ComputeNextValue(IndicatorDataPoint input)
{
    _window.Add(input.Value);
    if (!_window.IsReady) return 0m;
    return _window.Average();
}
```

### ValidateAndComputeNextValue Pattern

```csharp
protected override IndicatorResult ValidateAndComputeNextValue(IndicatorDataPoint input)
{
    var value = ComputeNextValue(input);
    if (!IsReady)
        return new IndicatorResult(value, IndicatorStatus.ValueNotReady);
    if (/* some invalid condition */)
        return new IndicatorResult(value, IndicatorStatus.InvalidInput);
    return new IndicatorResult(value);
}
```

### Moving Average Type Extension

If contributing a new moving average type:

1. Add enum member to `Indicators/MovingAverageType.cs`
2. Add case to `Indicators/MovingAverageTypeExtensions.cs`
3. Add test to `Tests/Indicators/MovingAverageTypeExtensionsTests.cs`

---

## Third-Party Value Sources

See **Step 5 → Test Data Generation (3-tier approach)** above for the full
priority order and guidelines on sourcing reference values.

---

## Tips

- When in doubt about indicator type, look at what data the formula requires.
  If it only uses Close (or a single value stream), it's a data-point indicator.
- LEAN uses online (streaming) computation. Avoid bulk array operations.
- The `Samples` property (inherited) tracks how many data points have been fed.
- For indicators that are "ready" after N samples: `IsReady => Samples >= WarmUpPeriod`
- Always use `decimal` arithmetic. Use `0m` for decimal zero.
