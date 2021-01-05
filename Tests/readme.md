QuantConnect Testing
=============

Before starting any testing, follow the [installation instructions](https://github.com/QuantConnect/Lean#installation-instructions) to get LEAN running C# algorithms in your machine. 
For any Python related tests please ensure you have followed the setup as described [here](https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#install-python-36).

If the above installation, build, and initial run was succesful than we can move forward to testing.


## Visual Studio:

### Locating Tests

- Open Visual Studios
- Open Test Explorer ("Test" > "Test Explorer")
- The list should populate itself as it reads all the tests it found during the build process. If not, press "Run All Tests" and let VS find all of the tests.
- From here select the tests you would like to run and begin running them.


### Failed Test Logs

- On a failed test, check the test for information by clicking on the desired test and selecting "Open Additional Output"
- This will show the stack trace and where the code failed to meet the testing requirements. 


### Common Problems

#### Having .NetFramework issues with testing?
- Install [NUnit3TestAdapter](https://marketplace.visualstudio.com/items?itemName=NUnitDevelopers.NUnit3TestAdapter) for VS

#### Missing dependencies for Python Algorithm?
- Use pip or conda to install the module.


