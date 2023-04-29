$(document).ready(function () {
    //Make menu item active
    $('#navigation #navReviews' + $("#reviewTemplateId").val()).addClass('active').parents('#modules').addClass('active').addClass('open').find('.sub-menu').slideDown().find('#navReviewDropdown').addClass('active').addClass('open').find('.sub-menu').slideDown();

    var data = {
        id: $("#reviewTemplateId").val()
    };

    //Retrieves list of reviews
    $.ajax({
        url: '/spine-api/reviews',
        dataType: 'json',
        type: 'get',
        data: data,
        contentType: "application/json;",
        success: function (result) {
            var response = result.value;

            $("#reviewsTitle").html(response.name);
            $("#checkBeforeOnline").prop("checked", response.checkBeforeOnline).uniform();
            createList(response);

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

    //When the checkbox is clicked
    $('#checkBeforeOnline').on("click", function () {
        //Prevent change of checkbox
        $(this).prop('checked', !$(this).prop('checked'));

        $('#modelAlert').modal('show');

        if ($("#checkBeforeOnline").prop("checked") === true) {
            activateCheckbox = false;
        } else {
            activateCheckbox = true;
        }
    });

    //When someone click on save in the modal
    $("#save").click(function () {
        var data = {
            Id: $("#reviewTemplateId").val(),
            CheckBeforeOnline: activateCheckbox
        };

        //Sends json to webservices
        $.ajax({
            url: '/spine-api/reviewtemplate/checkbeforeonline',
            dataType: 'json',
            type: 'post',
            contentType: "application/json;",
            data: JSON.stringify(data),
            success: function (result) {
                toastr[result.messageType](result.message);

                if (result.success === "Valid") {
                    var active = $("#checkBeforeOnline").prop("checked", activateCheckbox);
                    $.uniform.update(active);
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

        $('#modelAlert').modal('hide');
    });
});

function createList(result) {
    if (result.reviews.length > 0) { $('#noData').remove(); }

    $.each(result.reviews, function (i, val) {
        $("#CloneRowObject").clone().appendTo("#reviews");
        $("#reviews").find("#CloneRowObject").removeClass("hidden").data("id", val.Id).attr("id", "r" + val.Id);

        if (val.ViewedByAdmin === false) {
            $("#r" + val.Id).find("#viewedByAdmin").addClass("icon-state-warning");
            $("#r" + val.Id).addClass("unread");
        } else {
            $("#r" + val.Id).addClass("read");
        }

        if (val.Anonymous === true) {
            $("#r" + val.Id).find("#reviewName").append("<i>Anonymous</i>");
        } else {
            $("#r" + val.Id).find("#reviewName").append(val.Name);
        }

        $("#r" + val.Id).find("#reviewEmail").append(val.Email);

        $("#r" + val.Id).find("#reviewRating").append(val.Rating + " / 100");

        var date = new Date(val.CreatedAt);
        $("#r" + val.Id).find("#reviewCreatedAt").append((date.getMonth() + 1) + '/' + date.getDate() + '/' + date.getFullYear());

        var status = "Inactive";
        if (val.Active === true)
        {
            status = "Active";
        }
        $("#r" + val.Id).find("#reviewStatus").append(status);

        $("#r" + val.Id + " a.reviewLink").each(function() {
            $(this).attr("href", $(this).attr("href") + val.Id);
        });
        //$("#r" + val.Id).find("a.reviewLink").attr("href", $("#r" + val.Id).find("a.reviewLink").attr("href") + val.Id);
    });

    $('#main-wrapper').fadeIn(500);

    return true;
}