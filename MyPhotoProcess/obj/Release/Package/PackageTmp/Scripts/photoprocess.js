$(function () {
    var result = [];
    $("#btn_process").click(function () {
        $("#btn_process").attr("disabled", "disabled");
        initBar();
        var txt = $("#txtFile").val();
        var value = $("#ddl_value").val();
        $(".pProcess").show();
        $.post("process.ashx?t=" + new Date(), { txt: txt, value: value, type: "process" }, function (data) {

            var pResult = eval('(' + data + ')');
            if (pResult.Base.ErrorCode!==0) {
                $(".pProcess").hide();
                clearInterval(timer);
                $("#btn_process").removeAttr("disabled");
                alert('出错啦！ ErrorCode：' + pResult.Base.ErrorCode + " ErrorMessage:" + pResult.Base.ErrorMessage);
                return false;
            }

            $("#spProcessTxt").css("color", "Green");
            $(".innerBar").css("width", "100%");
            $("#spProcessTxt").html("Finished!");
            clearInterval(timer);
            $("#btn_process").removeAttr("disabled");
        });
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