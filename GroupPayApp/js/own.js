
(function(own){
	
	//当页面hide的时候将其中的页面close掉
	own.closeChildWebviewOfhide = function(currentWebview, closedWebviewId){
		currentWebview.addEventListener('hide',function(){
			
			var closeWeb = plus.webview.getWebviewById(closedWebviewId);
			
			if (!currentWebview.getURL() ||!closeWeb ) {
				return;
			}
			closeWeb.close();
			closeWeb = null;
		},false);
	}
	//当页面close的时候将其中的页面close掉
	own.closeChildWebviewOfclose = function(currentWebview,closedWebviewId){
		currentWebview.addEventListener('close', function(){
			var closeWeb = plus.webview.getWebviewById(closedWebviewId);
			if (!currentWebview.getURL() ||!closeWeb ) {
				return;
			}
			closeWeb.close();
			closeWeb = null;
		},false);
	}
	
	//一般情况下设置anishow
	own.getaniShow = function(){
		var aniShow = 'pop-in';
		if (mui.os.android) {
			var androidlist = document.querySelectorAll('ios-only');
			if (androidlist) {
				mui.each(androidlist,function(num,obj){
					obj.style.display = 'none';
				});
			}
			
			if (parseFloat(mui.os.version) < 4.4) {
				aniShow = 'slide-in-right';
			}
		}
		
		return aniShow;
	}
	
	//检查手机号码格式
	own.isMobleFormat = function(mobile) {
		var reg = /^13[\d]{9}$|^14[5,7]{1}\d{8}$|^15[^4]{1}\d{8}$|^17[0,6,7,8]{1}\d{8}$|^18[\d]{9}$|^19[\d]{9}$/;
		return reg.test(mobile);
	}
	
	//图片转base64
	own.getBase64Image = function(img) {
	    var canvas = document.createElement("canvas");
	    canvas.width = img.width;
	    canvas.height = img.height;
	    var ctx = canvas.getContext("2d");
	    ctx.drawImage(img, 0, 0, img.width, img.height);
	    var ext = img.src.substring(img.src.lastIndexOf(".") + 1).toLowerCase();
	    var dataURL = canvas.toDataURL("image/" + ext);
	    return dataURL;
	}
	
	//检查银行卡号
	own.isBankCard = function(card) {
		var reg = /^\d{16}|\d{19}$/;
		return reg.test(card);
	}
	
	//
	own.formatTime = function(s) {
        var t;
        if(s > -1){
            var hour = Math.floor(s/3600);
            var min = Math.floor(s/60) % 60;
            var sec = s % 60;
            if(hour < 10) {
                t = '0'+ hour + ":";
            } else {
                t = hour + ":";
            }

            if(min < 10){
            	t += "0";
            }
            
            t += min + ":";
            if(sec < 10){
            	t += "0";
            }
            
            t += sec;
        }
        return t;
    }
	
	//参数copy是要复制的文本内容
	own.copy_fun = function(copy){
		mui.plusReady(function(){
			//判断是安卓还是ios
			if(mui.os.ios){
				//ios
				var UIPasteboard = plus.ios.importClass("UIPasteboard");
			    var generalPasteboard = UIPasteboard.generalPasteboard();
			    //设置/获取文本内容:
			    generalPasteboard.plusCallMethod({
			        setValue:copy,
			        forPasteboardType: "public.utf8-plain-text"
			    });
			    generalPasteboard.plusCallMethod({
			        valueForPasteboardType: "public.utf8-plain-text"
			    });
			    mui.toast("已成功复制到剪贴板");
			}else{
				//安卓
				var context = plus.android.importClass("android.content.Context");
				var main = plus.android.runtimeMainActivity();
				var clip = main.getSystemService(context.CLIPBOARD_SERVICE);
				plus.android.invoke(clip,"setText",copy);
				mui.toast("已成功复制到剪贴板");
			}
		});
	}
	
	own.CovertToDateTime = function(timestamp){
		var dateTime = new Date(timestamp);
		var h = dateTime.getHours();
		h = h>10 ? h : "0"+h;
		var m = dateTime.getMinutes();
		m = m>10 ? m : "0"+m;
		var s = dateTime.getSeconds();
		s = s>10 ? s : "0"+s;
		return h+":"+m+":"+s;
	};
})(window);
