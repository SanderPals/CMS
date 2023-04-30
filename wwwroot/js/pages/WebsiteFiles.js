var websiteId = $("#websiteId").val(),
    websiteLanguageId = $("#websiteLanguageId").val(),
    websiteFileId,
    activateCheckbox;

$(document).ready(function () {
    //Make menu item active
    $("#navigation #navResources").addClass("active");

    var data = {
        WebsiteId: websiteId
    };

    //Retrieves upload fields
    $.ajax({
        url: '/spine-api/website/uploads',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: "application/json;",
        success: function (result) {
            var response = result.value;
            fillUploads(response);

            //If there are no resources in the database, then hide buttons
            if (!response.resources) { $("#websiteBtns").remove(); }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });

    //Click event for deleting a file
    $("#deleteFile").on("click", function () {
        deleteFile(websiteFileId);
    });

    //Click event for changing file status
    $("#saveFileStatus").on("click", function () {
        saveFileStatus(websiteFileId);
    });
});

//Add upload boxes
function fillUploads(result) {
    $.each(result.data, function (i, val) {
        $("#cloneUploadObject").clone().appendTo("#websiteUploads");
        $("#websiteUploads").find("#cloneUploadObject").removeClass("hidden").attr("id", "wu" + val.Id).attr("data-id", val.Id);

        $("#wu" + val.Id).find("h4").html(val.Heading);
        $("#wu" + val.Id).find("#uploader").attr("id", "uploader" + val.Id);

        //To save the order of the files
        $(".saveFileOrder").off().on("click", function () {
            //Get Id of the upload field
            var wuId = $(this).parents(".websiteUploadField").data("id");

            saveFileOrder(wuId);
        });

        var uploader = $("#uploader" + val.Id).off().pluploadQueue({
            runtimes: 'html5,flash,silverlight,html4',
            url: "/spine-api/website/file/add",

            chunk_size: val.MaxSize + 'mb',
            rename: true,
            dragdrop: true,
            multiple_queues: true,

            filters: {
                max_file_size: val.MaxSize,
                mime_types: [
                    { title: "Files", extensions: val.FileExtensions }
                ]
            },
            multipart: true,
            multipart_params: {
                "width": val.Width,
                "height": val.Height,
                "autoCrop": false,
                "WebsiteUploadId": val.Id,
                "WebsiteId": websiteId,
                "WebsiteLanguageId": websiteLanguageId
            },

            // Flash settings
            flash_swf_url: '~/plugins/plupload/js/Moxie.swf',

            // Silverlight settings
            silverlight_xap_url: '~/plugins/plupload/js/Moxie.xap',

            init: {
                BeforeUpload: function (up, file) {
                    // Called right before the upload for a given file starts, can be used to cancel it if required
                    up.settings.multipart_params.filename = file.name;
                },
                UploadComplete: function (up, file, info) {
                    fillFiles(websiteId, val);
                },
                FileUploaded: function (uploader, file, result) {
                    var response = $.parseJSON(result.response);
                    toastr[response.value.messageType](response.value.message);
                },
                Error: function (uploader, result) {
                    //var response = result.responseJSON.value
                    if (result.status === 401 || result.status === 403) {
                        toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                        setTimeout(function () { location.href = '/'; }, 4500)
                    }
                }
            }
        });

        //Fill files
        fillFiles(websiteId, val);
    });

    $('#main-wrapper').fadeIn(500);
}

//Fill files
function fillFiles(websiteId, websiteUpload) {
    var data = {
        WebsiteLanguageId: websiteLanguageId,
        WebsiteUploadId: websiteUpload.Id
    };

    $.ajax({
        url: '/spine-api/website/files',
        dataType: 'json',
        type: 'get',
        data: data,
        success: function (result) {
            var response = result.value;

            //Clear the div before it will be filled with files
            $('#wu' + websiteUpload.Id + ' .nestable ol').html('');

            $.each(response.data, function (i, val) {
                $('#cloneListObject').clone().appendTo('#wu' + val.WebsiteUploadId + ' .nestable ol');
                $('#wu' + val.WebsiteUploadId + ' .nestable').find('#cloneListObject').removeClass('hidden').attr('id', 'wf' + val.Id).attr('data-id', val.Id);

                var extension = val.OriginalPath.substr((val.OriginalPath.lastIndexOf('.') + 1)).toLowerCase();
                console.log(extension);
                switch (extension) {
                    case 'jpg':
                    case 'jpeg':
                    case 'png':
                    case 'gif':
                        $('#wf' + val.Id).find('img').removeClass('hidden').attr('src', val.CompressedPath + '?lastmod=' + new Date());
                        $('#wf' + val.Id + ' .image-crop img').attr('src', val.OriginalPath);

                        $('#wf' + val.Id).find('#video').remove();
                        $('#wf' + val.Id).find('#pdf').remove();

                        if (extension !== 'png') {
                            $('#wf' + val.Id).find('.image-crop').addClass('image-crop-bg-black');
                            $('#wf' + val.Id).find('.img-preview').css('background-color', '#000000');
                        }
                        if (extension !== 'jpg') $('#wf' + val.Id).find('.image-quality').addClass('hidden');

                        $('#wf' + val.Id).find('#qualitySlider').change(function () {
                            $('#wf' + val.Id).find('#quality').html('(' + $(this).val() + ')');
                        });

                        var $image = $('#wf' + val.Id + ' .image-crop > img'),
                            width = websiteUpload.Width,
                            height = websiteUpload.Height;

                        $('#wf' + val.Id + ' #openCropMdl').on('click', function () {
                            $('#wf' + val.Id + ' #imageCropMdl').modal('show');

                            setTimeout(function () {
                                $image.cropper({
                                    aspectRatio: width / height,
                                    preview: '#wf' + val.Id + ' .img-preview'
                                });

                            }, 400);
                        });

                        $('#wf' + val.Id + ' #openCropMdl').on('hidden.bs.modal', function () {
                            $image.cropper('destroy');
                        });

                        $('#wf' + val.Id + ' #crop').on('click', function () {
                            if ($('.modal').modal('hide')) {
                                $image.cropper('getCroppedCanvas').toBlob(function (blob) {
                                    var formData = new FormData();
                                    formData.append('id', val.Id);
                                    formData.append('type', 'website');
                                    formData.append('croppedImage', blob);
                                    formData.append('quality', $('#wf' + val.Id).find('#qualitySlider').val());

                                    $.ajax('/spine-api/image/crop', {
                                        method: "POST",
                                        data: formData,
                                        processData: false,
                                        contentType: false,
                                        success: function (result) {
                                            var response = result.value;
                                            toastr[response.messageType](response.message);

                                            fillFiles(websiteId, websiteUpload);
                                        },
                                        error: function (result) {
                                            if (result.status === 401 || result.status === 403) {
                                                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                                                setTimeout(function () { location.href = '/'; }, 4500)
                                            } else {
                                                var response = result.responseJSON.value;
                                                toastr[response.messageType](response.message);
                                            }
                                        }
                                    });
                                });
                            }
                        });

                        break;
                    case 'mp4':
                    case 'ogv':
                    case 'webm':
                        if (extension === 'ogv') { extension = 'ogg' }
                        $('#wf' + val.Id).find('#video').removeClass('hidden').find('source').attr('src', val.OriginalPath + '?lastmod=' + new Date()).attr('type', 'video/' + extension);

                        $('#wf' + val.Id).find('img').remove();
                        $('#wf' + val.Id).find('#cropHolder').remove();
                        $('#wf' + val.Id).find('#imageCropMdl').remove();
                        $('#wf' + val.Id).find('#pdf').remove();
                        break;
                    case 'pdf':
                        $('#wf' + val.Id).find('#pdf').removeClass('hidden').find('a').attr('href', val.OriginalPath);

                        $('#wf' + val.Id).find('img').remove();
                        $('#wf' + val.Id).find('#cropHolder').remove();
                        $('#wf' + val.Id).find('#imageCropMdl').remove();
                        $('#wf' + val.Id).find('#video').remove();
                        break;
                    default:
                        console.log('who knows');
                }

                $('#wf' + val.Id).find('#checkbox').find('input').prop('checked', val.Active).uniform();
                $('#wf' + val.Id).find('#alt').val(val.Alt);
            });

            $('#wu' + websiteUpload.Id + ' .nestable').nestable({
                dragClass: "dd-dragel website-image-dd-item",
                maxDepth: 1
            });     

            //When the checkbox is clicked
            $('.active').off().on('click', function () {
                //Prevent change of checkbox
                $(this).prop('checked', !$(this).prop('checked'));

                $('#modalStatusAlert').modal('show');

                websiteFileId = $(this).parents('.dd-item').attr('data-id');
                if ($(this).prop('checked') === true) {
                    activateCheckbox = false;
                } else {
                    activateCheckbox = true;
                }
            });

            $('.saveAlt').off().on('click', function () {
                pageFileId = $(this).parents('li').data('id');
                var alt = $(this).parents('li').find('input#alt').val();

                saveFileAlt(pageFileId, alt);
            });

            $(".openDeleteFileModal").off().on("click", function () {
                websiteFileId = $(this).parents("li").data("id");
                $("#modalDeleteFileAlert").modal("show");
            });

            if (response.data.length > 1) {
                $('#wu' + websiteUpload.Id + ' .saveFileOrder').removeClass("hidden");
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//Delete a file
function deleteFile(id) {
    data = {
        Id: id
    };

    $.ajax({
        url: '/spine-api/website/file/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value
            toastr[response.messageType](response.message);

            $("#modalDeleteFileAlert").modal("hide");

            var wuId = $(".nestable li[data-id='" + id + "']").parents(".websiteUploadField").data("id");

            $(".nestable li[data-id='" + id + "']").remove();

            if ($('#wu' + wuId + ' ol > li').length < 2) {
                $("#wu" + wuId).find(".saveFileOrder").addClass("hidden");
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

function saveFileStatus(websiteFileId) {
    var data = {
        id: websiteFileId,
        active: activateCheckbox
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/website/file/active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $('#wf' + websiteFileId).find("#active").prop("checked", activateCheckbox);
            $.uniform.update(active);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });

    $('#modalStatusAlert').modal('hide');
}

function saveFileAlt(websiteFileId, alt) {
    var data = {
        id: websiteFileId,
        alt: alt
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/website/file/alt',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//Save order of files
function saveFileOrder(wuId) {
    //Get list on order with the id's
    var list = $(".websiteUploadField[data-id='" + wuId + "'] .nestable").nestable('serialize');

    $.ajax({
        url: '/spine-api/website/files/update',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(list),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}