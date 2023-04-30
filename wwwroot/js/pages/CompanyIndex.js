$(function () {
    //Make menu item active
    $("#navigation #navInformation").addClass("active").parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    //enable / disable
    $('#enable').click(function() {
        $('#user .editable').editable('toggleDisabled');
    });    

    $.fn.editableform.buttons = '<button type="submit" class="btn btn-primary editable-submit"><i class="glyphicon glyphicon-ok"></i></button><button type="button" class="btn btn-alert editable-cancel"><i class="glyphicon glyphicon-remove"></i></button>';
    
    //editables 
    $('#company').editable({
        inputclass: 'form-control',
        url: function (params) {
            return $.ajax({
                url: '/spine-api/company/update',
                dataType: 'json',
                type: 'post',
                data: {
                    Id: params.pk,
                    Company: params.value,
                    Vat: $('#vat').editable('getValue')['vat'],
                    Coc: $('#coc').editable('getValue')['coc'],
                    Email: $('#email').editable('getValue')['email'],
                    PhoneNumber: $('#phonenumber').editable('getValue')['phone number']
                },
                success: function (result, newValue) {
                    toastr[result.value.messageType](result.value.message);
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
    $('#vat').editable({
       inputclass: 'form-control',
        url: function (params) {
            return $.ajax({
                url: '/spine-api/company/update',
                dataType: 'json',
                type: 'post',
                data: {
                    Id: params.pk,
                    Company: $('#company').editable('getValue')['company'],
                    Vat: params.value,
                    Coc: $('#coc').editable('getValue')['coc'],
                    Email: $('#email').editable('getValue')['email'],
                    PhoneNumber: $('#phonenumber').editable('getValue')['phone number']
                },
                success: function (result, newValue) {
                    toastr[result.value.messageType](result.value.message);
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
    $('#coc').editable({
        inputclass: 'form-control',
        url: function (params) {
            return $.ajax({
                url: '/spine-api/company/update',
                dataType: 'json',
                type: 'post',
                data: {
                    Id: params.pk,
                    Company: $('#company').editable('getValue')['company'],
                    Vat: $('#vat').editable('getValue')['vat'],
                    Coc: params.value,
                    Email: $('#email').editable('getValue')['email'],
                    PhoneNumber: $('#phonenumber').editable('getValue')['phone number']
                },
                success: function (result, newValue) {
                    toastr[result.value.messageType](result.value.message);
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
    $('#email').editable({
        inputclass: 'form-control',
        url: function (params) {
            return $.ajax({
                url: '/spine-api/company/update',
                dataType: 'json',
                type: 'post',
                data: {
                    Id: params.pk,
                    Company: $('#company').editable('getValue')['company'],
                    Vat: $('#vat').editable('getValue')['vat'],
                    Coc: $('#coc').editable('getValue')['coc'],
                    Email: params.value,
                    PhoneNumber: $('#phonenumber').editable('getValue')['phone number']
                },
                success: function (result, newValue) {
                    toastr[result.value.messageType](result.value.message);
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
    $('#phonenumber').editable({
        inputclass: 'form-control',
        url: function (params) {
            return $.ajax({
                url: '/spine-api/company/update',
                dataType: 'json',
                type: 'post',
                data: {
                    Id: params.pk,
                    Company: $('#company').editable('getValue')['company'],
                    Vat: $('#vat').editable('getValue')['vat'],
                    Coc: $('#coc').editable('getValue')['coc'],
                    Email: $('#email').editable('getValue')['email'],
                    PhoneNumber: params.value
                },
                success: function (result, newValue) {
                    toastr[result.value.messageType](result.value.message);
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

    $('#main-wrapper').fadeIn(500);
});