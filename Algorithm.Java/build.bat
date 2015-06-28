SET dotnet_framework="C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5"
ikvmstub -nostdlib %dotnet_framework%\mscorlib.dll
ikvmstub -nostdlib -r:%dotnet_framework%\mscorlib.dll QuantConnect.Algorithm.dll
ikvmstub -nostdlib -r:%dotnet_framework%\mscorlib.dll QuantConnect.Interfaces.dll
ikvmstub -nostdlib -r:%dotnet_framework%\mscorlib.dll -r:%dotnet_framework%\System.dll -r:%dotnet_framework%\System.Core.dll -r:%dotnet_framework%\System.Xml.Linq.dll  QuantConnect.Common.dll
javac -cp QuantConnect.Algorithm.jar;QuantConnect.Interfaces.jar;mscorlib.jar;QuantConnect.common.jar *.java
ikvmc -debug -target:library -out:QuantConnect.Algorithm.Java.dll -nostdlib -r:QuantConnect.Common.dll -r:QuantConnect.Indicators.dll -r:QuantConnect.Algorithm.dll -r:QuantConnect.Interfaces.dll -r:Newtonsoft.json.dll -r:%dotnet_framework%\mscorlib.dll -r:%dotnet_framework%\System.dll -r:%dotnet_framework%\System.Core.dll -r:%dotnet_framework%\System.Xml.Linq.dll *.class
