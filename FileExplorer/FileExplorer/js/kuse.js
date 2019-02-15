function getEventPageX(event) {
    event = getEvent(event);
    var scrollX = document.documentElement.scrollLeft || document.body.scrollLeft;
    var x = event.pageX || event.clientX + scrollX;
    return x;
}
function getEventPageY(event) {
    event = getEvent(event);
    var scrollY = document.documentElement.scrollTop || document.body.scrollTop;
    var y = event.pageY || event.clientY + scrollY;
    return y;
}
function getEventButton(event) {
    event = getEvent(event);
    var i = event.which || event.button;
    return i;
}
//获取event对象,
function getEvent(event) {
    return window.event || event;
}
function stopPropagation(e) {
    if (e && e.stopPropagation) {
        //因此它支持W3C的stopPropagation()方法
        e.stopPropagation();
    }
    else {
        //否则，我们需要使用IE的方式来取消事件冒泡 
        window.event.cancelBubble = true;
        return false;
    }
}
//取消所有选择
function removeAllSelection() {
    if (!document.selection)
        window.getSelection().removeAllRanges();
    else
        document.selection.empty();
}
//判断鼠标右键是否按下
function mouseRightIsDown(event) {
    return event.which == 3 || event.button == 2;//兼容ie和非ie
}
function GetQueryString(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}
function isChrome() {
    var userAgent = navigator.userAgent; //取得浏览器的userAgent字符串
    if (userAgent.indexOf("Chrome") > -1) {
        return true;
    }
    else{
        return false;
    }
}
function setCapture(dom) {
    if (!isChrome()) {
        dom.setCapture();
    }
}
function releaseCapture(dom) {
    if (!isChrome()) {
        dom.releaseCapture();
    }
}



var kuse = function () { };
kuse.getEventPageX = function (e) {
    e = getEvent(e);
    var scrollX = document.documentElement.scrollLeft || document.body.scrollLeft;
    var x = e.pageX || e.clientX + scrollX;
    return x;
}
kuse.getEventPageY = function (e) {
    e = getEvent(e);
    var scrollY = document.documentElement.scrollTop || document.body.scrollTop;
    var y = e.pageY || e.clientY + scrollY;
    return y;
}
kuse.getEventButton = function (e) {
    e = getEvent(e);
    var i = e.which || e.button;
    return i;
}
kuse.getEvent = function (e) {
    return window.event || e;
}
kuse.stopPropagation = function (e) {
    if (e && e.stopPropagation) {
        //因此它支持W3C的stopPropagation()方法
        e.stopPropagation();
    }
    else {
        //否则，我们需要使用IE的方式来取消事件冒泡 
        window.event.cancelBubble = true;
        return false;
    }
}
//取消所有选择
kuse.stopPropagation = function removeAllSelection() {
    if (!document.selection)
        window.getSelection().removeAllRanges();
    else
        document.selection.empty();
}
//判断鼠标右键是否按下
kuse.mouseRightIsDown = function (e) {
    return e.which == 3 || e.button == 2;//兼容ie和非ie
}
kuse.GetQueryString = function (name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return unescape(r[2]); return null;
}
kuse.isChrome = function () {
    var userAgent = navigator.userAgent; //取得浏览器的userAgent字符串
    if (userAgent.indexOf("Chrome") > -1) {
        return true;
    }
    else {
        return false;
    }
    kuse.isIE = function () {
        var userAgent = navigator.userAgent; //取得浏览器的userAgent字符串
        if (userAgent.indexOf("Internet") > -1) {
            return true;
        }
        else {
            return false;
        }
    }
}
kuse.setCapture = function (dom) {
    if (!kuse.isChrome()) {
        dom.setCapture();
    }
}
kuse.releaseCapture = function (dom) {
    if (!isChrome()) {
        dom.releaseCapture();
    }
}