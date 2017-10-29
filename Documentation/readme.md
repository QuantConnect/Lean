For full documentation please see https://www.quantconnect.com/lean

## System Overview ##

![alt tag](2-Overview-Detailed-New.png)

Lean outsourced key infrastructure management to plugins. The most important plugins are:

 - **Result Processing**
   > Handle all messages from the algorithmic trading engine. Decide what should be sent, and where the messages should go. The result processing system can send messages to a local GUI, or the web interface.

 - **Datafeed Sourcing**
   > Connect and download data required for the algorithmic trading engine. For backtesting this sources files from the disk, for live trading it connects to a stream and generates the data objects.

 - **Transaction Processing**
   > Process new order requests; either using the fill models provided by the algorithm, or with an actual brokerage. Send the processed orders back to the algorithm's portfolio to be filled.

 - **Realtime Event Management**
   > Generate real time events - such as end of day events. Trigger callbacks to real time event handlers. For backtesting this is mocked-up an works on simulated time. 
 
 - **Algorithm State Setup**
   > Configure the algorithm cash, portfolio and data requested. Initialize all state parameters required.

For more information on the system design and contributing please see the Lean Website Documentation.

To update or change the above diagram, please see [this google sheet](https://docs.google.com/presentation/d/1LHOBjAjAOD0TTXu0jBc6pIqSGGeQ4ZxoUQgX6m7A8pM/edit?usp=sharing)

