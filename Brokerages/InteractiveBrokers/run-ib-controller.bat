:: LEAN IB Gateway Launcher

:: IB Controller Sourced-Modified from https://github.com/ib-controller/ib-controller
@echo off
set TWS_MAJOR_VRSN=974
set TRADING_MODE=%6

set IBC_PATH=%1
set TWS_PATH=%2
set TWSUSERID=%3
set TWSPASSWORD=%4
set APP=%5

set IBC_INI=%IBC_PATH%\IBController.ini
set LOG_PATH=%IBC_PATH%\Logs

set TITLE=IBController (%APP% %TWS_MAJOR_VRSN%)
set MIN=
if not defined LOG_PATH set MIN=/Min
set WAIT=
if /I "%~1" == "/WAIT" set WAIT=/wait
@echo on
start "%TITLE%" %MIN% %WAIT% "%IBC_PATH%\Scripts\DisplayBannerAndLaunch.bat"

:: clear out our history and the screen
doskey /reinstall
cls

:: leave this window running so we can find it later and close its child processes
pause