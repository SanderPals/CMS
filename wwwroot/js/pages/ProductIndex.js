$(document).ready(function () {
    //Make menu item active
    $('#navigation #navProducts').addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    //Retrieves list of reviews
    $.ajax({
        url: '/api/products',
        dataType: 'json',
        type: 'get',
        contentType: 'application/json;',
        success: function (result) {
            createList(result.value);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning']('You are unauthorized to do this. You will be redirected to the login screen.');
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
});

function createList(result) {
    if (result.length > 0) { $('#noData').remove(); }

    $.each(result, function (i, val) {
        var listId = '#list';
        var itemId = '#item' + val.id;

        $('#CloneRowObject').clone().appendTo(listId);
        $(listId).find('#CloneRowObject').removeClass('hidden').data('id', val.id).attr('id', 'item' + val.id);

        $(itemId).find('#name').append(val.title);
        $(itemId).find('#sku').append(val.sku);
        $(itemId).find('#price').append(val.price);
        $(itemId).find('#categories').append(val.categories);
        $(itemId).find('#title').append(val.title);

        $(itemId + ' a.link').attr('href', $(itemId + ' a.link').attr('href') + val.id);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}