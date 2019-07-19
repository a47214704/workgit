#!/bin/bash
if [ $# -eq 0 ] ; then
    echo usage: ./patch.sh \<version\> \<include-wwwroot\>
    exit 1
fi

filename=patch.$1.tar.gz
if [ -e $filename ] ; then
    echo $filename already exist, delete it
    rm $filename
fi

webroot=
if [ $# -eq 2 ] ; then
    if [ "$2" = "all" ] ; then
        tar -czvf $filename GroupPay.dll GroupPay.pdb MySqlConnector.dll GroupPay.Views.pdb GroupPay.Views.dll Core.dll Core.pdb wwwroot
    else 
        echo do not under stand $2
        exit 1
    fi
else
    tar -czvf $filename GroupPay.dll GroupPay.pdb MySqlConnector.dll GroupPay.Views.pdb GroupPay.Views.dll Core.dll Core.pdb
fi

scp -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -i ~/devel/keys/zzzf $filename andyc@23.101.13.105:./
