<?php
	try {
		$db = new PDO("mysql:host=13.209.72.196;dbname=lottery_2", "root", "123456", array(PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8"));
		$db-> setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
	} catch (PDOException $e) {
		die($e->getMessage());
	}
	$data = getdata($_POST);
	if($data['type'] == ''){
		echo '{"status":"fail","msg":"请选择类别","redirect":""}';
	}else if($data['number_1'] == ''){
		for($i=1;$i<=10;$i++){
			if($data['number_'.$i] == ''){
				echo '{"status":"fail","msg":"请选择号码","redirect":""}';
			}
		}
	}else{
		$number ='';
		for($i=1;$i<=10;$i++){
			if($i == 10){
				$number .= $data['number_'.$i];
			}else{
				$number .= $data['number_'.$i].',';
			}
		}
		$sql = 'INSERT INTO `lottery_bet`(`bet_type`, `bet_number`, `bet_timestamp`, `bet_openexpect`, `bet_time`, `bet_note`) 
			VALUES (:bet_type,:bet_number,:bet_timestamp,:bet_openexpect,:bet_time,:bet_note)';
		$statement = $db->prepare($sql);
		$statement->execute([
			'bet_type' => $data['type'],
			'bet_number' => $number,
			'bet_timestamp' => time(),
			'bet_openexpect' => '12345',
			'bet_time' => date('Y-m-d H:i:s'),
			'bet_note' => ''
		]);
		echo '{"status":"ok","msg":"'.$number.'","redirect":""}';
		
	}
	
	
	function getData(){
		foreach($_POST as $key=>$value){
			if(is_array($value)){
				$data[$key] = $value;
			}else{
				$data[$key] = trim($value); 
			}
		}
		return $data;
	}
?>