#/bin/sh
# Launch IB Gateway

# IB Controller Sourced-Modified from https://github.com/ib-controller/ib-controller

IBC_PATH=$1   #/Lean/Interactive/IBController
TWS_PATH=$2   #/Lean/Interactive/IBJts
TWSUSERID=$3
TWSPASSWORD=$4
TWS_MAJOR_VRSN=968
TRADING_MODE=$6
IBC_INI=$IBC_PATH/IBController.ini
LOG_PATH=$IBC_PATH/Logs

DATE=`date +%Y%m%d%H%M`

# Clean Process Space:
kill -9 `pidof xvfb-run`
kill -9 `pidof java`
kill -9 `pidof Xvfb`

# Launch a virtual screen
Xvfb :1 -screen 0 1024x768x24 2>&1 >/dev/null &
export DISPLAY=:1

# Launch the IB Controller + IB Gateway

APP=GATEWAY
TWS_CONFIG_PATH="$TWS_PATH"

export TWS_MAJOR_VRSN
export IBC_INI
export TRADING_MODE
export IBC_PATH
export TWS_PATH
export TWS_CONFIG_PATH
export LOG_PATH
export TWSUSERID
export TWSPASSWORD
export FIXUSERID
export FIXPASSWORD
export JAVA_PATH
export APP

xvfb-run "${IBC_PATH}/Scripts/DisplayBannerAndLaunch.sh" &

history -c


