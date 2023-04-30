$(function () {
    // Make menu item active
    $("#navigation #navCommerceSettings").addClass("active").parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    $.fn.editableform.buttons = '<button type="submit" class="btn btn-primary editable-submit"><i class="glyphicon glyphicon-ok"></i></button><button type="button" class="btn btn-alert editable-cancel"><i class="glyphicon glyphicon-remove"></i></button>';
    $.fn.editable.defaults.mode = 'inline';

    getSettings();
});

/**
 * Get settings
 */
getSettings = function () {
    $.ajax({
        url: '/api/commerce-settings',
        dataType: 'json',
        type: 'get',
        contentType: "application/json;",
        success: function (result) {
            fillSettings(result);
            $('#main-wrapper').fadeIn(500);

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

/**
 * Fill setting values
 */
fillSettings = function (result) {
    $('#invoicePrefix td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.invoicePrefix.id + '" class="editable editable-click" style="display: inline;">' + result.invoicePrefix.value + '</a>');
    $('#invoiceSuffix td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.invoiceSuffix.id + '" class="editable editable-click" style="display: inline;">' + result.invoiceSuffix.value + '</a>');
    $('#invoiceCurrent td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.invoiceCurrent.id + '" class="editable editable-click" style="display: inline;">' + result.invoiceCurrent.value + '</a>');
    $('#creditPrefix td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.creditPrefix.id + '" class="editable editable-click" style="display: inline;">' + result.creditPrefix.value + '</a>');
    $('#creditSuffix td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.creditSuffix.id + '" class="editable editable-click" style="display: inline;">' + result.creditSuffix.value + '</a>');
    $('#orderPrefix td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.orderPrefix.id + '" class="editable editable-click" style="display: inline;">' + result.orderPrefix.value + '</a>');
    $('#orderSuffix td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.orderSuffix.id + '" class="editable editable-click" style="display: inline;">' + result.orderSuffix.value + '</a>');
    $('#orderCurrent td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.orderCurrent.id + '" class="editable editable-click" style="display: inline;">' + result.orderCurrent.value + '</a>');
    $('#addressLine1 td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.addressLine1.id + '" class="editable editable-click" style="display: inline;">' + result.addressLine1.value + '</a>');
    $('#zipCode td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.zipCode.id + '" class="editable editable-click" style="display: inline;">' + result.zipCode.value + '</a>');
    $('#city td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.city.id + '" class="editable editable-click" style="display: inline;">' + result.city.value + '</a>');
    $('#country td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.country.id + '" class="editable editable-click" style="display: inline;">' + result.country.value + '</a>');
    $('#coc td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.coc.id + '" class="editable editable-click" style="display: inline;">' + result.coc.value + '</a>');
    $('#vat td:last-of-type').html('<a href="#" data-type="text" data-pk="' + result.vat.id + '" class="editable editable-click" style="display: inline;">' + result.vat.value + '</a>');

    initEditable();
}

/**
 * Initialize editable fields
 */
initEditable = function () {
    $('a.editable').editable({
        inputclass: 'form-control',
        url: function (params) {
            console.log(params);
            return $.ajax({
                url: '/api/setting',
                dataType: 'json',
                type: 'post',
                data: {
                    Id: params.pk,
                    Value: params.value,
                },
                success: function (result, newValue) {
                    toastr[result.value.messageType](result.value.message);
                },
                error: function (result) {
                    if (result.status === 401 || result.status === 403) {
                        toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
                        setTimeout(function () { location.href = '/'; }, 4500);
                    } else {
                        if (result.responseJSON) {
                            var response = result.responseJSON.value;
                            toastr[response.messageType](response.message);
                        }
                    }

                    return true;
                }
            });
        },
        success: function (response, newValue) {
            if (response.status === 'error') return response.msg; //msg will be shown in editable form
        }
    });
}