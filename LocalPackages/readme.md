# LocalPackages Directory

This directory of the repo is used for testing new package deployment in production without needing to publish the package before it is ready.

Placing any `.nupkg` files in this directory and using `nuget restore .\QuantConnect.Lean.sln` will allow any packages in this directory to be picked up and used by Lean as a dependency. 


#### Note: 

- `dotnet build` will **not** use this `LocalPackages` source, unless you add it to its sources like so: 

``` 
dotnet nuget add source *PathToLeanHere*/LocalPackages
```
- Using `nuget restore` works without adding the source because it will pick up on the file `.nuget/NuGet.Config` which defines this source.