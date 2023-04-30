var pageNumber = 1,
    pageSize = 10,
    totalPages = 1;

$(document).ready(function () {
    //Make menu item active
    $('#navigation #navOrders').addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    //Retrieves list of reviews
    getOrders();

    document.getElementById('previous').onclick = function () {
        --pageNumber;

        if (pageNumber === 1) {
            this.style.display = 'none';
        } else {
            this.setAttribute('disabled', 'disabled'); 
        }

        getOrders(pageNumber, pageSize);
    };

    document.getElementById('next').onclick = function () {
        ++pageNumber;

        if (pageNumber === totalPages) {
            this.style.display = 'none';
        } else {
            this.setAttribute('disabled', 'disabled');
        }

        getOrders(pageNumber, pageSize);
    };
});

createList = function (result) {
    if (result.data.length > 0) {
        $('#noData').remove();
        $('#list').empty();
    }

    $.each(result.data, function (i, val) {
        var listId = '#list';
        var itemId = '#item' + val.id;

        $('#CloneRowObject').clone().appendTo(listId);
        $(listId).find('#CloneRowObject').removeClass('hidden').data('id', val.id).attr('id', 'item' + val.id);

        $(itemId).find('#orderNumber').append(val.orderNumber);
        $(itemId).find('#name').append(val.name);
        $(itemId).find('#status span').addClass(val.statusClass).append(val.status);
        $(itemId).find('#transaction').append(val.transaction);
        $(itemId).find('#date').append(val.date);

        $(itemId + ' a.link').attr('href', $(itemId + ' a.link').attr('href') + val.id);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
};

getOrders = function (pageNumber = 1, pageSize = 10) {
    $.ajax({
        url: '/spine-api/orders',
        dataType: 'json',
        data: {
            pageNumber: pageNumber,
            pageSize: pageSize
        },
        type: 'get',
        contentType: 'application/json;',
        success: function (result) {
            createList(result.value);
            pageNumber = result.value.currentPage;
            totalPages = result.value.totalPages;

            if (pageNumber === 1) {
                document.getElementById('previous').style.display = 'none';
            } else {
                document.getElementById('previous').style.display = 'inline-block';
                document.getElementById('previous').removeAttribute('disabled');
            }

            if (pageNumber === totalPages) {
                document.getElementById('next').style.display = 'none';
            } else {
                document.getElementById('next').style.display = 'inline-block';
                document.getElementById('next').removeAttribute('disabled', 'disabled');
            }

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
};