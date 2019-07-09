<html>
<head>
	<link rel="stylesheet" href="./css/lottery.css">
	<script src="https://cdn.jsdelivr.net/npm/ractive"></script>
	<script type="text/javascript" src="http://www.google.com/jsapi"></script>
	<link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.2.1/css/bootstrap.min.css" integrity="sha384-GJzZqFGwb1QTTN6wy59ffF1BuGJpLSa9DkKMp0DgiMDm4iYMj70gZWKYbI706tWS" crossorigin="anonymous">
	<script src="https://stackpath.bootstrapcdn.com/bootstrap/4.2.1/js/bootstrap.min.js" integrity="sha384-B0UglyR+jN6CkvvICOB2joaf5I4l3gm9GU6Hc1og6Ls7i6U/mkkaduKaBhlAXv9k" crossorigin="anonymous"></script>
	<script type="text/javascript" language="javascript">
		google.load("jquery", "1.3");
	</script>
	<?php
	define('__ROOT__',dirname(__FILE__));
	require_once(__ROOT__.'/betadd.php');
	try {
		$db = new PDO("mysql:host=13.209.72.196;dbname=lottery_2", "root", "123456", array(PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8"));
		$db-> setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
	} catch (PDOException $e) {
		die($e->getMessage());
	}
	$sql="Select * from lottery_record where type='bjpk10'";
	$result=$db->query($sql);
	while($row=$result->fetch(PDO::FETCH_OBJ)){    
		echo $row->type."\n";  
		echo $row->expect."\n";   
	}
	?>
</head>

<body>
	<div align="center">
		<h2>彩票下注</h2>
	</div>
	<form class="form-horizontal" id="form_add">
		<table class="table">
			<tr>
				<div class="col-sm-3" style="padding-top:50px">
					<div class="input-group-prepend">
						<p class="text-uppercase">选择下注类型 : </p>
						<label class="label_style"><input name="status" value="xyft" type="radio" aria-label="Radio button for following text input">飞艇</label>
						<label class="label_style"><input name="status" value="bjpk10 " type="radio" aria-label="Radio button for following text input">PK10</label>
					</div>
				</div>
				<div class="col-sm-3">
					<p class="text-uppercase">下旗开奖号 : </p>
				</div>
				<div class="col-sm-3">
					<p class="text-uppercase">下棋开奖时间 : </p>
				</div>
			</tr>
			<tr>
				<div class="col-sm-3">
					<p class="text-uppercase">下注号码 : </p>
				</div>
				<?php for($i=1;$i<=10;$i++){?>
				<select name="number_<?php echo $i?>" style="margin:10px;width:150px" class="custom-select mb-3">
					<?php for($j=1;$j<=10;$j++){?>
						<option value="<?php echo $j?>"><?php echo $j?></option>
					<?php }?>
				</select>
				<?php }?>
			</tr>
			<tr>
				<div class="col-sm-3 ">
					<button type="button" class="btn btn-outline-primary" onclick="pushbtn()">确认下注</button>
				</div>
			</tr>
		</table>
	</form>
	
	<script>
		function pushbtn(){
			$.ajax({
			url:'betadd.php',
			type:'POST',
			data:($('#form_add').serialize()),
			success: function(response){
				console.log(response);
				obj = JSON.parse(response);
				if(obj.ok!='ok'){
					alert(obj.msg);
				}else{
					alert("状态:"+obj.msg+"讯习:"+obj.msg);
				}
			}
			});
			console.log("scandir");
		}
	</script>
</body>
</html>