# Lean-Report-Creator

Create beautiful HTML/PDF reports for sharing your LEAN backtest results in Python.

## Instruction on installing and running the program

Dear users, please refer to the following instructions to generate your strategy report!

### 1. Install Python3.

You are strongly recommended to use Anaconda (or Miniconda) to install Python3 and its dependencies. 

After downloading Anaconda (or Miniconda), open Anaconda Prompt and run "conda install python=3.6". Use the same method to install all its dependencies.

### 2. Prepare input files

(1) The first input file is the .json file which you can download once you finish your backtesting. You could put this file into a convenient directory, such as ./json/sample.json.

(2) Then please replace the file "AuthorProfile.jpg" with your own profile image, but do not change the file name.

### 3. Generate report

Execute the following command to generate your strategy report:

lrc = LeanReportCreator("./json/sample.json")
lrc.genearte_report()

### 4. Get the outputs

(1) Report.html

(2) all the individual images in the directory "./outputs"

## Explaination on the meaning of the charts

Here I am going to give you a detailed explaination on the meaning of each chart.

### 1. Cumulative Return

![GitHub Logo](/outputs/cumulative-return.png)
This chart shows the cumulative returns for both your strategy (in orange) and the benchmark (in grey).

The backtest version of this chart is calculated based on daily data. If the original price series in json file is not daily, we will first convert them into daily data.

The live version of this chart is calculated based on miniute data. Icons on the chart will show when the live trading started, stopped, or had runtime errors.

### 2. Daily Return

![GitHub Logo](/outputs/daily-returns.png)
This chart shows the daily returns for your strategy.

When the return is positive, a orange bar will show above the horizontal line; when the return is negative, a grey bar will show below the horizontal line.

### 3. Top 5 Drawdown Periods

![GitHub Logo](/outputs/drawdowns.png)
This chart shows the drawdown of each day.

A certain day's drawdown is defined as the percentage of loss compared to the maximum value prior to this day. The drawdowns are calculated based on daily data.

By this defination, we can infer that when cerntain day's value is the maximum so far, its drawdown is 0.

The top 5 drawdown periods are marked in the chart with different colors.

### 4. Monthly Returns

![GitHub Logo](/outputs/monthly-returns.png)
This chart shows the return of each month.

We convert original price series into monthly series, and calculate the returns of each month. 

The green color indicates positive return, the red color indicates negative return, and the greater the loss is, the darker the color is; the yellow color means the gain or loss is rather small; the white color means the month is not included in the backtest period.

The values in the cells are in percentage.

### 5. Annual Returns

![GitHub Logo](/outputs/annual-returns.png)
This chart shows the return of each year.

We calculate the total return within each year, shown by the blue bars. The red dotted line represents the average of the annual returns.

One thing needs mentioning: if the backtest covers less than 12 month of a certain year, then the value in the chart is the actual return which is not annualized.

### 5. Distribution of Monthly Returns

![GitHub Logo](/outputs/distribution-of-monthly-returns.png)
This chart shows the distribution of monthly returns.

The x-axis represents the value of return. The y-axis is the number of months which have a certain return. The red dotted line represents mean value of montly returns.

### 6. Crisis Events

9/11
![GitHub Logo](/outputs/crisis-9-11.png)
Lehman Brothers
![GitHub Logo](/outputs/crisis-lehman-brothers.png)
Us Downgrade/European Debt Crisis
![GitHub Logo](/outputs/crisis-us-downgrade-european-debt-crisis.png)
This group of charts shows the behaviors of both your strategy and the benchmark during a certain historical period. 

We set the value of your strategy the same as the benchmark at the beginning of each crisis event, and the lines represent the cumulative returns of your strategy and benchmark from the beginning of this crisis event.

We won't draw the crisis event charts whose time periods are not covered by your strategy.

### 7. Rolling Portfolio Beta to Equity

![GitHub Logo](/outputs/rolling-portfolio-beta-to-equity.png)
This chart shows the rolling portfolio beta to the benchmark.

This chart is drawn based on daily data. Every day, we calculate the beta of your portfolio to the benchmark over the past 6 months (grey line) or 12 months (blue line). 

A beta close to 1 means the strategy has a risk exposure similar to the benchmark; a beta higher than 1 means the strategy is riskier than the benchmark; a beta close to 0 means the strategy is "market neutral", which isn't much affected by market situation. Beta could also be negative, under which the strategy has opposite risk expousure to the benchmark.

We won't draw this chart when your backtest period is less than 12 months.

### 8. Rolling Sharpe Ratio

![GitHub Logo](/outputs/rolling-sharpe-ratio(6-month).png)
This chart shows the rolling sharpe ratio of your strategy.

The rolling sharpe ratio is calculated on daily data, and annualized. Every day, we calculate the sharpe ratio of your portfolio over the past 6 months, and connect the sharpe ratios into a line. The red dotted line represents the mean value of the total sharpe ratios.

We won't draw this chart when your backtest period is less than 6 months.

### 9. Net Holdings

![GitHub Logo](/outputs/net-holdings.png)
This chart shows the net holdings of your portfolio.

The net holding is the aggregated weight of risky assets in your portfolio. It could be either positive (when your total position is long), negative (when your total position is short) or 0 (when you only hold cash). The net holding changes only if new order is fired.

The chart is drawn based on minute data, which means we aggregate all the risky positions in every minute together.

### 10. Leverage

![GitHub Logo](/outputs/leverage.png)
This chart shows the leverage of your portfolio.

The value of the leverage is always non-negative. When you only hold cash, the leverage is 0; a leverage smaller than 1 means you either long assets with money less than your portfolio value or short assets with total value less than your portfolio value; a leverage larger than 1 means you either borrow money to buy assets or short assets whose value is larger than your portfolio value. The leverage changes only if new order is fired.

The chart is drawn based on minute data, which means we aggregate all the risky positions in every minute together.

### 11. Asset Allocations

![GitHub Logo](/outputs/asset-allocation-all.png)
![GitHub Logo](/outputs/asset-allocation-equity.png)
This group of charts show your asset allocations.

It is a time-weighted average of each class of asset to your portfolio. 

The first chart shows the percentages of all the assets together. The sum of the percentages is 100%. When a certain asset has very small percentage and is too small to be shown in the pie chart, it will be incorporated into "others" class. The value of the percentage could be either positive or negative. 

The rest of the pie charts shows the percentages of some more specific asset classes, for example, stocks, foreign exchanges, etc. We won't draw the chart if your portfolio doesn't include any asset within this class.
