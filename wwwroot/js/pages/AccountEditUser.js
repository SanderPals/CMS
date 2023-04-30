var concurrencyStamp,
    validation;

$(document).ready(function () {
    //Make menu item active
    $("#navigation #navUsers").addClass("active").parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    var data = {
        Id: $("#userId").val()
    };

    //Retrieves user informationy
    $.ajax({
        url: '/spine-api/user',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: "application/json;",
        success: function (result) {
            var response = result.value;
            concurrencyStamp = response.concurrencyStamp;
            fillUser(response);

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

    //Click event for deleting the page
    $("#deleteUser").on("click", function () {
        DeleteUser();
    });
});

function fillUser(result) {
    //Fill the fields with data that we'll get from our database
    $('#preamble option[value="' + result.preamble + '"]').attr('selected', true);

    $("#firstName").val(result.firstName);
    $("#lastName").val(result.lastName);

    if (result.email !== "" || $('#userId').val() === '') {
        $("#email").val(result.email);
        $("#emailHolder").removeClass('hidden');
    } else {
        $("#emailHolder").remove();
    }

    if (result.phoneNumber !== "" || $('#userId').val() === '') {
        $("#phoneNumber").val(result.phoneNumber);
        $("#phoneNumberHolder").removeClass('hidden');
    } else {
        $("#phoneNumberHolder").remove();
    }

    if (result.options) {
        $("#optionsHolder").removeClass('hidden');
    } else {
        $("#optionsHolder").remove();
    }

    if (result.roles.length !== 0) {
        $.each(result.roles, function (i, val) {
            var selected = false;
            if (val.Id === result.roleId) { selected = true; }
            $('#roles').append($('<option>', {
                value: val.Id,
                text: val.Name,
                selected: selected
            }));
        });

        $("#roleHolder").removeClass('hidden');
    } else {
        $("#roleHolder").remove();
    }

    if ($('#userId').val() !== '') {
        $('#displayFirstName').html(result.firstName);
        $('#firstName').off().keyup(function () {
            $('#displayFirstName').html($(this).val());
        });

        $('#displayLastName').html(result.lastName);
        $('#lastName').off().keyup(function () {
            $('#displayLastName').html($(this).val());
        });
    }

    $("#update").off().on("click", function () { UpdateUser(); });

    $('#main-wrapper').show('fade', 500, FormFadeCallback());
}

function UpdateUser() {
    //Validate the fields
    var valid = $("#form").valid();
    if (!valid) {
        validation.focusInvalid();
        return false;
    }

    var data = {
        Id: $("#userId").val(),
        Preamble: $("#preamble option:selected").val(),
        FirstName: $("#firstName").val(),
        LastName: $("#lastName").val(),
        Email: $("#email").val(),
        PhoneNumber: $("#phoneNumber").val(),
        ConcurrencyStamp: concurrencyStamp,
        Role: $("#roles option:selected").text()
    };

    $.ajax({
        url: '/spine-api/user',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);
            concurrencyStamp = response.concurrencyStamp;

            $("#preambleHolder").find(".help-block").addClass("hidden").html('');
            $("#firstNameHolder").find(".help-block").addClass("hidden").html('');
            $("#lastNameHolder").find(".help-block").addClass("hidden").html('');
            $("#emailHolder").find(".help-block").addClass("hidden").html('');
            $("#phoneNumberHolder").find(".help-block").addClass("hidden").html('');
            $("#roleHolder").find(".help-block").addClass("hidden").html('');

            if (response.redirect !== null && response.redirect !== undefined) {
                setTimeout(function () { location.href = response.redirect; }, 4500);
            }

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;
                if (response.messageType === "modelAlert") {
                    concurrencyStamp = response.concurrencyStamp;
                    $("#modelAlert").modal("show");

                    $("#preambleHolder").find(".help-block").removeClass("hidden").html(response.preamble);
                    $("#firstNameHolder").find(".help-block").removeClass("hidden").html(response.firstName);
                    $("#lastNameHolder").find(".help-block").removeClass("hidden").html(response.lastName);
                    $("#emailHolder").find(".help-block").removeClass("hidden").html(response.email);
                    $("#phoneNumberHolder").find(".help-block").removeClass("hidden").html(response.phoneNumber);
                    $("#roleHolder").find(".help-block").removeClass("hidden").html(response.role);
                } else {
                    toastr[response.messageType](response.message);
                }
            }

            return true;
        }
    });
}

function DeleteUser() {
    var data = {
        Id: $("#userId").val()
    };

    //Sends json to webservices.
    $.ajax({
        url: '/spine-api/user/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            setTimeout(function () { location.href = response.redirect; }, 4500);

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

function FormFadeCallback() {
    //Creating validation
    validation = $('#form').validate({
        rules: {
            firstName: {
                required: true,
                maxlength: 255
            },
            lastName: {
                required: true,
                maxlength: 255
            },
            email: {
                required: true,
                email: true,
                maxlength: 255
            }
        },
        messages: {
            firstName: {
                required: firstNameValidation,
                maxlength: maxLength255
            },
            lastName: {
                required: lastNameValidation,
                maxlength: maxLength255
            },
            email: {
                required: emailladdressValidation,
                email: emailValidValidation,
                maxlength: maxLength255
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
}