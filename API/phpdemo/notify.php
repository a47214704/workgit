<?php
define('__ROOT__',dirname(__FILE__));
require_once(__ROOT__.'/config.php');
require_once(__ROOT__.'/zealchinSdk.php');

$Sdk=new \zealchinSdk(INTERFACE_KEY,MEMBER_ID);
if ($Sdk->notify_verify($_POST)){
	//支付回调成功
	echo 'success';
}else{
	//签名验证失败
	echo 'error';
	
}
