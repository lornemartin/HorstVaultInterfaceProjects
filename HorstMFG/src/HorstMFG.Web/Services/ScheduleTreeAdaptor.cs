using HorstMFG.Core.DTOs;
using HorstMFG.Core.Interfaces;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Data;

namespace HorstMFG.Web.Services;

/// <summary>
/// Custom Syncfusion DataAdaptor for the Daily Schedule TreeGrid.
/// Resolved via DI (Scoped) — calls IBomService directly, avoiding HTTP auth issues
/// that occur when WebApiAdaptor makes server-side requests without the user's cookie.
/// </summary>
public class ScheduleTreeAdaptor : DataAdaptor
{
    private readonly IBomService _bomService;
    private readonly ScheduleTreeFilterState _state;

    public ScheduleTreeAdaptor(IBomService bomService, ScheduleTreeFilterState state)
    {
        _bomService = bomService;
        _state = state;
    }

    public override async Task<object> ReadAsync(DataManagerRequest dm, string key = null!)
    {
        // Syncfusion adds a WhereFilter on TreeParentId when loading children (load-on-demand).
        // Root load: no Where, or value == null.
        // Child load: Where[0].value == parentTreeId.
        int? parentTreeId = null;
        if (dm.Where?.Count > 0)
        {
            var filter = dm.Where.FirstOrDefault(w =>
                string.Equals(w.Field, "TreeParentId", StringComparison.OrdinalIgnoreCase));

            if (filter?.value is not null && int.TryParse(filter.value.ToString(), out int pid))
                parentTreeId = pid;
        }

        List<ExportTreeItem> items = parentTreeId.HasValue
            ? await _bomService.GetScheduleChildrenByParentTreeIdAsync(parentTreeId.Value, _state.IncludeProcessed)
            : await _bomService.GetScheduleTreeItemsAsync(_state.PlantId, _state.IncludeProcessed, _state.FromDate, _state.ToDate);

        // Apply sort operations so column sorting works with CustomAdaptor
        IEnumerable<ExportTreeItem> result = items;
        if (dm.Sorted?.Count > 0)
            result = DataOperations.PerformSorting(result, dm.Sorted);

        var list = result.ToList();
        return dm.RequiresCounts
            ? (object)new DataResult { Result = list, Count = list.Count }
            : list;
    }
}
