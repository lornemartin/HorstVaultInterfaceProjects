// ── Grid group-expansion helpers ──────────────────────────────────────────
// Per-grid flag set during programmatic restore so each grid's watchExpansion
// listener does not snapshot partial state mid-restore.
// Keyed by gridId so concurrent restores on different grids don't interfere.
var _horstGridRestoring = {};

window.horstGrid = {

    // Save the grid content pane's scroll position before a data reload.
    saveScrollTop: function (gridId) {
        var el = document.getElementById(gridId);
        var content = el && el.querySelector('.e-content');
        return content ? content.scrollTop : 0;
    },

    // Restore scroll position after reload + group-expansion animations finish.
    // Retries several times to beat Syncfusion's own scroll resets.
    restoreScrollTop: function (gridId, top) {
        if (!top) return;
        var attempts = 0;
        function trySet() {
            var el = document.getElementById(gridId);
            var content = el && el.querySelector('.e-content');
            if (content) content.scrollTop = top;
            if (++attempts < 6) setTimeout(trySet, 80);
        }
        setTimeout(trySet, 180);
    },

    // Read which groups are currently expanded.
    // Queries .e-recordplusexpand icons directly — no dependency on ej.
    getExpandedGroupKeys: function (gridId) {
        var el = document.getElementById(gridId);
        if (!el) { console.log('[horstGrid] getExpandedGroupKeys: element not found', gridId); return { level1: [], level2: [] }; }
        var level1 = [], level2 = [];
        el.querySelectorAll('.e-recordplusexpand').forEach(function (icon) {
            var row = icon.closest('tr');
            if (!row) return;
            var keyEl = row.querySelector('[data-group-key]');
            if (!keyEl) {
                console.log('[horstGrid] expanded row has no data-group-key — HTML:', row.innerHTML.substring(0, 400));
                return;
            }
            var key   = parseInt(keyEl.dataset.groupKey,  10);
            var level = parseInt(keyEl.dataset.groupLevel, 10);
            if (isNaN(key)) return;
            if (level === 1) level1.push(key);
            else if (level === 2) level2.push(key);
        });
        console.log('[horstGrid] getExpandedGroupKeys:', { level1: level1, level2: level2 });
        return { level1: level1, level2: level2 };
    },

    // Restore expansion state by programmatically clicking the collapse icons.
    // No dependency on ej — Syncfusion's own click handler does the expansion,
    // just as if the user had clicked the icon manually.
    restoreExpansion: function (gridId, level1Keys, level2Keys) {
        var el = document.getElementById(gridId);
        if (!el) { console.log('[horstGrid] restoreExpansion: element not found', gridId); return; }

        console.log('[horstGrid] restoreExpansion: keys to restore', { level1Keys: level1Keys, level2Keys: level2Keys });

        // Hold the per-grid restoring flag for the entire process so this grid's
        // watchExpansion stays silent. Uses gridId key so concurrent restores on
        // different grids don't interfere with each other.
        _horstGridRestoring[gridId] = true;
        var deadline = Date.now() + 5000;

        function tryExpand() {
            if (Date.now() > deadline) {
                _horstGridRestoring[gridId] = false;
                console.log('[horstGrid] restoreExpansion: done (deadline)', gridId);
                return;
            }

            var collapseIcons = el.querySelectorAll('.e-recordpluscollapse');
            console.log('[horstGrid] restoreExpansion: scanning', collapseIcons.length, 'collapsed icons', gridId);

            var clickedIcon = null;
            for (var i = 0; i < collapseIcons.length; i++) {
                var icon = collapseIcons[i];
                var row  = icon.closest('tr');
                if (!row) continue;
                var keyEl = row.querySelector('[data-group-key]');
                if (!keyEl) continue;
                var key   = parseInt(keyEl.dataset.groupKey,   10);
                var level = parseInt(keyEl.dataset.groupLevel, 10);
                var want  = (level === 1 && level1Keys.indexOf(key) !== -1) ||
                            (level === 2 && level2Keys.indexOf(key) !== -1);
                if (want) {
                    console.log('[horstGrid] clicking to expand level', level, 'key', key, gridId);
                    icon.click();
                    clickedIcon = icon;
                    break;
                }
            }

            if (clickedIcon) {
                // Wait for the clicked icon to flip to .e-recordplusexpand (lazy-load complete)
                // before rescanning. Without this wait we would double-click and cancel the expansion.
                var polls = 0;
                function waitForFlip() {
                    if (clickedIcon.classList.contains('e-recordplusexpand') || polls >= 30) {
                        setTimeout(tryExpand, 100);
                    } else {
                        polls++;
                        setTimeout(waitForFlip, 100);
                    }
                }
                setTimeout(waitForFlip, 80);
            } else {
                // Nothing to expand this pass — keep polling until deadline in case
                // Syncfusion fires a deferred collapse after its own render.
                if (Date.now() < deadline) {
                    setTimeout(tryExpand, 200);
                } else {
                    _horstGridRestoring[gridId] = false;
                    console.log('[horstGrid] restoreExpansion: done', gridId);
                }
            }
        }

        // Initial delay: let Syncfusion finish its post-filter DOM work before first scan.
        setTimeout(tryExpand, 300);
    },

    // Attach a click listener so we're notified when the user expands/collapses
    // a group row. Uses capture phase so it fires even if Syncfusion calls
    // stopPropagation. Skipped during programmatic restore.
    watchExpansion: function (gridId, dotNetRef) {
        var el = document.getElementById(gridId);
        if (!el) { console.log('[horstGrid] watchExpansion: element not found', gridId); return; }
        console.log('[horstGrid] watchExpansion: listener attached to', gridId);
        el.addEventListener('click', function (e) {
            if (_horstGridRestoring[gridId]) {
                console.log('[horstGrid] watchExpansion: click suppressed (restoring)', gridId);
                return;
            }
            var icon = e.target.closest('.e-recordpluscollapse, .e-recordplusexpand');
            if (!icon) return;
            console.log('[horstGrid] watchExpansion: USER expand/collapse click detected in', gridId);

            // Determine what class the icon should have after Syncfusion finishes.
            // A collapse icon (.e-recordpluscollapse) becomes expand (.e-recordplusexpand) once loaded.
            // An expand icon (.e-recordplusexpand) becomes collapse (.e-recordpluscollapse) immediately.
            var wasCollapse = icon.classList.contains('e-recordpluscollapse');
            var expectedClass = wasCollapse ? 'e-recordplusexpand' : 'e-recordpluscollapse';

            // Poll until the icon flips class (signals lazy-load is complete), then capture state.
            var polls = 0;
            function poll() {
                if (icon.classList.contains(expectedClass) || polls >= 20) {
                    setTimeout(function () {
                        var keys = window.horstGrid.getExpandedGroupKeys(gridId);
                        console.log('[horstGrid] watchExpansion: sending to UpdateExpansionState (poll=' + polls + ')', keys);
                        dotNetRef.invokeMethodAsync('UpdateExpansionState', keys.level1, keys.level2);
                    }, 80);
                } else {
                    polls++;
                    setTimeout(poll, 100);
                }
            }
            setTimeout(poll, 80);
        }, true);
    },

    // ── String-keyed group expansion (for grids with non-integer group keys) ───

    getExpandedGroupStringKeys: function (gridId) {
        var el = document.getElementById(gridId);
        if (!el) return [];
        var keys = [];
        el.querySelectorAll('.e-recordplusexpand').forEach(function (icon) {
            var row = icon.closest('tr');
            if (!row) return;
            var keyEl = row.querySelector('[data-group-key]');
            if (!keyEl) return;
            var key = keyEl.dataset.groupKey;
            if (key && keys.indexOf(key) === -1) keys.push(key);
        });
        return keys;
    },

    restoreStringExpansion: function (gridId, keys) {
        var el = document.getElementById(gridId);
        if (!el || !keys || keys.length === 0) return;

        _horstGridRestoring[gridId] = true;
        function tryExpand() {
            var collapseIcons = el.querySelectorAll('.e-recordpluscollapse');
            for (var i = 0; i < collapseIcons.length; i++) {
                var icon = collapseIcons[i];
                var row  = icon.closest('tr');
                if (!row) continue;
                var keyEl = row.querySelector('[data-group-key]');
                if (!keyEl) continue;
                var key = keyEl.dataset.groupKey;
                if (keys.indexOf(key) !== -1) {
                    icon.click();
                    setTimeout(function () {
                        tryExpand();
                    }, 150);
                    return;
                }
            }
            _horstGridRestoring[gridId] = false;
        }
        setTimeout(tryExpand, 150);
    },

    watchStringExpansion: function (gridId, dotNetRef) {
        var el = document.getElementById(gridId);
        if (!el) return;
        el.addEventListener('click', function (e) {
            if (_horstGridRestoring[gridId]) return;
            if (!e.target.closest('.e-recordpluscollapse, .e-recordplusexpand')) return;
            setTimeout(function () {
                var keys = window.horstGrid.getExpandedGroupStringKeys(gridId);
                dotNetRef.invokeMethodAsync('UpdateGroupExpansionState', keys);
            }, 150);
        }, true);
    },

    highlightCaptionRow: function (gridId, key, level) {
        var styleId = 'hor-highlight-style-' + gridId;
        var existing = document.getElementById(styleId);
        if (existing) existing.remove();
        if (!key) return;
        var style = document.createElement('style');
        style.id = styleId;
        style.textContent = '#' + gridId + ' tr:has(> td > span[data-group-key="' + key + '"][data-group-level="' + level + '"]) > td { background-color: #fefce8 !important; box-shadow: inset 3px 0 0 #d97706; }';
        document.head.appendChild(style);
    }
};

// Context menu item visibility for TreeGrid rows.
// Fires in capture phase so we know the row type before Syncfusion processes the right-click.
// requestAnimationFrame fires after Syncfusion shows the popup but before the browser paints.
// NOTE: Syncfusion does not render ContextMenuItemModel.Id as an HTML id attribute on <li> elements,
//       so visibility is controlled by matching item text content.

var _cmRowType    = null;  // 'header' | 'product' | 'leaf-pdf' | null
var _cmIsReleased = false;
var _userCanEdit  = true;  // false for ShopFloor (read-only)

window.setUserCanEdit = function (val) { _userCanEdit = val; };

document.addEventListener('contextmenu', function (e) {
    var row = e.target.closest('tr.e-row');
    _cmRowType    = null;
    _cmIsReleased = false;
    if (row) {
        if      (row.classList.contains('row-batch'))    _cmRowType = 'header';
        else if (row.classList.contains('row-product'))  _cmRowType = 'product';
        else if (row.classList.contains('row-leaf-pdf')) _cmRowType = 'leaf-pdf';
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

        // Skip items we don't manage
        if (!isSep && !isReport && !isHeaderDel && !isProductDel && !isRelease && !isPdf) return;

        var show;
        if (type === 'leaf-pdf') {
            // Part row with PDF: show only View PDF; hide everything else
            show = isPdf;
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
