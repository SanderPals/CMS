$(document).ready(function () {
    //var url = window.location.href;
    ////get rid of the trailing / before doing a simple split on /
    //var urlParts = url.replace(/\/\s*$/, '').split('/');
    ////since we do not need example.com
    //urlParts.shift();
    //console.log(urlParts[0]);

    //Make menu item active
    $('#navigation #navBreadcrumbs').addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    //Retrieves list of pages
    $.ajax({
        url: '/spine-api/pages',
        dataType: 'json',
        type: 'get',
        contentType: "application/json;",
        success: function (result) {
            var response = result.value;
            createList(response);

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
        var jsonObj = getChangedListItems();

        if (jsonObj.length !== 0) {
            $.ajax({
                url: '/spine-api/pages/update',
                dataType: 'json',
                type: 'post',
                data: JSON.stringify(jsonObj),
                contentType: "application/json;",
                success: function (result) {
                    toastr[result.messageType](result.message);

                    if (result.success === "Valid") {
                        $.each($("#nestable li"), function (i, val) {
                            $(this).data("changed", false);
                        });
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
    });
});

function createList(result) {
    if (result.data.length > 1) {
        $('#saveOrder').removeClass('hidden');
    }

    $.each(result.data, function (i, val) {
        if (val.parent === 0) {
            //If it isn't the root page
            if (!val.rootPage) {
                $("#cloneListObject").clone().appendTo("#pages");
                $("#pages").find("#cloneListObject").removeClass("hidden").data("id", val.id).attr("id", "p" + val.id);
                $("#p" + val.id).find("#name a").html(val.name).attr("href", ((val.type === 'page') ? '/pages/edit/' :  '/categories/edit/') + val.id);

                createPageChild(result.data, i, val);
            } else {
                $("#cloneListObject").clone().prependTo("#pages");
                $("#pages").find("#cloneListObject").removeClass("hidden").data("id", val.id).attr("id", "p" + val.id);
                $("#p" + val.id).find("#name a").html(val.name).attr("href", ((val.type === 'page') ? '/pages/edit/' : '/categories/edit/') + val.id);
                $("#p" + val.id).find(".dd-handle").removeClass("dd3-handle").addClass("dd-root-handle").addClass("fa").addClass("fa-home");
                $("#p" + val.id).removeClass("dd-item").removeClass("dd3-item");
            }
        }
    });

    $('#nestable').nestable({
        maxDepth: result.maxDepth,
        dragClass: "dd-dragel dd-pages",
        itemNodeName: 'li.dd-item',
        handleClass: "dd3-handle"
    });

    $('#nestable li').on("mousedown touchstart", function () {
        $(this).data("changed", true);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}

function createPageChild(result, i, val) {
    //pwpid = pages with parent id
    var pwpid = getObjects(result, 'parent', val.id);

    //If the json is NOT empty
    if (pwpid.length !== 0) {
        $("#cloneHeadListObject").clone().appendTo("#p" + val.id);
        $("#pages").find("#cloneHeadListObject").removeClass("hidden").attr("id", "pageHead" + val.id);

        $.each(pwpid, function (i, val) {
            $("#cloneListObject").clone().appendTo("#pageHead" + val.parent);
            $("#pages").find("#cloneListObject").removeClass("hidden").data("id", val.id).attr("id", "p" + val.id);
            $("#p" + val.id).find("#name a").html(val.name).attr("href", $("#p" + val.id).find("#name a").attr("href") + val.id);

            createPageChild(result, i, val);
        });
    }

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
function getChangedListItems() {
    jsonObj = [];
    $('#nestable li').each(function () {
        if ($(this).data("changed")) {
            item = {};
            item["Id"] = $(this).data("id");

            //If parent not exist
            if ($(this).parents("li").length === 0) {
                item["Parent"] = 0;
            } else {
                item["Parent"] = $(this).parents("li").data("id");
            }

            jsonObj.push(item);
        }
    });

    return jsonObj;
}