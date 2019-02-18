$(function () {
    if (('onhashchange' in window) && ((typeof document.documentMode === 'undefined') || document.documentMode == 8)) {
        // 浏览器支持onhashchange事件
        window.onhashchange = hashChange;  // TODO，对应新的hash执行的操作函数
    } else {
        // 不支持则用定时器检测的办法
        setInterval(function () {
            // 检测hash值或其中某一段是否更改的函数， 在低版本的iE浏览器中通过window.location.hash取出的指和其它的浏览器不同，要注意
            var ischanged = isHashChanged();
            if (ischanged) {
                hashChange();  // TODO，对应新的hash执行的操作函数
            }
        }, 150);
    };
    var path = decodeURI(window.location.hash).substr(1);
    _path = path;
    getFolder(path, "");
    hashChange(path, "");
    $("#body").bind('contextmenu', function (e) {//弹出右键菜单
        var select= $(".grid-view-item-selected");
        var hover = $(".grid-view-item-hover");
        var mm1 = "#mm1";
        var mm2 = "#mm2";
        if (hover.length == 0) {
            $(mm2).menu('show', {
                left: e.pageX + 2,
                top: e.pageY + 2,
            });
        }
        else {
            $(mm1).menu('show', {
                left: e.pageX + 2,
                top: e.pageY + 2,
            });
        }
        var tag=false;
        for (var i = 0; i < select.length ; i++) {
            if (hover[0]==select[i]) {
                tag = true;
                break;
            }
        }
        if (!tag) {//如果不是在已选择的item上按右键的话移除其他已选择的
           select.removeClass("grid-view-item-selected");
        }
        hover.addClass("grid-view-item-selected");
        if ($(".grid-view-item-selected").length == 1) {//如果只选中一个,
            $(mm1).menu("enableItem", $('#mm-rename'));
            $(mm1).menu("enableItem", $('#mm-open'));
        }
        else {
            $(mm1).menu('disableItem', $('#mm-rename'));
            $(mm1).menu('disableItem', $('#mm-open'));
        }
        if (copyItems.length>0) {
            $(mm2).menu("enableItem", $('#mm-stick'));
            $(mm2).menu("enableItem", $('#mm-clearStick'));
        }
        else {
            $(mm2).menu('disableItem', $('#mm-stick'));
            $(mm2).menu('disableItem', $('#mm-clearStick'));
        }
        if (copyPath == $("#path").val() && isCopy===false) {//如果是剪切并且原路径和现路径一样,则禁止粘贴
            $(mm2).menu('disableItem', $('#mm-stick'));
        }
        return false;
    });
});
function hashChange(path, search, order) {
    path = decodeURI(window.location.hash).substr(1);
    search = _search;
    order = _order;
    $.getJSON(
        "File/GetUserFolder?_=" + Math.random(),
        { "path": path, order: order },
        function (data) {
            if (!data.isError) {
                folderInfo = data.obj;
                showFolder(data.obj, path);
            }
            else {
                alert(data.errorMsg);
            }
        }
    );
}
function getFolder(path, search, order) {
    window.location.hash = path;
    _path = path;
    _search = search;
    _order = order;
}
var _path;
var _search;
var _order;
var folderInfo;//存储当前目录的信息
var uploading = [];//格式实例[{"path":path,$doms}]
var mouseIsDown = false;
var mouseIsDown_drop = false;
var dropMove = 0;
var startX = 0;
var startY = 0;
var copyItems = "";
var copyPath = "";
var isCopy = true;//为true是复制,false为剪切
var uid = "";
var selectItems = [];
var image = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico"];//图片类型的
var video = [".mp4", ".rmvb", ".avi", ".wmv", ".flv"];//视频类型的
var text = [".ini", ".txt"];//文本类型的
var code = [".html", ".cs", ".js", ".css", ".aspx", ".ashx", ".xml", ".htm", ".config"];//代码类型的
var zip = [".zip", ".7z", ".rar"];//压缩包类型的
var bt = [".torrent"];//种子类型的
var excel = [".xlsx", ".xls"];//Excel类型的
var word = [".docx", ".doc"];//word类型的
var ppt = [".pptx", ".ppt"];//ppt类型的
var json = [{ "content": image, "name": "image" }, { "content": text, "name": "text" }, { "content": code, "name": "code" }, { "content": zip, "name": "zip" }, { "content": bt, "name": "bt" }, { "content": excel, "name": "excel" }, { "content": word, "name": "word" }, { "content": ppt, "name": "ppt" }, { "content": video, "name": "video" }];
function showFolder(data, path) {
    //将正在上传的保存起来
    var path1 = $("#path").val();
    var isupload = $(".isupload");
    var index = -1;
    for (var i = 0; i < uploading.length; i++) {
        if (uploading[i].path == path1) {
            index = i;
            break;
        }
    }
    if (index > -1) {//已存在
        uploading[index] = { "path": path1, "cont": isupload };
    }
    else {//不存在
        uploading.push({ "path": path1, "cont": isupload });
    }

    $("#path").val(path);
    if (path=="") {
        uid = data.name;
    }
    //搞定导航
    fuckNav();
    var str = "";
    //先渲染文件夹
    for (var i = 0; i < data.folders.length; i++) {
        str += "<div onmousemove='dropFileMove(event)' onmousedown='dropDown(event,this);if(getEventButton(event)!=1) return;stopPropagation(event);$(\"#mm1\").menu(\"hide\");$(\"#mm2\").menu(\"hide\");' ondblclick='openFile(this,event,0)' onclick='selectItem(this,event)' onmouseover='itemmouseover(this)' onmouseout='itemmouseout(this)' class='grid-view-item' data-folder_name='" + data.folders[i].name + "'>";
        str += "<div class='fileicon dir-large'></div>";
        str += "<div class='file-name'><span>" + data.folders[i].name + "</span><input type='text' style='display:none' onkeydown='if (event.keyCode == 13)  rename(this,event)' ondblclick='stopPropagation(event)' onblur='rename(this,event)' value='" + data.folders[i].name + "'></div>";
        str += "</div>";
    }
    //再渲染文件
    for (var i = 0; i < data.files.length; i++) {
        str += "<div onmousemove='dropFileMove(event)' onmousedown='dropDown(event,this);if(getEventButton(event)!=1) return;stopPropagation(event);$(\"#mm1\").menu(\"hide\");$(\"#mm2\").menu(\"hide\");' ondblclick='openFile(this,event,1)' onclick='selectItem(this,event)' onmouseover='itemmouseover(this)' onmouseout='itemmouseout(this)' class='grid-view-item' data-file_name='" + data.files[i].name + "'>";
        var imei = checkIMEI(data.files[i].name);
        if (imei == "image") {//如果是图片类型的,则直接预览该图片
            str += "<div class='fileicon'><img src='File/Preview?path=" + path + "/" + data.files[i].name + "'/></div>";
        }
        else {
            str += "<div class='fileicon fileicon-large-" + imei + "'></div>";
        }     
        str += "<div class='file-name'><span>" + data.files[i].name + "</span><input type='text' style='display:none' onkeydown='if (event.keyCode == 13)  rename(this,event)' ondblclick='stopPropagation(event)' onblur='rename(this,event)' value='" + data.files[i].name + "'></div>";
        str += "</div>";
    }

    $("#body").html(str);

    var path1 = $("#path").val();
    var index = -1;
    for (var i = 0; i < uploading.length; i++) {
        if (uploading[i].path == path1) {
            index = i;
            break;
        }
    }
    if (index > -1) {//已存在
        for (var i = 0; i < uploading[index].cont.length; i++) {
            $("#body").prepend($(uploading[index].cont[i]));
        }
    }
    //把正在上传的文件追加进去
}
function checkIMEI(name) {
    var ex = name.substring(name.lastIndexOf(".")).toLowerCase();//获取文件后缀
    var res = "";
    for (var i = 0; i < json.length; i++) {
        for (var j = 0; j < json[i].content.length; j++) {
            if (json[i].content[j]==ex) {
                res = json[i].name;
                break;
            }
        }
        if (res!="") {
            break;
        }
    }
    if (res=="") {
        res = "other";
    }
    return res;
}
function itemmouseover(e)
{
    $(e).addClass("grid-view-item-hover");
}
function itemmouseout(e) {
    $(e).removeClass("grid-view-item-hover");
}
function openFile(item,event,n) {//传入的参数应该是一个grid-view-item的节点
    if (n == 1) {//为打开文件
        var path = $("#path").val() + "/" + $(item).data("file_name");
        switch (checkIMEI($(item).data("file_name"))) {
            //在此设置各种文件的打开方式
            case "text": window.open("textpreview.aspx?path=" + path); break;
            //...
            default: window.open("otherpreview.aspx?path=" + path); break;
        }
    }
    if (n==0) {//为打开文件夹
        var path = $("#path").val() + "/" + $(item).data("folder_name");
        getFolder(path, "", $(".order1.menu_select").attr("id") + $(".order2.menu_select").attr("id"));
    }
}
function selectItem(item,event) {
    if (event.ctrlKey != 1) {//没有按着ctrl键,不是多选
        clearSelect();
    }
    if (!$(item).hasClass("grid-view-item-selected")) {//如果本身不是选中状态,则选中,否则,取消选中
        $(item).addClass("grid-view-item-selected");
    }
    else {
        $(item).removeClass("grid-view-item-selected");
    }
    stopPropagation(event);
}
function clearSelect() {
    $(".grid-view-item-selected").removeClass("grid-view-item-selected");
}
function fuckNav() {
    var thispath = $("#path").val();
    //console.log(thispath);
    var path = thispath.split("/");
    var str = "";
    if (path.length == 1) {//只有一层,为顶层目录
        str += "<span>全部文件</span>";
    }
    else {
        str += "<a href='javascript:void(0)' onclick='comeback()'>返回上一级</a>";
        str += "<span> | </span>";
        str += "<a href='javascript:void(0)' onclick='getFolder(\"\",\"\", $(\".order1.menu_select\").attr(\"id\") + $(\".order2.menu_select\").attr(\"id\"))'>全部文件</a>";
        str += "<span> > </span>";
        for (var i = 1; i < path.length-1; i++) {
            str += "<a class='jumpFolder' href='javascript:void(0);' onclick=jumpFolder(this,event)>" + path[i] + "</a>";
            str += "<span> > </span>";
        }
        str += "<span>" + path[path.length - 1] + "</span>";
    }
    $("#nav").html(str);
}
function comeback() {
    var path = $("#path").val();
    path = path.substring(0,path.lastIndexOf("/"));
    //alert(path);
    getFolder(path, "", $(".order1.menu_select").attr("id") + $(".order2.menu_select").attr("id"));
}
function jumpFolder(item, e) {
    var arr = $("#nav>.jumpFolder ");
    var path = "/";
    for (var i = 0; i < arr.length; i++) {
        path += $(arr[i]).text();
        path += "/";
        if (arr[i] == item) {
            break;
        }
    }
    //alert(path);
    path = path.substring(0,path.length-1);
    getFolder(path, "", $(".order1.menu_select").attr("id") + $(".order2.menu_select").attr("id"));
}
function newFolder() {//创建文件夹的方式是先创建一个默认命名的文件夹再执行对文件夹重命名的方法
    var path = $("#path").val();
    $.post(
        "File/NewFolder",
        {"path":path},
        function (data) {
            clearSelect();
            var str = "";
            str += "<divonmousemove='dropFileMove(event)' onmousedown='dropDown(event,this);if(getEventButton(event)!=1) return;stopPropagation(event);$(\"#mm1\").menu(\"hide\");$(\"#mm2\").menu(\"hide\");' ondblclick='openFile(this,event,0)' onclick='selectItem(this,event)' onmouseover='itemmouseover(this)' onmouseout='itemmouseout(this)' class='grid-view-item grid-view-item-selected' data-folder_name='" + data + "'>";
            str += "<div class='fileicon dir-large'></div>";
            str += "<div class='file-name'><span style='display:none'>" + data + "</span><input type='text' onkeydown='if (event.keyCode == 13)  rename(this,event)' onblur='rename(this,event)' value='" + data + "'></div>";
            str += "</div>";
            var dom = $(str);
            $("#body").prepend($(dom));
            $(dom).children().children("input").select();
        }
    );
}
function rename(item, event) {//重命名
    var newName = $(item).val();
    var oldName = $(item).parent().children("span").text();//不能用兄弟元素获取,某些浏览器会将换行也认为是兄弟元素
    $(item).parent().children("span").show();
    $(item).hide();
    //console.log(newName);
    //console.log(oldName);
    var reg = new RegExp('^[^\\\\\\/:*?\\"<>|]+$');//验证文件名是否合法的正则,这么多\转义,日了狗了简直
    if (newName.length>100) {
        $(item).val(oldName);
        alert("你傻逼吧,文件夹的名这么长");
        return;
    }
    if (!reg.test(newName)) {
        $(item).val(oldName);
        alert("文件名不合法");
        return;
    }
    if (newName != oldName) {
        $(item).parent().children("span").text(newName);//将显示名字修改至一样,防止网速慢的时候修改两次而引发错误
        $.post(
            "File/Rename",
            { "path": $("#path").val(), "newName": newName, "oldName": oldName },
            function (data) {//改成功,则改名字,改不成,则还是原来的名字并弹窗说原因
                data = eval(data);
                if (!data.isError) {
                    //$(item).parent().children("span").text(newName);
                    if ($(item).parent().parent().data("folder_name")) {
                        $(item).parent().parent().data("folder_name", newName);
                    }
                    else {
                        $(item).parent().parent().data("file_name", newName);
                        //alert($(item).parent().parent().data("file_name"));
                    }
                }
                else {
                    $(item).val(oldName);
                    $(item).parent().children("span").text(oldName);//出错了再将名字修改回来
                    alert(data.errorMsg);
                }
            }
        );
    }
}
function dorename(item, event) {
    var select = $(".grid-view-item-selected");
    if (select.length==1) {
        //console.log(1);
        select.children().children("span").hide();
        select.children().children("input").show().select();
        clearSelect();
        select.addClass("grid-view-item-selected");
    }
    stopPropagation(event);
    //event.stopPropagation();
}
function refreshFolder() {
    var path = $("#path").val();
    getFolder(path, "",$(".order1.menu_select").attr("id") + $(".order2.menu_select").attr("id"));
    hashChange(path, "", $(".order1.menu_select").attr("id") + $(".order2.menu_select").attr("id"));
}
function doOpenFile(item, event) {
    var select = $(".grid-view-item-selected");
    if (select.length == 1) {
        var i = 0;
        if (!select.children().hasClass("dir-large")) {
            i = 1;
        }
        openFile(select[0], event, i);
        clearSelect();
        select.addClass("grid-view-item-selected");
    }
    stopPropagation(event);
    //event.stopPropagation();
}
function delFiles(item,event) {
    var select = $(".grid-view-item-selected");
    if (confirm("确认要彻底删除这" + select.length + "项吗？")) {
        //console.log("确认删除");
        var str = "";
        for (var i = 0; i < select.length; i++) {
            if ($(select[i]).data("folder_name")) {
                str += $(select[i]).data("folder_name") + "/";//用/分割
            }
            else {
                str += $(select[i]).data("file_name") + "/";
            }
        }
        //console.log(str);
        var path = $("#path").val();
        $.post(
            "File/DelFile",
            { "path": $("#path").val(), "names": str },
            function (data) {//删除
                data = eval(data);
                if (data.notError === true) {//没有错
                    select.remove();
                }
                else {
                    alert(data.errorMsg);
                }
            }
        );
    }
    stopPropagation(event);
    //event.stopPropagation();
}
function copyFile(item, event, i) {
    isCopy = i;
    copyItems = "";
    var select = $(".grid-view-item-selected");
    for (var i = 0; i < select.length; i++) {
        if ($(select[i]).data("folder_name")) {
            copyItems += $(select[i]).data("folder_name") + "/";//用/分割
        }
        else {
            copyItems += $(select[i]).data("file_name") + "/";
        }
    }  
    copyPath = $("#path").val();
    stopPropagation(event);
    //event.stopPropagation();
}
function stick(item, event) {
    if ($("#mm-stick").hasClass("menu-item-disabled")) {
        return;
    }
    if (copyItems.length > 0) {
        var names = copyItems.split("/");
        for (var i = 0; i < names.length; i++) {
            if (names[i]) {
                var np = "root" + $("#path").val() + "/" + names[i];
                var op = "root" + copyPath + "/" + names[i];
                if (np.indexOf(op) == 0 && np != op) {
                    alert("目标文件夹是源文件夹的子文件夹,因此无法复制或粘贴")
                    return;
                }
            }
        }
        $.post(
            "File/StickFiles",
            { "names": copyItems, "oldPath": copyPath, "newPath": $("#path").val(), "isCopy": isCopy },
            function (data) {
                data = JSON.parse(data);
                if (isCopy === true && data.error === true) {
                    alert("复制失败,原因:" + data.message);
                }
                else {
                    refreshFolder();
                    if (isCopy === false) {
                        copyItems = "";//剪贴完成后清空剪切板
                    }
                }
            }
        );
    }
    stopPropagation(event);
}
function mouseup(e) {
    mouseIsDown = false;
    dropUp(e);
    $('.onselect').hide();
    $('.grid-view-item-onselected').removeClass('grid-view-item-onselected').addClass('grid-view-item-selected');
    $('.unselected').removeClass('unselected');
    releaseCapture(document.body);//结束窗口外鼠标捕捉
    //stopPropagation(e);
    //e.stopPropagation();
    //console.log(2);
}
function mousemove(e) {
    if (mouseIsDown) {
        setCapture(document.body);
        //if (e.which != 1) {
        //    mouseup(e);
        //}
        //document.selection.empty();
        //if (!document.selection)
        //    window.getSelection().removeAllRanges();
        removeAllSelection();
        var endX = getEventPageX(e);
        var endY = getEventPageY(e);
        var height = Math.abs(endY - startY)-2;
        var width = Math.abs(endX - startX) - 2;
        var top = Math.min(startY, endY);//不得小于0
        var left = Math.min(startX, endX);
        if (top<0) {
            height += top;
            top = 0;
        }
        if (left<0) {
            width += left;
            left = 0;
        }
        $(".onselect").show().css("height", height).css("width", width).css("top", top).css("left", left);
        var allItems = $(".grid-view-item");//所有元素
        if (e.ctrlKey != 1) {//没有按着ctrl键,不是多选,则清空原本的选择
            clearSelect();
        }
        for (var i = 0; i < allItems.length; i++) {
            var top2 = $(allItems[i]).offset().top;
            var left2 = $(allItems[i]).offset().left;
            //if ((top2 > top && top2 < top + height && left2 > left && left2 < left + width) || (top2 > top && top2 < top + height && left2 + 120 > left && left2 + 120 < left + width) || (top2 + 127 > top && top2 + 127 < top + height && left2 > left && left2 < left + width) || (top2 + 127 > top && top2 + 127 < top + height && left2+120 > left && left2+120 < left + width)) {//选中了,
            if (isRectIntersect(top, left, top2, left2, height, width, 127, 120)) {
                if ($(allItems[i]).hasClass("grid-view-item-selected")) {//如果原本就是选中的,则取消选择,并标记,防止后续拖动再将其选中
                    $(allItems[i]).removeClass("grid-view-item-selected").addClass("unselected");
                }
                else {
                    if (!$(allItems[i]).hasClass("unselected")) {
                        $(allItems[i]).addClass("grid-view-item-onselected");
                    }
                }
            }
            else {
                $(allItems[i]).removeClass("grid-view-item-onselected");
                if ($(allItems[i]).hasClass("unselected")) {
                    $(allItems[i]).addClass("grid-view-item-selected");
                }
            }
        }
    }
}
function isRectIntersect(top,left,top2,left2,height,width,height2,width2) {//判断是否有交集,
    var x01 = left, x02 = left + width, y01 = top, y02 = top + height;
    var x11 = left2, x12 = left2 + width2, y11 = top2, y12 = top2 + height2;
    var zx = Math.abs(x01+x02-x11-x12); //两个矩形重心在x轴上的距离的两倍
    var x = Math.abs(x01-x02)+Math.abs(x11-x12); //两矩形在x方向的边长的和
    var zy = Math.abs(y01+y02-y11-y12); //重心在y轴上距离的两倍
    var y = Math.abs(y01-y02)+Math.abs(y11-y12); //y方向边长的和
    return (zx <= x && zy <= y);
}
var uploader;//声明全局变量
$(function () {//大文件上传在ie下仍然不理想
    //开始处理上传功能
    uploader = WebUploader.create({
        chunked: true,//分段上传
        chunkSize: 5242880,//5m分一段,据说理论上效果最好
        // swf文件路径
        swf: '~/lib/webuploader/Uploader.swf',
        auto: true,
        // 文件接收服务端。
        server: 'File/Upload',
        // 选择文件的按钮。可选。
        // 内部根据当前运行是创建，可能是input元素，也可能是flash.
        pick: '#picker',
        // 不压缩image, 默认如果是jpeg，文件上传前会压缩一把再上传！
        resize: false
    });
    uploader.on('fileQueued', function (file) {
        // 创建缩略图
        // 如果为非图片文件，可以不用调用此方法。
        // thumbnailWidth x thumbnailHeight 为 100 x 100
        var str = "";
        str += "<div id='p_" + file.id + "' class='isupload'><div id='" + file.id + "' class='upload_div'></div><div id='text_" + file.id + "' class='text_upload_div'>等待上传</div>";
        var imei = checkIMEI(file.name);
        if (imei == "image") {//如果是图片类型的,则直接预览该图片
            str += "<div class='fileicon'><img src='" + file.id + "'/></div>";
            uploader.makeThumb(file, function (error, src) {//这个方法是异步执行的
                if (error) {
                    return;
                }
                $("img[src='" + file.id + "']").attr('src', src);
            }, 100, 100);//特么的生成的预览图不是原比例的
        }
        else {
            str += "<div class='fileicon fileicon-large-" + imei + "'></div>";
        }
        str += "<div class='file-name'><span>" + file.name + "</span></div>";
        str += "</div>";
        var dom = $(str);
        $("#body").prepend($(dom));

        //创建该文件的预览并显示等待上传,
    });
    // 文件上传过程中创建进度条实时显示。
    uploader.on('uploadProgress', function (file, percentage) {
        //实时显示上传进度,有一下框框,框框中类似于水满的效果,
        $("#text_" + file.id).text("正在上传" + parseInt(percentage * 100) + "%");
        $("#" + file.id).css("height", (percentage * 100) + "%");
    });

    uploader.on('uploadSuccess', function (file, response) {
        response = $.parseJSON(response);
        //上传成功
        //合并文件夹
        //刷新页面
        if (response.hasError === true) {
            $("#text_" + file.id).text("上传失败");
            alert("文件 " + file.name + " 上传失败,原因:" + response.message);
            removeUploading(file.id, response.path);
        }
        else {
            if (response.chunked == true) {
                $("#text_" + file.id).text("正在合并");

                for (var i = 0; i < uploading.length; i++) {
                    if (uploading[i].path == response.path) {
                        for (var j = 0; j < uploading[i].cont.length; j++) {
                            if ($(uploading[i].cont[j]).attr("id") == "p_" + file.id) {
                                $(uploading[i].cont[j]).children("#text_" + file.id).text("正在合并");
                                $(uploading[i].cont[j]).children("#" + file.id).css("height", "100%");
                            }
                        }
                    }
                }

                $.post("File/MergeFile", { fileName: response.fileName, path: response.path },
                function (data) {
                    data = $.parseJSON(data);
                    if (data.hasError) {
                        alert('文件合并失败！');
                    } else {
                        removeUploading(file.id, response.path);
                        refreshFolder();//如果上传的路径和当前路径一样,则刷新
                    }
                });
            }
            else {
                removeUploading(file.id, response.path);
                refreshFolder();
            }
        }
        uploader.removeFile(file);
    });

    uploader.on('uploadError', function (file) {

    });

    uploader.on('uploadComplete', function (file) {

    });

    uploader.on('all', function (type) {

    });

    uploader.on('uploadStart', function () {
        uploader.option('formData', { path: $("#path").val() });
    });
});
function removeUploading(id,path) {
    $("#p_" + id).remove();//移除正在下载的
    for (var i = 0; i < uploading.length; i++) {
        if (uploading[i].path == path) {
            for (var j = 0; j < uploading[i].cont.length; j++) {
                if ($(uploading[i].cont[j]).attr("id") == "p_" + id) {
                    uploading[i].cont[j] = null;//晚上睡觉就在想这是不是多写个等号,妈逼的,果然
                }
            }
        }
    }
}
function downloadFiles() {
    var select = $(".grid-view-item-selected");
    var path = $("#path").val();
    var names = "";
    for (var i = 0; i < select.length; i++) {
        if ($(select[i]).data("folder_name")) {
            names += $(select[i]).data("folder_name") + "/";//用/分割
        }
        else {
            names += $(select[i]).data("file_name") + "/";
        }
    }
    var iframe = document.getElementById("TempCreatedIframeElement");
    if (iframe == null) {
        iframe = document.createElement("iframe");
        iframe.id = "TempCreatedIframeElement";
        iframe.style.display = "none";
        document.body.appendChild(iframe);
    }
    iframe.src = "File/DownloadFiles?names=" + escape(names) + "&path=" + escape(path);
}
//快捷键注册
document.onkeydown=function(event){
    var e = event || window.event || arguments.callee.caller.arguments[0];
    if (e && e.keyCode == 67) { // 按 C 键为复制
        if (e.ctrlKey == 1) {
            $("#mm-copy").trigger("click");
        }
    }
    if (e && e.keyCode == 88) { // 按 X 键为剪切
        if (e.ctrlKey == 1) {
            $("#mm-cut").trigger("click");
        }
    }
    if (e && e.keyCode == 86) { // 按 V 键为粘贴
        if (e.ctrlKey == 1) {
            $("#mm-stick").trigger("click");
        }
    }
    if (e && e.keyCode == 68) { // 按 D 键为下载
        if (e.ctrlKey == 1) {
            $("#mm-download").trigger("click");
            return false;
        }
    }
};
function showAttr(e) {//查看文件属性
    var select = $(".grid-view-item-selected");
    var title = "";
    var html = "";
    if (select.length == 1) {//选中了
        var isFile = !!$(select[0]).data("file_name");
        if (isFile) {
            for (var j = 0; j < select.length; j++) {
                for (var i = 0; i < folderInfo.files.length; i++) {
                    if (folderInfo.files[i].name == $(select[j]).data("file_name")) {
                        title = folderInfo.files[i].name;
                        var ex = "文件";
                        var size = folderInfo.files[i].size;
                        var creatDate = folderInfo.files[i].CreatDateTime2;
                        var ModifiedDate = folderInfo.files[i].ModifiedDate2;
                        var containFile = folderInfo.files[i].fileLength;
                        var containFolder = folderInfo.files[i].folderLength;
                        var sizetext = "";
                        if (size > 1024) {
                            sizetext = (size / 1024).toFixed(2) + "KB";
                        }
                        if (size > 1024 * 1024) {
                            sizetext = (size / 1024 / 1024).toFixed(2) + "MB";
                        }
                        if (size > 1024 * 1024 * 1024) {
                            sizetext = (size / 1024 / 1024 / 1024).toFixed(2) + "GB";
                        }
                        sizetext += sizetext && ",";
                        html = "<input type='text' value='" + folderInfo.files[i].name + "'><br/>";
                        html += "<div><span>类型:</span><span>文件</span></div>";
                        html += "<div><span>大小:</span><span>" + sizetext + size + "字节</span></div>";
                        html += "<div><span>创建时间:</span><span>" + creatDate + "</span></div>";
                        html += "<div><span>修改时间:</span><span>" + ModifiedDate + "</span></div>";
                        break;
                    }
                }
            }
        }
        else {
            for (var i = 0; i < folderInfo.folders.length; i++) {
                for (var j = 0; j < select.length; j++) {
                    if (folderInfo.folders[i].name == $(select[j]).data("folder_name")) {
                        title = folderInfo.folders[i].name;
                        var size = folderInfo.folders[i].size;
                        var creatDate = folderInfo.folders[i].CreatDateTime2;
                        var containFile = folderInfo.folders[i].fileLength;
                        var containFolder = folderInfo.folders[i].folderLength;
                        var sizetext = "";
                        if (size > 1024) {
                            sizetext = (size / 1024).toFixed(2) + "KB";
                        }
                        if (size > 1024 * 1024) {
                            sizetext = (size / 1024 / 1024).toFixed(2) + "MB";
                        }
                        if (size > 1024 * 1024 * 1024) {
                            sizetext = (size / 1024 / 1024 / 1024).toFixed(2) + "GB";
                        }
                        sizetext += sizetext && ",";
                        html = "<input type='text' value='" + folderInfo.folders[i].name + "'><br/>";
                        html += "<div><span>类型:</span><span>文件夹</span></div>";
                        html += "<div><span>大小:</span><span>" + sizetext + size + "字节</span></div>";
                        html += "<div><span>包含:</span><span>" + containFolder + "个文件夹," + containFile + "个文件</span></div>";
                        html += "<div><span>创建时间:</span><span>" + creatDate + "</span></div>";
                        break;
                    }
                }
            }
        }
    }
    if (select.length > 1) {//选中了多个
        title = ($(select[0]).data("folder_name") || $(select[0]).data("file_name")) + " 等" + select.length + "个"
        var size = "";
        var containFile = 0;
        var containFolder = 0;
        for (var i = 0; i < folderInfo.files.length; i++) {
            for (var j = 0; j < select.length; j++) {
                if (folderInfo.files[i].name == $(select[j]).data("file_name")) {
                    size = size * 1 + folderInfo.files[i].size * 1;
                    containFile++;
                }
            }
        }
        for (var i = 0; i < folderInfo.folders.length; i++) {
            for (var j = 0; j < select.length; j++) {
                if (folderInfo.folders[i].name == $(select[j]).data("folder_name")) {
                    size = size * 1 + folderInfo.folders[i].size * 1;;
                    containFolder++;
                    containFolder = containFolder * 1 + folderInfo.folders[i].folderLength * 1;
                    containFile = containFile * 1 + folderInfo.folders[i].fileLength * 1;
                }
            }
        }
        var sizetext = "";
        if (size > 1024) {
            sizetext = (size / 1024).toFixed(2) + "KB";
        }
        if (size > 1024 * 1024) {
            sizetext = (size / 1024 / 1024).toFixed(2) + "MB";
        }
        if (size > 1024 * 1024 * 1024) {
            sizetext = (size / 1024 / 1024 / 1024).toFixed(2) + "GB";
        }
        sizetext += sizetext && ",";
        html += "<div><span>大小:</span><span>" + sizetext + size + "字节</span></div>";
        html += "<div><span>包含:</span><span>" + containFolder + "个文件夹," + containFile + "个文件</span></div>";
    }
    if (select.length < 1) {//未选中
        if ($("#path").val() == "") {
            title = "全部文件";
        }
        else {
            title = folderInfo.name;
        }
        var size = folderInfo.size;
        var creatDate = folderInfo.CreatDateTime2;
        var containFile = folderInfo.fileLength;
        var containFolder = folderInfo.folderLength;
        var sizetext = "";
        if (size>1024) {
            sizetext = (size / 1024).toFixed(2) + "KB";
        }
        if (size>1024*1024) {
            sizetext = (size / 1024 / 1024).toFixed(2) + "MB";
        }
        if (size > 1024 * 1024 * 1024) {
            sizetext = (size / 1024 / 1024 / 1024).toFixed(2) + "GB";
        }
        html = "<input type='text' value='" + title + "'><br/>";
        html += "<div><span>类型:</span><span>文件夹</span></div>";
        html += "<div><span>大小:</span><span>" + sizetext + size + "字节</span></div>";
        html += "<div><span>包含:</span><span>" + containFolder + "个文件夹," + containFile + "个文件</span></div>";
        html += "<div><span>创建时间:</span><span>" + creatDate + "</span></div>";
    }
    title = ((title.length > 20 && title.substring(0, 20) + "...") || title) + " 属性";
    //创建一个随机id
    var id=parseInt(Math.random()*10000000000);
    var str = "<div class='attrWin' id='win" + id + "' style=\"display:none\">" + html + "</div>";
    $("body").append(str);
    $("#win" + id).window({
        width: 300,
        height: 350,
        title: title,
        resizable: false,
        collapsible: false,
        minimizable: false,
        maximizable: false,
        onClose: function () {
            $("#win" + id).window("destroy");//删除该窗口
        }
    });
    $("#win" + id).window("open");
    var top = $("#mm1").offset().top || $("#mm2").offset().top;
    var left = $("#mm1").offset().left || $("#mm2").offset().left;
    $("#win" + id).window("move", { left: left, top: top })
}
function mousedown(event) {
    var i = event.which || event.button;
    if (i!=1) {
        return;
    }
    mouseIsDown = true; startX = getEventPageX(event); startY = getEventPageY(event);
    //setCapture(document.body);//开始窗口外鼠标捕捉
    //alert(startX + "," + startY);
}
function changeOrder1(e)
{
    $(".order1").removeClass("menu_select");
    $(e).addClass("menu_select");
    refreshFolder();

}
function changeOrder2(e) {
    $(".order2").removeClass("menu_select");
    $(e).addClass("menu_select");
    refreshFolder();
}
function dropFileMove(e) {
    //先捕捉鼠标按下如果拖动条件,则继续,否则直接return,再捕捉鼠标移动,再捕捉鼠标抬起判断抬起时鼠标鼠否在文件夹上,在,则移动
    if (!mouseIsDown_drop) {
        return;
    }
    if (dropMove < 10) {
        dropMove++;
        return;
    }
    console.log(2);
    var Y = getEventPageY(e);
    var X = getEventPageX(e);
    if ($("#dropItem").is(":hidden")) {//首次拖动
        if (!$(".grid-view-item-hover").hasClass("grid-view-item-selected")) {//如果鼠标不在已选中的item上,则清除现有选择,将鼠标在的item加入选择
            clearSelect();
            $(".grid-view-item-hover").addClass("grid-view-item-selected");
        }
        $("#dropItem").show();
        $("#drop_file_num span").hide();
        var selectedItems = $(".grid-view-item-selected");
        if (selectedItems.length > 1) {
            $("#drop_file_num span").text(selectedItems.length);
            $("#drop_file_num span").show();
        }
        var str = "";
        for (var i = 0; i < selectedItems.length; i++) {
            if ($(selectedItems[i]).children(".fileicon").hasClass("dir-large")) {
                str += "<img src='../images/Folder.png' />";
                continue;
            }
            if ($(selectedItems[i]).children(".fileicon").children("img").length > 0) {
                var img_url = $(selectedItems[i]).children(".fileicon").children("img").attr("src");
                var imgid = "img_" + Math.floor(Math.random() * 1000000);
                str += "<img id='" + imgid + "' src='" + img_url + "' />";
                setimg(img_url, imgid)
            }
            else {
                var classStr = $(selectedItems[i]).children(".fileicon").attr("class");
                var startIndex = classStr.indexOf("fileicon-large-") + 15;
                str += "<img src='../images/" + classStr.substring(startIndex) + ".png' />";
            }
        }
        $("#drop_file_img").html(str);
    }
    var top = Y - 80;
    var left = X - 50;
    if (top < 0) {
        top = 0;
    }
    if (left < 0) {
        left = 0;
    }
    $("#dropItem").css("top", top).css("left", left);

    $(".drop-hover").removeClass("drop-hover");//移除所有文件夹的hover样式
    var obj = $(".dir-large");
    for (var i = 0; i < obj.length; i++) {
        var objY = $(obj[i]).parent().offset().top;
        var objX = $(obj[i]).parent().offset().left;
        if (objY < Y && objY + 129 > Y && objX < X && objX + 122 > X) {//如果鼠标落在了文件夹上,则为文件夹添加上hover的样式
            $(obj[i]).parent().addClass("drop-hover");
            break;
        }
    }
}
function dropDown(event, sender) {
    if ($(sender).find("input:hidden").length==0) {//如果是重命名状态则取消
        return;
    }
    if (true) {
        if (!kuse.isIE) {
            setCapture(document.body);
        }
        mouseIsDown_drop = true;
    }
}
function dropUp(e) {
    if (!mouseIsDown_drop) {
        return;
    }
    stopPropagation(e);
    mouseIsDown_drop = false;
    dropMove = 0;
    releaseCapture(document.body);//结束窗口外鼠标捕捉
    if ($(".drop-hover").length > 0 && !$("#dropItem").is(":hidden")) {//如果满足拖动完成的条件(有处于hover样式的文件夹)并且有正在拖动的对象
        var path = $("#path").val();
        var selectedItems = $(".grid-view-item-selected");
        var copyItems = "";
        for (var i = 0; i < selectedItems.length; i++) {
            if ($(selectedItems[i]).data("folder_name")) {
                copyItems += $(selectedItems[i]).data("folder_name") + "/";//用/分割
            }
            else {
                copyItems += $(selectedItems[i]).data("file_name") + "/";
            }
            if ($(selectedItems[i]).data("folder_name") == $(".drop-hover").data("folder_name") ) {
                if (selectedItems.length != 1) {
                    alert("目标文件夹是源文件夹的子文件夹,因此无法移动");
                }
                $("#dropItem").hide();
                $(".drop-hover").removeClass("drop-hover");//移除所有文件夹的hover样式
                return;
            }
        }
        $.post(
            "File/StickFiles",
            { "names": copyItems, "oldPath": path, "newPath": path + "/" + $(".drop-hover").data("folder_name"), "isCopy": false },
            function (data) {
                refreshFolder();
            }
        );
    }
    $("#dropItem").hide();
    $(".drop-hover").removeClass("drop-hover");//移除所有文件夹的hover样式
}
function setimg(img_url, imgid) {
    var img = new Image();
    img.src = img_url;
    img.onload = function () {
        var w = img.width;
        var h = img.height;
        var imgobj = $("#" + imgid);
        if (w > h) {
            var top = (95 - h / (w / 95)) / 2;//根据长宽计算出top和left的值                  
            imgobj.css("width", "95%").css("height", "auto").css("top", top);
        }
        else {
            var left = (95 - w / (h / 95)) / 2;//根据长宽计算出top和left的值     
            imgobj.css("height", "95%").css("width", "auto").css("left", left);
        }
    };
}