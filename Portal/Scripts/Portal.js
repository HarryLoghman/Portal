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

function textEditor(container, options) {
    $('<input style="width:150px;" data-bind="value: ' + options.field + '"/>')
        .appendTo(container);
}

function multiselectEditor(container, options) {
    $('<select style="width"150px;" data-bind="value:' + options.field + '"/>')
        .appendTo(container)
        .kendoMultiSelect({
            dataSource: options.values,
            suggest: true,
            valuePrimitive: true,
            columnName:"roles"
        });
}

function passwordEditor(container, options) {
    $('<input data-text-field="' + options.field + '" ' +
                //'class="k-input k-textbox" ' +
        'type="password" ' +
        'data-value-field="' + options.field + '" ' +
        'data-bind="value:' + options.field + '"/>')
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

function portalTooltip(gridName, columnName, columnIndex) {
    $("#"+gridName).kendoTooltip({
        show: function (e) {
            if (this.content.text().length > 30) {
                this.content.parent().css("visibility", "visible");
            }
        },
        hide: function (e) {
            this.content.parent().css("visibility", "hidden");
        },
        filter: "td:nth-child("+ columnIndex +")", //this filter selects the column cell number
        position: "center",
        content: function (e) {
            var dataItem = $("#"+gridName).data("kendoGrid").dataItem(e.target.closest("tr"));
            var content;
            if (columnName in dataItem)
                content = dataItem[columnName];
            return content;
        }
    }).data("kendoTooltip");
}