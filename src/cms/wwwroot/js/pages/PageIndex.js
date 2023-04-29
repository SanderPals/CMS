$(document).ready(function () {
    //Make menu item active
    $('#navigation #navPages').addClass('active');

    var data = {
        type: 'page'
    };

    //Retrieves list of reviews
    $.ajax({
        url: '/spine-api/pages-by-type',
        dataType: 'json',
        type: 'get',
        data: data,
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
    if (result.data.length > 0) { $('#noData').remove(); }

    $.each(result.data, function (i, val) {
        var listId = '#list';
        var listItemId = '#listItem' + val.id;

        $('#CloneRowObject').clone().appendTo(listId);
        $(listId).find('#CloneRowObject').removeClass('hidden').data('id', val.id).attr('id', 'listItem' + val.id);

        $(listItemId).find('#name').append(val.name);

        $(listItemId + ' a.link').attr('href', $(listItemId + ' a.link').attr('href') + val.id);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}