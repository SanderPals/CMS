var id = 0,
    validateForm,
    sectionVal,
    navigationItem;

$(document).ready(function () {
    //Make menu item active
    $('#navigation #navNavi' + $('#id').val()).addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    getList();

    //Creating validation for adding an item
    validateForm = $('#form').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } if (element.hasClass('select2-hidden-accessible')) {
                error.appendTo(element.parent());
            } else {
                error.insertAfter(element);
            }
        },
        rules: {
            name: {
                required: true,
                maxlength: 100
            },
            linkTo: {
                required: true
            }
        },
        messages: {
            name: {
                required: enterName
            },
            linkTo: {
                required: selectWhichType
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });

    $('#saveOrder').on('click', function () {
        var jsonObj = getListItems();

        if (jsonObj.length !== 0)
        {
            $.ajax({
                url: '/spine-api/navigation/items/update',
                dataType: 'json',
                type: 'post',
                data: JSON.stringify(jsonObj),
                contentType: 'application/json;',
                success: function (result) {
                    var response = result.value;
                    toastr[response.messageType](response.message);

                    $.each($('#nestable li'), function (i, val) {
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

    $('#addLink').on('click', function () {
        id = 0;
        sectionVal = 0;
        resetModalFields();
        $('#modalItem h4').html(addLink);
        $('#create').html(addLink);
        $('#closeHolder').removeClass('col-sm-4').addClass('col-sm-6');
        $('#createHolder').removeClass('col-sm-4').addClass('col-sm-6');
        $('#deleteHolder').addClass('hidden');
        $('#modalItem').modal('show');
    });

    $('#delete').on('click', function () {
        deleteItem();
    });

    $('#create').on('click', function () {
        //Check fields
        var valid = $('#form').valid();
        if (!valid) {
            validateForm.focusInvalid();
        } else {
            updateItem();
        }
    });

    $('#page select#pageSelect').on('change', function (e) {
        getSections(sectionVal);
        $('#page select#pageSelect').valid();
    });

    $('#linkTo').on('change', function () {
        switch ($(this).val().toLowerCase()) {
            case 'page':
                hideExternal();
                hideSelect();
                hideSelectSection();
                showTarget();
                getPages('', 'page', page, pleaseSelectPage);
                break;
            case 'category':
                hideExternal();
                hideSelect();
                hideSelectSection();
                showTarget();
                getPages('', 'eCommerceCategory', category, pleaseSelectCategory);
                break;
            case 'dataitem':
                hideExternal();
                hideSelect();
                hideSelectSection();
                showTarget();
                getDataItems();
                break;
            case 'product':
                hideExternal();
                hideSelect();
                hideSelectSection();
                showTarget();
                getProducts();
                break;
            case 'external':
                hideSelect();
                hideSelectSection();
                showTarget();
                fillExternal();
                break;
            default: //'nothing'
                hideExternal();
                hideSelect();
                hideSelectSection();
                hideTarget();
                break;
        }
    });

    //fix modal force focus
    $.fn.modal.Constructor.prototype.enforceFocus = $.noop;
});

function updateItem() {
    var data = {
        id: id,
        parent: (navigationItem !== undefined) ? navigationItem.parent : 0,
        navigationId: $('#id').val(),
        linkedToType: $('#linkTo').val(),
        filterAlternateGuid: ($('#section select#select').data('select2')) ? ($('#section select#select').val() !== null) ? $('#section select#select').val() : '0' : '0',
        linkedToAlternateGuid: ($('#page select#pageSelect').data('select2')) ? $('#page select#pageSelect').val() : '',
        name: $('#name').val(),
        target: ($('#linkTo').val() !== 'nothing') ? $('#target').val() : '',
        customUrl: $('#url').val(),
        customOrder: (navigationItem !== undefined) ? navigationItem.customOrder : 0
    };

    //Sends json to webservices
    $.ajax({
        url: '/spine-api/navigation/item/update',
        dataType: 'json',
        type: 'post',
        contentType: 'application/json;',
        data: JSON.stringify(data),
        success: function (result) {
            response = result.value;
            toastr[response.messageType](response.message);

            getList();
            $('#modalItem').modal('hide');

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

function createSelectOptions(title, data, placeholder, val) {
    var s = $('#page').find('label#heading').html(title).parents('#page').find('select#pageSelect').select2({
        allowClear: true,
        placeholder: {
            id: '0',
            text: placeholder,
            value: '',
            selected: 'selected'
        },
        data: data
    }).val(val).trigger('change').parents('#page').fadeIn(500).find('select#pageSelect').rules('add', {
        required: true,
        messages: {
            required: selectOption
        }
    });
}

function createSectionOptions(title, data, placeholder, val) {
    if (data.length !== 0) {
        var s = $('#section').find('label#heading').html(title).parents('#section').find('select#select').select2({
            allowClear: true,
            placeholder: {
                id: '0',
                text: placeholder,
                value: '',
                selected: 'selected'
            },
            data: data
        }).val(val).trigger('change').parents('#section').fadeIn(500);
    }
}

function generateList(result) {
    $('#navigationItems').empty();

    if (result.data.length > 1) $('#saveOrder').removeClass('hidden');

    $.each(result.data, function (i, val) {
        if (val.parent === 0) {
            $('#cloneListObject').clone().appendTo('#navigationItems');
            $('#navigationItems').find('#cloneListObject').removeClass('hidden').data('id', val.id).attr('id', 'n' + val.id);
            $('#n' + val.id).find('#navigationItemName a').html(val.name);

            createChilds(result.data, i, val);
        }
    });

    $('.dd-item a').off().on('click', function () {
        id = $(this).parents('li').data('id');
        $('#modalItem h4').html(editLink);
        $('#create').html(editLink);
        $('#closeHolder').addClass('col-sm-4').removeClass('col-sm-6');
        $('#createHolder').addClass('col-sm-4').removeClass('col-sm-6');
        $('#deleteHolder').removeClass('hidden');

        getItem();
    });

    $('#nestable').nestable({
        maxDepth: result.maxDepth,
        dragClass: 'dd-dragel dd-pages',
        itemNodeName: 'li.dd-item',
        handleClass: 'dd3-handle'
    });

    $('#nestable li').on('mousedown touchstart', function () {
        $(this).attr('data-changed', true);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}

function getItem() {
    var data = {
        id: id
    };

    $.ajax({
        url: '/spine-api/navigation/item',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: 'application/json;',
        success: function (result) {
            var response = result.value;
            resetModalFields();
            $('#modalItem').modal('show');

            navigationItem = response;
            $('#name').val(navigationItem.name);
            $('#linkTo').val(navigationItem.linkedToType.toLowerCase());

            sectionVal = (navigationItem.filterAlternateGuid !== '') ? navigationItem.linkedToSectionId + ':' + navigationItem.filterAlternateGuid : navigationItem.linkedToSectionId;
            switch (navigationItem.linkedToType.toLowerCase()) {
                case 'page':
                    showTarget();
                    getPages(navigationItem.linkedToAlternateGuid, 'page', page, pleaseSelectPage);
                    break;
                case 'category':
                    showTarget();
                    getPages(navigationItem.linkedToAlternateGuid, 'eCommerceCategory', category, pleaseSelectCategory);
                    break;
                case 'dataitem':
                    showTarget();
                    getDataItems(navigationItem.linkedToAlternateGuid);
                    break;
                case 'product':
                    showTarget();
                    getProducts(navigationItem.linkedToAlternateGuid);
                    break;
                case 'external':
                    showTarget();
                    fillExternal(navigationItem.customUrl);
                    break;
                default: //'nothing'
                    hideTarget();
                    break;
            }

            $('#target').val(navigationItem.target.toLowerCase());


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

getPages = function(val, type, title, placeholder) {
    $.ajax({
        url: '/spine-api/page-options',
        dataType: 'json',
        type: 'get',
        data: {
            type: type
        },
        contentType: 'application/json;',
        success: function (result) {
            var response = result.value;
            createSelectOptions(title, response.results, placeholder, val);

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


getSections = function(val) {
    var url = '/spine-api/page/template/sections/get/options';
    if ($('#linkTo').val() === 'detailPage') {
        url = '/spine-api/data/template/sections/get/options';
    } else if ($('#linkTo').val() !== 'page') { return; }

    var data = {
        alternateGuid: $('#page select#pageSelect').val()
    };

    $.ajax({
        url: url,
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: 'application/json;',
        success: function (result) {
            var response = result.value;

            hideSelectSection();
            createSectionOptions(goToSectionFilterOn, response.data, pleaseSelectSectionFilter, val);

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

getDataItems = function(val) {
    $.ajax({
        url: '/spine-api/data/get/options',
        dataType: 'json',
        type: 'get',
        contentType: 'application/json;',
        success: function (result) {
            var response = result.value;
            createSelectOptions(detailPage, response.data, pleaseSelectDetailPage, val);

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

getProducts = function (val) {
    $.ajax({
        url: '/spine-api/product-options',
        dataType: 'json',
        type: 'get',
        contentType: 'application/json;',
        success: function (result) {
            var response = result.value;
            createSelectOptions(product, response, pleaseSelectProduct, val);

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

createChilds = function(result, i, val) {
    //pwpid = pages with parent id
    var items = getObjects(result, 'parent', val.id);

    //If the json is NOT empty
    if (items.length !== 0) {
        $('#cloneHeadListObject').clone().appendTo('#n' + val.id);
        $('#navigationItems').find('#cloneHeadListObject').removeClass('hidden').attr('id', 'navItemHead' + val.id);

        $.each(items, function (i, val) {
            $('#cloneListObject').clone().appendTo('#navItemHead' + val.parent);
            $('#navigationItems').find('#cloneListObject').removeClass('hidden').data('id', val.id).attr('id', 'n' + val.id);
            $('#n' + val.id).find('#navigationItemName a').html(val.name);

            createChilds(result, i, val);
        });
    }

    return true;
}

//Get object out of json
getObjects = function(obj, key, val) {
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
getListItems = function() {
    jsonObj = [];

    var customOrder = 0;
    if ($('#nestable li[data-changed="true"]').length !== 0) {
        $('#nestable li').each(function () {
            item = {};
            item['id'] = $(this).data('id');
            item['customOrder'] = ++customOrder;

            //If parent not exist
            if ($(this).parents('li').length === 0) {
                item['parent'] = 0;
            } else {
                item['parent'] = $(this).parents('li').data('id');
            }

            jsonObj.push(item);
        });
    }
  
    return jsonObj;
}

resetModalFields = function() {
    $('#name').val('');
    hideExternal();
    hideSelect();
    hideSelectSection();
    resetLinkTo();
    resetTarget();
    validateForm.resetForm();
}

hideExternal = function() {
    $('#external').hide().find('input').val('');

    if (!jQuery.isEmptyObject($('#external #url').rules())) $('#external #url').rules('remove');
}

hideSelect = function() {
    if ($('#page select#pageSelect').data('select2')) {
        $('#page').hide().find('select#pageSelect').html('').select2('destroy');
    } else {
        $('#page').hide();
    }

    if (!jQuery.isEmptyObject($('#page select#pageSelect').rules())) $('#page select#pageSelect').rules('remove');
}

hideSelectSection = function() {
    if ($('#section select#select').data('select2')) {
        $('#section').hide().find('select#select').html('').select2('destroy');
    } else {
        if ($('#section').css('display') !== 'none') $('#section').hide();
    }
}

hideTarget = function() {
    if ($('#loadIn').css('display') !== 'none') $('#loadIn').hide();

    if (!jQuery.isEmptyObject($('#loadIn #target').rules())) $('#loadIn #target').rules('remove');
}

showTarget = function() {
    if ($('#loadIn').css('display') === 'none') $('#loadIn').fadeIn(500);

    if (jQuery.isEmptyObject($('#loadIn #target').rules())) $('#loadIn #target').rules('add', {
        required: true,
        messages: {
            required: selectTarget
        },
        maxlength: 500
    });

    validateForm.resetForm();
}

fillExternal = function(val) {
    $('#external').fadeIn(500).find('input').val(val).rules('add', {
        required: true,
        messages: {
            required: enterUrl
        },
        maxlength: 500
    });

    validateForm.resetForm();
}

resetLinkTo = function() {
    $('#linkTo>option:eq(0)').prop('selected', true);
}

resetTarget = function() {
    hideTarget();
    $('#target>option:eq(0)').prop('selected', true);
}

getList = function() {
    var data = {
        id: $('#id').val()
    };

    //Retrieves list of navigation items
    $.ajax({
        url: '/spine-api/navigation',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: 'application/json;',
        success: function (result) {
            var response = result.value;

            $('#title').html(response.name);
            setOptions(response.linkToOptions);
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
}

//delete item
deleteItem = function() {
    data = {
        Id: id
    };

    $.ajax({
        url: '/spine-api/navigation/item/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            getList();
            $('#modalDelete').modal('hide');
            $('#modalItem').modal('hide');

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

setOptions = function (result) {
    $('#linkTo').empty();
    $('#linkTo').append($('<option>', { value: '', text: selectOption }));

    $.each(result, function (i, val) {
        $('#linkTo').append($('<option>', { value: val.value, text: val.text }));
    });
}