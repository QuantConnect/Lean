# Updating Initial and Maintenance Margins

## CME Group Exchange Futures (CME, CBOT, NYMEX, COMEX)

CME publishes advisories containing the initial and maintenance margins on its website under the category:

```
Clearing -> Performance Bond-Margins
```

You can read more about how initial and maintenance margins are used by CME by visiting the [Performance Bonds/Margins FAQ](https://www.cmegroup.com/clearing/cme-clearing-overview/performance-bonds.html) page.

**Please note:** This tutorial only covers sourcing data starting from 2008 and going onwards.

To get started, visit the [Notices](https://www.cmegroup.com/tools-information/advisorySearch.html#cat=advisorynotices%3AAdvisory+Notices%2FClearing+Advisories&pageNumber=1&searchLocations=%2Fcontent%2Fcmegroup%2F&subcat=advisorynotices%3AAdvisory+Notices%2FClearing+Advisories%2FPerformance+Bond-Margins+Advisories) page, and set the starting date to a few months before your desired start date or contract listing if it is a new contract. 

In this tutorial, we will be updating the `CL (NYMEX; Crude Oil)` contract margins starting from 2008.

Let's begin by searching for `Crude` on the Notices page. CME includes the classes of futures affected by margin rate changes by commodity type, such as `Natural Gas, Refined Products, Agriculture, Coal, Interest Rates`, etc. By searching for the contract category, we can speed up the process in which we can source historical rates. 

![Advisories Page](https://cdn.quantconnect.com/i/tu/futures-margin-readme-cme-noticespage-rev0.png "CME Notices Page")

**Note:** If you are not getting results up until the current year, consider changing your search query to a more broad topic like `Crude -> Energy`. If that fails, consider omitting the search query and review each advisory for your ticker.

The report you want to be looking at is the `Performance Bond Requirement Changes` notice. Take note of the `Effective Date`, as that is the date you will want to input to the margin file. To view the margins, click on the text that says `For the full text of this advisory, please click here.`

![Advisory Notice Page](https://cdn.quantconnect.com/i/tu/futures-margin-readme-cme-advisorynotice-rev0.png "CME Advisory Notice Page")

CME provides two different types of advisories:

* PDF
* Excel

### Using PDF Advisories

A few notes before you begin:

* CME designates Summer as the months: `April - October` 
* CME designates Winter as the months: `November - March`
* The front month is the contract closest to expiry
* `Mth 1, Mnth 1, or Month 1` is the front month. Any subsequent increment in the month number is the month that comes after the front month.

To begin inputting information into the margin rate file, we must ensure the following. If any of the following steps result in **failure**, _continue to the next advisory_.

1. Begin by searching for your contract ticker surrounded in parenthesis, e.g. `(CL)`. 

2. Ensure that the contract is for `Outrights`

3. Ensure that the contract is not included as part of another contract. For example, `Crude Oil (CL) vs. WTI Houston (Argus) Financial (HIA)` does not apply to the `CL` contract.

4. Ensure that the data for the contract has at least one `Spec` entry. (Note: `Spec` could potentially appear as `Speculation`.)

5. Find the entry that has `Month 1` in the description.

6. In the initial margin column of our CSV file, input the `new initial margin` value from the PDF.

7. In the maintenance margin column of our CSV file, input the `new maintenance margin` value from the PDF.

An example of an entry for `CL` found in the PDF advisory is shown below.

![CL in PDF advisory notice](https://cdn.quantconnect.com/i/tu/futures-margin-readme-cme-advisorypdfentry-rev0.png "CME CL in PDF Advisory Notice")

### Using Excel Advisories

Prerequisites:

* Excel, LibreOffice, or Google Sheets

To begin inputting information into the margin rate file, we must ensure the following. If any of the following steps result in **failure**, _continue to the next advisory_.

1. Look for text mentioning the `initial margin` and note it down. This should usually be `110%`.

2. In the `Table of Contents` tab, search for the ticker in the `Product Code` column of the table provided.

3. Note down the `Combined Commodity` value the product code has.

4. Note down the `Scaling Factor` of the product

    An example entry for `CL` on the `Table of Contents` tab is shown below.
    ![CL in Table of Contents Spreadsheet Tab](https://cdn.quantconnect.com/i/tu/futures-margin-readme-cme-tableofcontentsspreadsheet-rev0.png "CME CL in Table of Contents Spreadsheet Tab")

5. Change to the `Outright` tab of the spreadsheet. If there is no `Outright` tab, continue to the next advisory.

    An example of the `Outright` tab entry is shown below.
    ![CL in Outright Spreadsheet Tab](https://cdn.quantconnect.com/i/tu/futures-margin-readme-cme-outrightsspreadsheet-rev0.png "CME CL in Outright Spreadsheet Tab")

6. Find the first entry for `Combined Commodity` matching your product's value that you noted down.

7. Write down the `New Margin` multiplied by the `Scaling Factor` in the `maintenance margin` column in our CSV file.

8. Write down the `New Margin` multiplied by the `Scaling Factor` multiplied by the `Initial Margin` percentage in the `initial margin` column in our CSV file.