function dateTimeEditor(container, options) {
    $('<input name="' + options.field + '" />')
        .appendTo(container)
        .kendoDateTimePicker({ format: "yyyy-MM-dd HH:mm", timeFormat: "HH:mm" });
}

function textAreaEditor(container, options) {
    $('<textarea style="width:200px;height:80px" data-bind="value: ' + options.field + '"></textarea>')
        .appendTo(container);
}