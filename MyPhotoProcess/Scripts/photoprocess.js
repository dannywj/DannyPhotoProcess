/*前台处理程序
 *Update:2014-01-07
 *处理进度条不准确问题，输入验证问题
 *
*/
String.prototype.Trim = function () {
    return this.replace(/(^\s*)|(\s*$)/g, "");
};

jQuery.fn.clickOnce = function (callback) {
    this.unbind('click');
    this.click(function () {
        callback();
    });
};

$(function () {
    var result = [];
    var gTxtBtnInit = '缩放我的照片 ^_^';
    var gTxtBtnProcessing = '处理中，请稍候..';
    $("#btn_process").clickOnce(function () {

        //var ttttt=window.clipboardData.getData("Text");
        //alert(ttttt);
        //return false;


        $("#btn_process").attr("disabled", "disabled");
        $("#btn_process").html(gTxtBtnProcessing);
        initBar();
        var txt = $("#txtFile").val().Trim();
        var value = $("#ddl_value").val();
        //check input
        if (txt=='') {
            alert('请输入处理图片的路径。');
            $("#btn_process").removeAttr("disabled");
            $("#btn_process").html(gTxtBtnInit);
            return false;
        }
        $(".pProcess").show();
        $.post("process.ashx?t=" + new Date(), { txt: txt, value: value, type: "process" }, function (data) {

            var pResult = eval('(' + data + ')');
            if (pResult.Base.ErrorCode!==0) {
                $(".pProcess").hide();
                clearInterval(timer);
                $("#btn_process").removeAttr("disabled");
                $("#btn_process").html(gTxtBtnInit);
                alert('出错啦！ ErrorCode：' + pResult.Base.ErrorCode + " ErrorMessage:" + pResult.Base.ErrorMessage);
                return false;
            }

            $("#spProcessTxt").css("color", "Green");
            $(".innerBar").css("width", "100%");
            $("#spProcessTxt").html("Finished!");
            clearInterval(timer);
            $("#btn_process").removeAttr("disabled");
            $("#btn_process").html(gTxtBtnInit);
            $("#txtFile").val('');
        });
        result = [];
        var timer = setInterval(getPresent, 500);
    });


    function getPresent() {
        $.post("process.ashx?t=" + new Date(), { type: "getPresent" }, function (data) {
            result.push(data);
            result.sort(function (a, b) { return a < b ? 1 : -1 });//从大到小排序
            var presentValue = parseInt((parseFloat(result[0]) * 100)); // (parseFloat(result[0]) * 100).toFixed(2);
            $("#spProcessTxt").html(presentValue + '%');
            $(".innerBar").css("width", presentValue + '%');
        });
    }

    function initBar()
    {
        $("#spProcessTxt").css("color", "");
        $(".innerBar").css("width", "0%");
        $("#spProcessTxt").html("");
    }
});

