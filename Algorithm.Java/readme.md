29th June 2015: LEAN Algorithmic Trading Engine Java Interop
Jared Broad (@jaredbroad) & Craig Stevenson (@craig-stevenson)
This is the Java-C# Interop project for connecting LEAN and Java Algorithms. Points to follow:

NOTE: Because Java project requires Java to be installed; we've removed it as a build dependendancy of LEAN. You'll need to make 
sure your QuantConnect.Algorithm.Java.dll is copied to the Lean/bin/Debug (or Release) directory.

=====
WINDOWS:
 
1. The project uses IKVM which requires version 1.7 of Java. You can obtain this here: 
Download the 1.7.80 SDK and install it. http://www.oracle.com/technetwork/java/javase/downloads/jdk7-downloads-1880260.html 

2. After installation confirm your "javac.exe" directory is one of the following:
	64 bit systems -- "C:\Program Files\Java\jdk1.7.0_80\bin\javac.exe" 
	32 bit systems -- "C:\Program Files (x86)\Java\jdk1.7.0_80\bin\javac.exe"

3. Build! Hopefully that is all that is required. Your DLL will output to "QuantConnect.Algorithm.Java.dll" in the binary directory.

If you get any build errors in the Java project it probably means you have a Java error. We recommend developing in Eclipse to fix these errors before using Lean.
If the error persists, then check the "output" tab of visual studio to read the error message from the raw build logs.

=====
LINUX:

1. Setup and install Java 1.7 & SDK: 
	sudo apt-get install openjdk-7-jre-headless -y
	sudo apt-get install openjdk-7-jdk -y

2. Manually call the "./build.sh" script after compiling Lean. At the moment it can't  be linked into the sln file