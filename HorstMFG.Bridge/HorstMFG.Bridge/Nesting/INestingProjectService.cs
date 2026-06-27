using System;

namespace HorstMFG.Bridge.Nesting;

public interface INestingProjectService
{
    /// <summary>
    /// Tells the running nesting software to flush its in-memory state (open nest + project)
    /// to disk before we read the RPD file. Call this before LoadProject on any write operation.
    /// </summary>
    void               FlushCurrentState();
    NestingProjectData LoadProject(string path);
    void               SaveProject(NestingProjectData project, string path);
    /// <summary>Tells the running nesting software to reload the project so changes are visible.</summary>
    void               NotifyProjectChanged(string path);
    long               GetNextId(NestingProjectData project);
    void               AddPart(NestingProjectData project, string symPath, long nestingId,
                               int qty, string? material, decimal thickness);
    void               RemoveParts(NestingProjectData project, long[] nestingIds);
    /// <summary>Sets each matching part's required quantity to its already-nested quantity.</summary>
    void               AdjustPartsQtyToMade(NestingProjectData project, long[] nestingIds);
    /// <summary>Updates the required quantity of an existing part in the project. Returns false if the part was not found.</summary>
    bool               UpdatePartQty(NestingProjectData project, long nestingId, int qty);
    SyncPayload        ReadSyncData(NestingProjectData project);
    string             CreateNewProject(string currentPath, DateTime date);
    /// <summary>
    /// Writes Radan attributes (material, thickness, description, order/batch context) into the .sym file.
    /// Must be called after the sym file exists on disk.
    /// </summary>
    void               SetPartAttributes(string symPath, string? material, decimal thickness,
                                         string? description, string? orderNumber,
                                         string? scheduleName, string? batchName, bool hasBends);
    byte[]?            ExtractThumbnail(string symPath);
    /// <summary>Reads description, material, and thickness stored in the .sym file XML.</summary>
    (string? Description, string? Material, decimal? Thickness) ReadPartAttributes(string symPath);
}
