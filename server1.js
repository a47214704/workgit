//解析
function parse(data){
	///todo
	var obj=JSON.parse(data);
	
	if(obj.data[0].expect > lastIssue && ((nextIssue != 0 && nextIssue == obj.data[0].expect) || nextIssue == 0))
	{
		lastIssue=obj.data[0].expect;
		nextIssue=parseInt(lastIssue)+1;
		//file save
		var fs = require('fs');
		// append data to file
		fs.appendFile('彩票记录.txt',"\r\n"+data, 'utf8',
			// callback function
			function(err) { 
				if (err) throw err;
				// if no error
				console.log(obj.data[0].opentime+"Data is appended to file successfully.")
		});
		//console.log(lastIssue+':'+nextIssue);//开奖旗号
		
		setTimeout(function(){
			delaytime();
		},1*1000);
	}
	
	//after 30S do something
	setTimeout(function(){
		request();
	},30*1000);
}

function delaytime(){
	
	var url = 'http://a.apilottery.com/api/7ce97e72844d02773bff10fe1c6714d8/bjpk10/json';
	http.get(url, (res) => {
		var result = "";
		res.setEncoding('utf8');
		res.on('data',(chunk)=>{
			result += chunk;
		});
		res.on('end',()=>{
			
			var obj=JSON.parse(result);
			var averagetime=0;
			
			for(var i=0;i<obj.data.length;i++){
				console.log("开奖号:"+obj.data[i].expect+"开奖资讯:"+JSON.stringify(obj.data[i]));//开奖资讯
				if((i+1)<obj.data.length && (obj.data[i].opentime.substring(8,10) == obj.data[i+1].opentime.substring(8,10)) ){
					averagetime+=(obj.data[i].opentimestamp-obj.data[i+1].opentimestamp)-1200;
					checkIssue(result,i);//判断漏开
				}
			}
			console.log("平均延迟时间:"+averagetime/obj.data.length+"秒");
		});
	});
}

function checkIssue(data,ver){
	var obj=JSON.parse(data);
	if(ver>0){
		var checkversion=parseInt(obj.data[ver-1].expect)-1;//上齐开奖号-1
			if(checkversion != obj.data[ver].expect){
				var count = checkversion - parseInt(obj.data[ver].expect);
				for(var i=1;i<count;i++){
					console.log(parseInt(obj.data[ver].expect)-i+"未开出");
					//file save
					var fs = require('fs');
					// append data to file
					fs.appendFile('未开记录.txt',"\r\n" + parseInt(obj.data[ver].expect)-i + "未开出", 'utf8',
						// callback function
						function(err) { 
							if (err) throw err;
							// if no error
							console.log("Data is appended to file successfully.")
					});
				}
		}
	}
	
}


function request(){
	try{
		
		http.get(url, (res) => {
			var result = "";
			res.setEncoding('utf8');
			res.on('data',(chunk)=>{
				result += chunk;
			});
			res.on('end',()=>{
				var obj=JSON.parse(result);
				console.log(result);
				
				parse(result);//执行更新判断
			});
		});
	}catch(e){
		console.log(e);
	}
}

request();