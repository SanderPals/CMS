var id,
    lineId,
    feeId,
    shippingMethodId,
    recovery,
    backUp,
    productId,
    validateBilling,
    validateShipping,
    validateShippingMethod,
    validateFee,
    validateProduct,
    tax;

$(document).ready(function () {
    //Make menu item active
    $('#navigation #navOrders').addClass('active').parents('li').addClass('active').addClass('open').find('.sub-menu').slideDown();

    //Get item id
    if ($('#id').val() !== '') {
        id = $('#id').val();
    } else {
        id = 0;
    }

    //Fix typing Select2 in modal
    $.fn.modal.Constructor.prototype.enforceFocus = $.noop;

    //Retrieves list of reviews
    $.ajax({
        url: '/spine-api/order',
        dataType: 'json',
        type: 'get',
        data: {
            id: id
        },
        contentType: 'application/json;',
        success: function (result) {
            backUp = result;
            fillItem(result);

            //if (id !== 0) {
            //    $('#title span').html(result.item.orderNumber);
            //}

            $('#main-wrapper').fadeIn(500);

            return true;
        },
        error: function (result) {
            if (result.status === 401 || result.status === 403) {
                toastr['warning'](unauthorized);
                setTimeout(function () { location.href = '/'; }, 4500);
            } else {
                var response = result.responseJSON.value;

                if (response.redirect !== null && typeof response.redirect !== 'undefined') {
                    location.href = response.redirect;
                }
            }

            return true;
        }
    });

    $('#rootwizard').bootstrapWizard({
        'tabClass': 'nav nav-tabs',
        onTabShow: function (tab, navigation, index) {
            $('#list .checkbox input, #shippings .checkbox input, #fees .checkbox input').each(function () {
                var checkbox = $(this).prop('checked', false);
                $.uniform.update(checkbox);
            });

            $('#deleteLines').fadeOut('500');
        }
    });

    $('form#shippingMethod').find('#taxable').uniform();

    $('#modalGeneral').on('shown.bs.modal', function (e) {
        fillGeneralModal();
    });

    $('#modalShipping').on('shown.bs.modal', function (e) {
        fillShippingModal();
    });

    $('#modalBilling').on('shown.bs.modal', function (e) {
        fillBillingModal();
    });

    setBillingValidation();
    setShippingValidation();
    setShippingMethodValidation();
    setFeeValidation();
    setProductValidation();

    $('#saveGeneral').on('click', function () { localSaveGeneral(); });
    $('#saveFee').on('click', function () { localSaveFee(feeId); });
    $('#saveProduct').on('click', function () { localSaveProduct(lineId, productId); });
    $('#saveShippingMethod').on('click', function () { localSaveShippingMethod(shippingMethodId); });
    $('#saveShipping').on('click', function () { localSaveShipping(); });
    $('#saveBilling').on('click', function () { localSaveBilling(); });
    $('#copyBilling').on('click', function () { copyBilling(); });

    $('select#status').on('change', function () {
        if ($(this).val() === 'credit') {
            $('input#refInvoiceNumber').parents('#refInvoiceNumberHolder').show();
        } else {
            $('input#refInvoiceNumber').val('').parents('#refInvoiceNumberHolder').hide();
        }
    });

    $('#openShippingMethodModal').on('click', function () {
        shippingMethodId = 0;
        fillShippingMethodModal(shippingMethodId);
        $('#modalShippingMethod').modal('show');
    });

    $('#openFeeModal').on('click', function () {
        feeId = 0;
        fillFeeModal(feeId);
        $('#modalFee').modal('show');
    });

    $('#openProductModal').on('click', function () {
        lineId = 0;
        fillProductModal(lineId);
        $('#modalProduct').modal('show');
    });

    $('#deleteLines').on('click', function () {
        createArrayList();
    });
});

createArrayList = function () {
    var selection = {};

    var shippings = [];
    $('#shippings .checkbox input').each(function () {
        if ($(this).is(':checked')) {
            shippings.push($(this).parents('tr').attr('data-id'));
        }
    });
    selection['shippings'] = shippings;

    var fees = [];
    $('#fees .checkbox input').each(function () {
        if ($(this).is(':checked')) {
            fees.push($(this).parents('tr').attr('data-id'));
        }
    });
    selection['fees'] = fees;

    var lines = [];
    $('#list .checkbox input').each(function () {
        if ($(this).is(':checked')) {
            lines.push($(this).parents('tr').attr('data-id'));
        }
    });
    selection['lines'] = lines;

    deleteSelection(selection);
};

deleteSelection = function (selection) {
    $.ajax({
        url: '/spine-api/delete-order-fees-shippings-lines',
        dataType: 'json',
        type: 'post',
        data: selection,
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            removeSelection(selection);
            updateTotalPrices();
            $('#deleteLines').fadeOut('500');

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
};

removeSelection = function (selection) {
    removeShippingListItems(selection['shippings']);
    removeFeeListItems(selection['fees']);
    removeListItems(selection['lines']);

    recovery = $.extend(true, {}, backUp);
};

removeShippingListItems = function (shippings) {
    $.each(shippings, function (i, val) {
        backUp.shippings = $.grep(backUp.shippings, function (e) {
            return e.id !== val;
        });
        $('#shippings tr[data-id="' + val + '"]').remove();
    });

    if (backUp.shippings.length === 0) { $('#shippings #noData').show(); }
};

removeFeeListItems = function (fees) {
    $.each(fees, function (i, val) {
        backUp.fees = $.grep(backUp.fees, function (e) {
            return e.id !== val;
        });
        $('#fees tr[data-id="' + val + '"]').remove();
    });

    if (backUp.fees.length === 0) { $('#fees #noData').show(); }
};

removeListItems = function (lines) {
    $.each(lines, function (i, val) {
        backUp.lines = $.grep(backUp.lines, function (e) {
            return e.id !== val;
        });
        $('#list tr[data-id="' + val + '"]').remove();
    });

    if (backUp.lines.length === 0) { $('#list #noData').show(); }
};

updateTotalPrices = function () {
    tax = parseFloat(0.00);

    var totalFeePrice = getTotalFeePrice(backUp.fees, backUp.digitsAfterDecimal);
    var totalLinePrice = getTotalLinePrice(backUp.lines, backUp.digitsAfterDecimal);
    var totalShippingPrice = getTotalShippingPrice(backUp.shippings, backUp.lines, backUp.fees, backUp.digitsAfterDecimal);
    var totalPrice = getTotalPrice(totalShippingPrice, totalLinePrice, totalFeePrice);

    $('#totalShipping').html(backUp.priceFormat.replace('price', totalShippingPrice));
    $('#totalFee').html(backUp.priceFormat.replace('price', totalFeePrice));
    $('#totalTax').html(backUp.priceFormat.replace('price', roundNumber(tax, backUp.digitsAfterDecimal)));
    $('#totalPrice').html(backUp.priceFormat.replace('price', totalPrice));
};

fillGeneralModal = function () {
    var form = 'form#general',
        item = backUp.item,
        date = getDate(new Date(item.createdDate));

    $(form).find('#date').datepicker({
        orientation: 'top auto',
        autoclose: true
    }).datepicker('update', date);

    $(form).find('#time').timepicker({ defaultTime: new Date(item.createdDate) });
    $(form).find('#status').val(item.status);
    if (item.status === 'completed' || item.status === 'credit') {
        $(form).find('#invoiceNumber').val(item.invoiceNumber);
        $(form).find('#invoiceNumberHolder').show();
    } else {
        $(form).find('#invoiceNumber').val('');
        $(form).find('#invoiceNumberHolder').hide();
    }
    if (item.status === 'credit') {
        $(form).find('#refInvoiceNumber').val(item.refInvoiceNumber);
        $(form).find('#refInvoiceNumberHolder').show();
    } else {
        $(form).find('#refInvoiceNumber').val('');
        $(form).find('#refInvoiceNumberHolder').hide();
    }
    $(form).find('#transactionId').val(item.transactionId);
    $(form).find('#note').val(item.note);
};

fillShippingModal = function () {
    var form = 'form#shipping',
        item = backUp.item;

    $(form).find('#company').val(item.shippingCompany);
    $(form).find('#firstName').val(item.shippingFirstName);
    $(form).find('#lastName').val(item.shippingLastName);
    $(form).find('#addressLine1').val(item.shippingAddressLine1);
    $(form).find('#addressLine2').val(item.shippingAddressLine2);
    $(form).find('#zipCode').val(item.shippingZipCode);
    $(form).find('#city').val(item.shippingCity);
    $(form).find('#country').val(item.billingCountry.toLowerCase());
    $(form).find('#state').val(item.shippingState);

    validateShipping.resetForm();
};

fillBillingModal = function () {
    var form = 'form#billing',
        item = backUp.item;

    $(form).find('#company').val(item.billingCompany);
    $(form).find('#vat').val(item.billingVatNumber);
    $(form).find('#firstName').val(item.billingFirstName);
    $(form).find('#lastName').val(item.billingLastName);
    $(form).find('#email').val(item.billingEmail);
    $(form).find('#phoneNumber').val(item.billingPhoneNumber);
    $(form).find('#addressLine1').val(item.billingAddressLine1);
    $(form).find('#addressLine2').val(item.billingAddressLine2);
    $(form).find('#zipCode').val(item.billingZipCode);
    $(form).find('#city').val(item.billingCity);
    $(form).find('#country').val(item.billingCountry.toLowerCase());
    $(form).find('#state').val(item.billingState);

    validateBilling.resetForm();
};

fillFeeModal = function (feeId) {
    var form = 'form#fee';

    if (feeId !== 0) {
        var item = backUp.fees.filter(id === feeId)[0];
        $(form).find('#name').val(item.name);
        $(form).find('#price').val(roundNumber(item.price, backUp.digitsAfterDecimal));
        $(form).find('#taxRate').val(roundNumber(item.taxRate, backUp.digitsAfterDecimal));
    } else {
        $(form).find('input[type=text]').val('');
    }

    validateFee.resetForm();
};

fillShippingMethodModal = function (shippingMethodId) {
    var form = 'form#shippingMethod';

    if (shippingMethodId !== 0) {
        var item = backUp.shippings.filter(x => x.id === shippingMethodId)[0];
        $(form).find('#name').val(item.name);
        $(form).find('#price').val(roundNumber(item.price, backUp.digitsAfterDecimal));

        var checkbox = $(form).find('#taxable').prop('checked', item.taxable);
        $.uniform.update(checkbox);
    } else {
        $(form).find('input[type=text]').val('');
        $(form).find('input[type=checkbox]').prop('checked', true);
    }

    validateShippingMethod.resetForm();
};

fillProductModal = function (lineId) {
    var form = 'form#product';

    if ($('select#product').hasClass('select2-hidden-accessible')) {
        $('select#product').select2('destroy');
    }

    if (lineId !== 0) {
        $('select#product').parents('.form-group').addClass('hidden');
        console.log(backUp);
        console.log(backUp.lines);
        var item = backUp.lines.filter(function (line) {
            return line.id == lineId;
        })[0];
        console.log(item);
        $(form).find('#name').val(item.name);
        $(form).find('#price').val(roundNumber(item.price, backUp.digitsAfterDecimal));
        $(form).find('#discount').val(roundNumber(item.discount, backUp.digitsAfterDecimal));
        $(form).find('#quantity').val(item.quantity);
        $(form).find('#tax').val(roundNumber(item.tax, backUp.digitsAfterDecimal));
        productId = item.productId;
    } else {
        $('select#product').select2({
            maximumSelectionLength: 1,
            allowClear: true,
            tabindex: false,
            placeholder: selectProduct,
            language: 'nl',
            ajax: {
                delay: 250,
                url: '/spine-api/products-by-name',
                dataType: 'json',
                data: function (params) {
                    var query = {
                        term: params.term
                    };

                    return query;
                },
                processResults: function (res) {
                    return res.value;
                }
            }
        }).on('select2:select', function (e) {
            productId = e.params.data.id;
        }).on('select2:unselect', function (e) {
            productId = 0;
        }).val('').trigger('change').parents('.form-group').removeClass('hidden');

        productId = 0;

        $(form).find('input[type=text]').val('');
    }

    validateProduct.resetForm();
};

setBillingValidation = function () {
    validateBilling = $('form#billing').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.tags-input').length) {
                error.appendTo(element.parent());
            } else if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        },
        rules: {
            company: {
                maxlength: 255
            },
            vat: {
                maxlength: 255
            },
            firstName: {
                required: true,
                maxlength: 255
            },
            lastName: {
                required: true,
                maxlength: 255
            },
            email: {
                email: true,
                maxlength: 255
            },
            phoneNumber: {
                phoneno: true,
                maxlength: 255
            },
            zipCode: {
                required: true,
                maxlength: 32
            },
            city: {
                required: true,
                maxlength: 100
            },
            country: {
                required: true,
                maxlength: 100
            },
            state: {
                maxlength: 100
            },
            addressLine1: {
                required: true,
                maxlength: 255
            },
            addressLine2: {
                maxlength: 255
            }
        },
        messages: {
            company: {
                maxlength: maxLength255
            },
            vat: {
                maxlength: maxLength255
            },
            firstName: {
                required: firstNameValidation,
                maxlength: maxLength255
            },
            lastName: {
                required: lastNameValidation,
                maxlength: maxLength255
            },
            email: {
                email: emailValidation,
                maxlength: maxLength255
            },
            phoneNumber: {
                maxlength: maxLength255
            },
            zipCode: {
                required: zipCodeValidation,
                maxlength: maxLength32
            },
            city: {
                required: cityValidation,
                maxlength: maxLength100
            },
            country: {
                required: countryValidation,
                maxlength: maxLength100
            },
            state: {
                maxlength: maxLength100
            },
            addressLine1: {
                required: addressValidation,
                maxlength: maxLength255
            },
            addressLine2: {
                maxlength: maxLength255
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
};

setShippingValidation = function () {
    validateShipping = $('form#shipping').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.tags-input').length) {
                error.appendTo(element.parent());
            } else if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        },
        rules: {
            company: {
                maxlength: 255
            },
            firstName: {
                required: true,
                maxlength: 255
            },
            lastName: {
                required: true,
                maxlength: 255
            },
            zipCode: {
                required: true,
                maxlength: 32
            },
            city: {
                required: true,
                maxlength: 100
            },
            country: {
                required: true,
                maxlength: 100
            },
            state: {
                maxlength: 100
            },
            addressLine1: {
                required: true,
                maxlength: 255
            },
            addressLine2: {
                maxlength: 255
            }
        },
        messages: {
            company: {
                maxlength: maxLength255
            },
            vat: {
                maxlength: maxLength255
            },
            firstName: {
                required: firstNameValidation,
                maxlength: maxLength255
            },
            lastName: {
                required: lastNameValidation,
                maxlength: maxLength255
            },
            zipCode: {
                required: zipCodeValidation,
                maxlength: maxLength32
            },
            city: {
                required: cityValidation,
                maxlength: maxLength100
            },
            country: {
                required: countryValidation,
                maxlength: maxLength100
            },
            state: {
                maxlength: maxLength100
            },
            addressLine1: {
                required: addressValidation,
                maxlength: maxLength255
            },
            addressLine2: {
                maxlength: maxLength255
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
};

setShippingMethodValidation = function () {
    validateShippingMethod = $('form#shippingMethod').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.tags-input').length) {
                error.appendTo(element.parent());
            } else if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        },
        rules: {
            name: {
                required: true,
                maxlength: 255
            },
            price: {
                required: true,
                decimal: true
            }
        },
        messages: {
            name: {
                required: nameValidation
            },
            price: {
                required: priceValidation
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
};

setFeeValidation = function () {
    validateFee = $('form#fee').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.tags-input').length) {
                error.appendTo(element.parent());
            } else if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        },
        rules: {
            name: {
                required: true,
                maxlength: 255
            },
            price: {
                required: true,
                decimal: true
            },
            taxRate: {
                required: true,
                decimal: true
            }
        },
        messages: {
            name: {
                required: nameValidation
            },
            price: {
                required: priceValidation
            },
            taxRate: {
                required: taxRateValidation
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
};

setProductValidation = function () {
    validateProduct = $('form#product').validate({
        ignore: [],
        errorPlacement: function (error, element) {
            if (element.parent('.tags-input').length) {
                error.appendTo(element.parent());
            } else if (element.parent('.input-group').length) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        },
        rules: {
            name: {
                required: true,
                maxlength: 255
            },
            price: {
                required: true,
                decimal: true
            },
            discount: {
                decimal: true
            },
            quantity: {
                required: true,
                digits: true
            },
            tax: {
                required: true,
                decimal: true
            }
        },
        messages: {
            name: {
                required: nameValidation,
                maxlength: maxLength255
            },
            price: {
                required: priceValidation
            },
            quantity: {
                required: quantityValidation,
                digits: digitsVal
            },
            tax: {
                required: taxRateValidation
            }
        },
        onkeyup: function (element) { $(element).valid(); }
    });
};

copyBilling = function () {
    var form = 'form#shipping',
        item = backUp.item;

    $(form).find('#company').val(item.billingCompany);
    $(form).find('#firstName').val(item.billingFirstName);
    $(form).find('#lastName').val(item.billingLastName);
    $(form).find('#addressLine1').val(item.billingAddressLine1);
    $(form).find('#addressLine2').val(item.billingAddressLine2);
    $(form).find('#zipCode').val(item.billingZipCode);
    $(form).find('#city').val(item.billingCity);
    if (item.billingCountry !== '') {
        $(form).find('#country').val(item.billingCountry);
    } else {
        $(form).find('#country option:first-of-type').prop('selected', 'selected');
    }
    $(form).find('#state').val(item.billingState);

    $(form).valid();
};

localSaveGeneral = function () {
    // Update recovery that can be used if server side returns 404
    recovery = $.extend(true, {}, backUp);

    var item = backUp.item,
        form = 'form#general';

    item.createdDate = $(form).find('#date').val() + ' ' + $(form).find('#time').val();
    item.status = $(form).find('#status').val();
    item.invoiceNumber = $(form).find('#invoiceNumber').val();
    item.refInvoiceNumber = $(form).find('#refInvoiceNumber').val();
    item.transactionId = $(form).find('#transactionId').val();
    item.note = $(form).find('#note').val();

    saveOrder(item);
};

localSaveShipping = function () {
    // Update recovery that can be used if server side returns 404
    recovery = $.extend(true, {}, backUp);

    var item = backUp.item,
        form = 'form#shipping';

    //Check fields
    var valid = $(form).valid();
    if (!valid) {
        validateShipping.focusInvalid();
        return false;
    }

    item.shippingCompany = $(form).find('#company').val();
    item.shippingFirstName = $(form).find('#firstName').val();
    item.shippingLastName = $(form).find('#lastName').val();
    item.shippingAddressLine1 = $(form).find('#addressLine1').val();
    item.shippingAddressLine2 = $(form).find('#addressLine2').val();
    item.shippingZipCode = $(form).find('#zipCode').val();
    item.shippingCity = $(form).find('#city').val();
    item.shippingCountry = $(form).find('#country').val();
    item.shippingState = $(form).find('#state').val();

    saveOrder(item);
};

localSaveBilling = function () {
    // Update recovery that can be used if server side returns 404
    recovery = $.extend(true, {}, backUp);

    var item = backUp.item,
        form = 'form#billing';

    //Check fields
    var valid = $(form).valid();
    if (!valid) {
        validateBilling.focusInvalid();
        return false;
    }

    item.billingCompany = $(form).find('#company').val();
    item.billingVatNumber = $(form).find('#vat').val();
    item.billingFirstName = $(form).find('#firstName').val();
    item.billingLastName = $(form).find('#lastName').val();
    item.billingEmail = $(form).find('#email').val();
    item.billingPhoneNumber = $(form).find('#phoneNumber').val();
    item.billingAddressLine1 = $(form).find('#addressLine1').val();
    item.billingAddressLine2 = $(form).find('#addressLine2').val();
    item.billingZipCode = $(form).find('#zipCode').val();
    item.billingCity = $(form).find('#city').val();
    item.billingCountry = $(form).find('#country').val();
    item.billingState = $(form).find('#state').val();

    saveOrder(item);
};

localSaveProduct = function (lineId, productId) {
    // Update recovery that can be used if server side returns 404
    recovery = $.extend(true, {}, backUp);

    var item = lineId !== 0
        ? backUp.lines.filter(function (line) {
                return line.id == lineId;
            })[0]
        : { id: 0, productId: productId },
        form = 'form#product';

    //Check fields
    var valid = $(form).valid();
    if (!valid) {
        validateProduct.focusInvalid();
        return false;
    }

    item.name = $(form).find('#name').val();
    item.price = $(form).find('#price').val();
    item.discount = $(form).find('#discount').val() === "" ? 0.00 : $(form).find('#discount').val();
    item.quantity = $(form).find('#quantity').val();
    item.tax = $(form).find('#tax').val();
    item.total = item.price * item.quantity;

    saveProduct(item, lineId, productId);
};

localSaveShippingMethod = function (shippingMethodId) {
    // Update recovery that can be used if server side returns 404
    recovery = $.extend(true, {}, backUp);

    var item = shippingMethodId !== 0 ? backUp.shippings.filter(x => x.id === shippingMethodId)[0] : { id: 0, orderId: id },
        form = 'form#shippingMethod';

    //Check fields
    var valid = $(form).valid();
    if (!valid) {
        validateShippingMethod.focusInvalid();
        return false;
    }

    item.name = $(form).find('#name').val();
    item.price = $(form).find('#price').val();
    item.taxable = $(form).find('#taxable').is(':checked');

    saveShippingMethod(item, shippingMethodId);
};

localSaveFee = function (feeId) {
    // Update recovery that can be used if server side returns 404
    recovery = $.extend(true, {}, backUp);

    var item = feeId !== 0 ? backUp.fees.filter(x => x.id === feeId)[0] : { id: 0, orderId: id },
        form = 'form#fee';

    //Check fields
    var valid = $(form).valid();
    if (!valid) {
        validateFee.focusInvalid();
        return false;
    }

    item.name = $(form).find('#name').val();
    item.price = $(form).find('#price').val();
    item.taxRate = $(form).find('#taxRate').val();

    saveFee(item, feeId);
};

saveOrder = function (item) {
    $.ajax({
        url: '/spine-api/order',
        dataType: 'json',
        type: 'post',
        contentType: 'application/json;',
        data: JSON.stringify(item),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);
            backUp.item = response.item;

            recovery = $.extend(true, {}, backUp);

            fillGeneral(item);
            fillBilling(item);
            fillShipping(item);

            $('#modalBilling, #modalShipping, #modalGeneral').modal('hide');

            return true;
        },
        error: function (result) {
            backUp = $.extend(true, {}, recovery);

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
};

saveShippingMethod = function (item, shippingMethodId) {
    $.ajax({
        url: '/spine-api/order-shipping',
        dataType: 'json',
        type: 'post',
        contentType: 'application/json;',
        data: JSON.stringify(item),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            if (shippingMethodId !== 0) {
                backUp.shippings.filter(x => x.id === shippingMethodId)[0] = item;
            } else {
                backUp.shippings.push(response.item);
            }

            recovery = $.extend(true, {}, backUp);

            $('#shippings').empty();
            createShippingList(backUp);
            updateTotalPrices();

            $('#modalShippingMethod').modal('hide');

            return true;
        },
        error: function (result) {
            backUp = $.extend(true, {}, recovery);

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
};

saveFee = function (item, feeId) {
    $.ajax({
        url: '/spine-api/order-fee',
        dataType: 'json',
        type: 'post',
        contentType: 'application/json;',
        data: JSON.stringify(item),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            if (feeId !== 0) {
                backUp.fees.filter(x => x.id === feeId)[0] = item;
            } else {
                backUp.fees.push(response.item);
            }

            recovery = $.extend(true, {}, backUp);

            $('#fees').empty();
            createFeeList(backUp);
            updateTotalPrices();

            $('#modalFee').modal('hide');

            return true;
        },
        error: function (result) {
            backUp = $.extend(true, {}, recovery);

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
};

saveProduct = function (item, lineId, productId) {
    $.ajax({
        url: '/spine-api/order-line',
        dataType: 'json',
        type: 'post',
        contentType: 'application/json;',
        data: JSON.stringify({
            id: lineId,
            productId: productId,
            orderId: id,
            name: item.name,
            price: item.price,
            discount: item.discount,
            quantity: item.quantity,
            taxRate: item.tax
        }),
        success: function (result) {
            var response = result.value;
            toastr[response.messageType](response.message);

            if (lineId !== 0) {
                backUp.lines.filter(function (line) {
                    return line.id == lineId;
                })[0] = item;
            } else {
                backUp.lines.push({
                    id: response.item.id,
                    productId: response.item.productId,
                    name: response.item.name,
                    discount: response.item.discount,
                    price: response.item.price,
                    product: response.item.productId !== 0 ? '/products/edit/' + response.item.productId : '',
                    quantity: response.item.quantity,
                    refunds: [],
                    tax: response.item.taxRate,
                    total: response.item.price * response.item.quantity
                });
            }

            recovery = $.extend(true, {}, backUp);

            $('#list').empty();
            createList(backUp);
            updateTotalPrices();

            $('#modalProduct').modal('hide');

            return true;
        },
        error: function (result) {
            backUp = $.extend(true, {}, recovery);

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
};

fillItem = function (result) {
    var item = result.item;

    fillGeneral(item);
    fillBilling(item);
    fillShipping(item);

    $('#totalShipping').html(result.priceFormat.replace('price', roundNumber(result.totalShipping, result.digitsAfterDecimal)));
    $('#totalFee').html(result.priceFormat.replace('price', roundNumber(result.totalFee, result.digitsAfterDecimal)));
    $('#totalTax').html(result.priceFormat.replace('price', roundNumber(result.totalTax, result.digitsAfterDecimal)));
    $('#totalPrice').html(result.priceFormat.replace('price', roundNumber(result.total, result.digitsAfterDecimal)));

    createList(result);
    createShippingList(result);
    createFeeList(result);

    addCustomValidations(result);
};

addCustomValidations = function (result) {
    jQuery.validator.addMethod('phoneno', function (e, a) {
        return e = e.replace(/\s+/g, ''), this.optional(a) || e.length > 9 && e.match(/^((\+[1-9]{1,4}[ \-]*)|(\([0-9]{2,3}\)[ \-]*)|([0-9]{2,4})[ \-]*)*?[0-9]{3,4}?[ \-]*[0-9]{3,4}?$/);
    }, phoneNumberValidation);

    jQuery.validator.addMethod('decimal', function (e, a) {
        return this.optional(a) || e.match(new RegExp('^\\d{0,14}(?:\\.\\d{0,' + result.digitsAfterDecimal + '})?$'));
    }, digitsValidation + result.digitsAfterDecimal + digitsValidationEnd);
};

fillGeneral = function (item) {
    //Set date and time
    var date = getDate(new Date(item.createdDate));
    $('#date').html(date);

    item.transactionId !== '' ? $('#transactionId').html(item.transactionId).parent('#transactionIdHolder').show() : $('#transactionIdHolder').hide();
    item.note !== '' ? $('#note').html(item.note.replace(/(?:\r\n|\r|\n)/g, '<br />')).parent('#noteHolder').show() : $('#noteHolder').hide();
    item.invoiceNumber !== '' && item.invoiceNumber !== null ? $('#invoiceNumber').html(item.invoiceNumber).parent('#invoiceNumberHolder').show() : $('#invoiceNumberHolder').hide();
    item.refInvoiceNumber !== '' && item.refInvoiceNumber !== null ? $('#refInvoiceNumber').html(item.refInvoiceNumber).parent('#refInvoiceNumberHolder').show() : $('#refInvoiceNumberHolder').hide();

    //Set status
    switch (item.status) {
        case 'concept':
            $('#status').html(conceptVal);
            $('#downloadInvoice').hide();
            break;
        case 'pending':
            $('#status').html(pendingVal);
            $('#downloadInvoice').hide();
            break;
        case 'processing':
            $('#status').html(processingVal);
            $('#downloadInvoice').hide();
            break;
        case 'hold':
            $('#status').html(onHoldVal);
            $('#downloadInvoice').hide();
            break;
        case 'completed':
            $('#status').html(completedVal);
            $('#downloadInvoice').show();
            break;
        case 'cancelled':
            $('#status').html(cancelledVal);
            $('#downloadInvoice').hide();
            break;
        case 'credit':
            $('#status').html(creditVal);
            $('#downloadInvoice').show();
            break;
        case 'failed':
            $('#status').html(failedVal);
            $('#downloadInvoice').hide();
            break;
    }
};

fillBilling = function (item) {
    var billing = item.billingCompany !== '' ? item.billingCompany + '<br />' : '';

    var name = [];
    if (item.billingFirstName !== '') { name.push(item.billingFirstName); }
    if (item.billingLastName !== '') { name.push(item.billingLastName); }

    billing += name.length !== 0 ? name.join(' ') + '<br />' : '';
    billing += item.billingAddressLine1 !== '' ? item.billingAddressLine1 + '<br />' : '';
    billing += item.billingAddressLine2 !== '' ? item.billingAddressLine2 + '<br />' : '';

    var address = [];
    if (item.billingZipCode !== '') { address.push(item.billingZipCode); }
    if (item.billingCity !== '') { address.push(item.billingCity); }
    if (item.billingCountry !== '') { '<br />' + address.push('Nederland'); }
    if (item.billingState !== '') { '<br />' + address.push(item.billingState); }

    billing += address.length !== 0 ? address.join(' ') + '<br />' : '';

    if (billing !== '') { $('p#billing').html(billing); }

    if (billing === '' && item.billingEmail === '' && item.billingPhoneNumber === '' && item.billingVatNumber === '') {
        $('#noBillingInfo').show();
    } else {
        $('#noBillingInfo').hide();
    }

    item.billingEmail !== '' ? $('#email').html(item.billingEmail).parent('#emailHolder').show() : $('#emailHolder').hide();
    item.billingPhoneNumber !== '' ? $('#phoneNumber').html(item.billingPhoneNumber).parent('#phoneNumberHolder').show() : $('#phoneNumberHolder').hide();
    item.billingVatNumber !== '' ? $('#vat').html(item.billingVatNumber).parent('#vatHolder').show() : $('#vatHolder').hide();
};

fillShipping = function (item) {
    var shipping = item.shippingCompany !== '' ? item.shippingCompany + '<br />' : '';

    var name = [];
    if (item.shippingFirstName !== '') { name.push(item.shippingFirstName); }
    if (item.shippingLastName !== '') { name.push(item.shippingLastName); }

    shipping += name.length !== 0 ? name.join(' ') + '<br />' : '';
    shipping += item.shippingAddressLine1 !== '' ? item.shippingAddressLine1 + '<br />' : '';
    shipping += item.shippingAddressLine2 !== '' ? item.shippingAddressLine2 + '<br />' : '';

    var address = [];
    if (item.shippingZipCode !== '') { address.push(item.shippingZipCode); }
    if (item.shippingCity !== '') { address.push(item.shippingCity); }
    if (item.shippingCountry !== '') { '<br />' + address.push('Nederland'); }
    if (item.shippingState !== '') { '<br />' + address.push(item.shippingState); }

    shipping += (address.length !== 0) ? address.join(' ') + '<br />' : '';

    if (shipping !== '') {
        $('p#shipping').html(shipping);
        $('#noShippingInfo').hide();
    } else {
        $('#noShippingInfo').show();
    }
};

createList = function (result) {
    if (result.lines.length > 0) { $('#list #noData').hide(); }

    var list = '#list';
    $.each(result.lines, function (i, val) {
        var item = '#list #item' + val.id;

        $('#cloneRowObject').clone().appendTo(list);
        $(list).find('#cloneRowObject').attr('data-id', val.id).attr('id', 'item' + val.id);
        $(item).find('#item').append(val.product !== '' ? '<a href="' + val.product + '" target="_blank">' + val.name + '</a>' : val.name);
        $(item).find('#price').append(val.discount > 0 ? '<span style="text-decoration: line-through;">' + result.priceFormat.replace('price', roundNumber(parseFloat(val.price) + parseFloat(val.discount), result.digitsAfterDecimal)) + '</span ><br />' + result.priceFormat.replace('price', roundNumber(val.price, result.digitsAfterDecimal)) : result.priceFormat.replace('price', roundNumber(val.price, result.digitsAfterDecimal)));
        $(item).find('#quantity').append(val.quantity);
        $(item).find('#tax').append(roundNumber(val.tax, result.digitsAfterDecimal) + '%');
        $(item).find('#total').append(roundNumber(val.total, result.digitsAfterDecimal));

        $(item).find('.checkbox input').uniform().on('change', function () {
            var show = false;
            $('#list .checkbox input, #shippings .checkbox input, #fees .checkbox input').each(function () {
                if ($(this).is(':checked')) {
                    show = true;
                }
            });

            show ? $('#deleteLines').fadeIn('500') : $('#deleteLines').fadeOut('500');
        });
    });

    $(list).find('#edit').off().on('click', function () {
        lineId = $(this).parents('tr').attr('data-id');

        fillProductModal(lineId);
        $('#modalProduct').modal('show');
    });

    return true;
};

createFeeList = function (result) {
    if (result.fees.length > 0) { $('#fees #noData').hide(); }

    var list = '#fees';
    $.each(result.fees, function (i, val) {
        var item = '#fees #item' + val.id;

        $('#cloneObject').clone().appendTo(list);
        $(list).find('#cloneObject').removeClass('hidden').attr('data-id', val.id).attr('id', 'item' + val.id);

        $(item).find('#item').append(val.name);
        $(item).find('#price').append(result.priceFormat.replace('price', roundNumber(val.price, result.digitsAfterDecimal)));
        $(item).find('#tax').append(roundNumber(val.taxRate, result.digitsAfterDecimal) + '%');

        $(item).find('.checkbox input').uniform().on('change', function () {
            var show = false;
            $('#list .checkbox input, #shippings .checkbox input, #fees .checkbox input').each(function () {
                if ($(this).is(':checked')) {
                    show = true;
                }
            });

            show ? $('#deleteLines').fadeIn('500') : $('#deleteLines').fadeOut('500');
        });
    });

    $(list).find('#edit').off().on('click', function () {
        feeId = $(this).parents('tr').attr('data-id');

        fillFeeModal(feeId);
        $('#modalFee').modal('show');
    });

    return true;
};

createShippingList = function (result) {
    if (result.shippings.length > 0) { $('#shippings #noData').hide(); }

    var list = '#shippings';
    $.each(result.shippings, function (i, val) {
        var item = '#shippings #item' + val.id;

        $('#cloneObject').clone().appendTo(list);
        $(list).find('#cloneObject').removeClass('hidden').attr('data-id', val.id).attr('id', 'item' + val.id);

        $(item).find('#item').append(val.name);
        $(item).find('#price').append(result.priceFormat.replace('price', roundNumber(val.price, result.digitsAfterDecimal)));
        $(item).find('#tax').append(val.taxable ? 'Yes' : 'No');

        $(item).find('.checkbox input').uniform().on('change', function () {
            var show = false;
            $('#list .checkbox input, #shippings .checkbox input, #fees .checkbox input').each(function () {
                if ($(this).is(':checked')) {
                    show = true;
                }
            });

            show ? $('#deleteLines').fadeIn('500') : $('#deleteLines').fadeOut('500');
        });
    });

    $(list).find('#edit').off().on('click', function () {
        shippingMethodId = $(this).parents('tr').attr('data-id');

        fillShippingMethodModal(shippingMethodId);
        $('#modalShippingMethod').modal('show');
    });

    return true;
};

getDate = function (datetime) {
    var fullDate = new Date(datetime);
    var twoDigitMonth = fullDate.getMonth() + 1;
    var twoDigitDate = fullDate.getDate();
    return currentDate = twoDigitMonth + '/' + twoDigitDate + '/' + fullDate.getFullYear();
};

roundNumber = function (num, scale) {
    var s = +(Math.round(num + "e+" + scale).toFixed(scale) + "e-" + scale);
    return s.toFixed(scale);
};

getTotalFeePrice = function (fees, digitsAfterDecimal) {
    var total = 0.00;

    $.each(fees, function (i, val) {
        var productTax = (parseFloat(roundNumber(val.price, digitsAfterDecimal)) / (100 + val.taxRate)) * val.taxRate;
        tax = parseFloat(tax) + parseFloat(productTax);
        total = parseFloat(roundNumber(total, digitsAfterDecimal)) + parseFloat(roundNumber(val.price, digitsAfterDecimal));
    });

    return roundNumber(total, digitsAfterDecimal);
};

getTotalLinePrice = function (lines, digitsAfterDecimal) {
    var total = 0.00;

    $.each(lines, function (i, val) {
        //var productTax = (((parseFloat(roundNumber(val.price, digitsAfterDecimal)) / 100) * val.tax) * val.quantity);
        //tax = parseFloat(roundNumber(tax, digitsAfterDecimal)) + parseFloat(roundNumber(productTax, digitsAfterDecimal));

        var productTax = ((parseFloat(roundNumber(val.price, digitsAfterDecimal)) / (100 + parseFloat(val.tax))) * parseFloat(val.tax));
        var totalProductTax = parseFloat((productTax * val.quantity));
        tax = parseFloat(tax) + parseFloat(totalProductTax);

        var price = (parseFloat(roundNumber(val.price, digitsAfterDecimal)) * val.quantity);
        total = parseFloat(roundNumber(total, digitsAfterDecimal)) + parseFloat(roundNumber(price, digitsAfterDecimal));
    });

    return roundNumber(total, digitsAfterDecimal);
};

getTotalShippingPrice = function (shippings, lines, fees, digitsAfterDecimal) {
    var total = 0.00;
    var prices = {};
    $.each(lines, function (i, val) {
        var price = (parseFloat(roundNumber(val.price, digitsAfterDecimal)) * val.quantity);
        total = parseFloat(total) + parseFloat(roundNumber(price, digitsAfterDecimal));
        if (val.tax in prices) {
            prices[val.tax] = prices[val.tax] + parseFloat(roundNumber(price, digitsAfterDecimal));
        } else {
            prices[val.tax] = roundNumber(price, digitsAfterDecimal);
        }
    });

    $.each(fees, function (i, val) {
        total = parseFloat(total) + parseFloat(roundNumber(val.price, digitsAfterDecimal));
        if (val.taxRate in prices) {
            prices[val.taxRate] = prices[val.taxRate] + roundNumber(val.price, digitsAfterDecimal);
        } else {
            prices[val.taxRate] = roundNumber(val.price, digitsAfterDecimal);
        }
    });

    var percentages = {};
    for (var key in prices) {
        percentages[key] = (100 / total) * parseFloat(prices[key]);
    }

    total = 0.00;
    var notTaxable = 0;
    $.each(shippings, function (i, val) {
        val.taxable ? total = parseFloat(roundNumber(total, digitsAfterDecimal)) + parseFloat(roundNumber(val.price, digitsAfterDecimal)) : notTaxable = parseFloat(roundNumber(notTaxable, digitsAfterDecimal)) + parseFloat(roundNumber(val.price, digitsAfterDecimal));
    });

    var totalShippingPrice = 0.00;
    for (var pKey in percentages) {
        var price = ((total / 100) * percentages[pKey]);
        tax = parseFloat(tax) + parseFloat(((parseFloat(price) / (100 + parseFloat(pKey))) * parseFloat(pKey)));
        totalShippingPrice = totalShippingPrice + price;
    }

    return roundNumber(totalShippingPrice + notTaxable, digitsAfterDecimal);
};

getTotalPrice = function (totalShipping, totalLine, totalFee) {
    return roundNumber((parseFloat(totalFee) + parseFloat(totalShipping)) + parseFloat(totalLine), backUp.digitsAfterDecimal);
};