
var imageCropWidth = 0;
var imageCropHeight = 0;
var cropPointX = 0;
var cropPointY = 0;

// ToDo - change the size limit of the file. You may need to change web.config if larger files are necessary.
var maxSizeAllowed = 2;     // Upload limit in MB
var maxSizeInBytes = maxSizeAllowed * 1024 * 1024;
var keepUploadBox = false;  // ToDo - Remove if you want to keep the upload box
var keepCropBox = false;    // ToDo - Remove if you want to keep the crop box

$(function () {
    if (typeof $("#avatar-upload-form") !== undefined) {
        initAvatarUpload();
        $("#avatar-max-size").html(maxSizeAllowed);
        $("#avatar-upload-form input:file").on("change", function (e) {
            var files = e.currentTarget.files;
            for (var x in files) {
                if (files[x].name !== "item" && typeof files[x].name != "undefined") {
                    if (files[x].size <= maxSizeInBytes) {
                        // Submit the selected file
                        $("#avatar-upload-form .upload-file-notice").removeClass("bg-danger");
                        $("#avatar-upload-form").submit();
                    } else {
                        // File too large
                        $("#avatar-upload-form .upload-file-notice").addClass("bg-danger");
                    }
                }
            }
        });
    }

    $("#profile-image-save").on("click", function () {
        saveAvatar();
    });

    $(".avatar-img").on("click", function() {
        $("#avatar-container").hide();
        $("#avatar-edit-container").show();
    });
});

function initAvatarUpload() {
    $("#avatar-upload-form").ajaxForm({
        beforeSend: function () {
            updateProgress(0);
            $("#avatar-upload-form").addClass("hidden");
        },
        uploadProgress: function (event, position, total, percentComplete) {
            updateProgress(percentComplete);
        },
        success: function (data) {
            updateProgress(100);
            if (data.success === false) {
                $("#status").html(data.errorMessage);
            } else {
                var img = $("#crop-avatar-target");
                img.attr("src", data.fileName);
                if (!keepUploadBox) {
                    $("#avatar-upload-box").addClass("hidden");
                }
                $("#avatar-crop-box").removeClass("hidden");

                initAvatarCrop(img);
            }
        },
        error: function (data) {
            var r = jQuery.parseJSON(data.responseText);
            alert("Message: " + r.Message);
            alert("StackTrace: " + r.StackTrace);
            alert("ExceptionType: " + r.ExceptionType);
        },
        complete: function (xhr) {
        }
    });
}

function updateProgress(percentComplete) {
    $(".upload-percent-bar").width(percentComplete + "%");
    $(".upload-percent-value").html(percentComplete + "%");
    if (percentComplete === 0) {
        $("#upload-status").empty();
        $(".upload-progress").removeClass("hidden");
    }
}

function initAvatarCrop(img) {
    img.Jcrop({
        onChange: setCoordsAndImgSize,
        aspectRatio: 1,
        setSelect: [175, 100, 400, 300]
    }, function () {
        var jcropApi = this;
        var thumbnail = jcropApi.initComponent("Thumbnailer", { width: 130, height: 130 });
    });
}

function setCoordsAndImgSize(e) {
    imageCropWidth = e.w;
    imageCropHeight = e.h;
    cropPointX = e.x;
    cropPointY = e.y;
}

function saveAvatar() {
    var personId = $("#Id").attr("value");
    var img = $(".jcrop-thumb");
    $("#avatar-crop-box button").addClass("disabled");

    $.ajax({
        type: "POST",
        url: "/People/Save",
        traditional: true,
        data: {
            id: personId,
            imagePath: $("#crop-avatar-target").attr("src"),
            cropPointX: cropPointX,
            cropPointY: cropPointY,
            imageCropWidth: imageCropWidth,
            imageCropHeight: imageCropHeight
        }
    }).done(function (data) {
        if (data.success === true) {
            $("#avatar-result img").attr("src", "data:image/png;base64," + data.avatar);

            $("#avatar-result").removeClass("hidden");

            if (!keepCropBox) {
                $("#avatar-crop-box").addClass("hidden");
            }
        } else {
            alert(data.errorMessage);
        }
    }).fail(function (e) {
        alert("Cannot upload avatar at this time");
    });
}