if(window.WebSocket){
	console.log('This browser supports WebSocket');
}else{
	throw(new Error("This browser does not supports WebSocket"));
}

if (typeof serverHost === 'undefined') {
	throw(new Error("server host not defined!"));
}

var scrollPosition;
var loadMore = false;
var reconnectTime = 0;
var compare = function (obj1, obj2) {
    var val1 = obj1.sendTime;
    var val2 = obj2.sendTime;
    if (val1 < val2) {
        return 1;
    } else if (val1 > val2) {
        return -1;
    } else {
        return 0;
    }            
} 

function formatDateTime(inputTime) {    
    var date = new Date(inputTime);  
    var m = date.getMonth() + 1;    
    m = m < 10 ? ('0' + m) : m;    
    var d = date.getDate();    
    d = d < 10 ? ('0' + d) : d;    
    var h = date.getHours();  
    h = h < 10 ? ('0' + h) : h;  
    var minute = date.getMinutes();  
    minute = minute < 10 ? ('0' + minute) : minute; 
    return m + '-' + d+' '+h+':'+minute;    
}

function updateScroll(position) {
	setTimeout(function(){
		$(".message-container").mCustomScrollbar("scrollTo", position);
		loadMore = true;
	}, 300);
}

function appendMessage(data, self, pre, addCache) {
	if (data.type == 3)	{
		receiver = data.receiver;
	} else {
		if (addCache) {
			addCacheMessage(data, self ? data.receiver : data.sender, !pre);
		}

		if (receiver == '' || (!self && receiver == data.sender || self && receiver == data.receiver)) {
			if (receiver == '' && data.receiver != data.sender) {
				receiver = self ? data.receiver : data.sender;
				receiver = receiver == 'nonUser' ? '' : receiver;
			}
			
			var html = '<div class="message-row" id="msg'+ data.sendTime +'">';
			if (data.sender == userName) {
				html += '<div class="comment fr"><div class="text">';
			} else {
				html += '<div class="comment fl"><div class="text">';
				if (manager == 1){
					html += '<p>'+ data.sender +'</p>';
				} else {
					html += '<p>'+ data.senderName +'</p>';
				}
			}

			if (data.type == 1) {
				html += '<div class="image"><img src="'+ data.message +'" onerror="this.src=\'upload/default.jpg\'"/></div>'
			} else {
				data.message = message_filter(data.message);
				html += '<pre>' + data.message + '</pre>';
			}

			html += '<span class="time">' +formatDateTime(data.sendTime)+ '</span>';
			html += '</div></div></div>';
			if (pre) {
				$('#chat-box').prepend(html);
			} else {
				$('#chat-box').append(html);
				updateScroll("bottom");
			}

			if (!pre) {
				showTips(self?data.receiver:data.sender, (self || data.sender == data.senderName) ? null : data.senderName, data.message, false);
				if (!self && data.isNew) {
					data.isNew = 0;
					preSend(userName, receiver, 6, '');
				}
			}
		} else if((!pre || data.isNew) && !self) {
			showTips(data.sender, data.sender == data.senderName?null:data.senderName, data.message, true);
		}
	}
}

function message_filter(message) {
	var domain = window.location.host;
	if (message.indexOf(domain) == -1) {
		var pattern =/(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?/i;
		message = trim(message, 'g');
		return message.replace(pattern, '[链接已屏蔽]');
	} else {
		return message;
	}
}

function trim(str, is_global) {
	var result;
	result = str.replace(/(^\s+)|(\s+$)/g,"");
	if(is_global.toLowerCase()=="g") {
		result = result.replace(/\s/g,"");
	}

	return result;
}

function showTips(msgSender, senderName, message, tips) {
	if (tips){
		$('.users').css({"-webkit-animation":"twinkling 1s infinite ease-in-out"});
	}
	var updated = false;
	$.each($('#user-list>li'), function() {
		if ($(this).attr('receiver') == msgSender) {
			var unode = $(this);
			if (tips) {
				unode.find('em').show();
			}
			unode.find('.msg').html(message);
			if(senderName != null) {
				unode.find('.nick').html('~' + senderName);
			}
			update = true;
			$('#user-list').prepend(unode);
		}
	});

	if (!updated){
		$.each($('#top-list>li'), function() {
			if ($(this).attr('receiver') == msgSender) {
				var unode = $(this);
				if (tips) {
					unode.find('em').show();
				}
				$(this).find('.msg').html(message);
				if(senderName != null) {
					unode.find('.nick').html('~' + senderName);
				}
				update = true;
				$('#top-list').prepend(unode);
			}
		});
	}
}

function showNotice() {
	if(notice && manager != 1) {
		var html = '<div class="message-row">';
		html += '<div class="comment fl"><div class="text">';
		html += '<p>系统信息</p>';
		html += '<pre>' + notice + '</pre>';
		html += '<span class="time">' +formatDateTime(new Date().getTime())+ '</span>';
		html += '</div></div></div>';
		$('#chat-box').append(html);
	}
}

function loadHistory() {
	if (messageList[receiver] && loadMore){
		console.log('load history');
		//$('.loading').show();
		var timeOffset = 0;
		$.each(messageList[receiver], function(k, v) {
			if (timeOffset == 0 || timeOffset > v.sendTime) {
				timeOffset = v.sendTime
			}
		});
		
		scrollPosition = 'msg' + timeOffset;
		preSend(userName, receiver, 5, timeOffset);
	}
}

function setUserList(user, data) {
	if (user == userName) {
		return;
	}

	var html = '';
	if (user == 'nonUser'){
		user = '众发客服';
	}

	if (user == receiver){
		html = '<li class="active" receiver = "'+user+'")">';
	} else {
		html = '<li  receiver = "'+user+'">';
	}
	
	var nickname = '';
	if (user == data.sender && data.sender != data.senderName)
	{
		nickname = '~' + data.senderName;
	}

	if (manager != 1) {
		user = RandomNum(100000, 999999);
	}

	html += '<em class="tips"></em>';
	html += '<a href="#" class="fa fa-thumb-tack"></a>';
	html += '<i class="fa fa-user"></i><div><p>'+user+'<span class="nick">'+nickname+'</span></p>';
	html += '<span class="msg">' + data.message + '</span></div></li>';
	$('#user-list').prepend(html);
}

function RandomNum(Min, Max) {
      var Range = Max - Min;
      var Rand = Math.random();
      var num = Min + Math.floor(Rand * Range); //舍去
      return num;
}

function switchUser(username) {
	receiver = username;
	$('#chat-box').html('');
	var hasNew = false;
	if (messageList[receiver]) {
		loadMore = false;
		messageList[receiver].sort(compare);
		$.each(messageList[receiver], function(k, v) {
			if (v.isNew) {
				hasNew = true;
			}
			var self = v.sender == userName ? true : false;
			appendMessage(v, self, true, false);
		});

		updateScroll("bottom");
	}
	
	if (hasNew) {
		preSend(userName, receiver, 6, '');
	}
}

function addCacheMessage(data, vkey, clear) {
	if (vkey == '')
	{
		return;
	}
	if (messageList[vkey]) {
		if (clear && messageList[vkey].length >= 10) {
			messageList[vkey].sort(compare);
			messageList[vkey].pop();
		}
		messageList[vkey].push(data);
	} else {
		messageList[vkey] = [data];
		setUserList(vkey, data);
	}
}

function preSend(userName, receiver, type, message) {
	var data = {
		sender: userName,
		receiver: receiver,
		type: type,
		message: message,
		sendTime: new Date().getTime(),
	}
	
	websocket.send(JSON.stringify(data));

	if (type == 1 || type == 2) {
		appendMessage(data, true, false, true);
		$('#message').val('');
	}
	
	loadMore = false;
}

function checkSend() {
	var message = $('#message').val();
	if (message && message != ''){
		message = $('<div/>').text(message).html();
		preSend(userName, receiver, 2, message);
	}
}

function imgShow(outerdiv, innerdiv, bigimg, _this){
	var src = _this.attr("src");
	$(bigimg).attr("src", src);

	$("<img/>").attr("src", src).load(function(){
		var windowW = $(window).width();
		var windowH = $(window).height();
		var realWidth = this.width
		var realHeight = this.height;
		var imgWidth, imgHeight;
		var scale = 1;
		
		if(realHeight > windowH*scale) {
			imgHeight = windowH*scale;
			imgWidth = imgHeight/realHeight*realWidth;
			if(imgWidth > windowW*scale) {
				imgWidth = windowW*scale;
			}
		} else if(realWidth > windowW*scale) {
			imgWidth = windowW*scale;
            imgHeight = imgWidth/realWidth*realHeight;
		} else {
			imgWidth = realWidth;
			imgHeight = realHeight;
		}
        $(bigimg).css("width",imgWidth);
		
		var w = (windowW-imgWidth)/2;
		var h = (windowH-imgHeight)/2;
		$(innerdiv).css({"top":h, "left":w});
		$(outerdiv).fadeIn("fast");
	});
	
	$(outerdiv).click(function(){
		$(this).fadeOut("fast");
	});
}

function reconnect() {
	if (++reconnectTime > 20) {
		alert('重连次数过多，请重新登入');
		return;
	}
	$.get("index.php?m=member&c=chat&a=index&ajax=1", function(result){
		console.log(result);
		if (!result.error) {
			auth = result.ticket;
			websocket.connect(function(res) {
				if (!res) {
					alert('重连失败，点击确定重试！');
				}
			});
		} else {
			alert('连接已断开！');
		}
	}, 'json');
}

function resortUserList() {
	$.each($('#user-list>li'), function() {
		var unode = $(this).find('em');
		if (unode.css('display') != 'none') {
			$('#user-list').prepend($(this));
		}
	});
}

var websocket;
(function(ws){
	var connect = false;
	var socket;
	ws.connect = function (_call_back) {
	if (socket) {
		socket.close();
	}
	socket = new WebSocket(serverHost + '?auth=' + auth); 
        socket.onopen = function(evt) { 
		heartBeat.reset();
		connect = true;
		_call_back(true);
        }; 
        socket.onclose = function(evt) { 
		connect = false;
		console.log('Connection lost !!');
		reconnect();
        }; 
        socket.onmessage = function(evt) { 
            	ws.onMessage(evt.data);
        }; 
        socket.onerror = function(evt) { 
            	connect = false;
		console.log('Connection lost2 !!');
		_call_back(false);
        }; 
	}
	
	ws.send = function(data) {
		if (!connect) {
			heartBeat.end();
			throw(new Error("No Connection!!!"));
		}

		socket.send(data);
		heartBeat.reset();
	}

	ws.onMessage = function(data) {
		var msg = JSON.parse(data);
		if (msg.type == 4 || msg.type == 5) {
			var message = msg.message;
			if (message) {
				message = JSON.parse(message);
				$.each(message, function(k, v) {
					var self = v.sender == userName ? true : false;
					appendMessage(v, self, true, true);
				});
				if (msg.type == 5) {
					//$('.loading').hide();
					if (scrollPosition) {
						//updateScroll("#" + scrollPosition);
					}
				} else {
					showNotice();
					updateScroll("bottom");
					resortUserList();
				}
			}
		} else {
			appendMessage(JSON.parse(data), false, false, true);
		}
	}

	var heartBeat = {
		interval: 50000,
		timer: null,
		reset: function() {
			this.end();
			this.start();
		},
		start: function() {
			this.timer = setInterval(function() {
				ws.send('@heart');
			}, this.interval)
		},
		end: function() {
			if (this.timer) {
				clearInterval(this.timer);	
			}
		}
	}
})(websocket || (websocket = {}))

var cat = window.cat || {};  
cat.touchjs = {  
    left: 0,  
    top: 0,  
    scaleVal: 1,    //缩放  
    rotateVal: 0,   //旋转  
    curStatus: 0,   //记录当前手势的状态, 0:拖动, 1:缩放, 2:旋转  
    //初始化  
    init: function ($targetObj, callback) {  
        touch.on($targetObj, 'touchstart', function (ev) {  
            cat.touchjs.curStatus = 0;  
            ev.preventDefault();//阻止默认事件  
        });  
        if (!window.localStorage.cat_touchjs_data)  
            callback(0, 0, 1, 0);  
        else {  
            var jsonObj = JSON.parse(window.localStorage.cat_touchjs_data);  
            cat.touchjs.left = parseFloat(jsonObj.left), cat.touchjs.top = parseFloat(jsonObj.top), cat.touchjs.scaleVal = parseFloat(jsonObj.scale), cat.touchjs.rotateVal = parseFloat(jsonObj.rotate);  
            callback(cat.touchjs.left, cat.touchjs.top, cat.touchjs.scaleVal, cat.touchjs.rotateVal);  
        }  
    },  
    //拖动  
    drag: function ($targetObj, callback) {  
        touch.on($targetObj, 'drag', function (ev) {  
            $targetObj.css("left", cat.touchjs.left + ev.x).css("top", cat.touchjs.top + ev.y);  
        });  
        touch.on($targetObj, 'dragend', function (ev) {  
            cat.touchjs.left = cat.touchjs.left + ev.x;  
            cat.touchjs.top = cat.touchjs.top + ev.y;  
            callback(cat.touchjs.left, cat.touchjs.top);  
        });  
    },  
    //缩放  
    scale: function ($targetObj, callback) {  
        var initialScale = cat.touchjs.scaleVal || 1;  
        var currentScale;  
        touch.on($targetObj, 'pinch', function (ev) {  
            if (cat.touchjs.curStatus == 2) {  
                return;  
            }  
            cat.touchjs.curStatus = 1;  
            currentScale = ev.scale - 1;  
            currentScale = initialScale + currentScale;  
			if (currentScale <= 0.5) {
				currentScale = 0.5;
			}
            cat.touchjs.scaleVal = currentScale;  
            var transformStyle = 'scale(' + cat.touchjs.scaleVal + ') rotate(' + cat.touchjs.rotateVal + 'deg)';  
            $targetObj.css("transform", transformStyle).css("-webkit-transform", transformStyle);  
            callback(cat.touchjs.scaleVal);  
        });  
  
        touch.on($targetObj, 'pinchend', function (ev) {  
            if (cat.touchjs.curStatus == 2) {  
                return;  
            }  
            initialScale = currentScale;  
            cat.touchjs.scaleVal = currentScale;  
            callback(cat.touchjs.scaleVal);  
        });  
    },  
    //旋转  
    rotate: function ($targetObj, callback) {  
        var angle = cat.touchjs.rotateVal || 0;  
        touch.on($targetObj, 'rotate', function (ev) {  
            if (cat.touchjs.curStatus == 1) {  
                return;  
            }  
            cat.touchjs.curStatus = 2;  
            var totalAngle = angle + ev.rotation;  
            if (ev.fingerStatus === 'end') {  
                angle = angle + ev.rotation;  
            }  
            cat.touchjs.rotateVal = totalAngle;  
            var transformStyle = 'scale(' + cat.touchjs.scaleVal + ') rotate(' + cat.touchjs.rotateVal + 'deg)';  
            $targetObj.css("transform", transformStyle).css("-webkit-transform", transformStyle);  
            $targetObj.attr('data-rotate', cat.touchjs.rotateVal);  
            callback(cat.touchjs.rotateVal);  
        });  
    }
};  
