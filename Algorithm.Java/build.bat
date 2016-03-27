REM Set the location of your .NET Framework
SET dotnet_framework="C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5"

REM Set the location of your Java Compiler.
SET javaCompilerx64="C:\Program Files\Java\jdk1.7.0_80\bin\javac.exe"
SET javaCompilerx86="C:\Program Files (x86)\Java\jdk1.7.0_80\bin\javac.exe"


ikvmstub -nostdlib %dotnet_framework%\mscorlib.dll
ikvmstub -nostdlib -r:%dotnet_framework%\mscorlib.dll -r:%dotnet_framework%\System.Drawing.dll QuantConnect.Algorithm.dll
ikvmstub -nostdlib -r:%dotnet_framework%\mscorlib.dll -r:%dotnet_framework%\System.dll -r:%dotnet_framework%\System.Core.dll -r:%dotnet_framework%\System.Xml.Linq.dll -r:%dotnet_framework%\System.Drawing.dll  QuantConnect.Common.dll

:CheckOS
IF EXIST "%PROGRAMFILES(X86)%" (GOTO 64BIT) ELSE (GOTO 32BIT)

:64BIT
	%javaCompilerx64% -cp QuantConnect.Algorithm.jar;mscorlib.jar;QuantConnect.Common.jar *.java
GOTO END

:32BIT
	%javaCompilerx86% -cp QuantConnect.Algorithm.jar;mscorlib.jar;QuantConnect.Common.jar *.java
GOTO END

:END
ikvmc -debug -target:library -out:QuantConnect.Algorithm.Java.dll -nostdlib -r:QuantConnect.Common.dll -r:QuantConnect.Indicators.dll -r:QuantConnect.Algorithm.dll -r:Newtonsoft.json.dll -r:%dotnet_framework%\mscorlib.dll -r:%dotnet_framework%\System.dll -r:%dotnet_framework%\System.Core.dll -r:%dotnet_framework%\System.Xml.Linq.dll -r:%dotnet_framework%\System.Drawing.dll -r:%dotnet_framework%\System.ComponentModel.Composition.dll *.class
