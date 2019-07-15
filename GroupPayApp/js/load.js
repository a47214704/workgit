	//添加数据加载时候的动画
	 var dataload = null;
	 var body = document.getElementsByTagName('body')[0];
	 //开始加载
	 function startLoad(){
	 	if (!dataload) {
	 		dataload = document.createElement('div');
	 		dataload.style.position = 'absolute';
	 		dataload.style.width = '100%';
	 		dataload.style.textAlign = 'center';
			dataload.style.top = '70px';
	 		dataload.style.zIndex = 1000000;
	 		body.appendChild(dataload);
	 		
	 		
	 		var span = document.createElement('span');
	 		span.innerHTML = '<a>\
				<span class="mui-icon mui-spinner"></span>\
			</a>\
			<br />\
			<span>加载中...</span>';
			span.style.fontSize = '0.8em';
			span.style.textAlign = 'center';
			span.style.color = 'gray';
	 		dataload.appendChild(span);
	 		
	 	}else {
	 		dataload.style.display = 'block';
	 	}
	 }
	 //progess bar load
	 function startProgessLoad(message){
	 	if (!dataload) {
	 		dataload = document.createElement('div');
	 		dataload.style.position = 'absolute';
	 		dataload.style.width = '100%';
	 		dataload.style.textAlign = 'center';
			dataload.style.top = '70px';
			dataload
	 		
	 		dataload.style.zIndex = 1000000;
	 		body.appendChild(dataload);
	 		
	 		
	 		var span = document.createElement('span');
	 		span.innerHTML = '<a>\<span class="mui-icon mui-spinner"></span>\</a>\<br />\<span id="loadMessage">'+ 
	 		(message == null ? '加载中...' : message) + '</span>';
			span.style.fontSize = '0.8em';
			span.style.textAlign = 'center';
			span.style.color = 'gray';
			
			var prog = document.createElement('progress');
			prog.max = 100;
			prog.id = "loadProgress";
			
			dataload.appendChild(prog);
	 		dataload.appendChild(span);
	 		
	 	}else {
	 		dataload.style.display = 'block';
	 	}
	 }
	 
	 //progess bar load
	 function countDownProgessLoad(message,count,timeIntervel,callback){
	 	var countDown = 0
	 	function countDownFunction(){
	 		if(countDown < count){
	 			countDown++;
	 			prog.value -= (100/count);
				setTimeout(countDownFunction,timeIntervel);	 			
	 		}else{
	 			callback();
	 		}
	 	}
	 	if (!dataload) {
	 		dataload = document.createElement('div');
	 		dataload.style.position = 'absolute';
	 		dataload.style.width = '100%';
	 		dataload.style.textAlign = 'center';
			dataload.style.top = '70px';
			dataload
	 		
	 		dataload.style.zIndex = 1000000;
	 		body.appendChild(dataload);
	 		
	 		
	 		var span = document.createElement('span');
	 		span.innerHTML = '<a>\<span class="mui-icon mui-spinner"></span>\</a>\<br />\<span id="loadMessage">'+ 
	 		(message == null ? '加载中...' : message) + '</span>';
			span.style.fontSize = '0.8em';
			span.style.textAlign = 'center';
			span.style.color = 'gray';
			
			var prog = document.createElement('progress');
			prog.max = 100;
			prog.value = 100;
			prog.id = "loadProgress";
			
			dataload.appendChild(prog);
	 		dataload.appendChild(span);
	 		countDownFunction();
	 		
	 	}else {
	 		dataload.style.display = 'block';
	 	}
	 }
	 //结束加载
	 function endLoad(){
	 	dataload.style.display = 'none';
	 }