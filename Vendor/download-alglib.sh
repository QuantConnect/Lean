#!/bin/sh
ARCFILE=alglib-3.14.0.csharp.gpl.tgz
URL=http://www.alglib.net/translator/re/$ARCFILE
LIBFILE=alglibnet2.dll

# locations
INSTALLDIR=Vendor
CDBN=`basename $PWD`
if [ $CDBN = $INSTALLDIR ]; then
	INSTALLDIR=$PWD
else
	INSTALLDIR=$PWD/$INSTALLDIR
	if [ ! -d $INSTALLDIR ]; then
		echo Directory 'Vendor' must exist in the solution folder
		exit 1
	fi
fi

# set locations
ARCDIR=$INSTALLDIR/arc
mkdir -p $ARCDIR

echo Downoloading...
curl -o $ARCDIR/$ARCFILE $URL

echo Unpacking...
tar xf $ARCDIR/$ARCFILE --directory $ARCDIR

# clean up
mv $ARCDIR/csharp/net-core/$LIBFILE $INSTALLDIR/$LIBFILE
rm -rf $ARCDIR
