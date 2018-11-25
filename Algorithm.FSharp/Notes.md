## F# Notes
These notes are to help troubleshoot issues with running F# samples.

There are two main issues to consider:

- F# project may not build 
- F# project ouput DLL is not availble to 'Launcher project

### Build
The build may fail for several reasons but the most common one is that the nuget packages are not available to the project even after restoring nuget. This is because the references inside project file (.fsproj)  may still be invalid. Edit the .fsproj file manually to fix the references so that they point to the correct locations.

Note: In Visual Studio (as of version '17) you have 'unload' the project first and then edit the project file using context (right-click) menus available.

### F# Samples DLL not available to 'Launcher
Two possible reasons for the F# DLL to not show in the 'Launcher bin folder (i.e. Debug or Release depending on the configuration) are:

- The project is not referenced to by the 'Launcher project
- The F# project target framework is not compatible with Launcher's target.

To fix the first, simply add a reference to the the F# samples project in 'Launcher.

If there is still an issue, then the likely cause is the differnce between the .Net targets for the two projects. For example, 'Launcher may refer to .Net 4.5.2 and F# samples project may refer to 4.6.1. To fix, manually edit the .fsproj file to match what's in 'Launcher.



