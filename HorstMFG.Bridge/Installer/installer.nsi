; HorstMFG Bridge Installer
; Requires NSIS 3.x  (https://nsis.sourceforge.io/)
;
; Build:
;   makensis installer.nsi
;
; The script expects these builds to already exist (relative to this .nsi file):
;   ..\HorstMFG.Bridge\bin\Release\net48\           Bridge, Release
;   ..\..\ItemExport\bin\x64\Release\               ItemExport Vault extension, Release|x64

!define APP_NAME        "HorstMFG Bridge"
!define SERVICE_NAME    "HorstMFGBridge"
!define SERVICE_DISPLAY "Horst MFG Bridge"
!define APP_VERSION     "1.0.0"
!define PUBLISHER       "Horst Manufacturing"
!define UNINST_KEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SERVICE_NAME}"
; ItemExport ("Export Item Direct") writes the BOM export file the Bridge watches.
; Must match the Vault SDK ItemExport is built against.
!define VAULT_VERSION   "2026"
!define ITEMEXPORT_SRC  "..\..\ItemExport\bin\x64\Release"

;--------------------------------------------------------------------
; Basic setup
;--------------------------------------------------------------------
Unicode          True
Name             "${APP_NAME} ${APP_VERSION}"
OutFile          "HorstMFG.Bridge.Setup.exe"
InstallDir       "$PROGRAMFILES64\Horst Manufacturing\Bridge"
InstallDirRegKey HKLM "${UNINST_KEY}" "InstallLocation"
RequestExecutionLevel admin
SetCompressor    lzma

;--------------------------------------------------------------------
; Includes
;--------------------------------------------------------------------
!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"
!include "StrFunc.nsh"
!include "FileFunc.nsh"

; Declare StrRep — must appear outside of sections/functions
${StrRep}

;--------------------------------------------------------------------
; Variables
;--------------------------------------------------------------------
Var StationId
Var HorstMfgUrl
Var ApiKey
Var VaultServer
Var VaultName
Var VaultUsername
Var VaultPassword
Var RadanPath
Var SymPath
Var BomPath

; Dialog control handles
Var hStationId
Var hHorstMfgUrl
Var hApiKey
Var hVaultServer
Var hVaultName
Var hVaultUsername
Var hVaultPassword
Var hRadanPath
Var hSymPath
Var hBomPath

;--------------------------------------------------------------------
; MUI settings
;--------------------------------------------------------------------
!define MUI_ABORTWARNING

;--------------------------------------------------------------------
; Pages (install)
;--------------------------------------------------------------------
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
Page custom PageStation PageStationLeave
Page custom PageVault   PageVaultLeave
Page custom PagePaths   PagePathsLeave
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

;--------------------------------------------------------------------
; Pages (uninstall)
;--------------------------------------------------------------------
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

;--------------------------------------------------------------------
; .onInit — default values shown in the config pages
;--------------------------------------------------------------------
Function .onInit
  StrCpy $StationId     ""
  StrCpy $HorstMfgUrl   ""
  StrCpy $ApiKey        ""
  StrCpy $VaultServer   ""
  StrCpy $VaultName     ""
  StrCpy $VaultUsername ""
  StrCpy $VaultPassword ""
  StrCpy $RadanPath     ""
  StrCpy $SymPath       ""
  StrCpy $BomPath       ""

  ; Pre-populate from an existing install's appsettings.json if present
  IfFileExists "$INSTDIR\appsettings.json" +1 init_done

  GetTempFileName $R9

  ; Write a PS1 that parses the JSON and writes a temp INI NSIS can read
  FileOpen $R8 "$R9.ps1" w
  FileWrite $R8 "$$j = Get-Content '$INSTDIR\appsettings.json' | ConvertFrom-Json$\n"
  FileWrite $R8 "$$out = '$R9.ini'$\n"
  FileWrite $R8 "Set-Content  $$out '[S]' -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('sid=' + [string]$$j.Bridge.StationId)             -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('url=' + [string]$$j.Bridge.HorstMfgUrl)           -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('key=' + [string]$$j.Bridge.ApiKey)                -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('vs='  + [string]$$j.Vault.Server)                 -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('vn='  + [string]$$j.Vault.Vault)                  -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('vu='  + [string]$$j.Vault.Username)               -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('vp='  + [string]$$j.Vault.Password)               -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('rp='  + [string]$$j.Bridge.RadanProjectsRootPath) -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('sp='  + [string]$$j.Bridge.SymNetworkSharePath)   -Encoding ascii$\n"
  FileWrite $R8 "Add-Content  $$out ('bp='  + [string]$$j.Bridge.BomExportFilePath)     -Encoding ascii$\n"
  FileClose $R8

  nsExec::ExecToStack 'powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$R9.ps1"'
  Pop $0  ; return code  (discard)
  Pop $0  ; stdout       (discard)

  IfFileExists "$R9.ini" +1 init_cleanup
  ReadINIStr $StationId     "$R9.ini" "S" "sid"
  ReadINIStr $HorstMfgUrl   "$R9.ini" "S" "url"
  ReadINIStr $ApiKey        "$R9.ini" "S" "key"
  ReadINIStr $VaultServer   "$R9.ini" "S" "vs"
  ReadINIStr $VaultName     "$R9.ini" "S" "vn"
  ReadINIStr $VaultUsername "$R9.ini" "S" "vu"
  ReadINIStr $VaultPassword "$R9.ini" "S" "vp"
  ReadINIStr $RadanPath     "$R9.ini" "S" "rp"
  ReadINIStr $SymPath       "$R9.ini" "S" "sp"
  ReadINIStr $BomPath       "$R9.ini" "S" "bp"

  init_cleanup:
  Delete "$R9.ps1"
  Delete "$R9.ini"
  Delete "$R9"

  init_done:
FunctionEnd

;====================================================================
; Custom page: Station Settings
;====================================================================
Function PageStation
  !insertmacro MUI_HEADER_TEXT "Station Settings" "Configure this Bridge station's identity and HorstMFG connection."
  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel}  0   0u 100% 12u "Station ID (from HorstMFG database):"
  Pop $0
  ${NSD_CreateNumber} 0  14u  60u 13u "$StationId"
  Pop $hStationId

  ${NSD_CreateLabel}  0  38u 100% 12u "HorstMFG URL:"
  Pop $0
  ${NSD_CreateText}   0  52u 100% 13u "$HorstMfgUrl"
  Pop $hHorstMfgUrl

  ${NSD_CreateLabel}  0  76u 100% 12u "API Key:"
  Pop $0
  ${NSD_CreateText}   0  90u 100% 13u "$ApiKey"
  Pop $hApiKey

  nsDialogs::Show
FunctionEnd

Function PageStationLeave
  ${NSD_GetText} $hStationId   $StationId
  ${NSD_GetText} $hHorstMfgUrl $HorstMfgUrl
  ${NSD_GetText} $hApiKey      $ApiKey

  ${If} $StationId == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "Station ID is required."
    Abort
  ${EndIf}
  ${If} $HorstMfgUrl == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "HorstMFG URL is required."
    Abort
  ${EndIf}
  ${If} $ApiKey == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "API Key is required."
    Abort
  ${EndIf}
FunctionEnd

;====================================================================
; Custom page: Vault Credentials
;====================================================================
Function PageVault
  !insertmacro MUI_HEADER_TEXT "Vault Credentials" "Enter Autodesk Vault connection details."
  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel}    0   0u 100% 12u "Vault Server:"
  Pop $0
  ${NSD_CreateText}     0  14u 100% 13u "$VaultServer"
  Pop $hVaultServer

  ${NSD_CreateLabel}    0  38u 100% 12u "Vault Name:"
  Pop $0
  ${NSD_CreateText}     0  52u 100% 13u "$VaultName"
  Pop $hVaultName

  ${NSD_CreateLabel}    0  76u 100% 12u "Username:"
  Pop $0
  ${NSD_CreateText}     0  90u 100% 13u "$VaultUsername"
  Pop $hVaultUsername

  ${NSD_CreateLabel}    0 114u 100% 12u "Password:"
  Pop $0
  ${NSD_CreatePassword} 0 128u 100% 13u ""
  Pop $hVaultPassword

  nsDialogs::Show
FunctionEnd

Function PageVaultLeave
  ${NSD_GetText} $hVaultServer   $VaultServer
  ${NSD_GetText} $hVaultName     $VaultName
  ${NSD_GetText} $hVaultUsername $VaultUsername
  ${NSD_GetText} $hVaultPassword $VaultPassword

  ${If} $VaultServer == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "Vault Server is required."
    Abort
  ${EndIf}
  ${If} $VaultName == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "Vault Name is required."
    Abort
  ${EndIf}
  ${If} $VaultUsername == ""
    MessageBox MB_OK|MB_ICONEXCLAMATION "Vault Username is required."
    Abort
  ${EndIf}
FunctionEnd

;====================================================================
; Custom page: File Paths
;====================================================================
Function PagePaths
  !insertmacro MUI_HEADER_TEXT "File Paths" "Configure local and network paths used by the Bridge."
  nsDialogs::Create 1018
  Pop $0
  ${If} $0 == error
    Abort
  ${EndIf}

  ${NSD_CreateLabel} 0   0u 100% 12u "Radan Projects Root Path:"
  Pop $0
  ${NSD_CreateText}  0  14u 100% 13u "$RadanPath"
  Pop $hRadanPath

  ${NSD_CreateLabel} 0  38u 100% 12u "Sym Network Share Path:"
  Pop $0
  ${NSD_CreateText}  0  52u 100% 13u "$SymPath"
  Pop $hSymPath

  ${NSD_CreateLabel} 0  76u 100% 12u "BOM Export File Path:"
  Pop $0
  ${NSD_CreateText}  0  90u 100% 13u "$BomPath"
  Pop $hBomPath

  nsDialogs::Show
FunctionEnd

Function PagePathsLeave
  ${NSD_GetText} $hRadanPath $RadanPath
  ${NSD_GetText} $hSymPath   $SymPath
  ${NSD_GetText} $hBomPath   $BomPath
FunctionEnd

;====================================================================
; Install Section
;====================================================================
Section "Install" SecMain

  ;------------------------------------------------------------------
  ; Stop and remove any existing installation
  ;------------------------------------------------------------------
  DetailPrint "Stopping existing task (if present)..."
  nsExec::ExecToLog "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command $\"Stop-ScheduledTask -TaskName '${SERVICE_NAME}' -ErrorAction SilentlyContinue; Unregister-ScheduledTask -TaskName '${SERVICE_NAME}' -Confirm:$$false -ErrorAction SilentlyContinue$\""
  Pop $0
  Sleep 500

  ;------------------------------------------------------------------
  ; Copy files
  ;------------------------------------------------------------------
  SetOutPath "$INSTDIR"
  DetailPrint "Copying files..."
  File /r /x "*.pdb" /x "appsettings.Development.json" /x "logs" "..\HorstMFG.Bridge\bin\Release\net48\*.*"

  ;------------------------------------------------------------------
  ; Write appsettings.json
  ; Backslashes in paths must be doubled for valid JSON.
  ;------------------------------------------------------------------
  DetailPrint "Writing appsettings.json..."
  ${StrRep} $R0 "$RadanPath" "\" "\\"
  ${StrRep} $R1 "$SymPath"   "\" "\\"
  ${StrRep} $R2 "$BomPath"   "\" "\\"

  FileOpen  $9 "$INSTDIR\appsettings.json" w
  FileWrite $9 '{$\n'
  FileWrite $9 '  "Bridge": {$\n'
  FileWrite $9 '    "StationId": $StationId,$\n'
  FileWrite $9 '    "NestingSoftware": "Radan",$\n'
  FileWrite $9 '    "HorstMfgUrl": "$HorstMfgUrl",$\n'
  FileWrite $9 '    "ApiKey": "$ApiKey",$\n'
  FileWrite $9 '    "RadanProjectsRootPath": "$R0",$\n'
  FileWrite $9 '    "SymNetworkSharePath": "$R1",$\n'
  FileWrite $9 '    "BomExportFilePath": "$R2"$\n'
  FileWrite $9 '  },$\n'
  FileWrite $9 '  "Vault": {$\n'
  FileWrite $9 '    "Server": "$VaultServer",$\n'
  FileWrite $9 '    "Vault": "$VaultName",$\n'
  FileWrite $9 '    "Username": "$VaultUsername",$\n'
  FileWrite $9 '    "Password": "$VaultPassword"$\n'
  FileWrite $9 '  },$\n'
  FileWrite $9 '  "Serilog": {$\n'
  FileWrite $9 '    "MinimumLevel": {$\n'
  FileWrite $9 '      "Default": "Information",$\n'
  FileWrite $9 '      "Override": {$\n'
  FileWrite $9 '        "Microsoft": "Warning",$\n'
  FileWrite $9 '        "System": "Warning"$\n'
  FileWrite $9 '      }$\n'
  FileWrite $9 '    }$\n'
  FileWrite $9 '  }$\n'
  FileWrite $9 '}$\n'
  FileClose $9

  ;------------------------------------------------------------------
  ; Register as a Task Scheduler logon task (runs in the user's
  ; interactive session so Radan COM is accessible). RunLevel must be
  ; Highest — at Limited, the Vault Connectivity SDK's COM interop hangs
  ; indefinitely at startup instead of failing cleanly (same class of
  ; issue as HorstMFG.VaultGateway, which needs -RunLevel Highest too).
  ;------------------------------------------------------------------
  DetailPrint "Registering logon task..."
  nsExec::ExecToLog "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command $\"Register-ScheduledTask -TaskName '${SERVICE_NAME}' -Action (New-ScheduledTaskAction -Execute '$INSTDIR\HorstMFG.Bridge.exe' -WorkingDirectory '$INSTDIR') -Trigger (New-ScheduledTaskTrigger -AtLogOn) -Settings (New-ScheduledTaskSettingsSet -ExecutionTimeLimit 0) -RunLevel Highest -Force$\""
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK|MB_ICONEXCLAMATION \
      "Task registration failed (error $0).$\n$\nYou can register it manually via Task Scheduler: run '$INSTDIR\HorstMFG.Bridge.exe' at logon."
  ${Else}
    Sleep 500
    DetailPrint "Starting task..."
    nsExec::ExecToLog "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command $\"Start-ScheduledTask -TaskName '${SERVICE_NAME}'$\""
    Pop $0
  ${EndIf}

  ;------------------------------------------------------------------
  ; Add/Remove Programs registry entry
  ;------------------------------------------------------------------
  WriteRegStr   HKLM "${UNINST_KEY}" "DisplayName"          "${APP_NAME}"
  WriteRegStr   HKLM "${UNINST_KEY}" "UninstallString"       '"$INSTDIR\Uninstall.exe"'
  WriteRegStr   HKLM "${UNINST_KEY}" "QuietUninstallString"  '"$INSTDIR\Uninstall.exe" /S'
  WriteRegStr   HKLM "${UNINST_KEY}" "InstallLocation"      "$INSTDIR"
  WriteRegStr   HKLM "${UNINST_KEY}" "Publisher"            "${PUBLISHER}"
  WriteRegStr   HKLM "${UNINST_KEY}" "DisplayVersion"       "${APP_VERSION}"
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify"             1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair"             1

  WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

;====================================================================
; Vault Item Export extension
; Installs ItemExport into Vault Explorer so "Export Item Direct" writes
; the BOM export file the Bridge forwards to HorstMFG. Skipped when the
; Vault client isn't installed. Only ItemExport's own files are copied;
; the Autodesk assemblies come from the Vault install.
;====================================================================
Section "Vault Item Export" SecItemExport

  ReadEnvStr $R0 PROGRAMDATA
  StrCpy $R1 "$R0\Autodesk\Vault ${VAULT_VERSION}"
  StrCpy $R2 "$R1\Extensions\ItemExport"

  IfFileExists "$R1\*.*" itemexport_check_vault 0
    DetailPrint "Vault ${VAULT_VERSION} client not found; skipping the Item Export extension."
    Goto itemexport_done

  ; Vault Explorer locks the extension DLL while it's open.
  itemexport_check_vault:
  nsExec::ExecToStack "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command $\"if (Get-Process -Name 'Connectivity.VaultPro','Connectivity.VaultWkg','Connectivity.Vault' -ErrorAction SilentlyContinue) { exit 1 }$\""
  Pop $0
  Pop $1
  ${If} $0 == 1
    MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION \
      "Vault Explorer is running. Close it, then click Retry to install the Item Export extension.$\n$\nCancel skips the extension; the rest of the Bridge is still installed." \
      IDRETRY itemexport_check_vault
    DetailPrint "Vault Explorer still open; skipped the Item Export extension."
    Goto itemexport_done
  ${EndIf}

  DetailPrint "Installing the Item Export Vault extension..."
  SetOutPath "$R2"
  File "${ITEMEXPORT_SRC}\ItemExport.dll"
  File "${ITEMEXPORT_SRC}\ItemExport.dll.config"
  File "${ITEMEXPORT_SRC}\ItemExport.vcet.config"
  File "${ITEMEXPORT_SRC}\Microsoft.Web.Services3.dll"

  ; ItemExport reads where to write from ProgramData\VaultExtensions\AppSettings.xml.
  ; VaultItemProcessor shares that file, so an existing one is never touched; a missing
  ; one is created pointing at the Bridge's BOM Export File Path.
  StrCpy $R3 "$R0\VaultExtensions"
  CreateDirectory "$R3"
  IfFileExists "$R3\AppSettings.xml" itemexport_settings_kept 0
    StrCpy $R4 "$BomPath"
    ${If} $R4 == ""
      StrCpy $R4 "$R3\VaultExportData.txt"
    ${EndIf}
    ${GetParent} "$R4" $R5
    CreateDirectory "$R5"
    DetailPrint "Writing $R3\AppSettings.xml..."
    FileOpen  $9 "$R3\AppSettings.xml" w
    FileWrite $9 '<?xml version="1.0" encoding="utf-8"?>$\r$\n'
    FileWrite $9 '<ArrayOfSetting xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">$\r$\n'
    FileWrite $9 '  <Setting>$\r$\n'
    FileWrite $9 '    <Key>VaultExportFilePath</Key>$\r$\n'
    FileWrite $9 '    <Value xsi:type="xsd:string">$R5\</Value>$\r$\n'
    FileWrite $9 '  </Setting>$\r$\n'
    FileWrite $9 '  <Setting>$\r$\n'
    FileWrite $9 '    <Key>ExportFile</Key>$\r$\n'
    FileWrite $9 '    <Value xsi:type="xsd:string">$R4</Value>$\r$\n'
    FileWrite $9 '  </Setting>$\r$\n'
    FileWrite $9 '</ArrayOfSetting>$\r$\n'
    FileClose $9
    Goto itemexport_done

  itemexport_settings_kept:
  DetailPrint "Kept existing $R3\AppSettings.xml (shared with VaultItemProcessor)."

  itemexport_done:
SectionEnd

;====================================================================
; Uninstall Section
;====================================================================
Section "Uninstall"

  DetailPrint "Removing logon task..."
  nsExec::ExecToLog "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command $\"Stop-ScheduledTask -TaskName '${SERVICE_NAME}' -ErrorAction SilentlyContinue; Unregister-ScheduledTask -TaskName '${SERVICE_NAME}' -Confirm:$$false -ErrorAction SilentlyContinue$\""
  Pop $0
  Sleep 500

  DetailPrint "Removing files..."
  RMDir /r "$INSTDIR"

  DeleteRegKey HKLM "${UNINST_KEY}"

SectionEnd
