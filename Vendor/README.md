# Vendor Assets

## QP and NLP Solver

In order to enable a protfolio optimization algorithms, ALGLIB library `alglibnet2.dll` file need to be placed into `Vendor` directory.

You can use following scripts for downloading library:

- Windows: download-alglib.ps1
- Linux & macOS: : download-alglib.sh

The project `QuantConnect.Algorithm.Framework` must be reloaded in order to have ALGLIB library properly referenced.
