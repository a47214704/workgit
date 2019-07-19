function initUserListPage() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            keyword: '',
            roleId: 100,
            users: [],
            currentPage: 0,
            pages: [],
            message: '',
            pageToken: '',
            roles: [],
            merchants: [],
            totalRecords: 0,
            pageSummary: {
                balance: 0,
                pendingBalance: 0,
                award: 0,
                commission: 0
            },
            totalSummary: {
                balance: 0,
                pendingBalance: 0,
                award: 0,
                commission: 0
            }
        },
        oncomplete: function() {
            this._loadData();
        },
        on: {
            doSearch: function(ctx) {
                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function(ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            addCredit: function(ctx) {
                var credits = parseInt(prompt('请输入要加的分数，正数为加，负数为减:', "0"));

                if (isNaN(credits) || credits == 0) return;

                var userId = ctx.get("id");
                this.set('message', '正在提交请求');
                $.ajax({
                    url: '/api/User/' + userId + '/AddCredit',
                    type: 'POST',
                    data: "{credit:" + credits + "}",
                    contentType: "application/json",
                    context: this,
                    dataType: 'json'
                })
                .done(function(data){
                    if (data.error != 'success') {
                        this.set('message', '服务器返回错误:' + data.error);
                    } else {
                        this.set('message', '成功为' +userId + (credits > 0 ? '添加' : '减少') + credits + '分');
                        ctx.set('balance', data.result.balance);
                    }
                })
                .fail(function(xhr, status){
                    this.set('message', '加载错误:' + status);
                });
            },
            userEdit: function (ctx) {
                var that = this;
                var userId = ctx.get("id");
                jDialog.iframe('/Home/UserEdit?userId=' + userId, {
                    title: '用户编辑',
                    width: 450,
                    height: 350,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            addUser: function (ctx) {
                var that = this;
                jDialog.iframe('/Home/AddUser', {
                    title: '添加用户',
                    width: 450,
                    height: 410,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            addEvaluation: function (ctx) {
                var that = this;
                jDialog.iframe('/Home/AddEvaluation?userid='+ctx.get('id'), {
                    title: '修改服务分',
                    width: 600,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            addChildUser: function (ctx) {
                var that = this;
                var referrerId = ctx.get('promotionCode');
                jDialog.iframe('/Home/AddUser?referrerId=' + referrerId, {
                    title: '添加下级用户',
                    width: 450,
                    height: 410,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            childUserList: function (ctx) {
                var userId = ctx.get("id");
                window.location = '/home/ListChildUsers?userid=' + userId;
            },
            verifyUser: function (ctx) {
                var userId = ctx.get("id");
                var request = { Status: 1 };
                webApiCall(
                    '/api/User/' + userId,
                    'PUT',
                    this,
                    function (status, data) {
                        if (status < 400) {
                            this.set('message', '成功启用' + userId);
                            that._loadData();
                        } else {
                            this.set('message', '服务器返回错误:' + data.errorMessage);
                        }
                    }, request);
            }
        },
        _loadData: function() {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/UserRole',
                type: 'GET',
                context: this,
                dataType: 'json',
            }).done(function (data) {
                this.set('roles', data.result);
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });

            $.ajax({
                url: '/api/merchant',
                type: 'GET',
                context: this,
                dataType: 'json',
            }).done(function (data) {
                var merchants = data.result;
                merchants.push({ id: 0, name: "无指定" });
                this.set('merchants', merchants);
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });

            $.ajax({
                url: '/api/User?roleId=' + this.get('roleId') + '&status=' + this.get('status') + '&page=' + this.get('currentPage') + '&pageToken=' + this.get('pageToken') + '&accountName=' + this.get('keyword'),
                type: 'GET',
                context: this,
                dataType: 'json'
            })
            .done(function(data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    var dataPage = data.result.dataPage;
                    var summary = data.result.summary;
                    this.set('users', dataPage.records);
                    this.set('currentPage', dataPage.page);
                    this.set('pageToken', dataPage.pageToken);
                    this.set('totalRecords', dataPage.totalRecords);

                    if (summary != null) {
                        var pageSummary = {
                            balance: 0,
                            pendingBalance: 0,
                            award: 0,
                            commission: 0
                        };

                        var totalSummary = {
                            balance: 0,
                            pendingBalance: 0,
                            award: 0,
                            commission: 0
                        };

                        dataPage.records.forEach(function (record) {
                            pageSummary.balance += record.balance;
                            pageSummary.pendingBalance += record.pendingBalance;
                            pageSummary.award += record.award;
                            pageSummary.commission += record.commission;
                        });

                        this.set('pageSummary', pageSummary);
                        
                        summary.forEach(function (record) {
                            totalSummary.balance += record.balance;
                            totalSummary.pendingBalance += record.pendingBalance;
                            totalSummary.award += record.award;
                            totalSummary.commission += record.commission;
                        });

                        this.set('totalSummary', totalSummary);
                    } else {
                        this.set('totalSummary', null);
                        this.set('pageSummary', null);
                    }
                    
                    var totalPages = dataPage.totalPages;
                    var pages = [];
                    var startPage = 0;
                    if (dataPage.page > 2) {
                        startPage = dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i < totalPages; ++i) {
                            pages.push(i);
                        }
                    }
                        
                    this.set('pages', pages);
                    this.set('message', '');
                }
            })
            .fail(function(xhr, status){
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initPaymentListPage() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            channelType: ['-', '微信', '支付宝', '银行卡', '支付宝红包', '云闪付', '支付宝转银行卡', '支付宝wap', '微信wap', '支付宝H5', '微信H5'],
            orderId: '',
            platformOrderId: '',
            startTimeStamp: '',
            endTimeStamp: '',
            status: 0,
            channel: 0,
            accountName: '',
            payments: [],
            currentPage: 1,
            pages: [],
            totalRecords: 0,
            message: ''
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            reOrder: function (ctx) {
                var orderId = ctx.get("id");
                var that = this;
                jDialog.iframe('/Home/Reorder?orderId=' + orderId, {
                    title: '补单',
                    width: 450,
                    height: 430,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            createOrder: function () {
                var that = this;
                jDialog.iframe('/Home/CreateOrder', {
                    title: '新增订单',
                    width: 450,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Payment/ListPayments?page=' + this.get('currentPage') + '&orderId=' + this.get('orderId') + '&accountName=' + this.get('accountName') + '&platformOrderId=' + this.get('platformOrderId') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp') + '&status=' + this.get('status') + '&channel=' + this.get('channel'),
                type: 'GET',
                context: this,
                dataType: 'json'
            })
            .done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('payments', data.result.records);
                    this.set('currentPage', data.result.page);
                    this.set('totalRecords', data.result.totalRecords);
                    var totalPages = data.result.totalPages;
                    var pages = [];
                    var startPage = 1;
                    if (data.result.page > 2) {
                        startPage = data.result.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i <= totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            })
           .fail(function (xhr, status) {
                console.log(xhr.responseText);
                this.set('message', '加载错误:' + status);
            });

            this.set('message', '');
        }
    });
}

function initPaymentReport() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            accountName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('records', data.result.dataPage.records);
                    this.set('currentPage', data.result.dataPage.page);

                    var total = {
                        wechatAmount: 0, 
                        wechatWapAmount: 0,
                        wechatH5Amount: 0,
                        alipayAmount: 0,
                        aliWapAmount: 0,
                        aliH5Amount: 0,
                        unionpayAmount: 0,
                        aliToCardAmount: 0,
                        aliRedEnvelopeAmount: 0,
                        uBankAmount: 0
                    };
                    if (data.result.summary) {
                        for (var i = 0; i < data.result.summary.length; i++) {
                            switch (data.result.summary[i].channel) {
                                case 1:
                                    total.wechatAmount = data.result.summary[i].amount;
                                    break;
                                case 2:
                                    total.alipayAmount = data.result.summary[i].amount;
                                    break;
                                case 3:
                                    total.unionpayAmount = data.result.summary[i].amount;
                                    break;
                                case 4:
                                    total.aliRedEnvelopeAmount = data.result.summary[i].amount;
                                    break;
                                case 5:
                                    total.uBankAmount = data.result.summary[i].amount;  
                                    break;
                                case 6:
                                    total.aliToCardAmount = data.result.summary[i].amount;
                                    break;
                                case 7:
                                    total.aliWapAmount = data.result.summary[i].amount;
                                    break;
                                case 8:
                                    total.wechatWapAmount = data.result.summary[i].amount;
                                    break;
                                case 9:
                                    total.aliH5Amount = data.result.summary[i].amount;
                                    break;
                                case 10:
                                    total.wechatH5Amount = data.result.summary[i].amount;
                                    break;
                            }
                        }
                    }

                    this.set('total', total);
                    var totalPages = data.result.dataPage.totalPages;
                    var pages = [];
                    var startPage = 1;
                    if (data.result.dataPage.page > 2) {
                        startPage = data.result.dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i <= totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    }
    );
}

function initReorder() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            payment: null,
            message: '',
            amount: 0,
        },
        on: {
            reorder: function (ctx) {
                this.set('message', '正在提交请求');
                var amount = this.get('amount');
                var payment = this.get('payment');
                if (amount <= 0) {
                    this.set('message', '补单金额输入错误');
                } else {
                    $.ajax({
                        url: '/api/Payment/' + payment.id + '/Settle',
                        type: 'POST',
                        data: { amount: amount },
                        context: this,
                        dataType: 'json'
                    }).done(function (data) {
                        if (data.error != 'success') {
                            this.set('message', '服务器返回错误:' + data.error);
                        } else {
                            this.set('message', '补单成功');
                        }
                    }).fail(function (xhr, status) {
                        if (xhr.responseJSON.error == 'invalid_data') {
                            this.set('message', '补单错误:操作员余额不足');
                        } else if (xhr.responseJSON.error == 'object_not_found') {
                            this.set('message', '补单错误:订单状态已改变');
                        } else {
                            this.set('message', '加载错误:' + xhr.responseText);
                        }
                    });
                }
            } 
        },
        oncomplete: function () {
            this._loadData();
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            var orderId = getUrlParam('orderId');
            $.ajax({
                url: '/api/Payment/' + orderId,
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('payment', data.result);
                    this.set('amount', data.result.amount);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initCreateOrder(id) {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            userId: 0,
            message: '',
            amount: 0,
            merchant: 0,
            channel: 0,
            merchants: [],
            channels: [],
            captcha: ""
        },
        on: {
            refreshCaptcha: function () {
                $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?token=createPayment_' + id + '&t=' + new Date().getTime());
            },
            create: function (ctx) {
                this.set('message', '正在提交请求');
                var amount = this.get('amount').mul(100);
                if (amount <= 0) {
                    this.set('message', '订单金额输入错误');
                } else {
                    var that = this;
                    $.ajax({
                        url: '/api/Payment/Create?captcha=' + this.get("captcha"),
                        type: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: http_query_build({
                            amount: amount,
                            userId: this.get("userId"),
                            merchentId: this.get("merchant"),
                            channelId: this.get("channel")
                        })
                    }).done(function (data) {
                        if (data.error != 'success') {
                            that.set('message', '服务器返回错误:' + data.error);
                        } else {
                            that.set('message', '新增订单成功');
                        }
                    }).fail(function (xhr, status) {
                        switch (xhr.responseJSON.errorMessage) {
                            case "user not found":
                                that.set('message', '新增订单错误:找不到该用户');
                                break;
                            case "user balance not enough":
                                that.set('message', '新增订单错误:用户余额不足');
                                break;
                            case "user is not support this merchent":
                                that.set('message', '新增订单错误:该用户不能接收此商户');
                                break;
                            case "merchent not found":
                                that.set('message', '新增订单错误:找不到该商户');
                                break;
                            case "user collect account balance is not enough":
                                that.set('message', '新增订单错误:该用户收款账户余额不足');
                                break;
                            case "bad captcha":
                                that.set('message', '新增订单错误:错误的验证码');
                                break;
                            default:
                                that.set('message', '加载错误:' + xhr.responseText);
                                break;
                        }
                    });
                }
            }
        },
        oncomplete: function () {
            $.ajax({
                url: '/api/Merchant',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                }
                this.set('merchants', data.result);
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + xhr.responseText);
            });

            $.ajax({
                url: '/api/CollectChannel',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                }
                this.set('channels', data.result);
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + xhr.responseText);
            });
        }
    });
}

function initUserEdit() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            user: null,
            roles: [],
            merchants: [],
            message: '',
        },
        on: {
            openPasswordModel: function () {
                var t = jDialog.iframe('/Home/SelfMgmtSetPassword?userId=' + getUrlParam('userId'), {
                    title: '修改密码',
                    width: 450,
                    height: 410
                });
            },
            saveUser: function (ctx) {
                this.set('message', '正在提交请求');
                var user = this.get('user');
                var request = {
                    Password: user.password,
                    Status: user.status,
                    Role: { Id: user.role.id },
                    NickName: user.nickName
                };
                var that = this;
                webApiCall(
                    '/api/User/' + user.id,
                    'PUT',
                    this,
                    function (status, data) {
                        if (status < 400) {
                            that.set('message', '信息修改成功');
                        } else {
                            that.set('message', '服务器返回错误:' + data.errorMessage);
                        }
                    }, request);
            }
        },
        oncomplete: function () {
            this._loadData();
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            var userId = getUrlParam('userId');
            var that = this;
            $.ajax({
                url: '/api/User/' + userId,
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('user', data.result);
                    if (data.result.role.id == 100) {
                        $.ajax({
                            url: '/api/merchant',
                            type: 'GET',
                            context: this,
                            dataType: 'json',
                        }).done(function (data) {
                            that.set('merchants', data.result);
                        }).fail(function (xhr, status) {
                            that.set('message', '加载错误:' + status);
                        });
                    }
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });

            $.ajax({
                url: '/api/UserRole',
                type: 'GET',
                context: this,
                dataType: 'json',
            }).done(function (data) {
                this.set('roles', data.result);
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initSelfMgmtSetPassword() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            user: null,
            roles: [],
            merchants: [],
            message: '',
            passwordCheck: ''
        },
        on: {
            saveUser: function (ctx) {
                var user = this.get('user');
                var passwordCheck = this.get('passwordCheck');
                if (user.password != passwordCheck) {
                    this.set('message', '两次密码输入不相符');
                    return;
                }
                this.set('message', '正在提交请求');
                var request = {
                    Password: user.password
                };
                var that = this;
                webApiCall(
                    '/api/User/' + user.id,
                    'PUT',
                    this,
                    function (status, data) {
                        if (status < 400) {
                            that.set('message', '信息修改成功');
                            setTimeout(closeDialog, 1500);
                        } else {
                            that.set('message', '服务器返回错误:' + data.errorMessage);
                        }
                    }, request);
            }
        },
        oncomplete: function () {
            this._loadData();
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            var userId = getUrlParam('userId');
            $.ajax({
                url: '/api/User/' + userId,
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('user', data.result);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initAddUser() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            account: '',
            password: '',
            nickName: '',
            wechatAccount: '',
            captcha: '',
            message: '',
        },
        on: {
            refreshCaptcha: function () {
                $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime());
            },
            saveUser: function (ctx) {
                var accountName = this.get('account');
                var password = this.get('password');
                var nickName = this.get('nickName');
                var wechatAccount = this.get('wechatAccount');
                var captcha = this.get('captcha');
                var referrerId = getUrlParam('referrerId');
                if (!accountName || !wechatAccount || !password || !captcha || !nickName) {
                    return;
                }
                var param = { accountName: accountName, password: password, wechatAccount: wechatAccount, nickName: nickName };
                var agentQuery = '';
                if (referrerId != null) {
                    agentQuery = "&referrer=" + referrerId;
                }
                
                this.set('message', '正在提交请求');
                $.ajax({
                    url: '/api/User?captcha=' + captcha + agentQuery,
                    type: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    data: JSON.stringify(param),
                    context: this,
                    dataType: 'json'
                })
                .done(function (data) {
                    if (data.error == 'success' || data.error == 'more_data') {
                        this.set('message', '用户已添加');
                    } else {
                        this.set('message', '服务器返回错误:' + data.error);
                    }
                })
                .fail(function (xhr, status) {
                    $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime())
                    var error = JSON.parse(xhr.responseText);
                    if (error.errorMessage == "bad captcha") {
                        this.set('message', '验证码错误');
                    } else if (error.error == 'invalid_data') {
                        this.set('message', '手机号，密码和验证码都为必填项');
                    } else if (error.errorMessage == "account already registered") {
                        this.set('message', '您的手机已被注册过，请用其他手机注册');
                    } else if (error.errorMessage == "wechat already registered") {
                        this.set('message', '您的微信已被注册过，请用其他微信注册');
                    } else {
                        this.set('message', '错误:' + xhr.responseText);
                    }
                }); 
            }
        }
    });
}

function initChildUserList() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            childs: [],
            pages: [],
            currentPage: 0,
            message: '',
            pageToken: '',
            isAgent: 'true'
        },
        on: {
            saveUserRelation: function (ctx) {
                var memberId = parseInt(this.get('memberId'), 10);
                var agentId = this.get('agentId');
                var captcha = this.get('captcha');
                var param = { MemberId: memberId, AgentId: agentId, IsDirect: true, Status: true };
                this.set('message', '正在提交请求');
                $.ajax({
                    url: '/api/User/Relation?captcha=' + captcha,
                    type: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    data: JSON.stringify(param),
                    context: this,
                    dataType: 'json'
                })
                .done(function (data) {
                    if (data.error == 'success' || data.error == 'more_data') {
                        this.set('message', '用户已添加');
                    } else {
                        this.set('message', '服务器返回错误:' + data.error);
                    }
                })
                .fail(function (xhr, status) {
                    this.set('message', '错误:' + xhr.responseText);
                });
            },
            addChildUser: function (ctx) {
                var referrerId = ctx.get('promotionCode');
                jDialog.iframe('/Home/AddUser?referrerId=' + referrerId, {
                    title: '添加下级用户',
                    width: 450,
                    height: 410
                });
            },
            childUserList: function (ctx) {
                var userId = ctx.get("id");
                window.location = '/home/ListChildUsers?userid=' + userId;
            },
            loadChildData: function () {
                this._loadChildData(isAgent);
            }
        },
        oncomplete: function () {
            this._loadChildData("true");
        },
        _loadChildData: function () {
            this.set('message', '正在加载数据');
            var userId = getUrlParam('userid');
            var isAgent = this.get('isAgent');
            $.ajax({
                url: '/api/User/' + userId + '/' + (isAgent == "true" ? 'Agents' : 'Members') +'?page=' + this.get('currentPage') + '&pageToken=' + this.get('pageToken'),
                type: 'GET',
                context: this,
                dataType: 'json'
            })
            .done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('childs', data.result.records);
                    this.set('currentPage', data.result.page);
                    this.set('pageToken', data.result.pageToken);
                    this.set('totalRecords', data.result.totalRecords);
                    var totalPages = data.result.totalPages;
                    var pages = [];
                    var startPage = 0;
                    if (data.result.page > 2) {
                        startPage = data.result.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i < totalPages; ++i) {
                            pages.push(i);
                        }
                    }
                    this.set('pages', pages);
                    this.set('message', '');
                }
            })
            .fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initConsole() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            startTimeStamp: '',
            endTimeStamp: '',
            successRate: [],
            message: '',
            info: null,
            errorMessage: '',
            BankId: '',
            AccountOwner: '',
            AccountNumber: '',
            Amount: '',
            captcha: ''
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            refreshCaptcha: function () {
                $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime());
            },
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            addorder: function (ctx) {
                $('#addorderModal').modal('show');
            },
            postorder: function (ctx) {
                var bankid = this.get('BankId');
                var owner = this.get('AccountOwner');
                var number = this.get('AccountNumber');
                var amount = this.get('Amount');
                var captcha = this.get('captcha');
                var param = { AccountProvider: bankid, AccountHolder: owner, AccountName: number, Amount: amount };
                this.set('errorMessage', '正在提交申请');

                if (param.AccountProvider == null || param.AccountProvider == '') {
                    this.set('errorMessage', '请选择银行名称');
                } else if (param.AccountHolder == null || param.AccountHolder == '') {
                    this.set('errorMessage', '请输入持卡人名称');
                } else if (param.AccountName == null || param.AccountName == '') {
                    this.set('errorMessage', '请输入银行卡号');
                } else if (param.Amount == null || param.Amount == '') {
                    this.set('errorMessage', '请输入下发金额');
                } else if (param.Amount % 1000 != 0) {
                    this.set('errorMessage', '请输入已千为单位的下发金额');
                }else {
                    $.ajax({
                        url: '/api/Merchant/WithDraw?captcha=' + captcha,
                        type: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        data: JSON.stringify(param),
                        context: this,
                        dataType: 'json'
                    })
                    .done(function (data) {
                        if (data.error == 'success' || data.error == 'more_data') {
                            this.set('errorMessage', '下发已添加');
                        } else {
                            this.set('errorMessage', '服务器返回错误:' + data.error);
                        }
                    })
                    .fail(function (xhr, status) {
                        this.set('errorMessage', '错误:' + xhr.responseText);
                    });
                }
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Report/successRate?startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('successRate', data.result);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });

            $.ajax({
                url: '/api/Merchant/Info',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    if (data.result.appKey)
                        this.set('info', data.result);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initCommissionRaiosPage() {
    var ractive = new Ractive({
        target: '#target',
        template: '#template',
        data: {
            ratios: [],
            confirmMsg: '',
            confirmTitle: '',
            lowerBound: 0,
            upperBound: 0,
            commissionRatio: 0.0,
            requestProcessing: false
        },
        oncomplete: function() {
            this._loadData();
        },
        on: {
            'delete': function (ctx) {
                this.set('confirmTitle', '确定删除该项目?');
                this.set('confirmMsg', '确定删除[' + ctx.get('lowerBound') + ',' + ctx.get('upperBound') + ')->' + ctx.get('ratio') + ' 项目吗?');
                this.keyToDelete = {
                    lowerBound: ctx.get('lowerBound'),
                    upperBound: ctx.get('upperBound')
                };
                this.action = 'delete';
                $('#confirmationModal').modal('show');
            },
            'add': function (ctx) {
                $('#addRatioModal').modal('show');
            },
            'doAdd': function (ctx) {
                if (this.get('requestProcessing')) {
                    return;
                }

                var lowerBound = this.get('lowerBound');
                var upperBound = this.get('upperBound');
                var ratio = this.get('commissionRatio');
                if (isNaN(lowerBound)) {
                    $('#lowerBound').addClass('is-invalid');
                    return;
                } else {
                    $('#lowerBound').removeClass('is-invalid');
                }
                if (isNaN(upperBound)) {
                    $('#upperBound').addClass('is-invalid');
                    return;
                } else {
                    $('#upperBound').removeClass('is-invalid');
                }
                if (isNaN(ratio)) {
                    $('#commissionRatio').addClass('is-invalid');
                    return;
                } else {
                    $('#commissionRatio').removeClass('is-invalid');
                }
                this.set('requestProcessing', true);
                webApiCall(
                    '/api/CommissionRatio',
                    'POST',
                    this,
                    function (status) {
                        this.set('requestProcessing', false);
                        if (status < 400) {
                            $('#addRatioModal').modal('hide');
                            this._loadData();
                        } else {
                            this.set('confirmTitle', '添加结果');
                            this.set('confirmMsg', '添加失败，请检查输入信息并重试!');
                            $('#addRatioModal').modal('hide');
                            this.action = 'ok';
                            $('#confirmationModal').modal('show');
                        }
                    },
                    {
                        lowerBound: lowerBound,
                        upperBound: upperBound,
                        ratio: ratio
                    });
            },
            'actionConfirmed': function (ctx) {
                var action = this.action;
                this.action = '';
                if (action == undefined || !action || action == '' || this.get('requestProcessing')) {
                    return;
                }
                if (action == 'delete') {
                    this.set('requestProcessing', true);
                    webApiCall(
                        '/api/CommissionRatio/' + this.keyToDelete.lowerBound + '/' + this.keyToDelete.upperBound,
                        'DELETE',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#confirmationModal').modal('hide');
                                this._loadData();
                            } else {
                                this.action = 'ok';
                                this.set('confirmTitle', '删除结果');
                                this.set('confirmMsg', '无法删除该项目，请联系管理员!');
                            }
                        });
                } else if (action == 'ok') {
                    $('#confirmationModal').modal('hide');
                }
            }
        },
        _loadData: function() {
            this.set('message', '正在加载中');
            webApiCall(
                '/api/CommissionRatio',
                'GET',
                this,
                function (status, data) {
                    if (status == 200) {
                        this.set('message', '');
                        this.set('ratios', data.result);
                    } else {
                        this.set('message', '无法加载数据，请重试');
                    }
                }
            )
        }
    });
}

function initRegisterPage(token, referrer) {
    var page = new Ractive({
            target: "#target",
            template: "#template",
            data: {
                token: token,
                referrer: referrer,
                accountName: '',
                password: '',
                wechatAccount: '',
                nickName: '',
                captcha: '',
                errorMsg: '',
                requestProcessing: false
            },
            on: {
                'refreshCaptcha': function() {
                    $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime() + '&token=' + this.get('token'));
                },
                'showAppDownLoad': function () {
                    $('#DownLoadModal').modal('show');
                },
                'register': function() {
                    var request = {
                        accountName: this.get('accountName'),
                        password: this.get('password'),
                        nickName: this.get('nickName'),
                        wechatAccount: this.get('wechatAccount')
                    };
                    var error = '';
                    if (request.accountName === null || request.accountName == '') {
                        error = '手机号不能为空';
                    } else if (!isMobleFormat(request.accountName)) {
                        error = "错误的手机格式";
                    } else if (request.password === null || request.password == '') {
                        error = '密码不能为空';
                    } else if (this.get('password1') === null || this.get('password1') == '') {
                        error = '确认密码不能为空';
                    } else if (this.get('password1') != request.password) {
                        error = '两次密码不一致';
                    } else if (this.get('wechatAccount') == '') {
                        error = '微信号不能为空';
                    } else if (this.get('captcha') == '') {
                        error = '验证码不能为空';
                    }
                    
                    if (error != '') {
                        this.set('errorMsg', error);
                        $('#registerFailedModal').modal('show');
                    } else {
                        this.set('processingLogin', true);
                        webApiCall(
                            '/api/User?captcha=' + this.get('captcha') + '&token=' + this.get('token') + '&referrer=' + this.get('referrer'),
                            'POST',
                            this,
                            function (status, data) {
                                if (status < 400) {
                                    $('#registerSucceededModal').modal('show');
                                } else {
                                    this.set('processingLogin', false);
                                    $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime() + '&token=' + this.get('token'));
                                    if (status >= 500) {
                                        this.set('errorMsg', '未知服务错误，请联系管理员');
                                        $('#registerFailedModal').modal('show');
                                        return;
                                    }

                                    if (data.errorMessage == 'bad captcha') {
                                        this.set('errorMsg', '验证码错误');
                                        $('#registerFailedModal').modal('show');
                                    } else if (data.error == 'invalid_data') {
                                        this.set('errorMsg', '手机号，密码和验证码都为必填项');
                                        $('#registerFailedModal').modal('show');
                                    } else if (status == 409) {
                                        if (data.errorMessage == "account already registered") {
                                           this.set('errorMsg', '您的手机已被注册过，请用其他手机注册');
                                        } else {
                                           this.set('errorMsg', '您的微信已被注册过，请用其他微信注册');
                                        }
                                        
                                        $('#registerFailedModal').modal('show');
                                    } else {
                                        this.set('errorMsg', '未知错误，请联系客服');
                                        $('#registerFailedModal').modal('show');
                                    }
                                }
                            }, request);
                    }
                }
            }
        }
    );
}

function initSiteSettingsPage() {
    var fieldsMapping = {
        "name": "#settingName",
        "displayName": "#displayName",
        "value": "#settingValue"
    };
    var ractive = new Ractive({
        target: '#target',
        template: '#template',
        data: {
            operation: '',
            errorTitle: '',
            errorMsg: '',
            name: '',
            displayName: '',
            value: '',
            settings: []
        },
        oncomplete: function() {
            this._loadData();
        },
        on: {
            'addSetting': function (ctx) {
                this.action = 'add';
                this.set('operation', '添加设置');
                $('#settingModal').modal('show');
            },
            'doOperation': function (ctx) {
                var action = this.action;
                this.action = '';
                if (action == undefined || action == '') {
                    return;
                }
                var setting;
                if (action == 'add') {
                    setting = this.getSetting();
                    if (setting == null) {
                        this.action = action;
                        return;
                    }
                    webApiCall('/api/SiteConfig', 'POST', this, function (status, data){
                        if (status < 400) {
                            this.set('errorTitle', '添加成功');
                            this.set('errorMsg', '设置添加成功！');
                        } else {
                            this.set('errorTitle', '添加失败');
                            this.set('errorMsg', '无法添加网站设置，请重试或者联系管理员！');
                        }
                        $('#settingModal').modal('hide');
                        $('#errorModal').modal('show');
                        var _this = this;
                        $('#errorModal').on('hidden.bs.modal', function (e) {
                            _this._loadData();
                        });
                    }, setting);
                } else if (action == 'update') {
                    setting = this.getSetting();
                    if (setting == null) {
                        this.action = action;
                        return;
                    }
                    webApiCall('/api/SiteConfig/' + this.id, 'PUT', this, function (status, data){
                        if (status < 400) {
                            this.set('errorTitle', '修改成功');
                            this.set('errorMsg', '设置修改成功！');
                        } else {
                            this.set('errorTitle', '修改失败');
                            this.set('errorMsg', '无法修改网站设置，请重试或者联系管理员！');
                        }
                        $('#settingModal').modal('hide');
                        $('#errorModal').modal('show');
                        var _this = this;
                        $('#errorModal').on('hidden.bs.modal', function (e) {
                            _this._loadData();
                        });
                    }, setting);
                }
            },
            'updateSetting': function(ctx) {
                for (var n in fieldsMapping) {
                    this.set(n, ctx.get(n));
                }
                this.action = 'update';
                this.id = ctx.get('id');
                this.set('operation', '修改设置');
                $('#settingModal').modal('show');
            }
        },
        getSetting: function() {
            var setting = {};
            for (var n in fieldsMapping) {
                if (this.get(n) == '') {
                    $(fieldsMapping[n]).addClass('is-invalid');
                    return null;
                } else {
                    $(fieldsMapping[n]).removeClass('is-invalid');
                    setting[n] = this.get(n);
                }
            }
            return setting;
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            webApiCall('/api/SiteConfig', 'GET', this, function (status, data) {
                if (status == 200) {
                    this.set('message', '');
                    this.set('settings', data.result);
                } else {
                    this.set('message', '无法加载数据，请重试');
                }
            });
        }
    });
}

function initChannelSettingsPage() {
    var fieldsMapping = {
        "name": "#channelName",
        "enabled": "#channelStatus",
        "instrumentsLimit": "#channelSize",
        "validTime": "#validTime",
        "channelProvider": "#channelProvider"
    };
    var ractive = new Ractive({
        target: '#target',
        template: '#template',
        data: {
            operation: '',
            errorTitle: '',
            errorMsg: '',
            name: '',
            enabled: false,
            instrumentsLimit: '',
            channelType: '',
            channelProvider: '',
            type: '',
            channels: []
        },
        oncomplete: function () {
            this._loadData();
        },
        on: {
            'doOperation': function (ctx) {
                var setting = this.getSetting();
                if (setting == null) {
                    return;
                }
                webApiCall('/api/CollectChannel/' + this.id, 'PUT', this, function (status, data) {
                    if (status < 400) {
                        this.set('errorTitle', '修改成功');
                        this.set('errorMsg', '设置修改成功！');
                    } else {
                        this.set('errorTitle', '修改失败');
                        this.set('errorMsg', '无法修改网站设置，请重试或者联系管理员！');
                    }
                    $('#settingModal').modal('hide');
                    $('#errorModal').modal('show');
                    var _this = this;
                    $('#errorModal').on('hidden.bs.modal', function (e) {
                        _this._loadData();
                    });
                }, setting);
            },
            'updateChannels': function (ctx) {
                for (var n in fieldsMapping) {
                    this.set(n, ctx.get(n));
                }
                this.id = ctx.get('id');
                this.set("channelType", ctx.get("channelType"));
                $('#settingModal').modal('show');
            }
        },
        getSetting: function () {
            var setting = {};
            for (var n in fieldsMapping) {
                if (this.get(n) == null) {
                    $(fieldsMapping[n]).addClass('is-invalid');
                    return null;
                } else {
                    $(fieldsMapping[n]).removeClass('is-invalid');
                    setting[n] = this.get(n);
                }
            }
            setting.channelType = this.get("channelType");
            return setting;
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            webApiCall('/api/CollectChannel', 'GET', this, function (status, data) {
                if (status == 200) {
                    this.set('message', '');
                    this.set('channels', data.result);
                } else {
                    this.set('message', '无法加载数据，请重试');
                }
            });
        }
    });
}

function initChannelProviderSettingsPage() {
    var fieldsMapping = {
        "ratio": "#ratio",
        "channelProvider": "#channelProvider",
        "defaultDaliyLimit": "#defaultDaliyLimit"
    };
    var ractive = new Ractive({
        target: '#target2',
        template: '#template2',
        data: {
            errorTitle: '',
            errorMsg: '',
            radio:'',
            channelProvider: '',
            defaultDaliyLimit: '',
            providers: []
        },
        oncomplete: function () {
            this._loadData();
        },
        on: {
            'doOperation': function (ctx) {
                webApiCall(
                '/api/CollectChannel/Ratio',
                'PUT',
                this,
                function (status, data) {
                    if (status < 400) {
                        this.set('errorTitle', '修改成功');
                        this.set('errorMsg', '设置修改成功！');
                    } else {
                        this.set('errorTitle', '修改失败');
                        this.set('errorMsg', '无法修改网站设置，请重试或者联系管理员！');
                    }
                    $('#providerSettingModal').modal('hide');
                    $('#providerErrorModal').modal('show');
                    var _this = this;
                    $('#providerErrorModal').on('hidden.bs.modal', function (e) {
                        window.location.reload();
                    });
                }, {
                    Ratio: this.get('ratio'),
                    DefaultDaliyLimit: this.get('defaultDaliyLimit'),
                    ChannelProvider: this.get('channelProvider')
                });
            },
            'updateProvider': function (ctx) {
                for (var n in fieldsMapping) {
                    this.set(n, ctx.get(n));
                }
                $('#providerSettingModal').modal('show');
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            webApiCall('/api/CollectChannel/Ratio', 'GET', this, function (status, data) {
                if (status == 200) {
                    this.set('message', '');
                    this.set('providers', data.result);
                } else {
                    this.set('message', '无法加载数据，请重试');
                }
            });
        }
    });
}

function initAwardConfigPage() {
    var ractive = new Ractive({
        target: '#target',
        template: '#template',
        data: {
            id:'',
            awards: [],
            confirmMsg: '',
            confirmTitle: '',
            awardCondition: 0,
            bouns: 0,
            requestProcessing: false,
            accountName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            currentPage: 0,
            pages: [],
            reportMessage: '',
            total: 0
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this.searchReport(this);
            },
            'edit': function (ctx) {
                $('#editConditionModal').modal('show');
                this.set('awardCondition', ctx.get('awardCondition'));
                this.set('bouns', ctx.get('bouns'));
                this.set('id', ctx.get('id'));
            },
            'add': function (ctx) {
                $('#addConditionModal').modal('show');
            },
            'doAdd': function (ctx) {
                if (this.get('requestProcessing')) {
                    return;
                }

                var awardCondition = this.get('awardCondition');
                var bouns = this.get('bouns');
                if (isNaN(awardCondition)) {
                    $('#awardCondition').addClass('is-invalid');
                    return;
                } else {
                    $('#awardCondition').removeClass('is-invalid');
                }
                if (isNaN(bouns)) {
                    $('#bouns').addClass('is-invalid');
                    return;
                } else {
                    $('#bouns').removeClass('is-invalid');
                }
                this.set('requestProcessing', true);
                webApiCall(
                    '/api/Award',
                    'POST',
                    this,
                    function (status) {
                        this.set('requestProcessing', false);
                        if (status < 400) {
                            $('#addConditionModal').modal('hide');
                            this._loadData();
                        } else {
                            this.set('confirmTitle', '添加结果');
                            this.set('confirmMsg', '添加失败，请检查输入信息并重试!');
                            $('#addConditionModal').modal('hide');
                            this.action = 'ok';
                            $('#confirmationModal').modal('show');
                        }
                    },
                    {
                        awardCondition: awardCondition,
                        bouns: bouns
                    });
            },
            'doEdit': function (ctx) {
                if (this.get('requestProcessing')) {
                    return;
                }

                var awardCondition = this.get('awardCondition');
                var bouns = this.get('bouns');
                if (isNaN(awardCondition)) {
                    $('#awardCondition').addClass('is-invalid');
                    return;
                } else {
                    $('#awardCondition').removeClass('is-invalid');
                }
                if (isNaN(bouns)) {
                    $('#bouns').addClass('is-invalid');
                    return;
                } else {
                    $('#bouns').removeClass('is-invalid');
                }
                this.set('requestProcessing', true);
                webApiCall(
                    '/api/Award/' + this.get('id'),
                    'PUT',
                    this,
                    function (status) {
                        this.set('requestProcessing', false);
                        if (status < 400) {
                            $('#editConditionModal').modal('hide');
                            this._loadData();
                        } else {
                            this.set('confirmTitle', '添加结果');
                            this.set('confirmMsg', '添加失败，请检查输入信息并重试!');
                            $('#editConditionModal').modal('hide');
                            this.action = 'ok';
                            $('#confirmationModal').modal('show');
                        }
                    },
                    {
                        awardCondition: awardCondition,
                        bouns: bouns
                    });
            },
            'actionConfirmed': function (ctx) {
                var action = this.action;
                this.action = '';
                if (action == undefined || !action || action == '' || this.get('requestProcessing')) {
                    return;
                }
                if (action == 'delete') {
                    this.set('requestProcessing', true);
                    webApiCall(
                        '/api/CommissionRatio/' + this.keyToDelete.lowerBound + '/' + this.keyToDelete.upperBound,
                        'DELETE',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#confirmationModal').modal('hide');
                                this._loadData();
                            } else {
                                this.action = 'ok';
                                this.set('confirmTitle', '删除结果');
                                this.set('confirmMsg', '无法删除该项目，请联系管理员!');
                            }
                        });
                } else if (action == 'ok') {
                    $('#confirmationModal').modal('hide');
                }
            }
        },
        _loadData: function () {
            this.set('message', '正在加载中');
            webApiCall(
                '/api/Award',
                'GET',
                this,
                function (status, data) {
                    if (status == 200) {
                        this.set('message', '');
                        this.set('awards', data.result);
                    } else {
                        this.set('message', '无法加载数据，请重试');
                    }
                }
            );
            this.searchReport(this);
        },
        searchReport: function (that) {
            $.ajax({
                url: '/api/Award/WithdrawReport?page=' + that.get('currentPage') + '&accountName=' + that.get('accountName') + '&startTime=' + that.get('startTimeStamp') + '&endTime=' + that.get('endTimeStamp'),
                type: 'GET',
                context: that,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    that.set('reportMessage', '服务器返回错误:' + data.error);
                } else {
                    that.set('records', data.result.dataPage.records);
                    that.set('currentPage', data.result.dataPage.page);
                    that.set('total', data.result.summary);
                    var totalPages = data.result.dataPage.totalPages;
                    var pages = [];
                    var startPage = 1;
                    if (data.result.dataPage.page > 2) {
                        startPage = data.result.dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i <= totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    that.set('pages', pages);
                    that.set('reportMessage', '');
                }
            }).fail(function (xhr, status) {
                that.set('reportMessage', '加载错误:' + status);
            });
        }
    });
}

function initEvaluationConfigPage() {
    var ractive = new Ractive({
        target: '#target',
        template: '#template',
        data: {
            id: 0,
            condition: 0,
            count: 0,
            value: 0,
            group: 0,
            repeat: 0,
            payAllowLimits: [],
            overTimePunishs: [],
            speedPayCommends: [],
            requestProcessing: false,
            isPost: false
        },
        oncomplete: function () {
            this._loadData();
        },
        on: {
            'edit': function (ctx) {
                this.setTemp(this,ctx);
                this.set('isPost', false);
                switch (ctx.get('type')) {
                    case 0:
                        $('#overTimePunishsModal').modal('show');
                        break;
                    case 1:
                        $('#speedPayCommends').modal('show');
                        break;
                    case 2:
                        $('#payAllowModal').modal('show');
                        break;
                }
                
            },
            'addPayAllow': function (ctx) {
                this.clearTemp(this);
                this.set('isPost', true);
                $('#payAllowModal').modal('show');
            },
            'addOverTime': function (ctx) {
                this.clearTemp(this);
                this.set('isPost', true);
                $('#overTimePunishsModal').modal('show');
            },
            'addSpeedPay': function (ctx) {
                this.clearTemp(this);
                this.set('isPost', true);
                $('#speedPayCommends').modal('show');
            },
            'doPutPayAllow': function (ctx) {
                if (this.get('requestProcessing')) {
                    return;
                }

                var condition = this.get('condition');
                var value = this.get('value');
                if (this.get('isPost')) {
                    if (isNaN(condition)) {
                        $('#payAllowCondition').addClass('is-invalid');
                        return;
                    } else {
                        $('#payAllowCondition').removeClass('is-invalid');
                    }
                    if (isNaN(value)) {
                        $('#payAllowValue').addClass('is-invalid');
                        return;
                    } else {
                        $('#payAllowValue').removeClass('is-invalid');
                    }
                    this.set('requestProcessing', true);
                    webApiCall(
                        '/api/UserEvaluation',
                        'POST',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#payAllowModal').modal('hide');
                                this._loadData();
                            } else {
                                this.set('confirmTitle', '添加结果');
                                this.set('confirmMsg', '添加失败，请检查输入信息并重试!');
                                $('#payAllowModal').modal('hide');
                                this.action = 'ok';
                                $('#payAllowModal').modal('show');
                            }
                        },
                        {
                            type: 2,
                            condition: condition,
                            value: value
                        });
                } else {
                    var data = this.getTemp(ctx);
                    if (!isNaN(condition)) {
                        data.condition = condition;
                    }
                    if (!isNaN(value)) {
                        data.value = value;
                    }
                    webApiCall(
                        '/api/UserEvaluation/' + data.id,
                        'PUT',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#payAllowModal').modal('hide');
                                this._loadData();
                            } else {
                                this.set('confirmTitle', '修改结果');
                                this.set('confirmMsg', '修改失败，请检查输入信息并重试!');
                                $('#payAllowModal').modal('hide');
                                this.action = 'ok';
                                $('#payAllowModal').modal('show');
                            }
                        }, data);
                }
            },
            'doPutOverTime': function (ctx) {
                if (this.get('requestProcessing')) {
                    return;
                }

                var condition = this.get('condition');
                var value = this.get('value');
                if (this.get('isPost')) {
                    if (isNaN(condition)) {
                        $('#overTimeCondition').addClass('is-invalid');
                        return;
                    } else {
                        $('#overTimeCondition').removeClass('is-invalid');
                    }
                    if (isNaN(value)) {
                        $('#overTimeValue').addClass('is-invalid');
                        return;
                    } else {
                        $('#overTimeValue').removeClass('is-invalid');
                    }
                    this.set('requestProcessing', true);
                    webApiCall(
                        '/api/UserEvaluation',
                        'POST',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#payAllowModal').modal('hide');
                                this._loadData();
                            } else {
                                this.set('confirmTitle', '添加结果');
                                this.set('confirmMsg', '添加失败，请检查输入信息并重试!');
                                $('#overTimePunishsModal').modal('hide');
                                this.action = 'ok';
                                $('#overTimePunishsModal').modal('show');
                            }
                        },
                        {
                            type: 0,
                            condition: condition,
                            value: value
                        });
                } else {
                    var data = this.getTemp(ctx);
                    if (!isNaN(condition)) {
                        data.condition = condition;
                    }
                    if (!isNaN(value)) {
                        data.value = value;
                    }
                    webApiCall(
                        '/api/UserEvaluation/' + data.id,
                        'PUT',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#overTimePunishsModal').modal('hide');
                                this._loadData();
                            } else {
                                this.set('confirmTitle', '修改结果');
                                this.set('confirmMsg', '修改失败，请检查输入信息并重试!');
                                $('#overTimePunishsModal').modal('hide');
                                this.action = 'ok';
                                $('#overTimePunishsModal').modal('show');
                            }
                        }, data);
                }
            },
            'doPutSpeedPay': function (ctx) {
                if (this.get('requestProcessing')) {
                    return;
                }

                var condition = this.get('condition');
                var value = this.get('value');
                var count = this.get('count');
                if (this.get('isPost')) {
                    if (isNaN(condition)) {
                        $('#speedPayCondition').addClass('is-invalid');
                        return;
                    } else {
                        $('#speedPayCondition').removeClass('is-invalid');
                    }
                    if (isNaN(value)) {
                        $('#speedPayValue').addClass('is-invalid');
                        return;
                    } else {
                        $('#speedPayValue').removeClass('is-invalid');
                    }
                    if (isNaN(count)) {
                        $('#speedPayCount').addClass('is-invalid');
                        return;
                    } else {
                        $('#speedPayCount').removeClass('is-invalid');
                    }
                    this.set('requestProcessing', true);
                    webApiCall(
                        '/api/UserEvaluation',
                        'POST',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#speedPayCommends').modal('hide');
                                this._loadData();
                            } else {
                                this.set('confirmTitle', '添加结果');
                                this.set('confirmMsg', '添加失败，请检查输入信息并重试!');
                                $('#speedPayCommends').modal('hide');
                                this.action = 'ok';
                                $('#speedPayCommends').modal('show');
                            }
                        },
                        {
                            type: 1,
                            condition: condition,
                            value: value,
                            count: count
                        });
                } else {
                    var data = this.getTemp(ctx);
                    if (!isNaN(condition)) {
                        data.condition = condition;
                    }
                    if (!isNaN(value)) {
                        data.value = value;
                    }
                    if (isNaN(count)) {
                        data.count = count;
                    }
                    webApiCall(
                        '/api/UserEvaluation/' + data.id,
                        'PUT',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#payAllowModal').modal('hide');
                                this._loadData();
                            } else {
                                this.set('confirmTitle', '修改结果');
                                this.set('confirmMsg', '修改失败，请检查输入信息并重试!');
                                $('#speedPayCommends').modal('hide');
                                this.action = 'ok';
                                $('#speedPayCommends').modal('show');
                            }
                        }, data);
                }
            },
            'delete': function (ctx) {
                this.set('confirmTitle', '确定删除该项目?');
                switch (ctx.get('type')) {
                    case 0:
                        this.set('confirmMsg', '确定删除[每日超时订单数超过' + ctx.get('condition') + '时扣' + ctx.get('value') + '分]项目吗?');
                        break;
                    case 1:
                        this.set('confirmMsg', '确定删除[每日订单在' + ctx.get('condition') + '分内完成' + ctx.get('count') + '次加' + ctx.get('value') + '分]项目吗?');
                        break;
                    case 2:
                        this.set('confirmMsg', '确定删除[服务分' + ctx.get('condition') + '时可抢单上限' + ctx.get('value') + ']项目吗?');
                        break;
                }

                this.set('id', ctx.get('id'));
                this.action = 'delete';
                $('#confirmationModal').modal('show');
            },
            'actionConfirmed': function (ctx) {
                var action = this.action;
                this.action = '';
                if (action == undefined || !action || action == '' || this.get('requestProcessing')) {
                    return;
                }
                if (action == 'delete') {
                    this.set('requestProcessing', true);
                    webApiCall(
                        '/api/UserEvaluation/' + this.get('id'),
                        'DELETE',
                        this,
                        function (status) {
                            this.set('requestProcessing', false);
                            if (status < 400) {
                                $('#confirmationModal').modal('hide');
                                this._loadData();
                            } else {
                                this.action = 'ok';
                                this.set('confirmTitle', '删除结果');
                                this.set('confirmMsg', '无法删除该项目，请联系管理员!');
                            }
                        });
                } else if (action == 'ok') {
                    $('#confirmationModal').modal('hide');
                }
            }
        },
        _loadData: function () {
            this.set('message', '正在加载中');
            webApiCall(
                '/api/UserEvaluation',
                'GET',
                this,
                function (status, data) {
                    if (status == 200) {
                        this.set('message', '');
                        this.set('payAllowLimits', data.result.payAllowLimits);
                        this.set('overTimePunishs', data.result.overTimePunishs);
                        this.set('speedPayCommends', data.result.speedPayCommends);
                    } else {
                        this.set('message', '无法加载数据，请重试');
                    }
                }
            );
        },
        clearTemp: function (that) {
            that.set('id', 0);
            that.set('condition', 0);
            that.set('count', 0);
            that.set('value', 0);
            that.set('group', 0);
            that.set('repeat', 0);
        },
        setTemp: function (that, ctx) {
            console.log(ctx.get('id'));
            that.set('id', ctx.get('id'));
            that.set('condition', ctx.get('condition'));
            that.set('count', ctx.get('count'));
            that.set('value', ctx.get('value'));
            that.set('group', ctx.get('group'));
            that.set('repeat', ctx.get('repeat'));
        },
        getTemp: function (ctx) {
            return {
                id: ctx.get('id'),
                condition: ctx.get('condition'),
                count: ctx.get('count'),
                value: ctx.get('value'),
                group: ctx.get('group'),
                repeat: ctx.get('repeat')
            };
        }
    });
}

function initAddEvaluation() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            message: '',
            userId: 0,
            note: '',
            type: -1,
            point: 0
        },
        oncomplete: function () {
            this.set('userId', getUrlParam('userid'));
        },
        on: {
            refreshCaptcha: function () {
                $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime());
            },
            submit: function () {
                var type = this.get('type');
                if (type != -1 && type != 0 && type != 1 && type != 2) {
                    this.set('message', '类型错误');
                    return;
                }

                var data = {
                    point: this.get('point'),
                    type: type,
                    note: this.get('note'),
                    captcha: this.get('captcha')
                };

                $.ajax({
                    url: '/api/User/' + this.get('userId') + '/Evaluation',
                    type: 'POST',
                    data: data,
                    context: this,
                    dataType: 'json'
                })
                .done(function (data) {
                    if (data.error != 'success') {
                        this.set('message', '服务器返回错误:' + data.error);
                    } else {
                        this.set('message', '修改成功');
                    }
                })
                .fail(function (xhr, status) {
                    this.set('message', '加载错误:' + status);
                });
            }
        }
    });
}

function initRechargeReport() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            accountName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            detail: function (ctx) {
                jDialog.iframe('/Home/RechargeList?id=' + ctx.get("userId") + "&startTime=" + this.get("startTime") + "&endTime=" + this.get("endTime"), {
                    title: '充值记录',
                    width: 800,
                    height: 600
                });
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Recharge/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('records', data.result.dataPage.records);
                    this.set('currentPage', data.result.dataPage.page);

                    this.set('total', data.result.summary);

                    var totalPages = data.result.dataPage.totalPages;
                    var pages = [];
                    var startPage = 1;
                    if (data.result.dataPage.page > 2) {
                        startPage = data.result.dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i <= totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initRechargeList() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            id: 0,
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            message: '',
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Recharge?userId=' + getUrlParam('id') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('records', data.result);
                    
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    }
    );
}

function initTransactionLog() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            accountName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/TransactionLog/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('records', data.result.dataPage.records);
                    this.set('currentPage', data.result.dataPage.page);

                    var total = null;
                    if (data.result.summary != null) {
                        total = {
                            redeemAmount: 0,
                            refundAmount: 0,
                            refillAmount: 0,
                            rewardsAmount: 0,
                            manualRedeemAmount: 0,
                            commissionAmount: 0,
                            modifyAmount: 0
                        };
                        data.result.summary.forEach(function (summary) {
                            switch (summary.type) {
                                case 0:
                                    total.modifyAmount += summary.amount;
                                    break;
                                case 1:
                                    total.redeemAmount += summary.amount;
                                    break;
                                case 2:
                                    total.refundAmount += summary.amount;
                                    break;
                                case 3:
                                    total.refillAmount += summary.amount;
                                    break;
                                case 6:
                                    total.rewardsAmount += summary.amount;
                                    break;
                                case 7:
                                    total.commissionAmount += summary.amount;
                                    break;
                                case 8:
                                    total.manualRedeemAmount += summary.amount;
                                    break;
                            }
                        });

                    }

                    this.set('total', total);
                    var totalPages = data.result.dataPage.totalPages;
                    var pages = [];
                    var startPage = 1;
                    if (data.result.dataPage.page > 2) {
                        startPage = data.result.dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i <= totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    }
    );
}

function initAgentListPage() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            keyword: '',
            users: [],
            currentPage: 0,
            pages: [],
            message: '',
            pageToken: '',
            totalRecords: 0
        },
        oncomplete: function () {
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            agentEdit: function (ctx) {
                var that = this;
                var userId = ctx.get("id");
                jDialog.iframe('/Home/AgentEdit?userId=' + userId, {
                    title: '商户编辑',
                    width: 600,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            addAgent: function (ctx) {
                var that = this;
                jDialog.iframe('/Home/AddAgent', {
                    title: '添加商户',
                    width: 600,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            addAgentUser: function (ctx) {
                var that = this;
                var referrerId = ctx.get('promotionCode');
                jDialog.iframe('/Home/AddAgent?referrerId=' + referrerId, {
                    title: '添加下级商戶',
                    width: 600,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            childUserList: function (ctx) {
                var userId = ctx.get("id");
                window.location = '/home/ListChildUsers?userid=' + userId;
            },
            verifyUser: function (ctx) {
                var userId = ctx.get("id");
                var request = { Status: 1 };
                webApiCall(
                    '/api/User/' + userId,
                    'PUT',
                    this,
                    function (status, data) {
                        if (status < 400) {
                            this.set('message', '成功启用' + userId);
                            this._loadData();
                        } else {
                            this.set('message', '服务器返回错误:' + data.errorMessage);
                        }
                    }, request);
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Merchant/ListReport?status=' + this.get('status') + '&page=' + this.get('currentPage') + '&pageToken=' + this.get('pageToken') + '&accountName=' + this.get('keyword'),
                type: 'GET',
                context: this,
                dataType: 'json'
            })
            .done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    var dataPage = data.result.dataPage;
                    this.set('users', dataPage.records);
                    this.set('currentPage', dataPage.page);
                    this.set('pageToken', dataPage.pageToken);
                    this.set('totalRecords', dataPage.totalRecords);
                   
                    var totalPages = data.result.totalPages;
                    var pages = [];
                    var startPage = 0;
                    if (data.result.page > 2) {
                        startPage = data.result.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i < totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            })
            .fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}

function initAddAgent() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            selfRatio: null,
            account: '',
            password: '',
            nickName: '',
            wechatAccount: '',
            wechatRatio: 0,
            bankRatio: 0,
            aliRatio: 0,
            captcha: '',
            message: '',
            channel: []
        },
        oncomplete: function () {
            var rid = getUrlParam('referrerId');
            $.ajax({
                url: '/api/Merchant/Info' + (rid == "" ? "" : "?referrerId=" + rid),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('selfRatio', data.result);
                }
            });

            $.ajax({
                url: '/api/CollectChannel',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('channel', data.result);
                }
            });
        },
        on: {
            refreshCaptcha: function () {
                $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime());
            },
            saveUser: function (ctx) {
                var selfRatio = this.get("selfRatio");
                var accountName = this.get('account');
                var password = this.get('password');
                var nickName = this.get('nickName');
                var wechatAccount = this.get('wechatAccount');
                var captcha = this.get('captcha');
                var wechatRatio = this.get('wechatRatio');
                var bankRatio = this.get('bankRatio');
                var aliRatio = this.get('aliRatio');
                var referrerId = getUrlParam('referrerId');

                var selectChannel = [];
                $("[name='OpenChannel']").each(function (index, obj) {
                    if ($(obj).attr("type") == "hidden") {
                        selectChannel.push(false);
                    }
                    if ($(obj).attr("type") == "checkbox") {
                        selectChannel.push($(obj).is(":checked"));
                    }
                });

                if (!accountName || !password || !captcha || !nickName) {
                    return;
                }

                if (wechatRatio <= selfRatio.wechatRatio) {
                    this.set('message', '微信手续费最低' + (selfRatio.wechatRatio + 1) + '%');
                    return;
                }

                if (wechatRatio > 20) {
                    this.set('message', '微信手续费最高20%');
                    return;
                }

                if (aliRatio <= selfRatio.aliRatio) {
                    this.set('message', '支付宝手续费最低' + (selfRatio.wechatRatio + 1) + '%');
                    return;
                }

                if (aliRatio > 20) {
                    this.set('message', '支付宝手续费最高20%');
                    return;
                }

                if (bankRatio <= selfRatio.bankRatio) {
                    this.set('message', '银行卡手续费最低' + (selfRatio.bankRatio + 1) + '%');
                    return;
                }

                if (bankRatio > 20) {
                    this.set('message', '银行卡手续费最高20%');
                    return;
                }

                var param = {
                    accountName: accountName,
                    password: password,
                    wechatAccount: wechatAccount,
                    nickName: nickName,
                    merchant: {
                        wechatRatio: wechatRatio,
                        aliRatio: aliRatio,
                        bankRatio: bankRatio,
                        channelEnabledList: selectChannel
                    }
                };
                var agentQuery = '';
                if (referrerId != null) {
                    agentQuery = "&referrer=" + referrerId;
                }

                this.set('message', '正在提交请求');
                $.ajax({
                    url: '/api/User?captcha=' + captcha + agentQuery,
                    type: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    data: JSON.stringify(param),
                    context: this,
                    dataType: 'json'
                })
                .done(function (data) {
                    if (data.error == 'success' || data.error == 'more_data') {
                        this.set('message', '商户已添加');
                    } else {
                        this.set('message', '服务器返回错误:' + data.error);
                    }
                })
                .fail(function (xhr, status) {
                    $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime())
                    var error = JSON.parse(xhr.responseText);
                    if (error.errorMessage == "bad captcha") {
                        this.set('message', '验证码错误');
                    } else if (error.error == 'invalid_data') {
                        this.set('message', '手机号，密码和验证码都为必填项');
                    } else if (error.errorMessage == "account already registered") {
                        this.set('message', '您的手机已被注册过，请用其他手机注册');
                    } else if (error.errorMessage == "wechat already registered") {
                        this.set('message', '您的微信已被注册过，请用其他微信注册');
                    } else {
                        this.set('message', '错误:' + xhr.responseText);
                    }
                });
            }
        }
    });
}

function initAgentEdit() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            selfRatio: null,
            user: null,
            message: '',
        },
        on: {
            saveAgent: function (ctx) {
                var selfRatio = this.get("selfRatio");
                this.set('message', '正在提交请求');
                var user = this.get('user');

                if (user.merchant.wechatRatio <= selfRatio.wechatRatio) {
                    this.set('message', '微信手续费最低' + (selfRatio.wechatRatio + 1) + '%');
                    return;
                }

                if (user.merchant.aliRatio <= selfRatio.aliRatio) {
                    this.set('message', '支付宝手续费最低' + (selfRatio.aliRatio + 1) + '%');
                    return;
                }

                if (user.merchant.bankRatio <= selfRatio.bankRatio) {
                    this.set('message', '银行卡手续费最低' + (selfRatio.bankRatio + 1)+ '%');
                    return;
                }
                
                var selectChannel = [];
                $("[name='OpenChannel']").each(function (index, obj) {
                    if ($(obj).attr("type") == "hidden") {
                        selectChannel.push(false);
                    }
                    if ($(obj).attr("type") == "checkbox") {
                        selectChannel.push($(obj).is(":checked"));
                    }
                });

                var request = {
                    Password: user.password,
                    Status: user.status,
                    Role: { Id: user.role.id },
                    NickName: user.nickName,
                    Merchant: {
                        wechatRatio: user.merchant.wechatRatio,
                        aliRatio: user.merchant.aliRatio,
                        bankRatio: user.merchant.bankRatio,
                        channelEnabledList: selectChannel
                    }
                };
                var that = this;
                webApiCall(
                    '/api/User/' + user.id,
                    'PUT',
                    this,
                    function (status, data) {
                        if (status < 400) {
                            that.set('message', '成功修改' + user.id);
                        } else {
                            that.set('message', '服务器返回错误:' + data.errorMessage);
                        }
                    }, request);
            }
        },
        oncomplete: function () {
            var channel = [];
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/CollectChannel',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    channel = data.result;
                }
            });

            $.ajax({
                url: '/api/Merchant/Info',
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('selfRatio', data.result);
                    var userId = getUrlParam('userId');
                    $.ajax({
                        url: '/api/User/' + userId + "?isAgent=true",
                        type: 'GET',
                        context: this,
                        dataType: 'json'
                    }).done(function (data) {
                        channel.forEach(function (c, index) {
                            c.selected = false;
                            if (data.result.merchant.channelEnabledList[index]) {
                                c.selected = true;
                            }
                        });
                        this.set("channel",channel);
                        console.log(data);
                        if (data.error != 'success') {
                            this.set('message', '服务器返回错误:' + data.error);
                        } else {
                            this.set('user', data.result);
                            this.set('message', '');
                        }
                    }).fail(function (xhr, status) {
                        this.set('message', '加载错误:' + status);
                    });
                }
            });
        }
    });
}

function initMerchantReport() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            accountName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Merchant/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    var selfRatio = this.get('selfRatio');
                    var total = {
                        wechatAmount: 0,
                        alipayAmount: 0,
                        unionpayAmount: 0,
                        aliRedEnvelopeAmount: 0,
                        uBankAmount: 0,
                        totalAmount: 0,
                        totalCommission: 0
                    };
                    for (var i = 0; i < data.result.dataPage.records.length; i++) {
                        var record = data.result.dataPage.records[i];
                       
                        total.wechatAmount += record.wechatAmount;
                        total.alipayAmount += record.alipayAmount;
                        total.aliRedEnvelopeAmount += record.aliRedEnvelopeAmount;
                        total.unionpayAmount += record.unionpayAmount;
                        total.uBankAmount += record.uBankAmount;

                        total.totalAmount += record.totalAmount;
                        total.totalCommission += record.totalCommission;
                    }

                    this.set('records', data.result.dataPage.records);
                    this.set('currentPage', data.result.dataPage.page);
                    this.set('total', total);
                    var totalPages = data.result.dataPage.totalPages;
                    var pages = [];
                    var startPage = 1;
                    if (data.result.dataPage.page > 2) {
                        startPage = data.result.dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i <= totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    }
    );
}

function initAgentReport() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        //變數預設值
        data: {
            selfData: {
                ID: '',
                NickName: '找不到该用户',
                Name: '找不到该用户',
                Balance: 0,
                History: 0,
                JobName: '',
                Channels: [],
                Total: {},
                WechatRatio: 0,
                AliRatio: 0,
                BankRatio: 0,
                promotionCode: ''
            },
            search: {
                currentPage: 0,
                childId: '',
                childAccountName: '',
                childNickName: '',
                startTimeStamp: '',
                endTimeStamp: ''
            },
            childData: {
                record: [],
                total: {}
            },
            showType: true,
            message: '',
            RefreshTime: 60000,
            _changeType: function (type) {
                this.set("showType", type);
                this._loadData();
            }
        },
        oncomplete: function () {
            var Self = this.get("selfData");
            Self.ID = getUrlParam("userId");
            this.set("selfData", Self);

            var Search = this.get("search");
            Search.startTimeStamp = getUrlParam("startTime");
            Search.endTimeStamp = getUrlParam("endTime");

            var lastHour = new Date();
            if (Search.startTimeStamp) {
                lastHour = new Date(Search.startTimeStamp);
            } else {
                lastHour.setHours(lastHour.getHours() - 1);
            }

            var now = new Date();
            if (Search.endTimeStamp) {
                now = new Date(Search.endTimeStamp);
            }

            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            Search.startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            Search.endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set("search", Search);

            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
            this._refresh();
        },
        on: {
            agentEdit: function (ctx) {
                var that = this;
                var userId = ctx.get("accountId");
                jDialog.iframe('/Home/AgentEdit?userId=' + userId, {
                    title: '商户编辑',
                    width: 600,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            addAgent: function (ctx) {
                var that = this;
                var referrerId = this.get('selfData.promotionCode');
                jDialog.iframe('/Home/AddAgent?referrerId=' + referrerId, {
                    title: '添加商户',
                    width: 600,
                    height: 500,
                    events: {
                        close: function (evt) {
                            that._loadData();
                        }
                    }
                });
            },
            doSearch: function (ctx) {
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                var Search = this.get("search");
                Search.startTimeStamp = startTime;
                Search.endTimeStamp = endTime;
                this.set('currentPage', 0);
                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            setDateAndTime: function(context, type) {
                var lastType = new Date();
                if (type == 'Day') {
                    lastType.setDate(lastType.getDate() - 1);
                } else if (type == 'Week') {
                    lastType.setDate(lastType.getDate() - 7);
                } else if (type == 'Month') {
                    lastType.setMonth(lastType.getMonth() - 1);
                }
                var startDate = formatTimestampDate(lastType);
                laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            },
            GoToChild: function (ctx) {
                window.location = window.location.origin + "/home/AgentReport?userId=" + ctx.get("accountId") + "&startTime=" + formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val()) + "&endTime=" + formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            var Search = this.get("search");
            var Self = this.get("selfData");
            $.ajax({
                url: '/api/Merchant/' + Self.ID + '/Report?all=' + this.get('showType') +'&startTime=' + Search.startTimeStamp + '&endTime=' + Search.endTimeStamp,
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    var Self = this.get("selfData");
                    var SelfData = data.result.selfData;
                    var total = {
                        amount: 0,
                        commission: 0,
                        commissionprofit: 0,
                        successRate: 0
                    };

                    for (var i = 0; i < SelfData.channels.length; i++){
                        SelfData.channels[i].amount = SelfData.channels[i].amount / 100;
                        SelfData.channels[i].commission = SelfData.channels[i].commission / 100;
                        SelfData.channels[i].commissionprofit = SelfData.channels[i].amount - SelfData.channels[i].commission;
                        SelfData.channels[i].successRate = SelfData.channels[i].success / SelfData.channels[i].total * 100;
                        SelfData.channels[i].id = SelfData.channels[i].type;

                        total.amount += SelfData.channels[i].amount;
                        total.commission += SelfData.channels[i].commission;
                        total.commissionprofit += SelfData.channels[i].commissionprofit;
                        total.successRate += SelfData.channels[i].successRate / SelfData.channels.length;
                    }
                    Self.Name = SelfData.merchant.accountName;
                    Self.NickName = SelfData.merchant.nickName;
                    Self.Channels = SelfData.channels;
                    Self.Balance = SelfData.merchant.balance;
                    Self.JobName = SelfData.merchant.jobName;
                    Self.Total = total;
                    Self.WechatRatio = SelfData.merchant.wechatRatio;
                    Self.AliRatio = SelfData.merchant.aliRatio;
                    Self.BankRatio = SelfData.merchant.bankRatio;
                    Self.promotionCode = SelfData.merchant.promotionCode;
                    this.set("selfData", Self);

                    var Child = this.get("childData");
                    Child.record = [];
                    var ChildData = data.result.childData.records;
                    var ChildTotal = {
                        balance: 0,
                        WechatAmount: 0,
                        WechatCommission: 0,
                        AliAmount: 0,
                        AliCommission: 0,
                        BankAmount: 0,
                        BankCommission: 0,
                        TotalCommission: 0
                    };
                    ChildData.forEach(function (child) {
                        var childInfo = child.merchant;
                        childInfo.WechatAmount = 0;
                        childInfo.WechatCommission = 0;
                        childInfo.AliAmount = 0;
                        childInfo.AliCommission = 0;
                        childInfo.BankAmount = 0;
                        childInfo.BankCommission = 0;
                        childInfo.TotalCommission = 0;
                        child.channels.forEach(function (payment) {
                            switch (payment.provider) {
                                case 1:
                                    childInfo.WechatAmount += payment.amount / 100;
                                    childInfo.WechatCommission += payment.commission / 100;
                                    break;
                                case 2:
                                    childInfo.AliAmount += payment.amount / 100;
                                    childInfo.AliCommission += payment.commission / 100;
                                    break;
                                case 3:
                                    childInfo.BankAmount += payment.amount / 100;
                                    childInfo.BankCommission += payment.commission / 100;
                                    break;
                            }
                            childInfo.TotalCommission += payment.commission / 100;
                        });
                        ChildTotal.balance += childInfo.balance;
                        ChildTotal.WechatAmount += childInfo.WechatAmount;
                        ChildTotal.WechatCommission += childInfo.WechatCommission;
                        ChildTotal.AliAmount += childInfo.AliAmount;
                        ChildTotal.AliCommission += childInfo.AliCommission;
                        ChildTotal.BankAmount += childInfo.BankAmount;
                        ChildTotal.BankCommission += childInfo.BankCommission;
                        ChildTotal.TotalCommission += childInfo.TotalCommission;
                        Child.record.push(childInfo);
                    });
                    Child.total = ChildTotal;
                    console.log(Child);
                    this.set("childData", Child);
                    this.set('message', '');
                    
                }
            }).fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        },
        _refresh: function () {
            var self = this;
            setTimeout(function () { self._loadData(); self._refresh(); }, this.get("RefreshTime"));
        }
    });
}

function initWireoutOrderListPage() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        data: {
            orderId: '',
            merchantOwner: '',
            status: -1,
            message: '',
            orders: [],
            totalRecords: 0,
            pages: [],
            currentPage: 0,
            editData: {},
            errorMessage: '',
            captcha: '',
            startTimeStamp: '',
            endTimeStamp: '',
        },
        oncomplete: function () {
            var lastHour = new Date();
            lastHour.setHours(lastHour.getHours() - 1);
            var now = new Date();
            var startDate = formatTimestampDate(lastHour);
            var endDate = formatTimestampDate(now);
            var startTime = formatTimestampTime(lastHour);
            var endTime = formatTimestampTime(now);
            var startTimeStamp = formatDateTimeByDateAndTime(startDate, startTime);
            var endTimeStamp = formatDateTimeByDateAndTime(endDate, endTime);
            this.set('startTimeStamp', startTimeStamp);
            this.set('endTimeStamp', endTimeStamp);
            laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            laydate.render({ elem: '#endDate', theme: '#008CBA', value: endDate });
            laydate.render({ elem: '#startTime', type: 'time', theme: '#008CBA', value: startTime });
            laydate.render({ elem: '#endTime', type: 'time', theme: '#008CBA', value: endTime });
            this._loadData();
        },
        on: {
            refreshCaptcha: function () {
                this._refresh();
            },
            doSearch: function (ctx) {
                this.set('currentPage', 0);
                var startTime = formatDateTimeByDateAndTime($('#startDate').val(), $('#startTime').val());
                var endTime = formatDateTimeByDateAndTime($('#endDate').val(), $('#endTime').val());
                if (new Date(endTime) < new Date(startTime)) {
                    this.set("message", "结束时间不得比开始时间早");
                    return;
                }
                this.set('startTimeStamp', startTime);
                this.set('endTimeStamp', endTime);

                this._loadData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            editOrder: function (ctx) {
                this.set('errorMessage', '加载中');
                $.ajax({
                    url: '/api/Merchant/WithDrawSettle/' + ctx.get('id'),
                    type: 'GET',
                    context: this,
                    dataType: 'json'
                })
                .done(function (data) {
                    if (data.error != 'success') {
                        this.set('errorMessage', '服务器返回错误:' + data.error);
                    } else {
                        this.set('editData', data.result);
                        this.set('errorMessage', '');
                    }
                })
                .fail(function (xhr, status) {
                    this.set('errorMessage', '加载错误:' + status);
                });
                this._refresh();
                $('#editorderModal').modal('show');
            },
            changeToPaying: function () {
                this._changeType(1);
            },
            changeToSettled: function () {
                this._changeType(2);
            },
            changeToCancel: function () {
                this._changeType(3);
            },
            changeToRefuse: function () {
                this._changeType(4);
            }
        },
        _changeType: function (type) {
            if (this.get('captcha') == '') {
                this.set('errorMessage', '验证码不得为空');
                return;
            }

            this.set('errorMessage', '加载中');
            webApiCall(
                '/api/Merchant/WithDrawSettle/' + this.get('editData.id'),
                'PUT',
                this,
                function () {
                    this.set('errorMessage', '');
                    var typeMessage = "";
                    switch (type) {
                        case 1:
                            typeMessage = "交易中";
                            break;
                        case 2:
                            typeMessage = "已完成";
                            break;
                        case 3:
                            typeMessage = "已取消";
                            break;
                        case 4:
                            typeMessage = "已拒绝";
                            break;
                    }
                    alert("成功修改订单为:" + typeMessage);
                    this._loadData();
                }, http_query_build({ status: type })
            );
        },
        _refresh: function () {
            $('#inputCaptchaImg').attr('src', '/Home/CaptchaImage?t=' + new Date().getTime());
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Merchant/WithDrawSettle?status=' + this.get('status') + '&page=' + this.get('currentPage') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
                type: 'GET',
                context: this,
                dataType: 'json'
            })
            .done(function (data) {
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    var dataPage = data.result;
                    this.set('orders', dataPage.records);
                    this.set('currentPage', dataPage.page);
                    this.set('pageToken', dataPage.pageToken);
                    this.set('totalRecords', dataPage.totalRecords);
                    
                    var totalPages = dataPage.totalPages;
                    var pages = [];
                    var startPage = 0;
                    if (dataPage.page > 2) {
                        startPage = dataPage.page - 2;
                    }

                    if (totalPages - startPage > 10) {
                        for (var i = startPage; i < startPage + 5; ++i) {
                            pages.push(i);
                        }

                        pages.push(-1);
                        pages.push(totalPages - 1);
                    } else {
                        for (var i = startPage; i < totalPages; ++i) {
                            pages.push(i);
                        }
                    }

                    this.set('pages', pages);
                    this.set('message', '');
                }
            })
            .fail(function (xhr, status) {
                this.set('message', '加载错误:' + status);
            });
        }
    });
}
function orderCount() {
    var ractive = new Ractive({
        target: "#HearderTarget",
        template: "#HearderTemplate",
        data: {
            count: 0,
            herf: ''
        },
        oncomplete: function () {
            var that = this;
            that._loadData();
            setInterval(function () {
                that._loadData();
            }, 5000);
        },
        _loadData: function () {
            $.ajax({
                url: '/api/Merchant/WithDrawCount?type=0' ,
                type: 'GET',
                context: this,
                dataType: 'json'
            }).done(function (data) {
                console.log(data);
                if (data.error != 'success') {
                    this.set('message', '服务器返回错误:' + data.error);
                } else {
                    this.set('count', data.result);
                }
            }).fail(function (xhr, status) {
                console.error(xhr);
            });
        }
    });
}