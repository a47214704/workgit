<?php
		try {
		$db = new PDO("mysql:host=13.209.72.196;dbname=lottery_2", "root", "123456", array(PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8"));
		$db-> setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
		} catch (PDOException $e) {
			die($e->getMessage());
		}
		if($_POST['status'] = ''){
			echo '{"status":"fail","msg":"资料不完整","redirect":""}';
		}else if($_POST['number'] = ''){
			echo '{"status":"fail","msg":"资料不完整","redirect":""}';
		}
		$sql = 'Insert into lottery_bet values('.$_POST['status'].','.$_POST['status'].')';
		echo '{"status":"ok","msg":"success","redirect":""}';
?>