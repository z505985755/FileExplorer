window.onerror = function (errorMessage, scriptURI, lineNumber, columnNumber, errorObj) {
    //Console.log("错误信息：" + errorMessage);
    //Console.log("出错文件：" + scriptURI);
    //Console.log("出错行号：" + lineNumber);
    //Console.log("出错列号：" + columnNumber);
    //Console.log("错误详情：" + errorObj);
    var xmlhttp = new XMLHttpRequest() || new ActiveXObject("Microsoft.XMLHTTP");
    var url = "ashx/errorCatch.ashx";//错误处理的接收url
    xmlhttp.open("POST", url, true);
    xmlhttp.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
    xmlhttp.send(
        "errorMessage=" + encodeURIComponent(errorMessage) +
        "&scriptURI=" + encodeURIComponent(scriptURI) +
        "&lineNumber=" + encodeURIComponent(lineNumber) +
        "&columnNumber=" + encodeURIComponent(columnNumber) +
        "&errorObj=" + encodeURIComponent(errorObj)
    );
}