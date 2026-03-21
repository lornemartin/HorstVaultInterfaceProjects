using HorstMFG.Core.DTOs;
using HorstMFG.Core.Interfaces;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Data;

namespace HorstMFG.Web.Services;

public class BatchTreeAdaptor : DataAdaptor
{
    private readonly IBomService _bomService;
    private readonly BatchTreeFilterState _state;

    public BatchTreeAdaptor(IBomService bomService, BatchTreeFilterState state)
    {
        _bomService = bomService;
        _state = state;
    }

    public override async Task<object> ReadAsync(DataManagerRequest dm, string key = null!)
    {
        int? parentTreeId = null;
        if (dm.Where?.Count > 0)
        {
            var filter = dm.Where.FirstOrDefault(w =>
                string.Equals(w.Field, "TreeParentId", StringComparison.OrdinalIgnoreCase));

            if (filter?.value is not null && int.TryParse(filter.value.ToString(), out int pid))
                parentTreeId = pid;
        }

        _state.LastReadWasRootLoad = !parentTreeId.HasValue;

        List<ExportTreeItem> items = parentTreeId.HasValue
            ? await _bomService.GetBatchChildrenByParentTreeIdAsync(parentTreeId.Value, _state.IncludeProcessed)
            : await _bomService.GetBatchTreeItemsAsync(_state.PlantId, _state.IncludeProcessed, _state.FromDate, _state.ToDate);

        IEnumerable<ExportTreeItem> result = items;
        if (dm.Sorted?.Count > 0)
            result = DataOperations.PerformSorting(result, dm.Sorted);

        var list = result.ToList();
        return dm.RequiresCounts
            ? (object)new DataResult { Result = list, Count = list.Count }
            : list;
    }
}
