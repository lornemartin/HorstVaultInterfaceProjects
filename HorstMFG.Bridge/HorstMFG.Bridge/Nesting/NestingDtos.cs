using System;
using System.Collections.Generic;

namespace HorstMFG.Bridge.Nesting;

// ── Generic project representation ───────────────────────────────────────────

public class NestingProjectData
{
    public string  Path    { get; set; } = "";
    public object? Native  { get; set; }  // Holds the software-specific object (e.g. RadanProject)
}

// ── Sync payload returned to HorstMFG ────────────────────────────────────────

public class SyncPayload
{
    public List<SyncPart> Parts { get; set; } = new();
    public List<SyncNest> Nests { get; set; } = new();
}

public class SyncPart
{
    public long RadanIdNumber { get; set; }
    public int  QtyNested     { get; set; }
}

public class SyncNest
{
    public string          NestName       { get; set; } = "";
    public string?         NestPath       { get; set; }
    public byte[]?         ThumbnailBytes { get; set; }
    public List<NestEntry> Parts          { get; set; } = new();
}

public class NestEntry
{
    public long RadanIdNumber { get; set; }
    public int  Qty           { get; set; }
}

// ── Command payloads (bridge receives from HorstMFG) ─────────────────────────

public class SendToNestingItem
{
    public int     ItemId        { get; set; }
    public string  ItemType      { get; set; } = "";  // "Order" | "Batch"
    public string  FileName      { get; set; } = "";
    public int     QtyRequired   { get; set; }
    public string? Material      { get; set; }
    public decimal Thickness     { get; set; }
    public string? OrderNumber   { get; set; }
    public string? Description   { get; set; }
    public string? ScheduleName  { get; set; }
    public string? BatchName     { get; set; }
    public bool    HasBends      { get; set; }
    /// <summary>Set when re-sending a partially-nested part that already exists in the Radan project.</summary>
    public long?   RadanIdNumber { get; set; }
}

public class RetrieveFromNestingItem
{
    public int  ItemId        { get; set; }
    public long RadanIdNumber { get; set; }
}

public class RetrieveFromVaultItem
{
    public int    ItemId   { get; set; }
    public string FileName { get; set; } = "";
}

public class UpdateThumbnailItem
{
    public int    PartId   { get; set; }
    public string FileName { get; set; } = "";
}

// ── Command result payloads (bridge sends to HorstMFG) ───────────────────────

public class SendToNestingResult
{
    public int   ItemId         { get; set; }
    public long  RadanIdNumber  { get; set; }
    public bool  MissingSymFile { get; set; }
}

public class RetrieveFromVaultResult
{
    public int  ItemId  { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class UpdateThumbnailResult
{
    public int     PartId         { get; set; }
    public byte[]? ThumbnailBytes { get; set; }
    public bool    Success        { get; set; }
}

public class RetrieveFromNestingResult
{
    public SyncPayload Sync            { get; set; } = new();
    public List<int>   ClearedItemIds  { get; set; } = new();
    /// <summary>Items still in the project but whose required qty was adjusted to match QtyNested.</summary>
    public List<int>   AdjustedItemIds { get; set; } = new();
}

public class FinalizeResult
{
    public SyncPayload  Sync           { get; set; } = new();
    public List<int>    ClearedItemIds { get; set; } = new();
    public string       NewProjectName { get; set; } = "";
    public string       NewProjectPath { get; set; } = "";
}
