function dateTimeEditor(container, options) {
    $('<input name="' + options.field + '" />')
        .appendTo(container)
        .kendoDateTimePicker({ format: "yyyy-MM-dd HH:mm", timeFormat: "HH:mm" });
}

function timeEditor(container, options) {
    $('<input name="' + options.field + '" />')
        .appendTo(container)
        .kendoTimePicker({ format: "HH:mm", parseFormats: ["HH:mm"] });
}

function textAreaEditor(container, options) {
    $('<textarea style="width:300px;height:80px" data-bind="value: ' + options.field + '"></textarea>')
        .appendTo(container);
}

var subscriberNotSendedMoInDaysDataSource = [{
    "Id": 0,
    "Value": "همه اعضا",
}, {
    "Id": 1,
    "Value": "1 روز",
}, {
    "Id": 2,
    "Value": "2 روز",
}, {
    "Id": 3,
    "Value": "3 روز",
}, {
    "Id": 7,
    "Value": "1 هفته",
}, {
    "Id": 14,
    "Value": "2 هفته",
}, {
    "Id": 30,
    "Value": "1 ماه",
}];

function subscriberNotSendedMoInDaysDropDownEditor(container, options) {
    $('<input data-text-field="Value" data-value-field="Id" data-bind="value:' + options.field + '"/>')
        .appendTo(container)
       .kendoDropDownList({
           dataSource: subscriberNotSendedMoInDaysDataSource,
           dataTextField: "Value",
           dataValueField: "Id"
       });
}

function getSubscriberNotSendedMoInDaysValue(Id) {
    for (var idx = 0, length = subscriberNotSendedMoInDaysDataSource.length; idx < length; idx++) {
        if (subscriberNotSendedMoInDaysDataSource[idx].Id === Id) {
            return subscriberNotSendedMoInDaysDataSource[idx].Value;
        }
    }
}

function getProcessStatus(Status) {
    if (Status === 1)
        return "در صف";
    else if (Status === 2)
        return "در حال ارسال";
    else if (Status === 3)
        return "موفق";
    else if (Status === 4)
        return "خطا";
    else if (Status === 5)
        return "پایان یافته";
    else if (Status === 6)
        return "متوقف شده";
    else
        return "نامشخص";
}