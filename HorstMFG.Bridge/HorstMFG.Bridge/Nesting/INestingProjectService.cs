using System;

namespace HorstMFG.Bridge.Nesting;

public interface INestingProjectService
{
    NestingProjectData LoadProject(string path);
    void               SaveProject(NestingProjectData project, string path);
    /// <summary>Tells the running nesting software to reload the project so changes are visible.</summary>
    void               NotifyProjectChanged(string path);
    long               GetNextId(NestingProjectData project);
    void               AddPart(NestingProjectData project, string symPath, long nestingId,
                               int qty, string? material, decimal thickness);
    void               RemoveParts(NestingProjectData project, long[] nestingIds);
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
}
