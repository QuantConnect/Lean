<h2>Guide to Developing Indicators in Lean</h2>


In this document we will be exploring developing indicators under QuantConnect’s Lean engine. By default this guide assumes that the user is developing within Visual Studio for ease of documentation, other IDE’s should have similar setups.

<h2>Getting Started</h2>


Before developing a new indicator it is important to understand a few core things about Lean’s indicators and how they are implemented, this section hopes to cover all the basics. First let's look at a few common roadblocks in developing new indicators.

<h3>Common Roadblocks</h3>




*   Understanding Lean’s indicator class structure
*   Using indicator extensions
*   Finding an accurate information about an indicator
*   Acquiring expected results for testing
*   Setting up testing for the indicator

<h3>Indicator Class Structure</h3>


In order to understand indicators in Lean it is crucial to understand the class implementation of indicators. In Figure 1.1 you will find a UML Class Implementation diagram meant to show the class interactions and hierarchy.

<p>Figure 1.1: Indicator Class Implementation
<p> ![Indicator_Class_Structure](Documentation/Indicator/Indicator_Class_Structure.png)


The figure shows the fundamental concepts for Lean indicators including the base interfaces that are used by all indicators. To better understand the diagram and indicator class structure it is best to take a look at already implemented indicators and run their respective unit tests with a debugger to see how they operate. 

<h3></h3>


<h3>Indicator Extensions</h3>


Indicator extensions offer a means of chaining together indicators through various functions. When building complex indicators that build on simpler ones, this is extremely useful as it reduces repetitive code in the Lean engine and simplifies the process of creating new indicators. 

Take a look at our examples of using indicator extensions [here](https://www.quantconnect.com/docs/algorithm-reference/indicators#Indicators-Indicator-Extensions).

<h3>Finding Information About an Indicator</h3>


Before implementing an indicator we must understand the concept behind the indicator and the mathematical formulation; then we can break it down into components that we can implement.

Here are some good examples of sources that may be useful in research:



*   [Investopedia](https://www.investopedia.com/technical-analysis-4689657) 
*   [Tradingpedia ](https://www.tradingpedia.com/forex-trading-indicators/)
*   [TechnicalIndicators.net](https://www.technicalindicators.net/indicators-technical-analysis)
*   [Trading Technologies](https://library.tradingtechnologies.com/trade/chrt-technical-indicators.html)

Although these sources should be enough to understand the concept of the indicator sometimes they may not provide information on the actual formulation. If this is the case be sure to look elsewhere for the formula or even for other code implementations of the indicator.

<h2>Developing the Indicator</h2>


Now that we have covered the basics of understanding how indicators in Lean operate, how to research the indicator to be developed, 

and how it might work within the Lean engine we can start implementing it.

<h3></h3>


<h3>Creating the Indicator Class</h3>


Under the Indicator project in Lean you can find a directory for the indicators. To create a new indicator class create a new class file (.cs). This file should follow the following format: 

***IndicatorName*.cs**

<p>Figure 2.1: Indicator Directory
![Indicator_directory](Documentation/Indicator/Indicator_Directory.png)


Once this new file has been created we must use the superclass IndicatorBase for our new class. Figure 2.2 shows an example of a basic indicator class template. It is also wise to take a look at other indicator classes to compare and contrast their implementation.

<p>Figure 2.2: Indicator Class Template


```
namespace QuantConnect.Indicators
{
	public class ExampleIndicator : IndicatorBase<IBaseData>
	{
            private **CLASS DATA MEMBERS HERE**;

            public override bool IsReady => **READY CONDITION HERE**

            public int WarmUpPeriod { get; }

            public ExampleIndicator(string name, int period)
                : base(name)
            {
                **CLASS CONSTRUCTION HERE**
            }

            protected override decimal ComputeNextValue(IBaseData input)
            {
                **CALCULATIONS HERE**
            }

            public override void Reset()
            {
                **RESET OPERATIONS HERE**
            }
	}
}
```


Depending on the type of data we want to use for the indicator, input the appropriate IBaseData type for your indicator base. This controls the type of data available to your indicator with every new input.

<h3>Implementation</h3>


This part is particularly tricky as it requires the most tweaking and testing. We strongly recommend intermittently testing and debugging your implementation to ensure the indicator will work. 

There are no specific instructions to implementation as every indicator is built on different principles. Start by building off of the basic template in Figure 2.2 and follow the testing instructions to see where you are at. All of the other already implemented indicators are a great resource to reference during this part.

<h2>Verification of Indicator Results</h2>


In order to ensure your indicator is operating correctly you will need to either:



*   Find a source that provides real indicator data to compare against

    **OR**

*   Calculate the expected results using excel sheets or manually

One such source that can be used is [TradingView](https://www.tradingview.com/); with lots of community scripts for indicators it is likely you can find an implementation of your desired indicator. Just be certain to verify the integrity of these indicators via cross comparison. Another benefit to using TradingView is that it is possible to export the indicator data with market data which is all you need to move into testing your implementation.

Once you have this data in hand, you will be able to check your indicator output against the expected outcome. This is best done using our CommonIndicatorTests routines or by creating your own indicator specific tests.

<h3>Formatting the Test Data</h3>


At this point you should have some form of external data to test your indicator against. With the proper formatting this data can be used in Leans indicator testing interface. The format of the data should be a comma delimited table containing headers for each column. The number of headers does not matter, but it is required that the following headers do exist:

<p>Table 3.1 : Required Headers for Test File


<table>
  <tr>
   <td><strong>HEADER</strong>
   </td>
   <td><strong>DESCRIPTION</strong>
   </td>
  </tr>
  <tr>
   <td>Date
   </td>
   <td>A timestamp that can be converted by DateTimes parsing method. More info <a href="https://www.c-sharpcorner.com/UploadFile/manas1/string-to-datetime-conversion-in-C-Sharp/">here</a>.
   </td>
  </tr>
  <tr>
   <td>Open
   </td>
   <td>Price at open of the timeframe
   </td>
  </tr>
  <tr>
   <td>High
   </td>
   <td>Highest price during the timeframe
   </td>
  </tr>
  <tr>
   <td>Low
   </td>
   <td>Lowest price during the timeframe
   </td>
  </tr>
  <tr>
   <td>Close
   </td>
   <td>Price at close of the timeframe
   </td>
  </tr>
  <tr>
   <td><em>Indicator*</em>
   </td>
   <td>Contains the expected value of the indicator at that time.
   </td>
  </tr>
</table>


<p>* This header should be named either the indicator name or just an abbreviation, you will enter this header into your testing class later.

<p>Figure 3.1 : Example Data File
![Example_Data_File](Documentation/Indicator/Example_Data_File.png)


Reference other test files in /Tests/TestData for more examples of the data formatting before moving on to testing your indicator. Once the formatting is complete move your test file to the TestData directory.

<h3>Creating the Unit Tests</h3>


Under the Tests project in Lean you can find a directory for indicator tests. To adapt your indicator to this test create a new class file (.cs). This file should follow the following format: 

***_IndicatorName*_Test.cs**

<p>Figure 3.2: Indicator Tests Directory
![Indicator_Test_Directory](Documentation/Indicator/Indicator_Test_Directory.png)


Once this new file has been created there is a basic template format in Figure 3.3 that showcases how to make use of the CommonIndicatorTests interface. Just be sure to use the appropriate data type for your indicator with this interface. 

<p>Figure 3.3: Test Class Example


```
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
	[TestFixture]
	public class BalanceOfPowerTests : CommonIndicatorTests<IBaseDataBar>
	{
          protected override IndicatorBase<IBaseDataBar> CreateIndicator()
          {
              return new BalanceOfPower();
          }

          protected override string TestFileName => "spy_bop.txt";

          protected override string TestColumnName => "BOP";
	}
}
```


As you can see we must adapt a few things in order to take advantage of this testing interface. 



*   Change the class name to the appropriate name defined as the file name.
*   In your CreateIndicator() function replace the return statement with:

    _return new ***IndicatorName***();_

*   Replace the TestFileName and TestColumnName data members with the appropriate values from when you compiled and formatted the test data above. 

It is also possible to place custom indicator specific tests in this same file, reference other testing files for examples of this.

<h3>Running Tests</h3>


Now that our testing structure has been implemented we can run tests against our new indicator. For this part we will require that you have the [NUnit3TestAdapter](https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter) VS extension. 

Here is the step by step process to run the tests we just created:



1. Build the project with your new indicator and indicator tests. Be sure to repeat this step with any changes made to these files.
2. Open VS Test Explorer (Test > Test Explorer)
3. Scroll to QuantConnect.Tests.Indicators and open the set of tests.
4. Scroll to your new indicator tests and open the lists of tests.
5. Run the desired tests against your indicator and verify results.

If your implementation was successful you may find the results of your indicator match very closely with your expected values and you pass your tests. If the tests aren’t passing, please check the output of the tests and see how exactly they failed and try debugging through the process to see the variance directly.

Continue to tweak your implementation and debug until you are confident it is configured correctly. Troubleshooting this process may be difficult but we hope that this documentation has helped you better understand the way indicators work in Lean to make it easier to understand.

**Congratulations, you’ve developed an Indicator!**
