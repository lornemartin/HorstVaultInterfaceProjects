// Context menu item visibility for TreeGrid rows.
// Fires in capture phase so we know the row type before Syncfusion processes the right-click.
// requestAnimationFrame fires after Syncfusion shows the popup but before the browser paints.

var _cmRowType = null;

document.addEventListener('contextmenu', function (e) {
    var row = e.target.closest('tr.e-row');
    _cmRowType = null;
    if (row) {
        if (row.classList.contains('row-batch'))   _cmRowType = 'header';
        else if (row.classList.contains('row-product')) _cmRowType = 'product';
    }

    if (!_cmRowType) return;

    requestAnimationFrame(function () {
        applyContextMenuVisibility(_cmRowType);
    });

    // Belt-and-suspenders: also run after a short delay in case rAF fires too early
    setTimeout(function () {
        applyContextMenuVisibility(_cmRowType);
    }, 30);

}, true); // capture phase

function applyContextMenuVisibility(type) {
    if (!type) return;

    var reportTexts  = ['Laser Parts Report', 'Iron Worker Report', 'Machine Shop Report', 'Bandsaw Report', 'Purchased Parts Report'];
    var headerTexts  = ['Delete Batch', 'Delete Schedule'];
    var productTexts = ['Delete Batch Item', 'Delete Order'];

    // Find all visible context menu list items
    var items = document.querySelectorAll('.e-contextmenu-wrapper li, .e-contextmenu li');
    if (!items.length) {
        // Fallback selector
        items = document.querySelectorAll('ul.e-ul li');
    }

    items.forEach(function (li) {
        var text = li.textContent.trim();
        var isSep     = li.classList.contains('e-separator');
        var isReport  = reportTexts.some(function (t) { return text.indexOf(t) !== -1; });
        var isHeader  = headerTexts.some(function (t) { return text.indexOf(t) !== -1; });
        var isProduct = productTexts.some(function (t) { return text.indexOf(t) !== -1; });

        if (!isSep && !isReport && !isHeader && !isProduct) return; // not our items

        if (type === 'header') {
            // Batch/Schedule row: show reports + delete-all, hide delete-product
            li.style.display = isProduct ? 'none' : '';
        } else {
            // Product row: show only delete-product, hide reports + delete-all + separator
            li.style.display = (isProduct) ? '' : 'none';
        }
    });
}
