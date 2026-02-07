# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is an Autodesk Vault integration and CAD interface suite for manufacturing workflows (Radan/CNC). It consists of ~19 Visual Studio solutions with 27+ C# projects built on .NET Framework 4.7-4.8, heavily integrated with Autodesk Vault SDKs (2019-2025).

## Build Commands

All projects use MSBuild (Visual Studio solutions). Build from command line:

```
msbuild <SolutionName>.sln /p:Configuration=Release /p:Platform=x64
```

Key solutions and their .sln files:
- `VaultItemProcessor/VaultItemProcessor.sln` — Main GUI application
- `RadanMaster/RadanMaster.sln` — Radan CAD integration (4 projects)
- `PrintPDF/PrintPDF.sln` — Core PDF printing library
- `JobProcessorPDFPrint/JobProcessorPrintPDF.sln` — Job Processor PDF extension
- `PDFPrintVaultExtension/PrintPDFVaultExtension.sln` — Vault Explorer extension
- `ReviewSymFiles/ReviewSymFiles.sln` — Symbol file sync utility

NuGet restore may be needed before building: `nuget restore <SolutionName>.sln`

There are no automated unit tests. Testing is done via manual GUI/console test projects (PrintPdfTest2, VaultDrawingRetrieveTest).

## Architecture

### Dependency Hierarchy

```
GUI Applications (VaultItemProcessor, RadanMaster)
    ↓
Business Libraries (VaultAccess, PrintPDF, ItemExport)
    ↓
Autodesk Vault SDK (Autodesk.Connectivity.*, Autodesk.DataManagement.*)
    ↓
Infrastructure (Ghostscript, PDFSharp/MigraDoc, Bullzip PDF Printer)
```

### Core Projects

- **VaultAccess** — Foundation library for Vault connectivity and authentication. Referenced by most other projects.
- **PrintPDF** — Core PDF printing/generation. Uses PDFSharp, MigraDoc, and Vault Job Processor Extensibility.
- **VaultItemProcessor** — Main WinForms GUI app (DevExpress controls). References VaultAccess, PrintPDF, and ItemExport.
- **ItemExport** — Vault item export, search, and filtering logic.
- **RadanMaster** — Radan CAD integration app. References VaultAccess, RadanInterface2, RadanProject.

### Extension Projects (loaded by Autodesk runtime)

- **JobProcessorPDFPrint** / **JobProcessorFilesUpdate** — Job Processor extensions for background PDF printing and file updates.
- **PDFPrintVaultExtension** — Adds PDF print commands to Vault Explorer UI.
- **ECOpen** / **DirectView2016** — Vault Explorer UI extensions.

### Utilities

- **ReviewSymFiles** — Console app to sync .sym files between Vault and local directories.
- **Send2CNC** — Console app for CNC machine interface.
- **PrintPDFCommandLine** — Command-line PDF printing.
- **MonitorVaultJP.ps1** — PowerShell watchdog script that monitors and auto-restarts Job Processor on failures.

## Key Technical Details

- **Target framework:** .NET Framework 4.7-4.8 (some legacy 4.5)
- **Platform:** Primarily x64, some x86/AnyCPU
- **UI:** WinForms with DevExpress v22.1 controls
- **Logging:** Serilog (file + console sinks)
- **PDF pipeline:** PDFSharp/MigraDoc → Ghostscript (gs9.21) → Bullzip PDF Printer
- **Database:** PostgreSQL via Npgsql in some projects
- **Configuration:** Custom XML-based AppSettings.xml files (not standard app.config) for most application settings
- **Assembly signing:** Multiple projects use strong name signing (.snk files)
- **Vault SDK:** References in `Vault 2025 SDK Binaries/` directory at repo root

## Configuration

Application settings are stored in custom `AppSettings.xml` files (not .NET app.config). These contain environment-specific paths, printer names, Vault credentials, and Ghostscript paths. Each deployable project has its own AppSettings.xml.
