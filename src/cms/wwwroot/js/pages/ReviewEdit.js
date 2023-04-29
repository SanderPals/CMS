var activateCheckbox = false;

$(document).ready(function () {
    //Make menu item active
    $("#navigation #navReviews").addClass("active");

    var data = {
        Id: $("#reviewId").val()
    };

    //Retrieves review
    $.ajax({
        url: '/spine-api/review/get',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: "application/json;",
        success: function (result) {
            if (result.success === "Valid") {
                fillReview(result);
            } else {
                toastr[result.messageType](result.message);
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

    //When the checkbox is clicked
    $('#active').on("click", function () {
        //Prevent change of checkbox
        $(this).prop('checked', !$(this).prop('checked'));

        $('#modelAlert').modal('show');

        if ($("#active").prop("checked") === true) {
            activateCheckbox = false;
        } else {
            activateCheckbox = true;
        }
    });

    //When someone click on save in the modal to change review status
    $("#save").click(function () {
        updateReviewActive();
    });

    //Click event for deleting the page
    $("#deleteReview").on("click", function () {
        deleteReview();
    });

    //Click event for deleting the page
    $("#markReviewAsUnread").on("click", function () {
        markReviewAsUnread();
    });
});

function markReviewAsUnread() {
    var data = {
        Id: $("#reviewId").val(),
        ViewedByAdmin: false
    };

    //Sends json to webservices.
    $.ajax({
        url: '/spine-api/review/viewedbyadmin',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

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

function deleteReview() {
    var data = {
        Id: $("#reviewId").val()
    };

    //Sends json to webservices.
    $.ajax({
        url: '/spine-api/review/delete',
        dataType: 'json',
        type: 'post',
        data: data,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            location.href = '/reviews';

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

function updateReviewActive() {
    var data = {
        Id: $("#reviewId").val(),
        Active: activateCheckbox
    };

    //Sends json to webservices.
    $.ajax({
        url: '/spine-api/review/active',
        dataType: 'json',
        type: 'post',
        contentType: "application/json;",
        data: JSON.stringify(data),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            var active = $("#active").prop("checked", activateCheckbox);
            $.uniform.update(active);

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

function fillReview(result) {
    //Fill the fields with data that we'll get from our database
    if (result.name !== "") {
        $("#name").html(result.name);
    } else {
        $("#nameHolder").hide();
    }

    if (result.email !== "")
    {
        $("#email").html(result.email);
    } else {
        $("#emailHolder").hide();
    }

    var date = new Date(result.createdAt);
    $("#createdAt").html((date.getMonth() + 1) + '/' + date.getDate() + '/' + date.getFullYear());

    $("#rating").html(result.rating + " / 100");

    var anonymous = "No";
    if (result.anonymous === true) {
        anonymous = "Yes";
    }

    $("#anonymous").html(anonymous);

    if (result.text !== "") {
        $("#text").html(result.text);
    } else {
        $("#textHolder").hide();
    }

    $("#active").prop("checked", result.active);
    $("#active").uniform();

    $.each(result.data, function (i, val) {
        $("#cloneObject").clone().appendTo("#reviewTemplateFields");
        $("#reviewTemplateFields").find("#cloneObject").removeClass("hidden").data("id", val.Id).data("type", val.Type).attr("id", "rtf" + val.Id);
        
        $("#rtf" + val.Id + " #title").html(val.Heading);

        if (val.Type.toLowerCase() === "checkbox") {
            var value = "No";
            if (val.Text.toLowerCase() === "true") {
                value = "Yes";
            }
            $("#rtf" + val.Id + " #value").html(value);
        } else if (val.Type.toLowerCase() === "rating") {
            $("#rtf" + val.Id + " #value").html(val.Text + " / 100");
        } else {
            $("#rtf" + val.Id + " #value").html(val.Text);
        }
    });
}