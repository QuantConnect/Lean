@ECHO OFF

SET javaCompiler=Not Found

SET javaCompilerx64="C:\Program Files\Java\jdk1.7.0_80\bin\javac.exe"
SET javaCompilerx86="C:\Program Files (x86)\Java\jdk1.7.0_80\bin\javac.exe"


dir "%javaCompilerx64%" > NUL 2>&1

if not errorlevel 1 (
    echo Directory exists.
) else (
    echo Directory does not exist.
)
