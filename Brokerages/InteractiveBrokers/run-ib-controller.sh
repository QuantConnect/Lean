#/bin/sh
# Launch IB Gateway

# IB Controller Sourced-Modified from https://github.com/ib-controller/ib-controller

IBCDIR=$1 #/Lean/Interactive/IBController
TWSDIR=$2 #/Lean/Interactive/IBJts
TWSUSERID=$3
TWSPASSWORD=$4
DATE=`date +%Y%m%d%H%M`
LOGFILE=/tmp/ibg_running_${DATE}.log

# Clean Process Space:
kill -9 `pidof xvfb-run`
kill -9 `pidof java`
kill -9 `pidof Xvfb`

# The IBController ini file:
IBCINI=$IBCDIR/IBController.ini

TWSCP=jts.jar:total.2013.jar

JAVAOPTS='-Xmx768M -XX:MaxPermSize=256M'

pushd $TWSDIR

# Launch a virtual screen
Xvfb :1 -screen 0 1024x768x24 2>&1 >/dev/null &
export DISPLAY=:1

# Launch the IB Controller + IB Gateway
xvfb-run java -cp  $TWSCP:$IBCDIR/IBController.jar $JAVAOPTS ibcontroller.IBGatewayController $IBCINI $TWSUSERID $TWSPASSWORD

popd

history -c
