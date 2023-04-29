//Set default variables
var alreadyCreated = false,
    fieldChanged = false,
    pageInformationChanged = false,
    pageId = 0,
    wizardButtonsDisabled = false,
    wizardGoTo = 0,
    backUpResources,
    backUpPage,
    backUpData,
    pageFileId,
    activateCheckbox,
    sureOfCreate = false,
    validatePageInformation,
    urlParts;

$(document).ready(function () {
    var url = window.location.href;
    //get rid of the trailing / before doing a simple split on /
    urlParts = url.replace(/\/\s*$/, '').split('/');

    //Make menu item active
    if (urlParts[3] === 'pages') {
        $("#navigation #navPages").addClass("active");
    } else {
        $('#pageTitle').html(addCategory);
        $('#navigation #navCategories').addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();
    }

    //Bug fix to set a paragraph instead of a div in the summernote editor
    document.execCommand('defaultParagraphSeparator', false, 'p');

    //Get page id
    if ($("#pageId").val() !== "") {
        pageId = $("#pageId").val();

        getPage();
    } else {
        $('#pageTitle').removeClass('hidden');
        $("#pageUrl").removeClass("hidden");
        $('#main-wrapper').fadeIn(500);
    }

    //Make the value url friendly while typing
    $('#url').on('keyup input propertychange', function (e) {
        if (e.keyCode !== 37 && e.keyCode !== 39 && e.keyCode !== 0) {
            createFriendlyUrl(this, '#url', '.form-group');
        }
    });

    //Set the variable to true, so the javascript will know there is something changed
    $("#tab1 input, #tab1 textarea").on("change paste keyup", function () {
        //Enable add/save button
        $("#savePage").attr("disabled", false);

        if (alreadyCreated) {
            pageInformationChanged = true;
        }
    });

    $("#create").click(function () {
        createPage();

        $('#modalCreateAlert').modal('hide');
    });

    //Creating validation for page information
    validatePageInformation = $('#wizardForm').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.tags-input').length) {
                error.appendTo(element.parent());
            } else if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            }  else {
                error.insertAfter(element);
            }
        },
        rules: {
            name: {
                required: true,
                maxlength: 200
            },
            title: {
                required: true,
                maxlength: 200
            },
            url: {
                maxlength: 200
            },
            keywords: {
                maxlength: 400
            },
            description: {
                maxlength: 400
            }
        },
        messages: {
            name: {
                required: nameValidation
            },
            title: {
                required: titleValidation
            },
        },
        onkeyup: function (element) { $(element).valid(); }
    });

    //When someone click on save in the modal
    $("#save").click(function () {
        savePageResources();

        $('#rootwizard').bootstrapWizard('show', wizardGoTo);
    });

    //When the modal will be closed
    $("#close").click(function () {
        fieldChanged = false;
        $('#pageTemplateFields input#input, #pageTemplateFields textarea#textarea, #pageTemplateFields #html5Editor, #pageTemplateFields .summernote-area .note-codable').each(function () {
            $(this).data("changed", false);
        });

        //Restore old recources back in to the fields
        $("#pageTemplateFields").empty();
        fillFields(backUpResources);

        $('#modelAlert').modal('hide');
        $('#rootwizard').bootstrapWizard('show', wizardGoTo);
    });

    $("#savePage").click(function () {
        //Check fields
        var valid = $("#wizardForm").valid();
        if (!valid) {
            validatePageInformation.focusInvalid();
            return false;
        }

        //Show modal to be sure the person wants to create a page with this template
        if (!sureOfCreate) {
            $('#modalCreateAlert').modal('show');

            return false;
        }

        //If page is already created
        if (alreadyCreated) {
            if (!updatePageInformation()) {
                return false;
            }
        }
    });

    $("#saveResources").click(function () {
        if (fieldChanged) {
            savePageResources();
        }
    });

    $('#rootwizard').bootstrapWizard({
        'tabClass': 'nav nav-tabs',
        //onTabShow: function (tab, navigation, index) {
        //    var $total = navigation.find('li').length;
        //    var $current = index + 1;
        //    var $percent = ($current / $total) * 100;
        //    $('#rootwizard').find('.progress-bar').css({ width: $percent + '%' });
        //},
        'onTabClick': function (tab, navigation, currentIndex, clickedIndex) {
            var valid = $("#wizardForm").valid();
            if (!valid) {
                validatePageInformation.focusInvalid();
                return false;
            }

            wizardGoTo = clickedIndex;

            switch (currentIndex) {
                case 0:
                    //Show modal to be sure the person wants to create a page with this template

                    if (!sureOfCreate) {
                        $('#modalCreateAlert').modal('show');

                        return false;
                    }

                    //If page is already created
                    if (alreadyCreated) {
                        if (!updatePageInformation()) {
                            return false;
                        }
                    }

                    break;
                case 1:
                    if (fieldChanged) {
                        $('#modelAlert').modal('show');

                        return false;
                    }

                    break;
            }

            //indexCheck(clickedIndex);
        }
    });

    //Hide tabs
    $('#rootwizard').bootstrapWizard('disable', 1);
    $('#rootwizard').bootstrapWizard('disable', 2);
    $('#rootwizard').bootstrapWizard('disable', 3);

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#tab3") {
            fillUploads(backUpData.uploads);
        }
    });

    //Click event for deleting the page
    $("#deletePage").on("click", function () {
        deletePage();
    });

    //Click event for deleting a image
    $("#deleteImage").on("click", function () {
        deleteFile(pageFileId);
    });

    //Click event for changing file status
    $("#saveFileStatus").on("click", function () {
        saveFileStatus(pageFileId);
    });



    //When someone click on save in the modal to change review status
    $("#savePageStatus").click(function () {
        updatePageActive();
    });
});

////Hide buttons depending on index
//function indexCheck(index) {
//    if (index === 0) {
//        $("#previous").addClass("hidden");
//        $("#previous").parent().addClass("disabled");
//    } else {
//        $("#previous").removeClass("hidden");
//        $("#previous").parent().removeClass("disabled");
//    }

//    if (index === 3) {
//        $("#previous").html("Previous");
//        $("#next").addClass("hidden");
//        $("#previous").parent().addClass("disabled");
//    } else {
//        $("#previous").html("Save and previous");
//        $("#next").removeClass("hidden");
//        $("#previous").parent().removeClass("disabled");
//    }

//    return true;
//}

//Makes the buttons enabled
function enableWizardButtons() {
    wizardButtonsDisabled = false;
}

//Makes the buttons disabled
function disableWizardButtons() {
    wizardButtonsDisabled = true;
}

//Create page
function createPage() {
    //Disable add/save button
    $("#savePage").attr("disabled", true);

    //Disable all buttons of the wizard
    disableWizardButtons();

    var data = {
        PageTemplateId: $("#pageTemplate option:selected").data("id"),
        Parent: 0,
        Url: $("#url").val(),
        Title: $("#title").val(),
        Keywords: $("#keywords").val(),
        Description: $("#description").val(),
        AlternateGuid: "",
        Name: $("#name").val(),
        Type: (urlParts[3] === 'pages') ? 'page' : 'eCommerceCategory'
    };

    $.ajax({
        url: '/spine-api/page/add',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            backUpPage = response;

            enableWizardButtons();
            afterCreate(response);
            $('#rootwizard').bootstrapWizard('enable', 3);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);

                //Enable add/save button
                $("#savePage").attr("disabled", false);

                //Enable all buttons of the wizard
                enableWizardButtons();

                //Check fields
                var valid = $("#wizardForm").valid();
                if (!valid) {
                    validatePageInformation.focusInvalid();
                    return false;
                }
            }

            return true;
        }
    });
}

//Function would called 1 time after the page is created
function afterCreate(result) {
    //Enable add/save button
    $("#savePage").attr("disabled", true);

    //Change te button text after the page is created
    $("#savePage").html(save);

    //After teh page is created, people can't change the page template
    $("#pageTemplate").attr("disabled", true);

    //Set the page id
    pageId = result.pageId;

    //Create the fields and image panels
    fillFields(result.data);
    fillUploads(result.uploads);

    //Create back-up for the restore button
    backUpData = result;
    backUpResources = result.data;

    alreadyCreated = true;
    $('#rootwizard').bootstrapWizard('show', wizardGoTo);

    //Give the publish checkbox styling
    $("#pageActive").prop("checked", result.active);
    $("#pageActive").uniform();

    //When the publish checkbox is clicked
    $('#pageActive').off().on("click", function () {
        //Prevent change of checkbox
        $(this).prop('checked', !$(this).prop('checked'));

        $('#modalPageStatusAlert').modal('show');

        if ($("#pageActive").prop("checked") === true) {
            activateCheckbox = false;
        } else {
            activateCheckbox = true;
        }
    });

    //Set variable to true so the system knows that the page already is created
    sureOfCreate = true;
}

//Add page template fields
function fillFields(result) {
    var rgba = false;
    if (result.length > 0) { $('#rootwizard').bootstrapWizard('enable', 1); }

    $.each(result, function (i, val) {
        $("#cloneObject").clone(true).appendTo("#pageTemplateFields");

        $("#pageTemplateFields").find("#cloneObject").removeClass("hidden").data("id", val.Id).data("type", val.Type.toLowerCase()).attr("id", "ptf" + val.Id);
        if (val.Type.toLowerCase() === "html5editor") {
            var summernote = $("#ptf" + val.Id).find("#html5Editor").removeClass("hidden").summernote({
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
                        $("#ptf" + val.Id + " #html5Editor").data("changed", true);
                        fieldChanged = true;

                        //Enable save button
                        $("#saveResources").attr("disabled", false);
                    },
                    onChange: function (contents, $editable) {
                        $("#ptf" + val.Id + " #html5Editor").data("changed", true);
                        fieldChanged = true;

                        //Enable save button
                        $("#saveResources").attr("disabled", false);
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
            summernote.summernote("code", val.Text);

            //After filling the field, set the data attribute and variable back to false. They are changed by the onChange of summernote after filling
            $("#ptf" + val.Id + " #html5Editor").data("changed", false);
            fieldChanged = false;

            $("#ptf" + val.Id).find("label#heading").html(val.Heading);
        } else if (val.Type === "text" || val.Type === "number" || val.Type === "email" || val.Type === "tel") {
            $("#ptf" + val.Id).find("input#input").removeClass("hidden").attr("type", val.Type).val(val.Text);
            $("#ptf" + val.Id).find("label#heading").html(val.Heading);
        } else if (val.Type === "textarea") {
            $("#ptf" + val.Id).find("textarea#textarea").removeClass("hidden").val(val.Text);
            $("#ptf" + val.Id).find("label#heading").html(val.Heading);
        } else if (val.Type.toLowerCase() === "checkbox") {
            $('#ptf' + val.Id).find('#checkbox').removeClass('hidden').find('input').prop('checked', (val.Text.toString().toLowerCase() === "true" ? true : false)).uniform();
            $("#ptf" + val.Id).find("label#heading").html(val.Heading);
        } else if (val.Type.toLowerCase() === "rgba") {
            rgba = true
            $("#ptf" + val.Id).find('input#rgba').removeClass('hidden').val(val.Text);
            $("#ptf" + val.Id).find("label#heading").html(val.Heading);
        }

        $('#ptf' + val.Id).find('.hidden').remove();
    });

    //If you type in the textboxes it will give a attribute to know that they need to update
    $("#pageTemplateFields input, #pageTemplateFields textarea").off().on("change paste keyup", function () {
        //Enable save button
        $("#saveResources").attr("disabled", false);

        $(this).data("changed", true);
        fieldChanged = true;
    });

    if (rgba) {
        $('#pageTemplateFields .rgba').off().colorpicker().on('changeColor',
            function (ev) {
                //Enable save button
                $("#saveResources").attr("disabled", false);

                $(this).data("changed", true);
                fieldChanged = true;
            }
        );
    }

    //On change that works for the checkbox
    $('#pageTemplateFields #checkbox input').off().change(function () {
        var checkbox = $(this).prop("checked", $(this).is(':checked'));
        $.uniform.update(checkbox);

        //Enable save button
        $("#saveResources").attr("disabled", false);

        $(this).data("changed", true);
        fieldChanged = true;
    });

    //Enable save button
    $("#saveResources").attr("disabled", true);
}

//Add upload boxes
function fillUploads(result) {
    $("#pageTemplateUploads").empty();
    if (result.length > 0) {
        $('#rootwizard').bootstrapWizard('enable', 2);
    } else {
        $('#rootwizard').bootstrapWizard('hide', 2);
    }

    $.each(result, function (i, val) {
        $("#cloneUploadObject").clone().appendTo("#pageTemplateUploads");
        $("#pageTemplateUploads").find("#cloneUploadObject").removeClass("hidden").attr("id", "ptu" + val.Id).attr("data-id", val.Id);
        $("#ptu" + val.Id).find("h4").html(val.Heading);
        $("#ptu" + val.Id).find("#uploader").attr("id", "uploader" + val.Id);

        //To save the order of the images
        $(".saveImageOrder").off().on("click", function () {
            //Get Id of the upload field
            var ptuId = $(this).parents(".pageUploadField").data("id");

            saveImagesOrder(ptuId);
        });

        var uploader = $("#uploader" + val.Id).off().pluploadQueue({
            runtimes: 'html5,flash,silverlight,html4',
            url: "/spine-api/page/file",

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
                "PageTemplateUploadId": val.Id,
                "PageId": pageId
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
                    fillFiles(pageId, val);
                },
                FileUploaded: function (uploader, file, result) {
                    var response = $.parseJSON(result.response);
                    toastr[response.value.messageType](response.value.message);
                },
                Error: function (uploader, result) {
                    //var response = result.responseJSON.value
                    if (result.status === 401 || result.status === 403) {
                        toastr['warning'](unauthorized);
                        setTimeout(function () { location.href = '/'; }, 4500)
                    }
                }
            }
        });

        $('#uploader' + val.Id + ' .plupload_add').on('click', function (e) {
            e.preventDefault();

            var input = document.getElementById(uploader[0].id + '_html5');
            if (input && !input.disabled) { // for some reason FF (up to 8.0.1 so far) lets to click disabled input[type=file]
                input.click();
            }
        });

        $('.imagesTab a').on('click', function () {
            uploader.splice();
        })

        //Fill files if they already exist in the database
        fillFiles(backUpPage.pageId, val);
    });

    //Change the upload buttons text
    $('.plupload_start').each(function () {
        $(this).html("Upload");
    });
}

//Update page information
function updatePageInformation() {
    //If page information is changed
    if (pageInformationChanged) {
        //Disable add/save button
        $("#savePage").attr("disabled", true);

        //Disable all buttons of the wizard
        disableWizardButtons();

        var url = "";
        if (!backUpPage.rootPage) {
            url = $("#url").val();
        }

        var data = {
            Id: pageId,
            Url: url,
            Name: $("#name").val(),
            Title: $("#title").val(),
            Keywords: $("#keywords").val(),
            Description: $("#description").val(),
            Type: (urlParts[3] === 'pages') ? 'page' : 'eCommerceCategory'
        };

        $.ajax({
            url: '/spine-api/page/update',
            dataType: 'json',
            type: 'post',
            data: data,
            success: function (result) {
                var response = result.value;
                toastr[response.messageType](response.message);

                pageInformationChanged = false;
                enableWizardButtons();
                    
                $('#rootwizard').bootstrapWizard('show', wizardGoTo);

                return true;
            },
            error: function (result) {
                if (result.status === 401 || result.status === 403) {
                    toastr['warning'](unauthorized);
                    setTimeout(function () { location.href = '/'; }, 4500);
                } else {
                    var response = result.responseJSON.value;
                    toastr[response.messageType](response.message);

                    //Enable add/save button
                    $("#savePage").attr("disabled", false);

                    //Enable all buttons of the wizard
                    enableWizardButtons();

                    //Check fields
                    var valid = $("#wizardForm").valid();
                    if (!valid) {
                        validatePageInformation.focusInvalid();
                        return false;
                    }
                }

                return true;
            }
        });

        return false;
    } else {
        return true;
    }
}

//Fill images
function fillFiles(pageId, pageTemplateUpload) {
    var data = {
        PageId: pageId,
        PageTemplateUploadId: pageTemplateUpload.Id
    };

    $.ajax({
        url: '/spine-api/page/files',
        dataType: 'json',
        type: 'get',
        data: data,
        success: function (result) {
            //Clear the div before it will be filled with files
            $('#ptu' + pageTemplateUpload.Id + ' .nestable ol').html('');

            $.each(result.data, function (i, val) {
                $('#cloneListObject').clone().appendTo('#ptu' + val.PageTemplateUploadId + ' .nestable ol');
                $('#ptu' + val.PageTemplateUploadId + ' .nestable').find('#cloneListObject').removeClass('hidden').attr('id', 'pf' + val.Id).attr('data-id', val.Id);

                var extension = val.OriginalPath.substr((val.OriginalPath.lastIndexOf('.') + 1)).toLowerCase();
                switch (extension) {
                    case 'jpg':
                    case 'jpeg':
                    case 'png':
                    case 'gif':
                        $('#pf' + val.Id).find('img').removeClass('hidden').attr('src', val.CompressedPath + '?lastmod=' + new Date());
                        $('#pf' + val.Id + ' .image-crop img').attr('src', val.OriginalPath);

                        $('#pf' + val.Id).find('#video').remove();
                        $('#pf' + val.Id).find('#pdf').remove();

                        if (extension !== 'png') {
                            $('#pf' + val.Id).find('.image-crop').addClass('image-crop-bg-black');
                            $('#pf' + val.Id).find('.img-preview').css('background-color', '#000000');
                        }
                        if (extension !== 'jpg') $('#pf' + val.Id).find('.image-quality').addClass('hidden');

                        $('#pf' + val.Id).find('#qualitySlider').change(function () {
                            $('#pf' + val.Id).find('#quality').html('(' + $(this).val() + ')');
                        });

                        var $image = $('#pf' + val.Id + ' .image-crop > img'),
                            width = pageTemplateUpload.Width,
                            height = pageTemplateUpload.Height;

                        $('#pf' + val.Id + ' #openCropMdl').on('click', function () {
                            $('#pf' + val.Id + ' #imageCropMdl').modal('show');

                            setTimeout(function () {
                                $image.cropper({
                                    aspectRatio: width / height,
                                    preview: '#pf' + val.Id + ' .img-preview'
                                });

                            }, 400);
                        });

                        $('#pf' + val.Id + ' #openCropMdl').on('hidden.bs.modal', function () {
                            $image.cropper('destroy');
                        });

                        $('#pf' + val.Id + ' #crop').on('click', function () {
                            if ($('.modal').modal('hide')) {
                                $image.cropper('getCroppedCanvas').toBlob(function (blob) {
                                    var formData = new FormData();
                                    formData.append('id', val.Id);
                                    formData.append('type', 'page');
                                    formData.append('croppedImage', blob);
                                    formData.append('quality', $('#pf' + val.Id).find('#qualitySlider').val());

                                    $.ajax('/spine-api/image/crop', {
                                        method: "POST",
                                        data: formData,
                                        processData: false,
                                        contentType: false,
                                        success: function (result) {
                                            var response = result.value;
                                            toastr[response.messageType](response.message);

                                            fillFiles(pageId, pageTemplateUpload);
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
                        if (extension === 'ogv') { extension = 'ogg'; }
                        $('#pf' + val.Id).find('#video').removeClass('hidden').find('source').attr('src', val.OriginalPath + '?lastmod=' + new Date()).attr('type', 'video/' + extension);

                        $('#pf' + val.Id).find('img').remove();
                        $('#pf' + val.Id).find('#cropHolder').remove();
                        $('#pf' + val.Id).find('#imageCropMdl').remove();
                        $('#pf' + val.Id).find('#pdf').remove();
                        break;
                    case 'pdf':
                        $('#pf' + val.Id).find('#pdf').removeClass('hidden').find('a').attr('href', val.OriginalPath);

                        $('#pf' + val.Id).find('img').remove();
                        $('#pf' + val.Id).find('#cropHolder').remove();
                        $('#pf' + val.Id).find('#imageCropMdl').remove();
                        $('#pf' + val.Id).find('#video').remove();
                        break;
                    default:
                        //console.log('who knows');
                }

                $('#pf' + val.Id).find('#checkbox').find('input').prop('checked', val.Active).uniform();
                $('#pf' + val.Id).find('#alt').val(val.Alt);
            });

            $('#ptu' + pageTemplateUpload.Id + ' .nestable').nestable({
                dragClass: 'dd-dragel page-file-dd-item',
                maxDepth: 1
            });

            //When the checkbox is clicked
            $('.dd-item .active').off().on('click', function () {
                //Prevent change of checkbox
                $(this).prop('checked', !$(this).prop('checked'));

                $('#modalStatusAlert').modal('show');

                pageFileId = $(this).parents('.dd-item').attr('data-id');
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

            $('.openDeleteFileModal').off().on('click', function () {
                pageFileId = $(this).parents('li').data('id');
                $('#modelDeleteImageAlert').modal('show');
            });

            if (result.data.length > 1) {
                $('#ptu' + pageTemplateUpload.Id + ' .saveImageOrder').removeClass('hidden');
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

//delete a page
function deletePage() {
    data = {
        Id: pageId,
        Type: (urlParts[3] === 'pages') ? 'page' : 'eCommerceCategory'
    };

    $.ajax({
        url: '/spine-api/page/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            location.href = '/' + urlParts[3];

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

//Get page by id
function getPage() {
    data = {
        Id: pageId,
        Type: (urlParts[3] === 'pages') ? 'page' : 'eCommerceCategory'
    };

    $.ajax({
        url: '/spine-api/page/get',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            backUpPage = response;
            fillPage();
            
            if (urlParts[3] === 'pages') {
                $("#pageTitle").html(editPage + ' <span id="displayName">' + response.name + '</span>').removeClass('hidden');
            } else {
                $("#pageTitle").html(editCategory + ' <span id="displayName">' + response.name + '</span>').removeClass('hidden');
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;

                if (response.redirect !== null && typeof response.redirect !== 'undefined') {
                    location.href = response.redirect;
                }

                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

function fillPage() {
    //People can't change the page template, so we disable this dropdown.
    $("#pageTemplate").attr("disabled", true);

    //Fill the fields with data that we'll get from our database
    $("#pageTemplate").find("option[data-id='" + backUpPage.pageTemplateId + "']").attr('selected', true);
    $("#name").val(backUpPage.name);

    //If it's the root page
    if (backUpPage.rootPage) {
        $('#settingsTab').remove();
        $("#url").val(backUpPage.url).attr('hide', true);
        $("#rootTitle").removeClass("hidden");
    } else {
        $('#rootwizard').bootstrapWizard('enable', 3);
        $("#pageUrl").removeClass("hidden");
        $("#url").val(backUpPage.url);
    }
    
    $("#title").val(backUpPage.title);
    $('#keywords').tagsinput('add', backUpPage.keywords);
    $("#description").val(backUpPage.description);

    $('#name').off().keyup(function () {
        $('#displayName').html($(this).val());

        //Enable add/save button
        $("#savePage").attr("disabled", false);

        if (alreadyCreated) {
            pageInformationChanged = true;
        }
    });

    alreadyCreated = true;
    sureOfCreate = true;
    $('#main-wrapper').fadeIn(500);
    afterCreate(backUpPage);
}

//Delete a file
function deleteFile(id) {
    data = {
        Id: id
    };

    $.ajax({
        url: '/spine-api/page/file/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            toastr[result.messageType](result.message);

            if (result.success === 'Valid') {
                $('#modelDeleteImageAlert').modal('hide');

                var ptuId = $('.nestable li[data-id="' + id + '"]').parents('.pageUploadField').data('id');

                $('.nestable li[data-id="' + id + '"]').remove();

                if ($('#ptu' + ptuId + ' ol > li').length < 2) {
                    $('#ptu' + ptuId).find('.saveImageOrder').addClass('hidden');
                }
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

function saveFileStatus(pageFileId) {
    var data = {
        id: pageFileId,
        active: activateCheckbox
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/page/file/active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $('#pf' + pageFileId).find("#active").prop("checked", activateCheckbox);
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

    $('#modalStatusAlert').modal('hide');
}

function saveFileAlt(pageFileId, alt) {
    var data = {
        id: pageFileId,
        alt: alt
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/page/file/alt',
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
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//Save order of images
function saveImagesOrder(ptuId) {
    //Get list on order with the id's
    var list = $(".pageUploadField[data-id='" + ptuId + "'] .nestable").nestable('serialize');

    $.ajax({
        url: '/spine-api/page/files/order/update',
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
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });
}

//Save page resources
function savePageResources() {
    //Disable save button
    $("#saveResources").attr("disabled", true);

    //Get value from fields and put it in the json
    $('#pageTemplateFields input#input, #pageTemplateFields input#cb, #pageTemplateFields input#rgba, #pageTemplateFields textarea#textarea, #pageTemplateFields #html5Editor, #pageTemplateFields select#select').each(function () {
        if ($(this).data("changed")) {
            var id = $(this).parents(".form-group").data("id");
            var text = "";
            if ($(this).is("input[type='checkbox']")) {
                text = $(this).is(':checked');
            } else if ($(this).parents(".form-group").data("type").toLowerCase() === "html5editor") {
                text = $(this).summernote('code');
            } else {
                text = $(this).val();
            }

            //Update backup resources
            for (var i = 0; i < backUpResources.length; i++) {
                if (backUpResources[i].Id === id) {
                    backUpResources[i].Text = text;
                    break;
                }
            }

            //Set everything back to false
            $(this).data("changed", false);
        }
    });


    //Fill fields from backup to fix the html 5 editor bug that duplicates the editor in to the editor
    $("#pageTemplateFields").empty();
    fillFields(backUpResources);

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/page/resources/update',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(backUpResources),
        success: function (result) {
            toastr[result.messageType](result.message);

            if (!result.success === "Valid") {
                //Enable save button
                $("#saveResources").attr("disabled", false);
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                //Enable save button
                $("#saveResources").attr("disabled", false);
            }

            return true;
        }
    });

    fieldChanged = false;
    $('#modelAlert').modal('hide');
}

function updatePageActive() {
    var data = {
        page: {
            id: pageId,
            active: activateCheckbox
        },
        type: (urlParts[3] === 'pages') ? 'page' : 'eCommerceCategory'
    };

    //Sends json to webservices.
    $.ajax({
        url: '/spine-api/page/active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $("#pageActive").prop("checked", activateCheckbox);
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