<?php 
	// U支付

	
	$mch_id	  = 10020;		// 商户号
	$secretkey = 'e043fb66ee43b8d0f21936f41287d545';       // 密钥
	$payurl = ;
	$orderid = 'test123456';  // 订单号
	$money = 5000;      // 总金额
	//$uid = ; 
	$payType	= 2;//付款方式
	$message	= '下单失败，请重试！';
	//$qrcode		= '';
	$jump		= false;
	//$isvalue	= true;
	
	
	//todo
	if ($payType != '' && !$isvisit) {
		setcookie("orderided", $orderid);
		$payType = explode(',', $payType);
		
		$post_data = array(
			"pay_memberid"      	=> $mch_id,
			"pay_orderid"     	  	=> $orderid,
			"pay_applydate"			=> date("Y-m-d H:i:s"),
			"pay_bankcode"			=> $payType[1],
			"pay_notifyurl"			=> 'http://23.101.5.11:9010/'.$paymentid.'/notify.php',
			"pay_callbackurl" 		=> 'https://127.0.0.1.zhongfa169.com/return.php',
			"pay_amount"            => $money,
		);

		ksort($post_data);
		$prestr = "";
		foreach ($post_data as $name => $value) {
			if ($value != null && $value != '') {
				$prestr .=  $name . '=' . $value.'&';
			}
		}

		$prestr .= "key=".$secretkey;
		$sign = strtoupper(md5($prestr));

		$post_data['pay_md5sign'] = $sign;
		$post_data['pay_productname'] = "platformpoint";
		$pay_url = $payurl;
		$jump = true;
	}
	
?>
<!DOCTYPE html>
<html>
<head>
<title></title>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1.0,user-scalable=no" />
<style>
	body{
		font-size:18px; border: 1px solid #ccc;
	}
	.clearfix:after{
		visibility: hidden; display: block; font-size: 0; content: "."; clear: both;height: 0;
	}
	* html .clearfix{zoom: 1;}
	*:first-child + html .clearfix{zoom: 1;}
	.main{
		position:relative;
	}
	.title{
		background:#d3d3d3;font-size:18px; padding:10px 20px; border-bottom: 1px solid #ccc;
	}
	.content{
		font-size:18px; padding:10px 20px;
	}
	.paymethod{
		font-size:16px; padding-top:15px; border-top:3px solid #ddd;
	}
	label{
		padding:10px; border:1px solid #eee;  background:#f9f9f9; margin-right:15px; display:block; width:160px; float:left; margin-top:10px;
	}
	.paycontent {
		padding-bottom:20px;
	}
	.tips {
		font-size:28px;  color:#FF0000; text-align:center; padding:20px 0 50px 0;
	}
	.note {
		padding-left:20px; padding-top:20px;
	}
	.frame {
		width:300px; height:300px;  overflow:hidden; margin: 0 auto;
	}
	.frame img {
		display:block;  width:90%; height:auto;
	}
	.frame_text {
		width:400px; height:170px;  overflow:hidden; margin: 0 auto;
	}
</style>
<script type="text/javascript">
	function click_sub () {
		document.getElementById('click_subbutton').disabled = true;
	}
</script>
</head>
<?php if($payType != '' && $jump) { ?>
<body onload="form1.submit();">
<form name="form1" method="post" action="<?php echo $pay_url;?>">
	<?php foreach($post_data as $k => $v) { ?>
	<input type="hidden" name="<?php echo $k?>" value="<?php echo $v; ?>" />
	<?php }?>
</form>
<?php } else { ?>
<body>
<div class="main">
	<div class="title">订单支付信息：</div>
	<div class="content">
		<div>订单号：<?php echo $orderid?></div><br/>
		<div>充值金额：<?php echo $money?></div><br/>
	</div>
</div>
<?php }?>
</body>
</html>
