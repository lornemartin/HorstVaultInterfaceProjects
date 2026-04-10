// Context menu item visibility for TreeGrid rows.
// Fires in capture phase so we know the row type before Syncfusion processes the right-click.
// requestAnimationFrame fires after Syncfusion shows the popup but before the browser paints.
// NOTE: Syncfusion does not render ContextMenuItemModel.Id as an HTML id attribute on <li> elements,
//       so visibility is controlled by matching item text content.

var _cmRowType    = null;  // 'header' | 'product' | 'leaf-pdf' | 'leaf-nopdf' | null
var _cmIsReleased = false;
var _userCanEdit  = true;  // false for ShopFloor (read-only)

window.setUserCanEdit = function (val) { _userCanEdit = val; };

document.addEventListener('contextmenu', function (e) {
    var row = e.target.closest('tr.e-row');
    _cmRowType    = null;
    _cmIsReleased = false;
    if (row) {
        if      (row.classList.contains('row-batch'))     _cmRowType = 'header';
        else if (row.classList.contains('row-product'))   _cmRowType = 'product';
        else if (row.classList.contains('row-leaf-pdf'))  _cmRowType = 'leaf-pdf';
        else if (row.classList.contains('row-leaf-nopdf'))_cmRowType = 'leaf-nopdf';
        _cmIsReleased = row.classList.contains('row-released');
    }

    if (!_cmRowType) return;

    requestAnimationFrame(function () { applyContextMenuVisibility(_cmRowType, _cmIsReleased); });
    setTimeout(function ()            { applyContextMenuVisibility(_cmRowType, _cmIsReleased); }, 30);

}, true); // capture phase

function applyContextMenuVisibility(type, isReleased) {
    if (!type) return;

    var reportTexts     = ['Laser Parts Report', 'Iron Worker Report', 'Machine Shop Report',
                           'Bandsaw Report', 'Purchased Parts Report'];
    var headerDelTexts  = ['Delete Batch', 'Delete Schedule'];
    var productDelTexts = ['Delete Batch Item', 'Delete Order'];
    var releaseTexts    = ['Release to Production'];
    var pdfTexts        = ['View PDF'];
    var genPdfTexts     = ['Generate PDF'];

    var items = document.querySelectorAll('.e-contextmenu-wrapper li, .e-contextmenu li');
    if (!items.length) items = document.querySelectorAll('ul.e-ul li');

    items.forEach(function (li) {
        var text  = li.textContent.trim();
        var isSep       = li.classList.contains('e-separator');
        var isReport    = reportTexts.some(function (t)      { return text.indexOf(t) !== -1; });
        var isHeaderDel = headerDelTexts.some(function (t)   { return text.indexOf(t) !== -1; });
        var isProductDel= productDelTexts.some(function (t)  { return text.indexOf(t) !== -1; });
        var isRelease   = releaseTexts.some(function (t)     { return text.indexOf(t) !== -1; });
        var isPdf       = pdfTexts.some(function (t)         { return text.indexOf(t) !== -1; });
        var isGenPdf    = genPdfTexts.some(function (t)      { return text.indexOf(t) !== -1; });

        // Generate PDF is always available for all rows
        if (isGenPdf) {
            li.style.display = '';
            return;
        }

        // Skip items we don't manage
        if (!isSep && !isReport && !isHeaderDel && !isProductDel && !isRelease && !isPdf) return;

        var show;
        if (type === 'leaf-pdf') {
            show = isPdf;
            if (isSep) show = false;
        } else if (type === 'leaf-nopdf') {
            show = false; // no View PDF for rows without a PDF
            if (isSep) show = false;
        } else if (!_userCanEdit) {
            // Read-only user (ShopFloor): only show report items on header rows
            show = (type === 'header') && isReport;
            if (isSep) show = false;
        } else if (type === 'header') {
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
