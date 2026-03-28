using System;

namespace HorstMFG.Bridge.Nesting;

public interface INestingProjectService
{
    NestingProjectData LoadProject(string path);
    void               SaveProject(NestingProjectData project, string path);
    long               GetNextId(NestingProjectData project);
    void               AddPart(NestingProjectData project, string symPath, long nestingId,
                               int qty, string? material, decimal thickness);
    void               RemoveParts(NestingProjectData project, long[] nestingIds);
    SyncPayload        ReadSyncData(NestingProjectData project);
    string             CreateNewProject(string currentPath, DateTime date);
    byte[]?            ExtractThumbnail(string symPath);
}
