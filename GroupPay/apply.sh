#!/bin/bash
if [ $# -eq 0 ] ; then
    echo usage: ./apply.sh \<version\>
    exit 1
fi

filename=~/patch.$1.tar.gz
if [ ! -e $filename ] ; then
    echo $filename does not exist.
    exit 1
fi

tar -xvzf $filename -C .

pid=$(pidof GroupPay)

if [ "$pid" == "" ] ; then
    echo service is not running
else
    echo pid of service is $pid
    kill $pid
    sleep 2
fi

nohup ./GroupPay >nohup.log 2>&1 &