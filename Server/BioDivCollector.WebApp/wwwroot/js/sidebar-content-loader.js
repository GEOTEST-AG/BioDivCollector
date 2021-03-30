function loadBeobachtungsURL(url) {
    $('#BeobachtungContent').html('');
    goToInsideUrl = url;
    $hidden = $('#BeobachtungUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#BeobachtungContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#loading-spinner");
    $hidden.attr("ic-on-complete", "loadedBeobachtung()");
    $hidden.trigger("click");
}

function loadedBeobachtung() {
}

$('.interLink').each(
    function () {
        var $this = $(this);
        if ($this.attr("ic-get-from") == null) {
            $this.attr("ic-get-from", $this.attr("href"));
            $this.attr("ic-target", "#BeobachtungContent");
            $this.attr("ic-push-url", "false");
            $this.attr("ic-indicator", "#loading-spinner");
            $this.removeAttr("href");
        }

    }
)

function loadLayersURL(url) {
    $('#LayerContent').html('');
    $hidden = $('#LayerUrl');
    $hidden.attr("ic-get-from", url);
    $hidden.attr("ic-src", url);
    $hidden.attr("ic-target", "#LayerContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#loading-spinner-layer");
    $hidden.trigger("click");
}

function loadRecordsURL(url) {
    $('#RecordsContent').html('');
    goToInsideUrl = url;
    $hidden = $('#RecordsUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#RecordsContent");
    $hidden.attr("ic-push-url", "false");
    $hidden.attr("ic-indicator", "#records-loading-spinner");
    $hidden.trigger("click");
}

function loadBeobachtungsTableURL(url) {
    $('#BeobachtungTableContent').html('');
    goToInsideUrl = url;
    $hidden = $('#BeobachtungTableUrl');
    $hidden.attr("ic-get-from", goToInsideUrl);
    $hidden.attr("ic-src", goToInsideUrl);
    $hidden.attr("ic-target", "#BeobachtungTableContent");
    $hidden.attr("ic-indicator", "#table-loadingTable-spinner");
    $hidden.attr("ic-push-url", "false");
    $hidden.trigger("click");
}


