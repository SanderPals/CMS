$(document).ready(function () {
    //Make menu item active
    $('#navigation #navData' + $('#id').val()).addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();;


    var data = {
        id: $('#id').val()
    };

    //Retrieves list of pages
    $.ajax({
        url: '/spine-api/data/items',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: "application/json;",
        success: function (result) {
            var response = result.value;

            $("#dataTitle").html(response.name);
            generateList(response);

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

    $("#saveOrder").on("click", function () {
        var jsonObj = getListItems();

        if (jsonObj.length !== 0)
        {
            $.ajax({
                url: '/spine-api/data/items/update',
                dataType: 'json',
                type: 'post',
                data: JSON.stringify(jsonObj),
                contentType: "application/json;",
                success: function (result) {
                    var response = result.value;
                    toastr[response.messageType](response.message);

                    $.each($("#nestable li"), function (i, val) {
                        $(this).attr('data-changed', false);
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
    });
});

function generateList(result) {
    if (result.data.length > 1) {
        $('#saveOrder').removeClass('hidden');
    }

    $.each(result.data, function (i, val) {
        $("#cloneListObject").clone().prependTo("#dataItems");
        $("#dataItems").find("#cloneListObject").removeClass("hidden").data("id", val.Id).attr("id", "d" + val.Id);
        $("#d" + val.Id).find("#dataItemTitle a").html(val.Title).attr("href", $("#d" + val.Id).find("#dataItemTitle a").attr("href") + val.Id);
    });

    $('#nestable').nestable({
        maxDepth: result.maxDepth,
        dragClass: "dd-dragel dd-pages",
        itemNodeName: 'li.dd-item',
        handleClass: "dd3-handle"
    });

    $('#nestable li').on("mousedown touchstart", function () {
        $(this).attr('data-changed', true);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}

//Get object out of json
function getObjects(obj, key, val) {
    var objects = [];
    for (var i in obj) {
        if (!obj.hasOwnProperty(i)) continue;
        if (typeof obj[i] === 'object') {
            objects = objects.concat(getObjects(obj[i], key, val));
        } else if (i === key && obj[key] === val) {
            objects.push(obj);
        }
    }

    return objects;
}

//Function checked if there are changed rows. If there is, the changed rows will put in a json
function getListItems() {
    jsonObj = [];

    var customOrder = 0;
    if ($('#nestable li[data-changed="true"]').length !== 0) {
        $('#nestable li').each(function () {
            item = {};
            item["Id"] = $(this).data("id");
            item["CustomOrder"] = ++customOrder;

            jsonObj.push(item);
        });
    }
  
    return jsonObj;
}