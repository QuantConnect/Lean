# Clean outputs
rm *.jar
rm *.class

# Set the location of your .NET Framework
dotnet_framework="/usr/lib/mono/4.5"

echo "Build the  JAR libraries from the C# dll..."
mono ikvmstub.exe -nostdlib $dotnet_framework/mscorlib.dll
mono ikvmstub.exe -nostdlib -r:$dotnet_framework/mscorlib.dll -r:$dotnet_framework/System.Drawing.dll QuantConnect.Algorithm.dll
mono ikvmstub.exe -nostdlib -r:$dotnet_framework/mscorlib.dll -r:$dotnet_framework/System.dll -r:$dotnet_framework/System.Core.dll -r:$dotnet_framework/System.Xml.Linq.dll -r:$dotnet_framework/System.Drawing.dll QuantConnect.Common.dll

echo "Compiling java to java-classes..." 
javac -cp "QuantConnect.Algorithm.jar:mscorlib.jar:QuantConnect.Common.jar" *.java

echo "Reverse compiling to il code..."
mono ikvmc.exe -target:library -out:QuantConnect.Algorithm.Java.dll -nostdlib -r:QuantConnect.Common.dll -r:QuantConnect.Indicators.dll -r:QuantConnect.Algorithm.dll -r:Newtonsoft.Json.dll -r:$dotnet_framework/mscorlib.dll -r:$dotnet_framework/System.dll -r:$dotnet_framework/System.Core.dll -r:$dotnet_framework/System.Xml.Linq.dll -r:$dotnet_framework/System.ComponentModel.Composition.dll -r:$dotnet_framework/System.Drawing.dll *.class

echo "Build complete."