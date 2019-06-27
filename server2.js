var http = require('http'); // 1 - 載入 Node.js 原生模組 http
var fs = require('fs');

var configs = 
	[{
		file_name : '北京pk10',
		nextIssue : 0,
		lastIssue : 0,
		refresh_time : 1200,
		url : 'http://a.apilottery.com/api/7ce97e72844d02773bff10fe1c6714d8/bjpk10/',
		urlall : 'http://a.apilottery.com/api/7ce97e72844d02773bff10fe1c6714d8/bjpk10/'},
	{
		file_name : '马耳他幸运飞艇',
		nextIssue : 0,
		lastIssue : 0,
		refresh_time : 300,
		url : 'http://a.apilottery.com/api/7ce97e72844d02773bff10fe1c6714d8/xyft/',
		urlall : 'http://a.apilottery.com/api/7ce97e72844d02773bff10fe1c6714d8/xyft/'}];

//解析
function parse(data,configs_obj,index){
	///todo
	var obj=JSON.parse(data);
	
	if(obj.data[0].expect > configs_obj.lastIssue && ((configs_obj.nextIssue != 0 && configs_obj.nextIssue == obj.data[0].expect) || configs_obj.nextIssue == 0))
	{
		configs_obj.lastIssue = obj.data[0].expect;
		configs_obj.nextIssue = parseInt(configs_obj.lastIssue)+1;
		//file save
		var fs = require('fs');
		// append data to file
		fs.appendFile(configs_obj.file_name + '.txt',"\r\n" + data, 'utf8',
			// callback function
			function(err) { 
				if (err) throw err;
				// if no error
				console.log(configs_obj.file_name + ':' + obj.data[0].expect+" Data is appended to file successfully.")
		});
		//console.log(lastIssue+':'+nextIssue);//开奖旗号
		
		setTimeout(function(){
			delaytime(configs_obj);
		},1*1000);
		
	}else if(obj.data[0].expect > configs_obj.nextIssue){
		//意外未开出reset Issue output Issue
		var fs = require('fs');//file save
		// append data to file
		fs.appendFile('未开记录.txt',"\r\n" + parseInt(configs_obj.lastIssue) + 1 + "~" + obj.data[0].expect + "未开出", 'utf8',
			// callback function
			function(err) { 
				if (err) throw err;
				// if no error
				console.log("Issue is appended to file successfully.")
		});
		//reset configs
		configs_obj.lastIssue = obj.data[0].expect;
		configs_obj.nextIssue = parseInt(configs_obj.lastIssue)+1;
	}
	
	//after 30S do something
	if(index == configs.length - 1){//假如到foreach到最后一个阵列重新search
		setTimeout(function(){
			research();
		},30*1000);
	}
}

function delaytime(configs_obj){//读取总延迟时间
	
	http.get(configs_obj.url + 'json', (res) => {
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
				if((i+1)<obj.data.length && ( parseInt(obj.data[i].opentimestamp) - parseInt(obj.data[i+1].opentimestamp) < 3600) ){//out of 1hour
					averagetime+=(obj.data[i].opentimestamp-obj.data[i+1].opentimestamp)-parseInt(configs_obj.refresh_time);
					//checkIssue(result,i);//检查历史彩票漏开输出存党
				}
			}
			console.log("平均延迟时间:"+averagetime/obj.data.length+"秒");
		});
	});
}

function checkIssue(data,ver){//检查历史彩票漏开输出存党
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


function request(configs,index){//url connect get json
	try{
		
		http.get(configs.url + '1-json', (res) => {
			var result = "";
			res.setEncoding('utf8');
			res.on('data',(chunk)=>{
				result += chunk;
			});
			res.on('end',()=>{
				var obj=JSON.parse(result);
				console.log(result);
				parse(result,configs,index);//执行更新判断
			});
		});
	}catch(e){
		console.log(e);
	}
}
function research(){//run all configs
	configs.forEach(request);
}
research();//excute