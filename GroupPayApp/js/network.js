var new_element=document.createElement("script");
new_element.setAttribute("type","text/javascript");
new_element.setAttribute("src","../js/md5.js");
document.body.appendChild(new_element);

var load_element=document.createElement("script");
load_element.setAttribute("type","text/javascript");
load_element.setAttribute("src","../js/load.js");
document.body.appendChild(load_element);

var httpUrl = "http://pay.hs9088.net/";
var wsUrl = "ws://pay.hs9088.net/ws/paysrv";
var rechargeUrl = "http://recharge.hs9088.net/"

//var httpUrl = "https://pay.tjbapp.com/";
//var wsUrl = "wss://pay.tjbapp.com/ws/paysrv";

//var httpUrl = "http://192.168.50.223:5000/";
//var wsUrl = "ws://192.168.50.223:5000/ws/paysrv";

function getTimestamp(){
	return (Date.parse(new Date())/1000).toString();
}

function getToken() {
	var token = localStorage.getItem('token');
	if (token) {
		return token;
	} else {
		if (window.plus) {
			token = plus.device.uuid;
		} else {
			token = getTimestamp() + '_' + Math.random();
		}
		
		token = hex_md5(token);
		localStorage.setItem('token', token);
		return token;
	}
}

function jsonData(data) {
	if (data != '' && typeof(data) == 'string') {
		return JSON.parse(data);
	}
	
	return data;
}

function http_query_build(data){
	var esc = encodeURIComponent;
 	return Object.keys(data)
    .map(k => esc(k) + '=' + esc(data[k]))
    .join('&');
}

function getHeader(auth) {
	if (auth) {
		return {
			'Content-Type':'application/json',
			'Authentication':'UserToken ' + localStorage.getItem('userToken')
		};
	} else {
		return {'Content-Type':'application/json'};
	}
}

var network;
(function(net){
	//网路测试
	net.networkTest = function(){
		mui.ajax(httpUrl + '/cdn-cgi/trace', {
			type: 'GET',
			timeout: 10000,
			headers: getHeader(false),
			success: function(data) {  
				networkTestSucess(data);
			},
			error: function(xhr, textStatus, errorThrown) {
				networkTestSucess("连线失败!");
			}
		});
	};
	//用户注册
	net.register = function (options) {
		var data = {
			accountName: options.account,
			password: options.password,
		};
		mui.ajax(httpUrl + 'api/User?token=' + getToken() + '&captcha=' + options.captcha, {
			data: JSON.stringify(data),
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(false),
			success: function(data) {
				console.log(data);  
				registerSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				if (xhr.status == 400) {
					mui.toast('验证码错误');
				} else if(xhr.status == 409) {
					mui.toast('该手机号码已被注册');
				} else {
					mui.toast('账号注册失败')
				}
			}
		});
	};
	
	//用户登陆
	net.login = function(options){
		var data = {
			accountName: options.account,
			password: options.password,
		};
		mui.ajax(httpUrl + 'api/User/Login?token=' + getToken() + '&captcha=' + options.captcha, {
			data: JSON.stringify(data),
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(false),
			success: function(data) {
				console.log(data);  
				loginSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				var response = JSON.parse(xhr.response);
				if (xhr.status == 400) {
					mui.toast('验证码错误');
					loginError(400);
				} else {
					if(response.errorMessage == "user account unready verified"){
						mui.toast('账号尚未启用，请联系客服');
					} else {
						mui.toast('账号或密码错误');	
					}
				}
			}
		});
	};
	
	//获取用户信息
	net.get_user_info = function(uid) {
		mui.ajax(httpUrl + 'api/User/' + uid, {
			dataType:'JSON',
			type:'GET',
			timeout:10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				userInfoSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				userInfoError(xhr.status);
				console.log(xhr.status + ',' + xhr.responseText);
			}
		});
	};
	
	//获取密保问题
	net.security_question = function() {
		mui.ajax(httpUrl + 'api/SecurityQuestion', {
			dataType:'JSON',
			type:'GET',
			timeout:10000,
			success:function(data){
				console.log(data);
				getQuestionSuccess(jsonData(data));
			},
			error:function(xhr,type,errorThrown){
				mui.toast('获取密保问题出错');
			}
		});
	};
	
	//设置密保
	net.save_question_answer = function(uid, data) {
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+uid+'/SecurityAnswers', {
			data: JSON.stringify(data),
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);  
				endLoad();
				saveAnswerSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				endLoad();
				mui.toast('信息保存失败');
			}
		});
	};
	
	//修改密码
	net.change_password = function(uid, data) {
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+uid+'/UpdatePassword', {
			data: JSON.stringify(data),
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);  
				endLoad();
				changePasswordSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				endLoad();
				mui.toast('修改失败，旧密码错误');
			}
		});
	};
	
	//获取收款方式
	net.get_collect_instrument = function() {
		startLoad();
		mui.ajax(httpUrl + 'api/CollectInstrument', {
			dataType:'JSON',
			type:'GET',
			timeout:10000,
			headers: getHeader(true),
			success:function(data){
				console.log(data);
				endLoad();
				getCollectInstrumentSuccess(jsonData(data));
			},
			error:function(xhr,type,errorThrown){
				mui.toast('获取收款方式时发生错误');
				endLoad();
			}
		});
	};
	
	//删除收款方式
	net.del_collect_instrument = function(data) {
		mui.ajax(httpUrl + 'api/CollectInstrument/' + data.id, {
			dataType:'JSON',
			type:'DELETE',
			timeout:10000,
			headers: getHeader(true),
			success:function(data){
				delCollectInstrumentSuccess(jsonData(data));
			},
			error:function(xhr,type,errorThrown){
				mui.toast('删除失败');
			}
		});
	};
	
	//获取收款通道
	net.get_collect_channel = function() {
		mui.ajax(httpUrl + 'api/CollectChannel', {
			dataType:'JSON',
			type:'GET',
			timeout:10000,
			headers: getHeader(true),
			success:function(data){
				console.log(data);
				getChannelSuccess(jsonData(data));
			},
			error:function(xhr,type,errorThrown){
				mui.toast('获取收款通道时发生错误');
			}
		});
	};
	
	//保存收款方式
	net.save_collect_instrument = function(data) {
		startLoad();
		mui.ajax(httpUrl + 'api/CollectInstrument', {
			data: JSON.stringify(data),
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);  
				endLoad();
				saveCollectInstrumentSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				var errorStatus = JSON.parse(xhr.responseText); 
				switch(errorStatus.errorMessage)
				{
					case "Instrument of channel already  full":
						mui.toast('保存失败，已达可以添加账户上限');
						break;
					default:
						mui.toast('保存失败，请检查你的输入信息');
						break;
				}
				endLoad();
			}
		});
	};
	
	//获取支付订单
	net.get_order_list = function(status) {
		mui.ajax(httpUrl + 'api/Payment?status=' + status, {
			dataType:'JSON',
			type:'GET',
			timeout:10000,
			headers: getHeader(true),
			success:function(data){
				console.log(data);
				getOrderListSuccess(jsonData(data));
			},
			error:function(xhr,type,errorThrown){
				mui.toast('获取确认订单时发生错误');
			}
		});
	};
	
	//确认付款
	net.order_settle = function(id) {
		startLoad();
		mui.ajax(httpUrl + 'api/Payment/'+id+'/Settle', {
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);  
				endLoad();
				orderSettleSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				endLoad();
				mui.toast('发放确认失败');
			}
		});
	};
	
	//获取团队资讯
	net.get_team_info = function(userId) {
		startLoad();
		var page = 0;
		var pageToken = "";
		mui.ajax(httpUrl + 'api/User/'+userId+'/Team' ,{
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				endLoad();
				teamInfoSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				endLoad();
				mui.toast('获取下级用户失败');
			}
		});
	}
	
	//获取下级用户列表
	net.get_child_user_list = function(agentId, isAgent) {
		startLoad();
		var action = isAgent ? "Agents" : "Members";
		var page = 0;
		var pageToken = "";
		mui.ajax(httpUrl + 'api/User/'+agentId+'/' + action + '?' + 'page=' + page + "&pageToken" + pageToken ,{
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				endLoad();
				childUserListSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				endLoad();
				mui.toast('获取下级用户失败');
			}
		});
	};
	
	//获取业绩信息
	net.get_revenue = function(userId){
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+userId+'/Revenue', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				endLoad();
				getRevenueSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				endLoad();
				mui.toast('获取业绩资料失败');
			}
		});
	}
	
	//获取可提现佣金
	net.get_balance = function(userId){
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+userId+'/CommissionBalance', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				endLoad();
				getBalanceSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				getBalanceSuccess(xhr);
			}
		});
	}
	
	//获取佣金级距
	net.get_balanceRank = function(){
		startLoad();
		mui.ajax(httpUrl + 'api/CommissionRatio', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				endLoad();
				getBalanceRankSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				endLoad();
				mui.toast('获取佣金等级资料失败');
			}
		});
	}
	
	//取得推广地址
	net.get_share_url = function(userId){
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+userId+'?promotionUrl=true', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				endLoad();
				getShareUrlSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);
				endLoad();
				mui.toast('获取推广地址失败');
			}
		});
	}
	
	//取得跑马灯资料
	net.get_marquee = function(){
		mui.ajax(httpUrl + 'api/SiteConfig/marquee', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				getMarqueeSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);	
				mui.toast('获取跑马灯资料失败');
			}
		});
	};
	
	//佣金提现
	net.reward_commission = function(userId){
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+ userId +'/CashCommission', {
			dataType: 'JSON',
			type: 'POST',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				endLoad();
				console.log(data);
				rewardCommissionSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);				
				endLoad();
				mui.toast('提现失败');
			}
		});
	};
	
	//获取提现记录
	net.reward_commission_records = function(userId){
		startLoad();
		mui.ajax(httpUrl + 'api/User/'+ userId +'/CommissionCashRecords', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				endLoad();
				console.log(data);
				getRewardListSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);				
				endLoad();
				mui.toast('获取提现记录失败');
			}
		});
	};
	
	//获取收款通道利率
	net.collect_ratio = function(){
		startLoad();
		mui.ajax(httpUrl + 'api/CollectChannel/Ratio', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				endLoad();
				console.log(data);
				getCollectRatioSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);				
				endLoad();
				mui.toast('获取收款通道利率失败');
			}
		});
	};
	
	//验证码图片
	net.captcha_url = function() {
		return httpUrl + 'Home/CaptchaImage?token=' + getToken();
	};
	
	//取得微信客服
	net.get_customer_service_wechat = function() {
		mui.ajax(httpUrl + 'api/SiteConfig/wxkf', {
			dataType: 'JSON',
			type: 'GET',
			timeout: 10000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				getCustomerServiceWechatSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);		
				mui.toast('获取客服资料失败');
			}
		});
	};
	
	//获取充值信息
	net.get_payment_infomation = function(payment){
		mui.ajax(httpUrl + 'api/pay/SelfRecharge', {
			dataType: 'JSON',
			data: JSON.stringify(payment),
			type: 'POST',
			timeout: 30000,
			headers: getHeader(true),
			success: function(data) {
				console.log(data);
				getPaymentInfomationSuccess(jsonData(data));
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);	
				var response = JSON.parse(xhr.responseText);
				switch(response.errorMessage)
				{
					case "fail amount":
						mui.toast('错误的订单金额，请输入大于零');
						break;
					default:
						mui.toast('提交充值信息失败');
						break;
				}
			}
		});
	};
	
	//发送充值信息
	net.send_payment_infomation = function(data){
		var headers = getHeader(true);
		headers["Content-Type"] = "application/x-www-form-urlencoded";
		headers["x-app-key"] = data.key;
		headers["x-signature"] = data.sign;
		var Payment = {
			Amount: data.payment.amount,
			Channel: data.payment.channel,
			MerchantReferenceNumber: data.payment.merchantReferenceNumber,
			NotifyUrl: data.payment.notifyUrl
		};
		mui.ajax(rechargeUrl + 'api/payment', {
			data: http_query_build(Payment),
			type: 'POST',
			timeout: 30000,
			headers: headers,
			success: function(data) {
				console.log(JSON.stringify(data));
				var paymentResult = {
					orderId: Payment.MerchantReferenceNumber,
					amount: Payment.Amount/100,
					name: data.result.accountName,
					bankNo: data.result.accountNumber,
					bankName: data.result.bankName
				};
				sendPaymentInfomationSuccess(paymentResult);
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);		
				mui.toast('提交充值信息失败');
				sendPaymentInfomationError();
			}
		});
	};
	
	//发送充值信息
	net.rechargeList_records = function(){
		mui.ajax(httpUrl + 'api/Recharge', {
			type: 'GET',
			timeout: 30000,
			headers: getHeader(true),
			success: function(data) {
				console.log(JSON.stringify(data));
				getRechargeListSuccess(data);
			},
			error: function(xhr, textStatus, errorThrown) {
				console.log(xhr.responseText);		
				mui.toast('查询充值信息失败');
				//sendPaymentInfomationError();
			}
		});
	};
	
	//websocket地址
	net.websocket_addr = function() {
		return wsUrl + '?_token=' + localStorage.getItem('userToken');
	};
})(network || (network = {}));