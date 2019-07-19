
function initRechargeReport() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        //變數預設值
        data: {
            accountName: '',
            accountID: '',
            accountFullName:'',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            Toprecords: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null,
            RefreshTime:'60000'
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
                this.RefreshData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            setDateAndTime(context, type) {
                var lastType = new Date();
                if (type == 'Day') {
                    lastType.setDate(lastType.getDate() - 1);
                } else if (type == 'Week') {
                    lastType.setDate(lastType.getDate() - 7);
                } else if (type == 'Month') {
                    lastType.setMonth(lastType.getMonth() - 1);
                }
                var startDate = formatTimestampDate(lastType);
                this.set('startDate', startDate);
                laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&accountID=' + this.get('accountID') + '&accountFullName=' + this.get('accountFullName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
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
                    var total = {//合計
                        amount: 0,
                        exchangeRate: 0,
                        commission: 0,
                        successRate: 0
                    };
                    if (data.result.summary) {
                        for (var i = 0; i < data.result.summary.length; i++) {
                            switch (data.result.summary[i].channel) {
                                case 1:
                                    total.amount = data.result.summary[i].amount;
                                    break;
                                case 2:
                                    total.exchangeRate = data.result.summary[i].amount;
                                    break;
                                case 3:
                                    total.commission = data.result.summary[i].amount;
                                    break;
                                case 4:
                                    total.successRate = data.result.summary[i].amount;
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
        },
        RefreshData: function () {
            setInterval(function () {
                ractive._loadData();
            }, this.get('RefreshTime'));
        }
    }
    );
}

function initAllAgentManage() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        //變數預設值
        data: {
            accountName: '',
            accountID: '',
            accountFullName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            Toprecords: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null,
            RefreshTime: '60000'
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
                this.RefreshData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            setDateAndTime(context, type) {
                var lastType = new Date();
                if (type == 'Day') {
                    lastType.setDate(lastType.getDate() - 1);
                } else if (type == 'Week') {
                    lastType.setDate(lastType.getDate() - 7);
                } else if (type == 'Month') {
                    lastType.setMonth(lastType.getMonth() - 1);
                }
                var startDate = formatTimestampDate(lastType);
                this.set('startDate', startDate);
                laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&accountID=' + this.get('accountID') + '&accountFullName=' + this.get('accountFullName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
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
                        surplus: 0,
                        wxprepaid: 0,
                        wxpercent: 0,
                        aliprepaid: 0,
                        alipercent: 0,
                        bankcardprepaid: 0,
                        bankcardpercent: 0,
                        totalrefund: 0,
                        totalrefundpercent: 0,
                        totalprofit: 0
                    };

                    if (data.result.summary) {
                        for (var i = 0; i < data.result.summary.length; i++) {
                            switch (data.result.summary[i].channel) {
                                case 1:
                                    total.surplus = data.result.summary[i].amount;
                                    break;
                                case 2:
                                    total.wxprepaid = data.result.summary[i].amount;
                                    break;
                                case 3:
                                    total.wxpercent = data.result.summary[i].amount;
                                    break;
                                case 4:
                                    total.aliprepaid = data.result.summary[i].amount;
                                    break;
                                case 5:
                                    total.alipercent = data.result.summary[i].amount;
                                    break;
                                case 6:
                                    total.bankcardprepaid = data.result.summary[i].amount;
                                    break;
                                case 7:
                                    total.bankcardpercent = data.result.summary[i].amount;
                                    break;
                                case 8:
                                    total.totalrefund = data.result.summary[i].amount;
                                    break;
                                case 9:
                                    total.totalrefundpercent = data.result.summary[i].amount;
                                    break;
                                case 10:
                                    total.totalprofit = data.result.summary[i].amount;
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
        },
        RefreshData: function () {
            setInterval(function () {
                ractive._loadData();
            }, this.get('RefreshTime'));
        }
    }
    );
}

function initAgentManage() {
    var ractive = new Ractive({
        target: "#target",
        template: "#ractiveTemplate",
        //變數預設值
        data: {
            accountName: '',
            accountID: '',
            accountFullName: '',
            startTimeStamp: '',
            endTimeStamp: '',
            records: [],
            Toprecords: [],
            currentPage: 0,
            pages: [],
            message: '',
            total: null,
            RefreshTime: '60000'
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
                this.RefreshData();
            },
            doPage: function (ctx) {
                this.set('currentPage', ctx.get());
                this._loadData();
            },
            setDateAndTime(context, type) {
                var lastType = new Date();
                if (type == 'Day') {
                    lastType.setDate(lastType.getDate() - 1);
                } else if (type == 'Week') {
                    lastType.setDate(lastType.getDate() - 7);
                } else if (type == 'Month') {
                    lastType.setMonth(lastType.getMonth() - 1);
                }
                var startDate = formatTimestampDate(lastType);
                this.set('startDate', startDate);
                laydate.render({ elem: '#startDate', theme: '#008CBA', value: startDate });
            }
        },
        _loadData: function () {
            this.set('message', '正在加载数据');
            $.ajax({
                url: '/api/Report?page=' + this.get('currentPage') + '&accountName=' + this.get('accountName') + '&accountID=' + this.get('accountID') + '&accountFullName=' + this.get('accountFullName') + '&startTime=' + this.get('startTimeStamp') + '&endTime=' + this.get('endTimeStamp'),
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
                        surplus: 0,
                        wxprepaid: 0,
                        wxpercent: 0,
                        aliprepaid: 0,
                        alipercent: 0,
                        bankcardprepaid: 0,
                        bankcardpercent: 0,
                        totalrefund: 0,
                        totalrefundpercent: 0,
                        totalprofit: 0
                    };

                    if (data.result.summary) {
                        for (var i = 0; i < data.result.summary.length; i++) {
                            switch (data.result.summary[i].channel) {
                                case 1:
                                    total.surplus = data.result.summary[i].amount;
                                    break;
                                case 2:
                                    total.wxprepaid = data.result.summary[i].amount;
                                    break;
                                case 3:
                                    total.wxpercent = data.result.summary[i].amount;
                                    break;
                                case 4:
                                    total.aliprepaid = data.result.summary[i].amount;
                                    break;
                                case 5:
                                    total.alipercent = data.result.summary[i].amount;
                                    break;
                                case 6:
                                    total.bankcardprepaid = data.result.summary[i].amount;
                                    break;
                                case 7:
                                    total.bankcardpercent = data.result.summary[i].amount;
                                    break;
                                case 8:
                                    total.totalrefund = data.result.summary[i].amount;
                                    break;
                                case 9:
                                    total.totalrefundpercent = data.result.summary[i].amount;
                                    break;
                                case 10:
                                    total.totalprofit = data.result.summary[i].amount;
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
        },
        RefreshData: function () {
            setInterval(function () {
                ractive._loadData();
            }, this.get('RefreshTime'));
        }
    }
    );
}