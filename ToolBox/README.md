Lean Data ToolBox 
=========
[![Join the chat at https://gitter.im/QuantConnect/Lean](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/QuantConnect/Lean?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[Lean Home][1] | [Documentation][2] | [Download Lean][3]
----------

## Introduction ##

Lean Engine is an open-source fully managed C# algorithmic trading engine built for desktop and cloud usage. It was designed in Mono and operates in Windows and Linux.

The following is a collection of helper projects for data download and storage to Lean.

## QuantQuote Converter ##

The QuantQuote converter ports an extracted QuantQuote order to QuantConnect data format and saves it to your LEAN directory. The user must enter three key parts of information:

 - Directory where your QuantQuote order is extracted.
 - Directory where Lean Data is located "Lean/Data".
 - Resolution of the QuantQuote data.

## Oanda Downloader ##

Download data directly from the Oanda database using your personal access token. The downloader will save the information to your FX directory.

## Dukascopy Downloader ##

Download data from Dukascopy website and convert it into LEAN engine format. Save the data to your personal LEAN data directory.

## Yahoo Downloader ##

Download data from Yahoo into daily files and store them in your local data directory.

## Google Downloader ##

Download data from Google and store them in your local data directory.

  [1]: https://lean.quantconnect.com "Lean Open Source Home Page"
  [2]: https://lean.quantconnect.com/docs "Lean Documentation"
  [3]: https://github.com/QuantConnect/Lean/archive/master.zip
