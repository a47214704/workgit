function countDown() {
    countDownSec--;
    var hour = parseInt(countDownSec / 3600);
    var min = parseInt((countDownSec - hour * 3600) / 60);
    var sec = countDownSec - hour * 3600 - min * 60;
    $("#hour").html(hour >= 10 ? hour : "0" + hour);
    $("#min").html(min >= 10 ? hour : "0" + min);
    $("#sec").html(sec >= 10 ? sec : "0" + sec);
    if (countDownSec > 0) {
        setTimeout(countDown, 1000);
    } else {
        $("#error").html("订单已过期");
        $("#error").show();
        $("#QrCode").hide();
    }
}

function sendPayment(self, callback) {
    $.ajax({
        url: '/api/pay/PaymentCache?appkey=' + getUrlParam("appkey") + "&id=" + getUrlParam("Id"),
        method: 'GET'
    })
    .done(function (params) {
        var data = params.result;
        self.set('amount', data.amount / 100);
        self.set('mrn', data.merchantReferenceNumber);
        self.set('callBackUrl', "window.location='" + data.callBackUrl + "'");

        self.set('message', '二维码生成中，请稍后');
        callback(self, data);
    });
}

function initAliScanWapPage(errorMsg) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            amount: 0,
            message: "加载中...."
        },
        oncomplete: function () {
            if (errorMsg != '') {
                $("#loadGif").hide();
                this.set('error', true);
                this.set('message', errorMsg);
            } else {
                sendPayment(this, function (that, params) {
                    $.ajax({
                        url: '/api/payment',
                        method: 'POST',
                        headers: {
                            'x-signature': params.sign,
                            'x-app-key': params.appKey,
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            Amount: params.amount,
                            Channel: 7,
                            MerchantReferenceNumber: params.merchantReferenceNumber,
                            NotifyUrl: params.notifyUrl,
                            CallBackUrl: params.callBackUrl
                        })
                    })
                    .done(function (data) {
                        that.set('amount', data.result.amount / 100);
                        $("#QRCode").qrcode(window.location.origin + "/api/Pay/AliScanWap/" + params.MerchantReferenceNumber);
                        $("#Loading").hide();
                        $("#ImportantWarning").show();
                    })
                    .fail(function (jqXHR) {
                        that.set('message', '发生错误，请截屏询问平台客服。' + jqXHR.responseText);
                        $("#loadGif").hide();
                    });
                });
            }
        }
    });
}

function initAliScanPage(errorMsg) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            amount: 0,
            message: "加载中...."
        },
        oncomplete: function () {
            if (errorMsg != '') {
                $("#loadGif").hide();
                this.set('error', true);
                this.set('message', errorMsg);
                this.set('callBackUrl', "javascript:history.go(-2);");
            } else {
                sendPayment(this, function (that, params) {
                    $.ajax({
                        url: '/api/payment',
                        method: 'POST',
                        headers: {
                            'x-signature': params.sign,
                            'x-app-key': params.appKey,
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            Amount: params.amount,
                            Channel: 2,
                            MerchantReferenceNumber: params.merchantReferenceNumber,
                            NotifyUrl: params.notifyUrl,
                            CallBackUrl: params.callBackUrl
                        })
                    })
                    .done(function (data) {
                        that.set('amount', data.result.amount / 100);
                        that.set("qrCode", data.result.qrCodeUrl);
                        that.set("accountName", data.result.accountName);
                        $("#Loading").hide();
                        $("#ImportantWarning").show();
                        countDown();
                    })
                    .fail(function (jqXHR) {
                        that.set('message', '发生错误，请截屏询问平台客服。' + jqXHR.responseText);
                        $("#loadGif").hide();
                    });
                });

                var Cp = new ClipboardJS('.cp');
                Cp.on('success', function (e) {
                    console.log(e);
                    alert("已复制订单金额：" + e.text);
                });

                Cp.on('error', function (e) {
                    console.log(e);
                });
            }
        }
    });
}

function initWechatWapPage(errorMsg) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            amount: 0,
            message: "加载中...."
        },
        oncomplete: function () {
            if (errorMsg != '') {
                $("#loadGif").hide();
                this.set('error', true);
                this.set('message', errorMsg);
                this.set('callBackUrl', "javascript:history.go(-2);");
            } else {
                sendPayment(this, function (that, params) {
                    $.ajax({
                        url: '/api/payment',
                        method: 'POST',
                        headers: {
                            'x-signature': params.sign,
                            'x-app-key': params.appKey,
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            Amount: params.amount,
                            Channel: 8,
                            MerchantReferenceNumber: params.merchantReferenceNumber,
                            NotifyUrl: params.notifyUrl,
                            CallBackUrl: params.callBackUrl
                        })
                    })
                    .done(function (data) {
                        that.set('amount', data.result.amount / 100);
                        $("#QRCode").qrcode(window.location.origin + "/api/Pay/WechatWap/" + params.MerchantReferenceNumber);
                        $("#Loading").hide();
                        $("#ImportantWarning").show();
                    })
                    .fail(function (jqXHR) {
                        that.set('message', '发生错误，请截屏询问平台客服。' + jqXHR.responseText);
                        $("#loadGif").hide();
                    });
                });
            }
        }
    });
}

function initWechatPage(errorMsg) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            amount: 0,
            message: "加载中...."
        },
        oncomplete: function () {
            if (errorMsg != '') {
                $("#loadGif").hide();
                this.set('error', true);
                this.set('message', errorMsg);
                this.set('callBackUrl', "javascript:history.go(-2);");
            } else {
                var Cp = new ClipboardJS('.cp');
                Cp.on('success', function (e) {
                    console.log(e);
                    alert("已复制订单金额：" + e.text);
                });

                Cp.on('error', function (e) {
                    console.log(e);
                });

                sendPayment(this, function (that, params) {
                    $.ajax({
                        url: '/api/payment',
                        method: 'POST',
                        headers: {
                            'x-signature': params.sign,
                            'x-app-key': params.appKey,
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            Amount: params.amount,
                            Channel: 1,
                            MerchantReferenceNumber: params.merchantReferenceNumber,
                            NotifyUrl: params.notifyUrl,
                            CallBackUrl: params.callBackUrl
                        })
                    })
                    .done(function (data) {
                        that.set('amount', data.result.amount / 100);
                        that.set("qrCode", data.result.qrCodeUrl);
                        that.set("accountName", data.result.accountName);
                        $("#Loading").hide();
                        $("#ImportantWarning").show();
                        countDown();
                    })
                    .fail(function (jqXHR) {
                        that.set('message', '发生错误，请截屏询问平台客服。' + jqXHR.responseText);
                        $("#loadGif").hide();
                    });
                });
            }
        }
    });
}

function initBankPage(errorMsg) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            amount: 0,
            message: "加载中...."
        },
        oncomplete: function () {
            if (errorMsg != '') {
                $("#loadGif").hide();
                this.set('error', true);
                this.set('message', errorMsg);
            } else {
                var Cpa = new ClipboardJS('.cpa');
                Cpa.on('success', function (e) {
                    console.log(e);
                    alert("已复制姓名：" + e.text);
                });

                Cpa.on('error', function (e) {
                    console.log(e);
                });

                var Cpb = new ClipboardJS('.cpb');
                Cpb.on('success', function (e) {
                    console.log(e);
                    alert("已复制订单金额：" + e.text);
                });

                Cpb.on('error', function (e) {
                    console.log(e);
                });

                var Cpc = new ClipboardJS('.cpc');
                Cpc.on('success', function (e) {
                    console.log(e);
                    alert("已复制卡号：" + e.text);
                });

                Cpc.on('error', function (e) {
                    console.log(e);
                });

                sendPayment(this, function (that, params) {
                    $.ajax({
                        url: '/api/payment',
                        method: 'POST',
                        headers: {
                            'x-signature': params.sign,
                            'x-app-key': params.appKey,
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            Amount: params.amount,
                            Channel: 3,
                            MerchantReferenceNumber: params.merchantReferenceNumber,
                            NotifyUrl: params.notifyUrl,
                            CallBackUrl: params.callBackUrl
                        })
                    })
                    .done(function (data) {
                        that.set('amount', data.result.amount / 100);
                        that.set('accountName', data.result.accountName);
                        that.set('bankCard', data.result.accountNumber);
                        that.set('bankName', data.result.bankName);
                        $("#Loading").hide();
                        $("#mainContent").show();
                    })
                    .fail(function (jqXHR) {
                        that.set('message', '发生错误，请截屏询问平台客服。' + jqXHR.responseText);
                        $("#loadGif").hide();
                    });
                });
            }
        }
    });
}

function initAliBankRedirectPage(errorMsg) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            amount: 0,
            message: "加载中...."
        },
        oncomplete: function () {
            if (errorMsg != '') {
                $("#loadGif").hide();
                this.set('error', true);
                this.set('message', errorMsg);
            } else {
                var Cp = new ClipboardJS('.cp');
                Cp.on('success', function (e) {
                    console.log(e);
                    alert('复制成功\n充值流程\n   一、打开支付宝，将该信息发给任意好友\n   二、点击发送的信息进行支付');
                });

                Cp.on('error', function (e) {
                    console.log(e);
                });

                sendPayment(this, function (that, params) {
                    $.ajax({
                        url: '/api/payment',
                        method: 'POST',
                        headers: {
                            'x-signature': params.sign,
                            'x-app-key': params.appKey,
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            Amount: params.amount,
                            Channel: 6,
                            MerchantReferenceNumber: params.merchantReferenceNumber,
                            NotifyUrl: params.notifyUrl,
                            CallBackUrl: params.callBackUrl
                        })
                    })
                    .done(function (data) {
                        that.set('amount', data.result.amount / 100);
                        that.set("payUrl", "订单金额" + data.result.amount / 100 + ", 付款链接: " + window.location.origin + "/api/Pay/AliBank/" + params.MerchantReferenceNumber);
                        $("#Loading").hide();
                        $("#mainContent").show();
                    })
                    .fail(function (jqXHR) {
                        that.set('message', '发生错误，请截屏询问平台客服。' + jqXHR.responseText);
                        $("#loadGif").hide();
                    });
                });
            }
        }
    });
}

function initPayMenuPage(key, amount, callBackUrl) {
    const keys = ['Wechat', 'Ali', 'Card', 'AliRed', 'UBank', 'AliToCard', 'AliWap', 'WechatWap', 'AliH5', 'WechatH5'];
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            channel: [],
            channelLimit: [],
            callBackUrl: callBackUrl
        },
        oncomplete: function () {
            $.ajax({
                url: '/api/Pay/Menu/' + key,
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    data.result.channelEnabledList.forEach(function (channel, index) {
                        if (channel) {
                            if (data.result.channelLimit[keys[index]].item1 > amount || data.result.channelLimit[keys[index]].item2 < amount) {
                                data.result.channelEnabledList[index] = false;
                            }
                        }
                    });
                    this.set('channel', data.result.channelEnabledList);
                    this.set('channelLimit', data.result.channelLimit);
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}