// Context menu item visibility for TreeGrid rows.
// Fires in capture phase so we know the row type before Syncfusion processes the right-click.
// requestAnimationFrame fires after Syncfusion shows the popup but before the browser paints.
// NOTE: Syncfusion does not render ContextMenuItemModel.Id as an HTML id attribute on <li> elements,
//       so visibility is controlled by matching item text content.

var _cmRowType   = null;  // 'header' | 'product' | null
var _cmIsReleased = false;

document.addEventListener('contextmenu', function (e) {
    var row = e.target.closest('tr.e-row');
    _cmRowType    = null;
    _cmIsReleased = false;
    if (row) {
        if      (row.classList.contains('row-batch'))   _cmRowType = 'header';
        else if (row.classList.contains('row-product')) _cmRowType = 'product';
        _cmIsReleased = row.classList.contains('row-released');
    }

    if (!_cmRowType) return;

    requestAnimationFrame(function () { applyContextMenuVisibility(_cmRowType, _cmIsReleased); });
    setTimeout(function ()            { applyContextMenuVisibility(_cmRowType, _cmIsReleased); }, 30);

}, true); // capture phase

function applyContextMenuVisibility(type, isReleased) {
    if (!type) return;

    var reportTexts  = ['Laser Parts Report', 'Iron Worker Report', 'Machine Shop Report',
                        'Bandsaw Report', 'Purchased Parts Report'];
    var headerDelTexts  = ['Delete Batch', 'Delete Schedule'];
    var productDelTexts = ['Delete Batch Item', 'Delete Order'];
    var releaseTexts    = ['Release to Production'];

    var items = document.querySelectorAll('.e-contextmenu-wrapper li, .e-contextmenu li');
    if (!items.length) items = document.querySelectorAll('ul.e-ul li');

    items.forEach(function (li) {
        var text  = li.textContent.trim();
        var isSep      = li.classList.contains('e-separator');
        var isReport   = reportTexts.some(function (t)     { return text.indexOf(t) !== -1; });
        var isHeaderDel= headerDelTexts.some(function (t)  { return text.indexOf(t) !== -1; });
        var isProductDel= productDelTexts.some(function (t){ return text.indexOf(t) !== -1; });
        var isRelease  = releaseTexts.some(function (t)    { return text.indexOf(t) !== -1; });

        // Skip items we don't manage
        if (!isSep && !isReport && !isHeaderDel && !isProductDel && !isRelease) return;

        var show;
        if (type === 'header') {
            if (isReleased) {
                // Already released: show reports only; hide release, delete items, and separators
                show = isReport;
            } else {
                // Unreleased header: show reports + release + delete-batch; hide delete-product + seps after delete section
                show = isReport || isRelease || isHeaderDel;
                if (isSep) show = isReport || isRelease; // keep sep between reports and release; hide sep between release and delete
            }
        } else {
            // Product/order row: show only product-level delete
            show = isProductDel;
            if (isSep) show = false;
        }

        li.style.display = show ? '' : 'none';
    });
}
