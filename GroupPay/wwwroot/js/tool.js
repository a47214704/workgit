const formatTimestampDate = function (timestamp) {
    var d = new Date();
    if (timestamp != undefined && timestamp > 0) {
        d = new Date(timestamp);
    }
    var month = d.getMonth() + 1;
    var date = d.getDate();
    return d.getFullYear() + '-' + (month < 10 ? '0' + month : month) + '-' + (date < 10 ? '0' + date : date);
};
const formatTimestampTime = function (timestamp) {
    var d = new Date();
    if (timestamp != undefined && timestamp > 0) {
        d = new Date(timestamp);
    }
    var hours = d.getHours();
    var mins = d.getMinutes();
    var secs = d.getSeconds();
    return (hours < 10 ? '0' + hours : hours) + ':' + (mins < 10 ? '0' + mins : mins) + ':' + (secs < 10 ? '0' + secs : secs);
};
const formatDateTimeByDateAndTime = function (date, time) {
    return date + ' ' + time;
};
const formatDigitalWithDot = function (x, len) {
    var xs = x + "";
    var subIndex = xs.indexOf(".");
    return subIndex == -1 ? xs + ".00" : xs.substring(0,subIndex + 1 + len);
};
const isMobleFormat = function (mobile) {
    var reg = /^13[\d]{9}$|^14[5,7]{1}\d{8}$|^15[^4]{1}\d{8}$|^17[0,6,7,8]{1}\d{8}$|^18[\d]{9}$|^19[\d]{9}$/;
    return reg.test(mobile);
};

Ractive.defaults.data.formatTimestampDate = formatTimestampDate;
Ractive.defaults.data.formatTimestampTime = formatTimestampTime;
Ractive.defaults.data.formatTimestamp = function (timestamp) {
    return formatDateTimeByDateAndTime(formatTimestampDate(timestamp), formatTimestampTime(timestamp));
};
Ractive.defaults.data.formatDigitalWithDot = formatDigitalWithDot;
function http_query_build(data) {
    var esc = encodeURIComponent;
    return Object.keys(data)
        .map(k => esc(k) + '=' + esc(data[k]))
        .join('&');
}

function getUrlParam(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}

function getCookiesParam(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}

function webApiCall(url, method, context, callback, data) {
    var request = {
        url: url,
        type: method,
        context: context,
        dataType: 'json'
    };
    if (data !== undefined && data != null && typeof data != 'string') {
        request.data = JSON.stringify(data);
        request.contentType = 'application/json';
    } else {
        request.data = data;
    }
    $.ajax(request)
        .done(function (data, statusText, jqXHR) {
            callback.apply(this, [jqXHR.status, data]);
        })
        .fail(function (jqXHR) {
            callback.apply(this, [jqXHR.status, jqXHR.responseJSON]);
        });
}

function accMul(arg1, arg2) {
    var m = 0, s1 = arg1.toString(), s2 = arg2.toString();
    try { m += s1.split(".")[1].length } catch (e) { }
    try { m += s2.split(".")[1].length } catch (e) { }
    return Number(s1.replace(".", "")) * Number(s2.replace(".", "")) / Math.pow(10, m);
}

Number.prototype.mul = function (arg) {
    return accMul(arg, this);
};