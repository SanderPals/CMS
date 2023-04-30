$(document).ready(function () {
    //Make menu item active
    $("#navigation #navUsers").addClass("active").parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    //Retrieves list of users
    $.ajax({
        url: '/spine-api/users',
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
});

function createList(result) {
    if (result.users.length > 0) { $('#noData').remove(); }

    $.each(result.users, function (i, val) {
        $("#CloneRowObject").clone().appendTo("#users");
        $("#users").find("#CloneRowObject").removeClass("hidden").data("id", val.Id).attr("id", "u" + val.Id);
        console.log(val);
        $("#u" + val.Id).find("#userFirstName").append(val.FirstName);
        $("#u" + val.Id).find("#userLastName").append(val.LastName);
        $("#u" + val.Id).find("#userEmail").append(val.Email);
        $("#u" + val.Id).find("#userPhoneNumber").append(val.PhoneNumber);
        $("#u" + val.Id).find("#userRoles").append(val.Roles);

        $("#u" + val.Id + " a.userLink").each(function () {
            $(this).attr("href", $(this).attr("href") + val.Id);
        });
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}