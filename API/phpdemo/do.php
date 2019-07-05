<?php
define('__ROOT__',dirname(__FILE__));
require_once(__ROOT__.'/config.php');
require_once(__ROOT__.'/zealchinSdk.php');

$cmd=$_POST['cmd'];


if ($cmd=='create'){
	$order_no=$_POST['order_no'];
	$money=$_POST['money'];
	
	$Sdk=new \zealchinSdk(INTERFACE_KEY,MEMBER_ID);
	$ResInfo=$Sdk->create_order($order_no,$money,CHANNEL_ID,NOTIFY_URL,REDIRECT_URL);
	echo json_encode($ResInfo);	
}

if($cmd=='query'){
	$order_no=$_POST['order_no'];
	
	$Sdk=new \zealchinSdk(INTERFACE_KEY,MEMBER_ID);
	$ResInfo=$Sdk->query_order($order_no);
	echo json_encode($ResInfo);	
}