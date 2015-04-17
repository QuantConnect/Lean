:: Launch IB Gateway

:: IB Controller Sourced-Modified from https://github.com/ib-controller/ib-controller

set IBCDIR=%1
set IBCINI=%~1\IBController.ini
set TWSDIR=%2
::set TWSUSERID=%3
::set TWSPASSWORD=%4

:: use tws or controller gateway
set CONTROLLER=ibcontroller.IBGatewayController
if /i "%5" EQU "-tws" set CONTROLLER=ibcontroller.IBController

set TWSCP=jts.jar;total.2012.jar
set JAVAOPTS=-Dsun.java2d.noddraw=false -Dswing.boldMetal=false -Dsun.locale.formatasdefault=true -Xmx768M

:: start ib controller
pushd %TWSDIR%
start java.exe -cp  %TWSCP%;%IBCDIR%\IBController.jar %JAVAOPTS% %CONTROLLER% %IBCINI% %3 %4
popd

:: clear out our history and the screen
doskey /reinstall
cls

:: leave this window running so we can find it later and close its child processes
pause