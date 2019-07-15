/**
 * 判断应用升级模块，从url地址下载升级描述文件到本地local路径
 * yanyilin@dcloud.io
 * 
 * 升级文件为JSON格式数据，如下：
{
	"appid":"HelloH5",
    "iOS":{
    	"version":"iOS新版本号，如：1.0.0",
    	"note":"iOS新版本描述信息，多行使用\n分割",
    	"url":"Appstore路径，如：itms-apps://itunes.apple.com/cn/app/hello-h5+/id682211190?l=zh&mt=8"
    },
    "Android":{
    	"version":"Android新版本号，如：1.0.1",
    	"note":"Android新版本描述信息，多行使用\n分割",
    	"url":"apk文件下载地址，如：http://www.dcloud.io/helloh5p/HelloH5.apk"
    }
}
 *
 */
//var server="http://pay.hs9088.net/update.json",//获取升级描述文件服务器地址
var server="https://pay.tjbapp.com/update.json",//获取升级描述文件服务器地址
//var server="http://192.168.50.223/update.json",//获取升级描述文件服务器地址
dir=null;
const curVer = "1.5.19";
/**
 * 准备升级操作
 * 创建升级文件保存目录
 */
function initUpdate(cb){		
	/**
	 * 从服务器获取升级数据
	 */
	function getUpdateData(){
		loadMessage.innerHTML = "取得最新版本";
		var xhr = new plus.net.XMLHttpRequest();
		xhr.onreadystatechange = function () {
	        switch ( xhr.readyState ) {
	            case 4:
	                if ( xhr.status == 200 ) {
	                	checkUpdateData(JSON.parse(xhr.responseText));
	                } else {
	                	loadMessage.innerHTML = "获取升级数据，联网请求失败："+xhr.status;
	                }
	                break;
	            default :
	                break;
	        }
		}
		xhr.open( "GET", server );
		xhr.send();
	}
	
	/**
	 * 检查升级数据
	 */
	function checkUpdateData(serverVersion){
		loadMessage.innerHTML = "比对本地版本";
		
		var inf = serverVersion[plus.os.name];
		if(inf){
			var srvVer = inf.version;
			loadMessage.innerHTML = "判断是否需要升级";
			if ( compareVersion(curVer,srvVer) ) {
				plus.nativeUI.confirm( inf.note, function(i){
					if ( 0==i.index ) {
						var options = {method:"GET"};
	 					var dtask = plus.downloader.createDownload( inf.url, options );
	 					dtask.addEventListener( "statechanged", function(task,status){             
					    switch(task.state) {
					            case 1: // 开始
					                loadMessage.innerHTML = "开始下载...";
					            break;
					            case 2: // 已连接到服务器
					                loadMessage.innerHTML = "链接到服务器...";
					            break;
					            case 3: // 已接收到数据                                
					                var a = Math.floor(task.downloadedSize/task.totalSize*100);
					                loadMessage.innerHTML = "下载完成..." + a + "%";
					                loadProgress.value=a;
					            break; 
					            case 4: // 下载完成
					                loadMessage.innerHTML = "下载完成！";                                                                      
					                install (task);
					            break;
					        }
						});
						dtask.start(); 
					}
				}, inf.title, ["立即更新"] );
			}else{
				cb();
			}
		}
	}
	
	function install (task){
	  	plus.runtime.install(task.filename, {force:true}, function() {
	    //完成更新向服务器进行通知
			loadMessage.innerHTML = "更新完毕，将重启应用！";
	    	plus.runtime.restart();
	 	},function(err){
		    alert(JSON.stringify(err));
		    mui.toast("安装升级失败");
		});
	}
	
	
	/**
	 * 比较版本大小，如果新版本nv大于旧版本ov则返回true，否则返回false
	 * @param {String} ov
	 * @param {String} nv
	 * @return {Boolean} 
	 */
	function compareVersion( ov, nv ){
		if ( !ov || !nv || ov=="" || nv=="" ){
			return false;
		}
		var b=false,
		ova = ov.split(".",4),
		nva = nv.split(".",4);
		for ( var i=0; i<ova.length&&i<nva.length; i++ ) {
			var so=ova[i],no=parseInt(so),sn=nva[i],nn=parseInt(sn);
			if ( nn>no || sn.length>so.length  ) {
				return true;
			} else if ( nn<no ) {
				return false;
			}
		}
		if ( nva.length>ova.length && 0==nv.indexOf(ov) ) {
			return true;
		}
	}
	
	// 在流应用模式下不需要检测升级操作	
	if(navigator.userAgent.indexOf('StreamApp')>=0){
		return;
	}
	
	var loadMessage = document.getElementById("loadMessage");
	var loadProgress = document.getElementById("loadProgress");
	getUpdateData();
}
