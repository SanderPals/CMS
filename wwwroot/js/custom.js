$(document).ready(function() {
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": false,
        "progressBar": true,
        "positionClass": "toast-top-left",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };
});

function createFriendlyUrl(i, a, p) {
    var $Divider = "-";
    var $Url = $(i).val()
                   .toLowerCase()
                   .replace(/^\s+|\s+$/g, $Divider)
                   .replace(/[_|\s]+/g, $Divider)
                   .replace(/[^a-zA-Zàáâäãå????èéêë??ìíîï??òóôöõøùúûü??ÿı??ñç?šÀÁÂÄÃÅ?????ÈÉÊËÌÍÎÏ???ÒÓÔÖÕØÙÚÛÜ??Ÿİ??ÑßÇŒÆ?Š?ğ\u0400-\u04FF0-9-]+/g, "")
                   .replace(/[-]+/g, $Divider);

    $(i).val($Url);
}

function pathFromEnd(url, pos) {
    return url.match(new RegExp("^(?:.*?:\\/\\/)?[^\\/]*(?:\\/([^\\/?]*))*?(?:\\/[^\\/?]*){" + pos + "}\\/?(?:\\?.*)?$"))[1];
}