//Set default variables
var fieldChanged = false,
    dataItemId = 0,
    wizardGoTo = 0,
    backUpData,
    dataItemFileId,
    activateCheckbox,
    validateDataInformation;

$(document).ready(function () {
    //Bug fix to set a paragraph instead of a div in the summernote editor
    document.execCommand('defaultParagraphSeparator', false, 'p');

    //Get data itemm id
    if ($("#dataItemId").val() !== "") {
        dataItemId = $("#dataItemId").val();
    } else {
        dataItemId = 0;
    }
    dataTemplateId = $("#dataTemplateId").val();

    //Make menu item active
    $('#navigation #navData' + dataTemplateId).addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    getDataitem();

    $("#create").click(function () {
        updateDataItem(wizardGoTo);
        $('#modalCreateAlert').modal('hide');
    });

    //When the modal will be closed
    $("#close").click(function () {
        fieldChanged = false;
        $('#dataTemplateFields input, #dataTemplateFields textarea#textarea, #dataTemplateFields #html5Editor, #dataTemplateFields .summernote-area .note-codable').each(function () {
            $(this).data("changed", false);
        });

        //Restore old recources back in to the fields
        $("#dataTemplateFields").empty();
        fillFields(backUpData);

        $('#modelAlert').modal('hide');
        $('#rootwizard').bootstrapWizard('show', wizardGoTo);
    });

    $('#save').on('click', function () {
        wizardGoTo = 0;

        //Check fields
        var valid = $("#wizardForm").valid();
        if (!valid) {
            validateDataInformation.focusInvalid();
        } else {
            if (fieldChanged) {
                //Show modal to be sure the person wants to create a item with this template
                if (dataItemId === 0) {
                    $('#modalCreateAlert').modal('show').find('.modal-body').html(sureCreate).parents('#modalCreateAlert').find('create').html(addItem);
                    return false;
                }

                //If data item is already created
                if (dataItemId !== 0) {
                    $('#modalCreateAlert').modal('show').find('.modal-body').html(updateChanges).parents('#modalCreateAlert').find('#create').html(update);
                    return false;
                }
            }
        }
    });

    $('#rootwizard').bootstrapWizard({
        'tabClass': 'nav nav-tabs',
        onTabShow: function (tab, navigation, index) {
            var $total = navigation.find('li').length;
            var $current = index + 1;
            var $percent = ($current / $total) * 100;
            $('#rootwizard').find('.progress-bar').css({ width: $percent + '%' });
        },
        'onTabClick': function (tab, navigation, currentIndex, clickedIndex) {
            var valid = $("#wizardForm").valid();
            if (!valid) {
                validateDataInformation.focusInvalid();
                return false;
            }

            wizardGoTo = clickedIndex;

            switch (currentIndex) {
                case 0:
                    if (fieldChanged) {
                        //Show modal to be sure the person wants to create a item with this template
                        if (dataItemId === 0) {
                            $('#modalCreateAlert').modal('show').find('.modal-body').html(sureCreate).parents('#modalCreateAlert').find('create').html(addItem);
                            return false;
                        }

                        //If data item is already created
                        if (dataItemId !== 0) {
                            $('#modalCreateAlert').modal('show').find('.modal-body').html(updateChanges).parents('#modalCreateAlert').find('#create').html(update);
                            return false;
                        }
                    }

                    return true;
            }
        }
    });

    //Hide tabs
    $('#rootwizard').bootstrapWizard('disable', 1);
    $('#rootwizard').bootstrapWizard('disable', 2);

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#tab3") {
            fillUploads(backUpData.uploads);
        }
    });

    //Click event for deleting the item
    $("#deleteDataItem").on("click", function () {
        deleteDataItem();
    });

    //Click event for deleting a file
    $("#deleteFile").on("click", function () {
        deleteFile(dataItemFileId);
    });

    //Click event for changing file status
    $("#saveFileStatus").on("click", function () {
        saveFileStatus(dataItemFileId);
    });

    //When someone click on save in the modal to change review status
    $("#saveDataItemStatus").click(function () {
        updateDataItemActive();
    });
});

//Get data item by id
function getDataitem() {
    data = {
        id: dataItemId,
        dataTemplateId: dataTemplateId
    };

    $.ajax({
        url: '/spine-api/data/item/get',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = toCamel(result.value);
            backUpData = response;
            fillItem(response);

            $('#url').html(response.url);

            if (dataItemId !== 0) {
                $('#rootwizard').bootstrapWizard('enable', 2);
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//Function would called 1 time after the item is created
function afterCreate(result) {
    //Enable add/save button
    $("#saveDataItem").attr("disabled", true);

    //Change te button text after the item is created
    $("#saveDataItem").html("Save");

    //After the item is created, people can't change the item template
    $("#dataTemplate").attr("disabled", true);

    //Set the data item id
    dataItemId = result.item !== null ? result.item.id : 0;

    //Create the fields and file panels
    $('#dataTemplateFields').empty();
    fillFields(result);
    fillUploads(result.uploads);

    //If you type in the textboxes it will give a attribute to know that they need to update
    $("#pageDetails input, #pageDetails textarea, #defaultInputs input:not(.date-picker), #defaultInputs textarea").off().on("change paste keyup", function () {
        //Enable save button
        $("#save").attr("disabled", false);

        fieldChanged = true;
    });

    //Make the value url friendly while typing
    $('#pageUrl').off().on('keyup input propertychange', function (e) {
        if (e.keyCode !== 37 && e.keyCode !== 39 && e.keyCode !== 0) {
            createFriendlyUrl(this, '#pageUrl', '.form-group');
        }

        //Enable save button
        $("#save").attr("disabled", false);

        fieldChanged = true;
    });

    //Create back-up for the restore button
    backUpData = result;

    $('#rootwizard').bootstrapWizard('show', wizardGoTo);

    //Give the publish checkbox styling
    $("#dataItemActive").prop("checked", result.item !== null ? result.item.active : false);
    $("#dataItemActive").uniform();

    //When the publish checkbox is clicked
    $('#dataItemActive').off().on("click", function () {
        //Prevent change of checkbox
        $(this).prop('checked', !$(this).prop('checked'));

        $('#modalDataStatusAlert').modal('show');

        if ($("#dataItemActive").prop("checked") === true) {
            activateCheckbox = false;
        } else {
            activateCheckbox = true;
        }
    });
}

//Add data template fields
function fillFields(result) {
    fillPageDetails(result);

    var rgba = false;
    if (result.resources.length > 0) {
        $('#dataFields').removeClass('hidden');
    }

    $.each(result.resources, function (i, val) {
        $("#cloneObject").clone(true).appendTo("#dataTemplateFields");

        $("#dataTemplateFields").find("#cloneObject").removeClass("hidden").data("id", val.dataTemplateFieldId).data("type", val.type.toLowerCase()).attr("id", "dtf" + val.dataTemplateFieldId);
        if (val.type.toLowerCase() === "html5editor") {
            var summernote = $("#dtf" + val.dataTemplateFieldId).find("#html5Editor").removeClass("hidden").summernote({
                height: 200,
                toolbar: [
                    ['cleaner', ['cleaner']], // The Button
                    ['style', ['style']],
                    ['font', ['bold', 'italic', 'underline', 'strikethrough', 'clear']],
                    ['color', ['color']],
                    ['para', ['ul', 'ol', 'paragraph']],
                    ['table', ['table']],
                    ['insert', ['picture', 'link', 'hr']],
                    ['view', ['fullscreen', 'codeview']],
                    ['help', ['help']]
                ],
                cleaner: {
                    notTime: 2400, // Time to display Notifications.
                    action: 'both', // both|button|paste 'button' only cleans via toolbar button, 'paste' only clean when pasting content, both does both options.
                    newline: '<br>', // Summernote's default is to use '<p><br></p>'
                    notStyle: 'position:absolute;top:0;left:0;right:0', // Position of Notification
                    icon: '<i class="note-icon">' + clean + '</i>',
                    keepHtml: false, // Remove all Html formats
                    keepOnlyTags: ['<p>', '<br>', '<ul>', '<li>', '<b>', '<strong>', '<i>', '<a>'], // If keepHtml is true, remove all tags except these
                    keepClasses: false, // Remove Classes
                    badTags: ['style', 'script', 'applet', 'embed', 'noframes', 'noscript', 'html', 'div'], // Remove full tags with contents
                    badAttributes: ['style', 'start'] // Remove attributes from remaining tags
                },
                popover: {
                    image: [
                        ['custom', ['imageAttributes']],
                        ['imagesize', ['imageSize100', 'imageSize50', 'imageSize25']],
                        ['float', ['floatLeft', 'floatRight', 'floatNone']],
                        ['remove', ['removeMedia']]
                    ],
                    link: [
                        ['link', ['linkDialogShow', 'unlink']]
                    ]
                },
                lang: 'nl-NL',
                imageAttributes: {
                    imageDialogLayout: 'default', // default|horizontal
                    icon: '<i class="note-icon-pencil"/>',
                    removeEmpty: false // true = remove attributes | false = leave empty if present
                },
                displayFields: {
                    imageBasic: true,  // show/hide Title, Source, Alt fields
                    imageExtra: false, // show/hide Alt, Class, Style, Role fields
                    linkBasic: true,   // show/hide URL and Target fields for link
                    linkExtra: false   // show/hide Class, Rel, Role fields for link
                },
                callbacks: {
                    onBlur: function (contents, $editable) {
                        $("#dtf" + val.dataTemplateFieldId + " #html5Editor").data("changed", true);
                        fieldChanged = true;

                        //Enable save button
                        $("#save").attr("disabled", false);
                    },
                    onChange: function (contents, $editable) {
                        $("#dtf" + val.dataTemplateFieldId + " #html5Editor").data("changed", true);
                        fieldChanged = true;

                        //Enable save button
                        $("#save").attr("disabled", false);
                    },
                    onImageUpload: function (files) {
                        var data = new FormData();
                        $.each(files, function (i, val) {
                            data.append(val.name, val);
                        });

                        $.ajax({
                            url: '/api/image',
                            type: 'post',
                            contentType: false,
                            processData: false,
                            data: data,
                            success: function (files) {
                                $.each(files, function (i, val) {
                                    summernote.summernote('insertImage', val, function ($image) {
                                    });
                                });

                                return true;
                            },
                            error: function (result) {
                                if (result.status === 401 || result.status === 403) {
                                    toastr['warning'](unauthorized);
                                    setTimeout(function () { location.href = '/'; }, 4500);
                                } else {
                                    var response = result.responseJSON.value;
                                    toastr[response.messageType](response.message);
                                }

                                return true;
                            }
                        });
                    }
                }
            });

            //Fill from database
            summernote.summernote("code", val.text);

            //After filling the field, set the data attribute and variable back to false. They are changed by the onChange of summernote after filling
            $("#dtf" + val.dataTemplateFieldId + " #html5Editor").data("changed", false);
            fieldChanged = false;

            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "text" || val.type === "number" || val.type === "email" || val.type === "tel") {
            $("#dtf" + val.dataTemplateFieldId).find("input#input").removeClass("hidden").attr("type", val.Type).val(val.text);
            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "textarea") {
            $("#dtf" + val.dataTemplateFieldId).find("textarea#textarea").removeClass("hidden").val(val.text);
            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "textarea") {
            $("#dtf" + val.dataTemplateFieldId).find("input#rgba").removeClass("hidden").val(val.text);
            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "checkbox") {
            $('#dtf' + val.dataTemplateFieldId).find('#checkbox').removeClass('hidden').find('input').prop('checked', (val.text.toString().toLowerCase() === "true" ? true : false)).uniform();
            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "rgba") {
            rgba = true
            $("#dtf" + val.dataTemplateFieldId).find('input#rgba').removeClass('hidden').val(val.text);
            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "selectlinkedto") {
            $("#dtf" + val.dataTemplateFieldId).find('select#select').removeClass('hidden').select2({
                multiple: true,
                placeholder: selectVal + ' ' + val.heading.toLowerCase() + '...',
                data: val.data,
                "language": {
                    "noResults": function () {
                        return noResultsFound;
                    }
                }
            }).val(val.text).trigger('change').on("change", function (e) {
                //Enable save button
                $("#save").attr("disabled", false);

                $(this).data("changed", true);
                fieldChanged = true;
            });
            $("#dtf" + val.dataTemplateFieldId).find("label#heading").html(val.heading);
        }

        $('#dtf' + val.dataTemplateFieldId).find('.hidden').remove();
    });

    //If you type in the textboxes it will give a attribute to know that they need to update
    $("#dataTemplateFields input#input, #dataTemplateFields textarea#textarea").off().on("change paste keyup", function () {
        //Enable save button
        $("#save").attr("disabled", false);

        $(this).data("changed", true);
        fieldChanged = true;
    });

    if (rgba) {
        $('#dataTemplateFields .rgba').off().colorpicker().on('changeColor',
            function (ev) {
                //Enable save button
                $("#save").attr("disabled", false);

                $(this).data("changed", true);
                fieldChanged = true;
            }
        );
    }

    //On change that works for the checkbox
    $('#dataTemplateFields #checkbox input').off().change(function () {
        var checkbox = $(this).prop("checked", $(this).is(':checked'));
        $.uniform.update(checkbox);

        //Enable save button
        $("#save").attr("disabled", false);

        $(this).data("changed", true);
        fieldChanged = true;
    });

    //Enable save button
    $("#save").attr("disabled", true);

    $('#main-wrapper').fadeIn(500);
}

//Add upload boxes
function fillUploads(result) {
    $('#dataTemplateUploads').empty();

    if (dataItemId !== 0) {
        if (result.length > 0) {
            $('#rootwizard').bootstrapWizard('enable', 1);
        } else {
            $('#rootwizard').bootstrapWizard('hide', 1);
        }
    } else {
        if (!result.length > 0) {
            $('#rootwizard').bootstrapWizard('hide', 1);
        }
    }

    $.each(result, function (i, val) {
        $("#cloneUploadObject").clone().appendTo("#dataTemplateUploads");
        $("#dataTemplateUploads").find("#cloneUploadObject").removeClass("hidden").attr("id", "dtu" + val.id).attr("data-id", val.id);
        $("#dtu" + val.id).find("h4").html(val.heading);
        $("#dtu" + val.id).find("#uploader").attr("id", "uploader" + val.id);

        //To save the order of the files
        $(".saveFileOrder").off().on("click", function () {
            //Get Id of the upload field
            var dtuId = $(this).parents(".dataUploadField").data("id");

            saveFileOrder(dtuId);
        });

        var uploader = $("#uploader" + val.id).off().pluploadQueue({
            runtimes: 'html5,flash,silverlight,html4',
            url: "/spine-api/data/item/file",

            chunk_size: val.maxSize + 'mb',
            rename: true,
            dragdrop: true,
            multiple_queues: true,

            filters: {
                max_file_size: val.maxSize,
                mime_types: [
                    { title: "Files", extensions: val.fileExtensions }
                ]
            },
            multipart: true,
            multipart_params: {
                "width": val.width,
                "height": val.height,
                "autoCrop": false,
                "dataTemplateUploadId": val.id,
                "dataItemId": dataItemId
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
                    fillFiles(dataItemId, val);
                },
                FileUploaded: function (uploader, file, result) {
                    var response = $.parseJSON(result.response);
                    toastr[response.value.messageType](response.value.message);
                },
                Error: function (uploader, result) {
                    //var response = result.responseJSON.value
                    if (result.status === 401 || result.status === 403) {
                        toastr['warning'](unauthorized);
                        setTimeout(function () { location.href = '/'; }, 4500);
                    }
                }
            }
        });

        $('#uploader' + val.id + ' .plupload_add').on('click', function (e) {
            e.preventDefault();

            var input = document.getElementById(uploader[0].id + '_html5');
            if (input && !input.disabled) { // for some reason FF (up to 8.0.1 so far) lets to click disabled input[type=file]
                input.click();
            }
        });

        //Fill files if they already exist in the database
        fillFiles(dataItemId, val);
    });

    //Change the upload buttons text
    $('.plupload_start').each(function () {
        $(this).html("Upload");
    });

    //$('.plupload_add').each(function () {
    //    $(this).removeAttr('href');
    //})
}

//Fill files
function fillFiles(dataItemId, dataTemplateUpload) {
    var data = {
        dataItemId: dataItemId,
        dataTemplateUploadId: dataTemplateUpload.id
    };

    $.ajax({
        url: '/spine-api/data/item/files',
        dataType: 'json',
        type: 'get',
        data: data,
        success: function (result) {
            //Clear the div before it will be filled with files
            $('#dtu' + dataTemplateUpload.id + ' .nestable ol').html('');

            $.each(result.data, function (i, val) {
                $('#cloneListObject').clone().appendTo('#dtu' + val.DataTemplateUploadId + ' .nestable ol');
                $('#dtu' + val.DataTemplateUploadId + ' .nestable').find('#cloneListObject').removeClass('hidden').attr('id', 'dif' + val.Id).attr('data-id', val.Id);

                var extension = val.OriginalPath.substr((val.OriginalPath.lastIndexOf('.') + 1)).toLowerCase();
                switch (extension) {
                    case 'jpg':
                    case 'jpeg':
                    case 'png':
                    case 'gif':
                        $('#dif' + val.Id).find('img').removeClass('hidden').attr('src', val.CompressedPath + '?lastmod=' + new Date());
                        $('#dif' + val.Id + ' .image-crop img').attr('src', val.OriginalPath);

                        $('#dif' + val.Id).find('#video').remove();
                        $('#dif' + val.Id).find('#pdf').remove();

                        if (extension !== 'png') {
                            $('#dif' + val.Id).find('.image-crop').addClass('image-crop-bg-black');
                            $('#dif' + val.Id).find('.img-preview').css('background-color', '#000000');
                        }
                        if (extension !== 'jpg') $('#dif' + val.Id).find('.image-quality').addClass('hidden');

                        $('#dif' + val.Id).find('#qualitySlider').change(function () {
                            $('#dif' + val.Id).find('#quality').html('(' + $(this).val() + ')');
                        });

                        var $image = $('#dif' + val.Id + ' .image-crop > img'),
                            width = dataTemplateUpload.width,
                            height = dataTemplateUpload.height;

                        $('#dif' + val.Id + ' #openCropMdl').on('click', function () {
                            $('#dif' + val.Id + ' #imageCropMdl').modal('show');

                            setTimeout(function () {
                                $image.cropper({
                                    aspectRatio: width / height,
                                    preview: '#dif' + val.Id + ' .img-preview'
                                });

                            }, 400);
                        });

                        $('#dif' + val.Id + ' #openCropMdl').on('hidden.bs.modal', function () {
                            $image.cropper('destroy');
                        });

                        $('#dif' + val.Id + ' #crop').on('click', function () {
                            if ($('.modal').modal('hide')) {
                                $image.cropper('getCroppedCanvas').toBlob(function (blob) {
                                    var formData = new FormData();
                                    formData.append('id', val.Id);
                                    formData.append('type', 'data');
                                    formData.append('croppedImage', blob);
                                    formData.append('quality', $('#dif' + val.Id).find('#qualitySlider').val());

                                    $.ajax('/spine-api/image/crop', {
                                        method: "POST",
                                        data: formData,
                                        processData: false,
                                        contentType: false,
                                        success: function (result) {
                                            var response = result.value;
                                            toastr[response.messageType](response.message);

                                            fillFiles(dataItemId, dataTemplateUpload);
                                        },
                                        error: function (result) {
                                            if (result.status === 401 || result.status === 403) {
                                                toastr['warning'](unauthorized);
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
                        $('#dif' + val.Id).find('#video').removeClass('hidden').find('source').attr('src', val.OriginalPath + '?lastmod=' + new Date()).attr('type', 'video/' + extension);

                        $('#dif' + val.Id).find('img').remove();
                        $('#dif' + val.Id).find('#cropHolder').remove();
                        $('#dif' + val.Id).find('#imageCropMdl').remove();
                        $('#dif' + val.Id).find('#pdf').remove();
                        break;
                    case 'pdf':
                        $('#dif' + val.Id).find('#pdf').removeClass('hidden').find('a').attr('href', val.OriginalPath);

                        $('#dif' + val.Id).find('img').remove();
                        $('#dif' + val.Id).find('#cropHolder').remove();
                        $('#dif' + val.Id).find('#imageCropMdl').remove();
                        $('#dif' + val.Id).find('#video').remove();
                        break;
                    default:
                        //console.log('who knows');
                }                

                $('#dif' + val.Id).find('#checkbox').find('input').prop('checked', val.Active).uniform();
                $('#dif' + val.Id).find('#alt').val(val.Alt);
            });

            $('#dtu' + dataTemplateUpload.id + ' .nestable').nestable({
                dragClass: 'dd-dragel page-file-dd-item',
                maxDepth: 1
            });

            //When the checkbox is clicked
            $('.dd-item .active').off().on('click', function () {
                //Prevent change of checkbox
                $(this).prop('checked', !$(this).prop('checked'));

                $('#modalStatusAlert').modal('show');

                dataItemFileId = $(this).parents('.dd-item').attr('data-id');
                if ($(this).prop('checked') === true) {
                    activateCheckbox = false;
                } else {
                    activateCheckbox = true;
                }
            });

            $('.saveAlt').off().on('click', function () {
                dataItemFileId = $(this).parents('li').data('id');
                var alt = $(this).parents('li').find('input#alt').val();

                saveFileAlt(dataItemFileId, alt);
            });

            $('.openDeleteFileModal').off().on('click', function () {
                dataItemFileId = $(this).parents('li').data('id');
                $('#modalDeleteFileAlert').modal('show');
            });

            if (result.data.length > 1) {
                $('#dtu' + dataTemplateUpload.id + ' .saveFileOrder').removeClass('hidden');
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//delete a data itam
function deleteDataItem() {
    data = {
        Id: dataItemId
    };

    $.ajax({
        url: '/spine-api/data/item/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            location.href = '/data/' + dataTemplateId;

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500)
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
        url: '/spine-api/data/item/file/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            toastr[result.messageType](result.message);


            if (result.success === 'Valid') {
                $('#modalDeleteFileAlert').modal('hide');

                var dtuId = $('.nestable li[data-id="' + id + '"]').parents('.dataUploadField').data('id');

                $('.nestable li[data-id="' + id + '"]').remove();

                if ($('#dtu' + dtuId + ' ol > li').length < 2) {
                    $('#dtu' + dtuId).find('.saveFileOrder').addClass('hidden');
                }
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

function saveFileStatus(dataItemFileId) {
    var data = {
        id: dataItemFileId,
        active: activateCheckbox
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/data/item/file/active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $('#dif' + dataItemFileId).find("#active").prop("checked", activateCheckbox);
            $.uniform.update(active);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });

    $('#modalStatusAlert').modal('hide');
}

function saveFileAlt(dataItemFileId, alt) {
    var data = {
        id: dataItemFileId,
        alt: alt
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/data/item/file/alt',
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
                toastr['warning'](unauthorized);
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
function saveFileOrder(dtuId) {
    //Get list on order with the id's
    var list = $(".dataUploadField[data-id='" + dtuId + "'] .nestable").nestable('serialize');

    $.ajax({
        url: '/spine-api/data/item/files/update/order',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(list),
        success: function (result) {
            toastr[result.messageType](result.message);
            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//Save data item resources
function updateDataItem(goTo) {
    //Disable save button
    $("#save").attr("disabled", true);

    if (backUpData.item === null) { backUpData.item = {}; }

    backUpData.item.pageTitle = $('#pageTitle').length === 1 ? $('#pageTitle').val() : backUpData.item.pageTitle === undefined ? "" : backUpData.item.pageTitle;
    backUpData.item.pageUrl = $('#pageUrl').length === 1 ? $('#pageUrl').val() : backUpData.item.pageUrl === undefined ? "" : backUpData.item.pageUrl;
    backUpData.item.pageKeywords = $('#pageKeywords').length === 1 ? $('#pageKeywords').val() : backUpData.item.pageKeywords === undefined ? "" : backUpData.item.pageKeywords;
    backUpData.item.pageDescription = $('#pageDescription').length === 1 ? $('#pageDescription').val() : backUpData.item.pageDescription === undefined ? "" : backUpData.item.pageDescription;
    backUpData.item.publishDate = $('#publishDate').length === 1 && $('#publishTime').length === 1 ? $('#publishDate').val() + " " + $('#publishTime').val() : backUpData.item.publishDate === undefined ? new Date() : backUpData.item.publishDate;
    backUpData.item.fromDate = $('#fromDate').length === 1 && $('#fromDate').length === 1 ? $('#fromDate').val() + " " + $('#fromTime').val() : backUpData.item.fromDate === undefined ? new Date() : backUpData.item.fromDate;
    backUpData.item.toDate = $('#toDate').length === 1 && $('#toDate').length === 1 ? $('#toDate').val() + " " + $('#toTime').val() : backUpData.item.toDate === undefined ? new Date() : backUpData.item.toDate;
    backUpData.item.title = $('#title').length === 1 ? $('#title').val() : backUpData.item.title === undefined ? "" : backUpData.item.title;
    backUpData.item.subtitle = $('#subtitle').length === 1 ? $('#subtitle').val() : backUpData.item.subtitle === undefined ? "" : backUpData.item.subtitle;
    backUpData.item.text = $('#text').length === 1 ? $('#text').val() : backUpData.item.text === undefined ? "" : backUpData.item.text;
    backUpData.item.htmlEditor = $('#htmlEditorHolder #html5Editor').length === 1 ? $('#htmlEditorHolder #html5Editor').summernote('code') : backUpData.item.htmlEditor === undefined ? "" : backUpData.item.htmlEditor;

    updateBackUpData();

    backUpData.item.dataTemplateId = dataTemplateId
    var data = {
        item: backUpData.item,
        resources: backUpData.resources
    }

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/data/item/update',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            //If the data item id is 0, then we just added a new item
            if (dataItemId === 0) {
                dataItemId = response.item.id;

                $("#dataTitle").html(editItem/* + ' <span id="displayName">' + response.item.title + '</span>'*/).removeClass('hidden');
            }

            getDataitem();

            $('#rootwizard').bootstrapWizard('show', goTo);
            fieldChanged = false;

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
                $("#save").attr("disabled", false);
                fieldChanged = false;
            }

            return true;
        }
    });
}

function updateDataItemActive() {
    var data = {
        Id: dataItemId,
        Active: activateCheckbox
    };

    //Sends json to webservices.
    $.ajax({
        url: '/spine-api/data/item/active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $("#dataItemActive").prop("checked", activateCheckbox);
            $.uniform.update(active);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

function fillPageDetails(result) {
    if (result.template.detailPage) {
        $('#pageDetails').removeClass('hidden');

        var item = result.item;
        $('#pageTitle').val(item !== null ? item.pageTitle : "");
        $('#pageUrl').val(item !== null ? item.pageUrl : "");
        $('#pageKeywords').tagsinput('add', item !== null ? item.pageKeywords : "");
        $('#pageDescription').val(item !== null ? item.pageDescription : "");

        //Creating validation for data information
        validateDataInformation = $('#wizardForm').validate({
            ignore: [],
            errorPlacement: function (error, element) {
                if (element.parent('.tags-input').length) {
                    error.appendTo(element.parent());
                } else if (element.parent('.input-group').length) {
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                }
            },
            rules: {
                pageTitle: {
                    required: true,
                    maxlength: 200
                },
                pageUrl: {
                    required: true,
                    maxlength: 200
                },
                pageKeywords: {
                    maxlength: 400
                },
                pageDescription: {
                    maxlength: 400
                }
            },
            messages: {
                pageTitle: {
                    required: titleValidation
                },
                pageUrl: {
                    required: urlValidation
                },
            },
            onkeyup: function (element) { $(element).valid(); }
        });

    }
}

function fillItem(result) {
    var template = result.template;
    var item = result.item;

    if (template.titleHeading !== "") {
        $('#titleHolder').removeClass('hidden').find('input').val(item !== null ? item.title : "").parent().find('label').html(template.titleHeading);
    }
    if (template.subtitleHeading !== "") {
        $('#subtitleHolder').removeClass('hidden').find('input').val(item !== null ? item.subtitle : "").parent().find('label').html(template.subtitleHeading);
    }
    if (template.textHeading !== "") {
        $('#textHolder').removeClass('hidden').find('textarea').val(item !== null ? item.text : "").parent().find('label').html(template.textHeading);
    }
    if (template.htmlEditorHeading !== "") {
        $('#htmlEditorHolder').removeClass('hidden').find('label#htmlEditorHeading').html(template.htmlEditorHeading).parent().find('.note-editor note-frame panel panel-default').remove();

        var summernote = $('#htmlEditorHolder #html5Editor').summernote({
            height: 200,
            toolbar: [
                ['cleaner', ['cleaner']], // The Button
                ['style', ['style']],
                ['font', ['bold', 'italic', 'underline', 'strikethrough', 'clear']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['picture', 'link', 'hr']],
                ['view', ['fullscreen', 'codeview']],
                ['help', ['help']]
            ],
            cleaner: {
                notTime: 2400, // Time to display Notifications.
                action: 'both', // both|button|paste 'button' only cleans via toolbar button, 'paste' only clean when pasting content, both does both options.
                newline: '<br>', // Summernote's default is to use '<p><br></p>'
                notStyle: 'position:absolute;top:0;left:0;right:0', // Position of Notification
                icon: '<i class="note-icon">' + clean + '</i>',
                keepHtml: false, // Remove all Html formats
                keepOnlyTags: ['<p>', '<br>', '<ul>', '<li>', '<b>', '<strong>', '<i>', '<a>'], // If keepHtml is true, remove all tags except these
                keepClasses: false, // Remove Classes
                badTags: ['style', 'script', 'applet', 'embed', 'noframes', 'noscript', 'html', 'div'], // Remove full tags with contents
                badAttributes: ['style', 'start'] // Remove attributes from remaining tags
            },
            popover: {
                image: [
                    ['custom', ['imageAttributes']],
                    ['imagesize', ['imageSize100', 'imageSize50', 'imageSize25']],
                    ['float', ['floatLeft', 'floatRight', 'floatNone']],
                    ['remove', ['removeMedia']]
                ],
                link: [
                    ['link', ['linkDialogShow', 'unlink']]
                ]
            },
            lang: 'nl-NL',
            imageAttributes: {
                imageDialogLayout: 'default', // default|horizontal
                icon: '<i class="note-icon-pencil"/>',
                removeEmpty: false // true = remove attributes | false = leave empty if present
            },
            displayFields: {
                imageBasic: true,  // show/hide Title, Source, Alt fields
                imageExtra: false, // show/hide Alt, Class, Style, Role fields
                linkBasic: true,   // show/hide URL and Target fields for link
                linkExtra: false   // show/hide Class, Rel, Role fields for link
            },
            callbacks: {
                onBlur: function (contents, $editable) {
                    fieldChanged = true;

                    //Enable save button
                    $("#save").attr("disabled", false);
                },
                onChange: function (contents, $editable) {
                    fieldChanged = true;

                    //Enable save button
                    $("#save").attr("disabled", false);
                },
                onImageUpload: function (files) {
                    var data = new FormData();
                    $.each(files, function (i, val) {
                        data.append(val.name, val);
                    });

                    $.ajax({
                        url: '/api/image',
                        type: 'post',
                        contentType: false,
                        processData: false,
                        data: data,
                        success: function (files) {
                            $.each(files, function (i, val) {
                                summernote.summernote('insertImage', val, function ($image) {
                                });
                            });

                            return true;
                        },
                        error: function (result) {
                            if (result.status === 401 || result.status === 403) {
                                toastr['warning'](unauthorized);
                                setTimeout(function () { location.href = '/'; }, 4500);
                            } else {
                                var response = result.responseJSON.value;
                                toastr[response.messageType](response.message);
                            }

                            return true;
                        }
                    });
                }
            }
        });

        summernote.summernote("code", item !== null ? item.htmlEditor : "");

        //After filling the field, set the variable back to false. They are changed by the onChange of summernote after filling
        fieldChanged = false;
    }
    var date;
    if (template.publishDateHeading !== "") {
        date = getDate(item !== null ? new Date(item.publishDate) : new Date());
        $('#publishDateHolder').removeClass('hidden').find('input#publishDate').datepicker({
            orientation: "top auto",
            autoclose: true
        }).datepicker("update", date).on("input change", function (e) {
            //Enable save button
            $("#save").attr("disabled", false);

            fieldChanged = true;
        });
        $('#publishDateHolder').find('input#publishTime').timepicker({ defaultTime: item !== null ? new Date(item.publishDate) : new Date() });
        $('#publishDateHolder').find('label').html(template.publishDateHeading);
    }
    if (template.fromDateHeading !== "") {
        date = getDate(item !== null ? new Date(item.fromDate) : new Date());
        $('#fromDateHolder').removeClass('hidden').find('input#fromDate').datepicker({
            orientation: "top auto",
            autoclose: true
        }).datepicker("update", date).on("input change", function (e) {
            //Enable save button
            $("#save").attr("disabled", false);

            fieldChanged = true;
        });
        $('#fromDateHolder').find('input#fromTime').timepicker({ defaultTime: item !== null ? new Date(item.fromDate) : new Date() });
        $('#fromDateHolder').find('label').html(template.fromDateHeading);
    }
    if (template.toDateHeading !== "") {
        date = getDate(item !== null ? new Date(item.toDate) : new Date());
        $('#toDateHolder').removeClass('hidden').find('input#toDate').datepicker({
            orientation: "top auto",
            autoclose: true
        }).datepicker("update", date).on("input change", function (e) {
            //Enable save button
            $("#save").attr("disabled", false);

            fieldChanged = true;
        });
        $('#toDateHolder').find('input#toTime').timepicker({ defaultTime: item !== null ? new Date(item.toDate) : new Date() });
        $('#toDateHolder').find('label').html(template.toDateHeading);
    }

    $('#defaultInputs .form-group.hidden').remove();
    
    afterCreate(backUpData);
}

function updateBackUpData() {
    //Get value from fields and put it in the json
    $('#dataTemplateFields input#input, #dataTemplateFields input#cb, #dataTemplateFields input#rgba, #dataTemplateFields textarea#textarea, #dataTemplateFields #html5Editor, #dataTemplateFields select#select').each(function () {
        if ($(this).data("changed")) {
            var id = $(this).parents(".form-group").data("id");
            var text = "";
            if ($(this).is("input[type='checkbox']")) {
                text = $(this).is(':checked').toString().toLowerCase();
            } else if ($(this).parents(".form-group").data("type") === "html5editor") {
                text = $(this).summernote('code');
            } else {
                text = $(this).val();
            }

            //Update backup resources
            for (var i = 0; i < backUpData.resources.length; i++) {
                if (backUpData.resources[i].dataTemplateFieldId === id) {
                    backUpData.resources[i].text = text;
                    break;
                }
            }

            //Set everything back to false
            $(this).data("changed", false);
        }
    });
}

function getDate(datetime) {
    var fullDate = new Date(datetime);
    var twoDigitMonth = (fullDate.getMonth() + 1);
    var twoDigitDate = fullDate.getDate();
    return currentDate = twoDigitMonth + "/" + twoDigitDate + "/" + fullDate.getFullYear();
}

function toCamel(o) {
    var newO, origKey, newKey, value;
    if (o instanceof Array) {
        newO = [];
        for (origKey in o) {
            value = o[origKey];
            if (typeof value === "object") {
                value = toCamel(value);
            }
            newO.push(value);
        }
    } else {
        newO = {};
        for (origKey in o) {
            if (o.hasOwnProperty(origKey)) {
                newKey = (origKey.charAt(0).toLowerCase() + origKey.slice(1) || origKey).toString();
                value = o[origKey];
                if (value instanceof Array || (value !== null && value.constructor === Object)) {
                    value = toCamel(value);
                }
                newO[newKey] = value;
            }
        }
    }
    return newO;
}