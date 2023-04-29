//Set default variables
var fieldChanged = false,
    id = 0,
    wizardGoTo = 0,
    backUp,
    fileId,
    activateCheckbox,
    validateDataInformation,
    validateProduct;

$(document).ready(function () {
    //Prevent users from submitting a form by hitting Enter
    $("form").keypress(function (e) {
        //Enter key
        if (e.target.localName !== 'textarea' && e.which === 13) {
            return false;
        }
    });

    //Bug fix to set a paragraph instead of a div in the summernote editor
    document.execCommand('defaultParagraphSeparator', false, 'p');

    $("#manageStock").uniform();
    $("#promoSchedule").uniform();
    $("#reviews").uniform();

    //When the checkbox is clicked
    $('#manageStock').on('click', function () {
        if ($(this).prop('checked') === true) {
            $('#manageStockInputs').removeClass('hidden');
            $('#stockStatusHolder').addClass('hidden');
        } else {
            $('#manageStockInputs').addClass('hidden');
            $('#stockStatusHolder').removeClass('hidden');
        }
    });

    //When the checkbox is clicked
    $('#promoSchedule').on('click', function () {
        if ($(this).prop('checked') === true) {
            $('#promoScheduleInputs').removeClass('hidden');
        } else {
            $('#promoScheduleInputs').addClass('hidden');
        }
    });

    getTaxClasses();
    getShippingClasses();

    //Get item id
    if ($("#productId").val() !== "") {
        id = $("#productId").val();
    } else {
        id = 0;
    }

    //Make menu item active
    $('#navigation #navProducts').addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    getItem();

    $("#create").click(function () {
        updateItem(wizardGoTo);
        $('#modalCreateAlert').modal('hide');
    });

    $("#updateResources").click(function () {
        updateResources(wizardGoTo);
        $('#modalUpdateAlert').modal('hide');
    });

    ////When the modal will be closed
    //$("#close").click(function () {
    //    fieldChanged = false;

    //    //Restore old recources back in to the fields
    //    $("#fields").empty();
    //    fillFieldLanguages(backUp.resources);

    //    $('#modelAlert').modal('hide');
    //    $('#rootwizard').bootstrapWizard('show', wizardGoTo);
    //});

    $('#saveProduct').on('click', function () {
        wizardGoTo = 0;

        //Check fields
        var valid = $('form#wizardForm').valid();
        if (!valid) {
            validateProduct.focusInvalid();
            return false;
        }

        //Show modal to be sure the person wants to create a item with this template
        if (id === 0) {
            $('#modalCreateAlert').find('#createText').removeClass('hidden');
            $('#modalCreateAlert').find('#updateText').addClass('hidden');
            $('#modalCreateAlert').find('#createBtn').removeClass('hidden');
            $('#modalCreateAlert').find('#updateBtn').addClass('hidden');
            $('#modalCreateAlert').modal('show');
            return false;
        }

        //If data item is already created
        if (id !== 0) {
            $('#modalCreateAlert').find('#updateText').removeClass('hidden');
            $('#modalCreateAlert').find('#createText').addClass('hidden');
            $('#modalCreateAlert').find('#updateBtn').removeClass('hidden');
            $('#modalCreateAlert').find('#createBtn').addClass('hidden');
            $('#modalCreateAlert').modal('show');
            return false;
        }
    });

    $('#save').on('click', function () {
        wizardGoTo = 1;

        $('#modalUpdateAlert').modal('show');
        return false;
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
            //var valid = $("#wizardForm").valid();
            //if (!valid) {
            //    validateDataInformation.focusInvalid();
            //    return false;
            //}

            wizardGoTo = clickedIndex;
            switch (currentIndex) {
                case 0:
                    //Check fields
                    var valid = $('form#wizardForm').valid();
                    if (!valid) {
                        validateProduct.focusInvalid();
                        return false;
                    }

                    //Show modal to be sure the person wants to create a item with this template
                    if (id === 0) {
                        $('#modalCreateAlert').find('#createText').removeClass('hidden');
                        $('#modalCreateAlert').find('#updateText').addClass('hidden');
                        $('#modalCreateAlert').find('#createBtn').removeClass('hidden');
                        $('#modalCreateAlert').find('#updateBtn').addClass('hidden');
                        $('#modalCreateAlert').modal('show');
                        return false;
                    }

                    if (fieldChanged) {
                        //If data item is already created
                        if (id !== 0) {
                            $('#modalCreateAlert').find('#updateText').removeClass('hidden');
                            $('#modalCreateAlert').find('#createText').addClass('hidden');
                            $('#modalCreateAlert').find('#updateBtn').removeClass('hidden');
                            $('#modalCreateAlert').find('#createBtn').addClass('hidden');
                            $('#modalCreateAlert').modal('show');
                            return false;
                        }
                    }

                    return true;
            }
        }
    });

    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#tab3") {
            fillUploads(backUp.uploads);
        }
    });

    //Click event for deleting the item
    $("#delete").on("click", function () {
        deleteItem();
    });

    //Click event for deleting a file
    $("#deleteFile").on("click", function () {
        deleteFile(fileId);
    });

    //Click event for changing file status
    $("#saveFileStatus").on("click", function () {
        saveFileStatus(fileId);
    });

    //When someone click on save in the modal to change review status
    $("#saveStatus").click(function () {
        updateActive();
    });

    jQuery.validator.addMethod('decimal', function (e, a) {
        return this.optional(a) || e.match(new RegExp('^\\d{0,14}(?:\\.\\d{0,' + '2' + '})?$'));
    }, digitsValidation + ' ' + '2' + ' ' + digitsValidationEnd);

    setProductValidation();
});

setProductValidation = function () {
    validateProduct = $('form#wizardForm').validate({
        ignore: [],
        ignoreTitle: true,
        errorPlacement: function (error, element) {
            if (error[0].outerText !== '') {
                if (element.parent('.tags-input').length) {
                    error.appendTo(element.parent());
                } else if (element.parent('.input-group').length) {
                    error.insertAfter(element.parent());
                } else {
                    error.insertAfter(element);
                }

                $('.nav-tabs li a[href="#' + element.parents('.tab-pane').attr('id') + '"]').css('color', '#c84348').tooltip({ placement: 'bottom', title: errorsTab });
                if ($('.nav-tabs li a[href="#' + element.parents('.tab-pane').attr('id') + '"] span').length === 0) {
                    $('.nav-tabs li a[href="#' + element.parents('.tab-pane').attr('id') + '"]').append('<span aria-hidden="true" class="icon-info pull-right" style="margin-top: 3px;"></span>');
                }
            }
        },
        unhighlight: function (element, errorClass, validClass) {
            if (element.id !== '') {
                var parent = $('#' + element.id).parents('.tab-pane');
                if (parent.find('label.error:visible').length === 0) {
                    $('.nav-tabs li a[href="#' + parent.attr('id') + '"]').removeAttr('style').tooltip('destroy').find('span').remove();
                }
            }
        },
        success: function (error, element) {
            var parent = $(element.id).parents('.tab-pane');
            error.remove();
            if (parent.find('label.error:visible').length === 0) {
                $('.nav-tabs li a[href="#' + parent.attr('id') + '"]').removeAttr('style').tooltip('destroy').find('span').remove();
            }
        },
        rules: {
            name: {
                required: true,
                maxlength: 255
            },
            price: {
                required: true,
                decimal: true
            },
            promoPrice: {
                required: false,
                decimal: true
            },
            stockQuantity: {
                required: false,
                digits: true
            },
            maxOrderProduct: {
                required: false,
                digits: true
            },
            weight: {
                required: false,
                decimal: true
            },
            length: {
                required: false,
                decimal: true
            },
            width: {
                required: false,
                decimal: true
            },
            height: {
                required: false,
                decimal: true
            },
            sku: {
                maxlength: 200
            }
        },
        messages: {
            name: {
                required: nameValidation,
                maxlength: maxLength255
            },
            price: {
                required: priceValidation
            },
            stockQuantity: {
                digits: digitsVal
            },
            maxOrderProduct: {
                digits: digitsVal
            },
            sku: {
                maxlength: maxLength200
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
};

//Get item by id
function getItem() {
    data = {
        id: id
    };

    $.ajax({
        url: '/api/product',
        dataType: 'json',
        type: 'get',
        data: data,
        success: function (result) {
            var response = result.value;
            backUp = response;
            fillItem(response);

            if (id !== 0) {
                $("#addTitle").addClass('hidden');
                $("#editTitle").removeClass('hidden');
                $("#editTitle").find('span').html(response.item.name);
            } else {
                $("#editTitle").addClass('hidden');
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                if (response.redirect !== 'undefined') {
                    location.href = response.redirect;
                } else {
                    toastr[response.messageType](response.message);
                }
            }

            return true;
        }
    });
}

//Get tax classes
function getTaxClasses() {
    $.ajax({
        url: '/spine-api/tax-classes',
        dataType: 'json',
        type: 'get',
        success: function (result) {
            var response = result.value;

            $.each(response, function (i, val) {
                $('#taxClass').append(new Option(val.name, val.id, val.default, val.default));
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

//Get shipping classes
function getShippingClasses() {
    $.ajax({
        url: '/spine-api/shipping-classes',
        dataType: 'json',
        type: 'get',
        success: function (result) {
            var response = result.value;

            $.each(response, function (i, val) {
                $('#shippingClass').append(new Option(val.name, val.id));
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

//Function would called 1 time after the item is created
function afterCreate(result) {
    //Change te button text after the item is created
    $("#saveProduct").html(save);

    //Set the data item id
    id = result.item.id !== 0 ? result.item.id : 0;

    //Create the fields and file panels
    fillFieldLanguages(result.resources);
    fillCategorieLanguages(result.categories);

    //When changing the value of an input, the value of the variable will be true
    $('.tab-item input, .tab-item textarea').on("change paste keyup", function () {
        fieldChanged = true;
    });

    //Give the publish checkbox styling
    $("#tab4 #active").prop("checked", result.item.id !== 0 ? result.item.active : false);
    $("#tab4 #active").uniform();

    //When the publish checkbox is clicked
    $('#tab4 #active').off().on("click", function () {
        //Prevent change of checkbox
        $(this).prop('checked', !$(this).prop('checked'));

        $('#modalItemStatusAlert').modal('show');

        if ($("#tab4 #active").prop("checked") === true) {
            activateCheckbox = false;
        } else {
            activateCheckbox = true;
        }
    });
}

//Add data template fields
function fillFields(result, div) {
    fillPageDetails(result.page, div);

    var rgba = false;
    if (result.fields.length > 0) {
        $(div + ' #fieldsHolder').removeClass('hidden');
    }
    console.log(result.page);
    console.log(result.fields);

    $.each(result.fields, function (i, val) {
        $("#cloneObject").clone(true).appendTo(div + " #fields");
        $(div + " #fields").find("#cloneObject").removeClass("hidden").data("id", val.fieldId).data("type", val.type.toLowerCase()).attr("id", "f" + val.fieldId);
        if (val.type.toLowerCase() === "html5editor") {
            var summernote = $(div + " #f" + val.fieldId).find("#html5Editor").removeClass("hidden").summernote({
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
                    icon: '<i class="note-icon">' + clean + ' </i>',
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
                    },
                    onChange: function (contents, $editable) {
                        fieldChanged = true;
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
            $(div + " #f" + val.fieldId + " #html5Editor").data("changed", false);
            fieldChanged = false;

            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "text" || val.type === "number" || val.type === "email" || val.type === "tel") {
            $(div + " #f" + val.fieldId).find("input#input").removeClass("hidden").attr("type", val.Type).val(val.text);
            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "textarea") {
            $(div + " #f" + val.fieldId).find("textarea#textarea").removeClass("hidden").val(val.text);
            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "textarea") {
            $(div + " #f" + val.fieldId).find("input#rgba").removeClass("hidden").val(val.text);
            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "checkbox") {
            $(div + " #f" + val.fieldId).find('#checkbox').removeClass('hidden').find('input').prop('checked', (val.text.toString().toLowerCase() === "true" ? true : false)).uniform();
            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "rgba") {
            rgba = true
            $(div + " #f" + val.fieldId).find('input#rgba').removeClass('hidden').val(val.text);
            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        } else if (val.type.toLowerCase() === "selectlinkedto") {
            $(div + " #f" + val.fieldId).find('select#select').removeClass('hidden').select2({
                multiple: true,
                "language": {
                    "noResults": function () {
                        return noResultsFound;
                    }
                },
                placeholder: select + ' ' + val.heading.toLowerCase() + '...',
                data: val.data
            }).val(val.text).trigger('change').on("change", function (e) {
                fieldChanged = true;
            });
            $(div + " #f" + val.fieldId).find("label#heading").html(val.heading);
        }

        $(div + " #f" + val.fieldId).find('.hidden').remove();
    });

    //When changing the value of an input, the value of the variable will be true
    $(div + " #fields input, " + div + " #fields textarea").off().on("change paste keyup", function () {
        fieldChanged = true;
    });

    if (rgba) {
        $(div + " #fields .rgba").off().colorpicker().on('changeColor',
            function (ev) {
                fieldChanged = true;
            }
        );
    }

    //On change that works for the checkbox
    $(div + ' #fields #checkbox input').off().change(function () {
        var checkbox = $(this).prop("checked", $(this).is(':checked'));
        $.uniform.update(checkbox);

        fieldChanged = true;
    });

    $('#main-wrapper').fadeIn(500);
}

//Add upload boxes
function fillUploads(result) {
    $('#uploads').empty();

    var length = result.length;
    $.each(result, function (i, val) {
        $("#cloneUploadObject").clone().appendTo("#uploads");
        $("#uploads").find("#cloneUploadObject").removeClass("hidden").attr("id", "u" + val.id).attr("data-id", val.id);
        $("#u" + val.id).find("h3").html(val.heading);
        $("#u" + val.id).find("#uploader").attr("id", "uploader" + val.id);

        //To save the order of the files
        $(".saveFileOrder").off().on("click", function () {
            //Get Id of the upload field
            var uId = $(this).parents(".uploadHolder").data("id");

            saveFileOrder(uId);
        });

        if (i === (length - 1)) {
            $("#u" + val.id).find("#divider").remove();
        }

        var uploader = $("#uploader" + val.id).off().pluploadQueue({
            runtimes: 'html5,flash,silverlight,html4',
            url: "/api/product-file",

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
                "uploadId": val.id,
                "id": id
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
                    fillFiles(id, val);
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
        fillFiles(id, val);
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
function fillFiles(id, upload) {
    var data = {
        id: id,
        uploadId: upload.id
    };

    $.ajax({
        url: '/api/product-files',
        dataType: 'json',
        type: 'get',
        data: data,
        success: function (result) {
            //Clear the div before it will be filled with files
            $('#u' + upload.id + ' .nestable ol').html('');

            $.each(result.value, function (i, val) {
                $('#cloneListObject').clone().appendTo('#u' + upload.id + ' .nestable ol');
                $('#u' + upload.id + ' .nestable').find('#cloneListObject').removeClass('hidden').attr('id', 'file' + val.id).attr('data-id', val.id);

                var extension = val.originalPath.substr((val.originalPath.lastIndexOf('.') + 1)).toLowerCase();
                switch (extension) {
                    case 'jpg':
                    case 'jpeg':
                    case 'png':
                    case 'gif':
                        $('#file' + val.id).find('.dd3-content img').removeClass('hidden').attr('src', val.compressedPath + '?lastmod=' + new Date());
                        $('#file' + val.id + ' .image-crop img').attr('src', val.originalPath);

                        $('#file' + val.id).find('#video').remove();
                        $('#file' + val.id).find('#pdf').remove();

                        if (extension !== 'png') {
                            $('#file' + val.id).find('.image-crop').addClass('image-crop-bg-black');
                            $('#file' + val.id).find('.img-preview').css('background-color', '#000000');
                        }
                        if (extension !== 'jpg') $('#file' + val.id).find('.image-quality').addClass('hidden');

                        $('#file' + val.id).find('#qualitySlider').change(function () {
                            $('#file' + val.id).find('#quality').html('(' + $(this).val() + ')');
                        });

                        var $image = $('#file' + val.id + ' .image-crop > img'),
                            width = upload.width,
                            height = upload.height

                        $('#file' + val.id + ' #openCropMdl').on('click', function () {
                            $('#file' + val.id + ' #imageCropMdl').modal('show');

                            setTimeout(function () {
                                $image.cropper({
                                    aspectRatio: width / height,
                                    preview: '#file' + val.id + ' .img-preview'
                                });
                            }, 400);
                        });

                        $('#file' + val.id + ' #openCropMdl').on('hidden.bs.modal', function () {
                            $image.cropper('destroy');
                        });

                        $('#file' + val.id + ' #crop').on('click', function () {
                            if ($('.modal').modal('hide')) {
                                $image.cropper('getCroppedCanvas').toBlob(function (blob) {
                                    var formData = new FormData();
                                    formData.append('id', val.id);
                                    formData.append('type', 'product');
                                    formData.append('croppedImage', blob);
                                    formData.append('quality', $('#file' + val.id).find('#qualitySlider').val());

                                    $.ajax('/spine-api/image/crop', {
                                        method: "POST",
                                        data: formData,
                                        processData: false,
                                        contentType: false,
                                        success: function (result) {
                                            var response = result.value;
                                            toastr[response.messageType](response.message);

                                            fillFiles(id, upload);
                                        },
                                        error: function (result) {
                                            if (result.status === 401 || result.status === 403) {
                                                toastr['warning'](unauthorized);
                                                setTimeout(function () { location.href = '/'; }, 4500);
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
                        $('#file' + val.id).find('#video').removeClass('hidden').find('source').attr('src', val.originalPath + '?lastmod=' + new Date()).attr('type', 'video/' + extension);

                        $('#file' + val.id).find('img').remove();
                        $('#file' + val.id).find('#cropHolder').remove();
                        $('#file' + val.id).find('#imageCropMdl').remove();
                        $('#file' + val.id).find('#pdf').remove();
                        break;
                    case 'pdf':
                        $('#file' + val.id).find('#pdf').removeClass('hidden').find('a').attr('href', val.originalPath);

                        $('#file' + val.id).find('img').remove();
                        $('#file' + val.id).find('#cropHolder').remove();
                        $('#file' + val.id).find('#imageCropMdl').remove();
                        $('#file' + val.id).find('#video').remove();
                        break;
                    default:
                        //console.log('who knows');
                }                

                $('#file' + val.id).find('#checkbox').find('input').prop('checked', val.active).uniform();
                $('#file' + val.id).find('#alt').val(val.Alt);
            });

            $('#u' + upload.id + ' .nestable').nestable({
                dragClass: 'dd-dragel page-file-dd-item',
                maxDepth: 1
            });

            //When the checkbox is clicked
            $('.dd-item .active').off().on('click', function () {
                //Prevent change of checkbox
                $(this).prop('checked', !$(this).prop('checked'));

                $('#modalStatusAlert').modal('show');

                fileId = $(this).parents('.dd-item').attr('data-id');
                if ($(this).prop('checked') === true) {
                    activateCheckbox = false;
                } else {
                    activateCheckbox = true;
                }
            });

            $('.saveAlt').off().on('click', function () {
                fileId = $(this).parents('li').data('id');
                var alt = $(this).parents('li').find('input#alt').val();

                saveFileAlt(fileId, alt);
            });

            $('.openDeleteFileModal').off().on('click', function () {
                fileId = $(this).parents('li').data('id');
                $('#modalDeleteFileAlert').modal('show');
            });

            if ((typeof result.value !== 'undefined') ? result.value.length > 1 : false) {
                $('#u' + upload.id + ' .saveFileOrder').removeClass('hidden');
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

//delete a data itam
function deleteItem() {
    data = {
        Id: id
    };

    $.ajax({
        url: '/api/delete-product',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            location.href = '/products';

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

//Delete a file
function deleteFile(id) {
    data = {
        Id: id
    };

    $.ajax({
        url: '/api/delete-product-file',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            $('#modalDeleteFileAlert').modal('hide');

            var uId = $('.nestable li[data-id="' + id + '"]').parents('.uploadHolder').data('id');

            $('.nestable li[data-id="' + id + '"]').remove();

            if ($('#u' + uId + ' ol > li').length < 2) {
                $('#u' + uId).find('.saveFileOrder').addClass('hidden');
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

function saveFileStatus(fileId) {
    var data = {
        id: fileId,
        active: activateCheckbox
    };

    //Sends json to webservices
    $.ajax({
        url: '/api/product-file-active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $('#file' + fileId).find("#active").prop("checked", activateCheckbox);
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

function saveFileAlt(fileId, alt) {
    var data = {
        id: fileId,
        alt: alt
    };

    //Sends json to webservices
    $.ajax({
        url: '/api/product-file-alt',
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
function saveFileOrder(uId) {
    //Get list on order with the id's
    var list = $(".uploadHolder[data-id='" + uId + "'] .nestable").nestable('serialize');

    $.ajax({
        url: '/api/product-files-custom-order',
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

//Save item
function updateItem(goTo) {
    updateItemBackup();

    var data = {
        item: backUp.item,
        categories: backUp.categories
    }

    //Sends json to webservices
    $.ajax({
        url: '/api/product',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            //If the data item id is 0, then we just added a new item
            if (id === 0) {
                id = response.item.id;
                backUp.item.id = response.item.id;
                $("#addTitle").addClass('hidden');
                $("#editTitle").removeClass('hidden');
                $("#editTitle").find('span').html(response.item.name);
            }

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

                fieldChanged = false;
            }

            return true;
        }
    });
}

//Save resources
function updateResources(goTo) {
    updateResourcesBackup();

    var data = {
        id: id,
        resources: backUp.resources
    };

    //Sends json to webservices
    $.ajax({
        url: '/api/product-resources',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            $('#tab2 #tabs').empty();
            $('#tab2 .languageSwitch').empty();
            fillFieldLanguages(response.resources);

            $('#rootwizard').bootstrapWizard('show', goTo);

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

            $('#tab2 #tabs').empty();
            $('#tab2 .languageSwitch').empty();
            fillFieldLanguages(backUp.resources);

            return true;
        }
    });
}

function updateActive() {
    var data = {
        id: id,
        active: activateCheckbox
    };

    //Sends json to webservices.
    $.ajax({
        url: '/api/product-active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $("#tab4 #active").prop("checked", activateCheckbox);
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

function fillPageDetails(result, div) {
    if (result !== null) {
        $(div + ' #pageTitle').val(result.title);
        $(div + ' #pageUrl').val(result.url);

        //Set url before slug
        $(div).find('#url').html(backUp.url);

        //Make the value url friendly while typing
        $(div).find('#pageUrl').on('keyup input propertychange', function (e) {
            if (e.keyCode !== 37 && e.keyCode !== 39 && e.keyCode !== 0) {
                createFriendlyUrl(this, div + ' #pageUrl', '.form-group');
            }
        });

        $(div + ' #pageKeywords').val(result.keywords).tagsinput();
        $(div + ' #pageDescription').val(result.description);
    } else {
        $(div + ' #pageKeywords').tagsinput();
    }

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

function fillItem(result) {
    var item = result.item;

    var active = false;
    if (item.id !== 0) {
        //General
        $('#name').val(item.name);
        $('#price').val(parseFloat(item.price).toFixed(result.settings.digitsAfterDecimal));
        $('#promoPrice').val(item.price !== item.promoPrice ? parseFloat(item.promoPrice).toFixed(result.settings.digitsAfterDecimal) : "");
        //$('#taxStatus').find("option[value='" + backUp.taxStatus + "']").attr('selected', true);
        $('#taxClass').find('option[value="' + item.taxClassId +'"]').attr('selected', true);

        if (item.promoSchedule === true) {
            $('#promoScheduleInputs').removeClass('hidden');

            active = $("#promoSchedule").prop("checked", item.promoSchedule);
            $.uniform.update(active);
        }

        //Inventory
        $('#sku').val(item.sku);
        $('#maxOrderProduct').val(item.maxPerOrder !== 0 ? item.maxPerOrder : "");

        if (item.manageStock === true) {
            $('#manageStockInputs').removeClass('hidden');
            $('#stockQuantity').val(item.stockQuantity);
            $('#backorders').find('option[value="' + item.backorders + '"]').attr('selected', 'selected');

            active = $("#manageStock").prop("checked", item.manageStock);
            $.uniform.update(active);
        } else {
            $('#stockStatusHolder').removeClass('hidden');
            $('#stockStatus').find('option[value="' + item.stockStatus + '"]').attr('selected', 'selected');
        }
    
        //Shipping
        $('#shippingClass').find('option[value="' + item.shippingClassId + '"]').attr('selected', 'selected');
        $('#weight').val(item.weight !== 0.00 ? item.weight : "");
        $('#length').val(item.length !== 0.00 ? item.length : "");
        $('#width').val(item.width !== 0.00 ? item.width : "");
        $('#height').val(item.height !== 0.00 ? item.height : "");
    } else {
        $('#stockStatusHolder').removeClass('hidden');
    }

    //Schedule promo price
    var date = getDate(item.id !== 0 ? new Date(item.promoFromDate) : new Date());
    $('input#promoFromDate').datepicker({
        orientation: "top auto",
        autoclose: true
    }).datepicker("update", date).on("input change", function (e) {
        //Enable save button
        $("#save").attr("disabled", false);

        fieldChanged = true;
    });
    $('input#promoFromTime').timepicker({ defaultTime: item.id !== 0 ? new Date(item.promoFromDate) : new Date() });

    date = getDate(item.id !== 0 ? new Date(item.promoToDate) : new Date());
    $('input#promoToDate').datepicker({
        orientation: "top auto",
        autoclose: true
    }).datepicker("update", date).on("input change", function (e) {
        //Enable save button
        $("#save").attr("disabled", false);

        fieldChanged = true;
    });
    $('input#promoToTime').timepicker({ defaultTime: item.id !== 0 ? new Date(item.promoToDate) : new Date() });

    //General
    if (result.settings.reviews) {
        active = $('#reviews').prop("checked", item.reviews);
        $.uniform.update(active);
    } else {
        $('#reviewsHolder').remove();
    }

    //Linekd products
    if (result.settings.upsells || result.settings.crossSells) {
        $('#linkedProducts').removeClass('hidden');
        $('#tabLinkedProducts').removeClass('hidden');
    }

    afterCreate(backUp);
}

function fillCategorieLanguages(result) {
    $.each(result, function (i, val) {
        $("#tab1 #cloneTab").clone(true).appendTo("#tab1 #tabs");
        $("#tab1 #tabs").find("#cloneTab").removeClass("hidden").data("id", val.id).data("language", val.code.toLowerCase()).attr("id", "c" + val.id).addClass('tab-categories');

        if (val.current) {
            $('#tab1 .languageSwitch').append('<a href="#c' + val.id + '" role="tab" data-toggle="tab" aria-expanded="false" class="btn btn-default active">' + val.code + '</a>');

            $('#tab1 .languageSwitch').addClass('active').addClass('in');
            $('#c' + val.id).addClass('active').addClass('in');
        } else {
            $('#tab1 .languageSwitch').append('<a href="#c' + val.id + '" role="tab" data-toggle="tab" aria-expanded="false" class="btn btn-default">' + val.code + '</a>');
        }

        var value = [];
        $.each(val.categories, function (i, v) {
            value.push(v.id);
        });

        $('#c' + val.id + ' #selectCategories').select2({
            multiple: true,
            placeholder: selectCategory,
            language: 'nl',
            allowClear: true,
            data: val.categories,
            ajax: {
                delay: 250,
                url: '/spine-api/categories-by-name',
                dataType: 'json',
                data: function (params) {
                    var query = {
                        term: params.term
                    };

                    return query;
                },
                processResults: function (res) {
                    return res.value.data;
                }

                // Additional AJAX parameters go here; see the end of this chapter for the full code of this example
            }
        }).val(value).trigger('change').on("change", function (e) {
            fieldChanged = true;
        });

        //Set fieldChanged back to false
        fieldChanged = false;
    });

    $('#tab1 .languageSwitch a').off().on('click', function () {
        $('#tab1 .languageSwitch a').removeClass('active');
        $(this).addClass('active');
    });

    if (result.length === 1) { $('#tab1 .languageSwitch').remove(); }
}

function fillFieldLanguages(result) {
    $.each(result, function (i, val) {
        $("#tab2 #cloneTab").clone(true).appendTo("#tab2 #tabs");
        $("#tab2 #tabs").find("#cloneTab").removeClass("hidden").data("id", val.id).data("language", val.code.toLowerCase()).attr("id", "t" + val.id).addClass('tab-resources');

        if (val.current) {
            $('#tab2 .languageSwitch').append('<a href="#t' + val.id + '" role="tab" data-toggle="tab" aria-expanded="false" class="btn btn-default active">' + val.code + '</a>');

            $('#t' + val.id).addClass('active').addClass('in');
        } else {
            $('#tab2 .languageSwitch').append('<a href="#t' + val.id + '" role="tab" data-toggle="tab" aria-expanded="false" class="btn btn-default">' + val.code + '</a>');
        }

        fillFields(val, '#t' + val.id);
    });

    $('#tab2 .languageSwitch a').off().on('click', function () {
        $('#tab2 .languageSwitch a').removeClass('active');
        $(this).addClass('active');
    });

    if (result.length === 1) { $('#tab2 .languageSwitch').remove(); }
}

function updateItemBackup() {
    if (backUp.item === null) { backUp.item = {}; }

    backUp.item.name = $('#name').length === 1 ? $('#name').val() : backUp.item.name === undefined ? '' : backUp.item.name;
    backUp.item.price = parseFloat(($('#price').length === 1 ? ($('#price').val() === "" ? 0 : $('#price').val()) : backUp.item.price === undefined ? 0 : backUp.item.price)).toFixed(2);
    backUp.item.promoPrice = parseFloat(($('#promoPrice').length === 1 ? ($('#promoPrice').val() === "" ? backUp.item.price : $('#promoPrice').val()) : backUp.item.promoPrice === undefined ? 0.00 : backUp.item.promoPrice)).toFixed(2);
    backUp.item.taxStatus = 'taxable';
    backUp.item.taxClassId = parseInt($('#taxClass').length === 1 ? $('#taxClass option:selected').val() : backUp.item.taxClass === undefined ? "" : backUp.item.taxClass);
    backUp.item.reviews = $('#reviews').length === 1 ? $('#reviews').is(':checked') : backUp.item.reviews === undefined ? false : backUp.item.reviews;
    backUp.item.sku = $('#sku').length === 1 ? $('#sku').val() : backUp.item.sku === undefined ? "" : backUp.item.sku;
    backUp.item.manageStock = $('#manageStock').length === 1 ? $('#manageStock').is(':checked') : backUp.item.manageStock === undefined ? false : backUp.item.manageStock;
    backUp.item.stockQuantity = parseInt($('#stockQuantity').length === 1 ? ($('#stockQuantity').val() === "" ? 0 : $('#stockQuantity').val()) : backUp.item.stockQuantity === undefined ? 0 : backUp.item.stockQuantity);
    backUp.item.backorders = $('#backorders').length === 1 ? $('#backorders option:selected').val() : backUp.item.backorders === undefined ? "no" : backUp.item.backorders;
    backUp.item.stockStatus = $('#stockStatus').length === 1 ? $('#stockStatus option:selected').val() : backUp.item.stockStatus === undefined ? "stock" : backUp.item.stockStatus;
    backUp.item.maxPerOrder = parseInt($('#maxOrderProduct').length === 1 ? ($('#maxOrderProduct').val() === "" ? 0 : $('#maxOrderProduct').val()) : backUp.item.maxPerOrder === undefined ? 0 : backUp.item.maxPerOrder);
    backUp.item.shippingClassId = parseInt($('#shippingClass').length === 1 ? $('#shippingClass').val() : backUp.item.shippingClass === undefined ? "" : backUp.item.shippingClass);
    backUp.item.weight = parseFloat(($('#weight').length === 1 ? ($('#weight').val() === "" ? 0 : $('#weight').val()) : backUp.item.weight === undefined ? 0 : backUp.item.weight)).toFixed(2);
    backUp.item.width = parseFloat(($('#width').length === 1 ? ($('#width').val() === "" ? 0 : $('#width').val()) : backUp.item.width === undefined ? 0 : backUp.item.width)).toFixed(2);
    backUp.item.height = parseFloat(($('#height').length === 1 ? ($('#height').val() === "" ? 0 : $('#height').val()) : backUp.item.height === undefined ? 0 : backUp.item.height)).toFixed(2);
    backUp.item.length = parseFloat(($('#length').length === 1 ? ($('#length').val() === "" ? 0 : $('#length').val()) : backUp.item.length === undefined ? 0 : backUp.item.length)).toFixed(2);
    backUp.item.promoSchedule = $('#promoSchedule').length === 1 ? $('#promoSchedule').is(':checked') : backUp.item.promoSchedule === undefined ? false : backUp.item.promoSchedule;
    backUp.item.promoFromDate = $('#promoFromDate').length === 1 && $('#promoFromTime').length === 1 ? $('#promoFromDate').val() + " " + $('#promoFromTime').val() : backUp.item.promoFromDate === undefined ? new Date() : backUp.item.promoFromDate;
    backUp.item.promoToDate = $('#promoToDate').length === 1 && $('#promoToTime').length === 1 ? $('#promoToDate').val() + " " + $('#promoToTime').val() : backUp.item.promoToDate === undefined ? new Date() : backUp.item.promoToDate;

    //Get value from fields and put it in to a json
    $('.tab-categories').each(function () {
        var code = $(this).data('language');

        //Update categories backup
        for (var i = 0; i < backUp.categories.length; i++) {
            if (backUp.categories[i].code === code) {
                backUp.categories[i].categories = $(this).find('select').val();
                break;
            }
        }
    });
}

function updateResourcesBackup() {
    //Get value from fields and put it in to a json
    $('.tab-resources').each(function () {
        var code = $(this).data('language');

        //Update resources backup
        for (var i = 0; i < backUp.resources.length; i++) {
            if (backUp.resources[i].code === code) {
                resources = backUp.resources[i];

                if (backUp.resources[i].page === null) { backUp.resources[i].page = {}; }
                backUp.resources[i].page.title = $(this).find('#pageTitle').val();
                backUp.resources[i].page.url = $(this).find('#pageUrl').val();
                backUp.resources[i].page.keywords = $(this).find('#pageKeywords').val();
                backUp.resources[i].page.description = $(this).find('#pageDescription').val();
                backUp.resources[i].page.active = true;

                $(this).find('#fields input#input, #fields input#cb, #fields input#rgba, #fields textarea#textarea, #fields #html5Editor, #fields select#select').each(function () {
                    var id = $(this).parents(".form-group").data("id");
                    var text = "";
                    if ($(this).is("input[type='checkbox']")) {
                        text = $(this).is(':checked').toString().toLowerCase();
                    } else if ($(this).parents(".form-group").data("type") === "html5editor") {
                        text = $(this).summernote('code');
                    } else {
                        text = $(this).val();
                    }

                    for (var i = 0; i < resources.fields.length; i++) {
                        if (resources.fields[i].fieldId === id) {
                            resources.fields[i].text = text;
                            break;
                        }
                    }
                });

                break;
            }
        }
    });
}

function getDate(datetime) {
    //var fullDate = new Date(datetime);
    var twoDigitMonth = datetime.getMonth() + 1;
    var twoDigitDate = datetime.getDate();
    return currentDate = twoDigitMonth + "/" + twoDigitDate + "/" + datetime.getFullYear();
}