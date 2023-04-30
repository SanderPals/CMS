var summernote,
    type,
    id;

$(document).ready(function () {
    //Bug fix to set a paragraph instead of a div in the summernote editor
    document.execCommand('defaultParagraphSeparator', false, 'p');

    //Make menu item active
    $("#navigation #navResources").addClass("active");

    $('button.close').on('click', function (event) {
        console.log('f');
        event.preventDefault();
        event.stopPropagation();
        $(this).closest('.modal').modal('hide');
    });

    var data = {
        WebsiteLanguageId: $("#websiteLanguageId").val()
    };

    //Retrieves list of resources
    $.ajax({
        url: '/spine-api/resources',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: "application/json;",
        success: function (result) {
            var response = result.value;
            createList(response);

            //If there are no resources in the database, then hide buttons
            if (!response.files) { $("#websiteBtns").remove(); }


            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                setTimeout(function () { location.href = '/'; }, 4500)
            } else {
                var response = result.responseJSON.value;
                toastr[response.messageType](response.message);
            }

            return true;
        }
    });

    $("#save").click(function () {
        var text = "";
        switch (type) {
            case "rgba":
                //text = val.Text;
                //$("#wr" + val.Id).find("#colorBox").css("background-color", result.Text).removeClass("hidden");
                break;
            case "checkbox":
                text = $("#checkbox input").val();
                break;
            case "radio":
                text = $("#radio input").val();
                break;
            case "text":
                text = $("#input").val();
                break;
            case "textarea":
                text = $("#textarea").val();
                break;
            case "html5editor":
                text = $("#html5Editor").summernote('code');
                break;
        }

        var data = {
            Id: id,
            Text: text
        };

        $.ajax({
            url: '/spine-api/website/resource/update',
            dataType: 'json',
            type: 'post',
            contentType: "application/json;",
            data: JSON.stringify(data),
            success: function (result) {
                var response = result.value;
                toastr[response.messageType](response.message);

                switch (type) {
                    case "rgba":
                        $("#wr" + id).find("#colorBox").css("background-color", text).removeClass("hidden");
                        break;
                    case "checkbox":
                    case "radio":
                        if (text.toLowerCase() === "true") { text = "Yes"; } else { text = "No"; }
                        break;
                    default:
                        //Variable text is already filled
                        break;
                }

                $("#wr" + id).find("#resourceText #textBox").html(text);

                $("#modalEdit").modal('hide');

                return true;
            },
            error: function (result) {
                if (result.status === 401 || result.status === 403) {
                    toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                    setTimeout(function () { location.href = '/'; }, 4500)
                } else {
                    var response = result.responseJSON.value;
                    toastr[response.messageType](response.message);
                }

                return true;
            }
        });
    });
});

function createList(result) {
    $.each(result.data, function (i, val) {
        $("#cloneRowObject").clone().appendTo("#resources");
        $("#resources").find("#cloneRowObject").removeClass("hidden").data("id", val.Id).attr("id", "wr" + val.Id).data("type", val.Type.toLowerCase());

        $("#wr" + val.Id).find("#resourceDescription").append(val.Heading);

        var text;
        if (val.Type.toLowerCase() === "rgba") {
            text = val.Text;
            $("#wr" + val.Id).find("#colorBox").css("background-color", val.Text).removeClass("hidden");
        } else if (val.Type.toLowerCase() === "checkbox" || val.Type.toLowerCase() === "radio") {
            if(val.Text.toLowerCase() === "true") { text = "Yes"; } else { text = "No"; }
        } else { text = val.Text; }

        $("#wr" + val.Id).find("#resourceText #textBox").html(text);
    });

    //Click on row to edit
    $(".resourceRow").on("click", function () {
        id = $(this).data("id");
        type = $(this).data("type");

        var data = {
            Id: id
        };

        //Sends json to webservices
        $.ajax({
            url: '/spine-api/website/resource/get',
            dataType: 'json',
            type: 'get',
            contentType: "application/json;",
            data: data,
            success: function (result) {
                var response = result.value;
                fillModal(response);

                return true;
            },
            error: function (result) {
                if (result.status === 401 || result.status === 403) {
                    toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                    setTimeout(function () { location.href = '/'; }, 4500)
                } else {
                    var response = result.responseJSON.value;
                    toastr[response.messageType](response.message);
                }

                return true;
            }
        });
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}

function fillModal(result) {
    resetFields();

    if (type.toLowerCase() === "rgba") {
        //text = val.Text;
        //$("#wr" + val.Id).find("#colorBox").css("background-color", result.Text).removeClass("hidden");
    } else if (type.toLowerCase() === "checkbox") {
        $("#checkbox").removeClass("hidden");
        $("#checkbox input").prop("checked", result.text);
    } else if (type.toLowerCase() === "radio") {
        $("#radio").removeClass("hidden");
        $("#radio input").prop("checked", result.text);
    } else if (type.toLowerCase() === "text") {
        $("#input").val(result.text).removeClass("hidden");
    } else if (type.toLowerCase() === "textarea") {
        $("#textarea").val(result.text).removeClass("hidden");
    } else if (type.toLowerCase() === "html5editor") {
        $("#html5Editor").removeClass("hidden");

        //Activate html 5 editor
        summernote = $("#html5Editor").summernote({
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
                icon: '<i class="note-icon">Verwijder opmaak</i>',
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
            }
        });

        $('.modal').on('shown.bs.modal', function (e) {
            $('button.close').off().on('click', function (event) {
                console.log('f');
                event.preventDefault();
                event.stopPropagation();
                $(this).closest('.modal').modal('hide');
            });
        });

        summernote.summernote("code", result.text);
    }

    $("#modalEdit").modal('show');
}

function resetFields() {
    //Destroy html 5 editor
    if (!$("#html5Editor").hasClass("hidden"))
    {
        summernote.summernote('destroy');
    }

    $("#radio input, #checkbox input, #input, #html5Editor, #textarea").addClass("hidden");
}