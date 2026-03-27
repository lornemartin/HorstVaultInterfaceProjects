// Context menu item visibility for TreeGrid rows.
// Fires in capture phase so we know the row type before Syncfusion processes the right-click.
// requestAnimationFrame fires after Syncfusion shows the popup but before the browser paints.

var _cmRowType = null;
var _cmIsReleased = false;

document.addEventListener('contextmenu', function (e) {
    var row = e.target.closest('tr.e-row');
    _cmRowType = null;
    _cmIsReleased = false;
    if (row) {
        if (row.classList.contains('row-batch'))        _cmRowType = 'header';
        else if (row.classList.contains('row-product')) _cmRowType = 'product';
        _cmIsReleased = row.classList.contains('row-released');
    }

    if (!_cmRowType) return;

    requestAnimationFrame(function () { applyContextMenuVisibility(_cmRowType, _cmIsReleased); });

    // Belt-and-suspenders: also run after a short delay in case rAF fires too early
    setTimeout(function () { applyContextMenuVisibility(_cmRowType, _cmIsReleased); }, 30);

}, true); // capture phase

function applyContextMenuVisibility(type, isReleased) {
    if (!type) return;

    // All item IDs we manage; anything else is left untouched.
    var managed = {
        'laser-report':    true,
        'op-iron-worker':  true,
        'op-machine-shop': true,
        'op-bandsaw':      true,
        'op-purchased':    true,
        'sep-reports':     true,
        'release-batch':   true,
        'release-schedule':true,
        'sep-release':     true,
        'delete-batch':    true,
        'delete-product':  true
    };

    var items = document.querySelectorAll('.e-contextmenu-wrapper li, .e-contextmenu li');
    if (!items.length) items = document.querySelectorAll('ul.e-ul li');

    items.forEach(function (li) {
        var id = li.id || '';
        if (!managed[id]) return;

        var show;
        if (type === 'header') {
            if (isReleased) {
                // Already released: show reports only; hide release, separators, and delete items.
                show = id === 'laser-report' || id === 'op-iron-worker' || id === 'op-machine-shop' ||
                       id === 'op-bandsaw'   || id === 'op-purchased';
            } else {
                // Batch/schedule header, not yet released: show everything except delete-product.
                show = id !== 'delete-product';
            }
        } else {
            // Product/order row: show only the product-level delete item.
            show = id === 'delete-product';
        }

        li.style.display = show ? '' : 'none';
    });
}
