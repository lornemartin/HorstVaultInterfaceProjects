; HorstMFG Bridge Installer
; Requires NSIS 3.x  (https://nsis.sourceforge.io/)
;
; Build:
;   makensis installer.nsi
;
; The script expects the Release build to already exist at:
;   ..\HorstMFG.Bridge\bin\Release\net48\
; (relative to this .nsi file)

!define APP_NAME        "HorstMFG Bridge"
!define SERVICE_NAME    "HorstMFGBridge"
!define SERVICE_DISPLAY "Horst MFG Bridge"
!define APP_VERSION     "1.0.0"
!define PUBLISHER       "Horst Manufacturing"
!define UNINST_KEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SERVICE_NAME}"

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
  StrCpy $StationId    "0"
  StrCpy $HorstMfgUrl  "https://hwvsweb01.hw.horst.com"
  StrCpy $ApiKey       ""
  StrCpy $VaultServer  "HWVSVT04"
  StrCpy $VaultName    "Vault"
  StrCpy $VaultUsername ""
  StrCpy $VaultPassword ""
  StrCpy $RadanPath    "C:\Radan Projects"
  StrCpy $SymPath      "S:\Radan Sym Files\Vault Sym Files"
  StrCpy $BomPath      "C:\ProgramData\VaultExtensions\VaultExportData.txt"
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
  DetailPrint "Stopping existing service (if present)..."
  nsExec::ExecToLog '"$SYSDIR\sc.exe" stop "${SERVICE_NAME}"'
  Pop $0
  Sleep 3000
  nsExec::ExecToLog '"$SYSDIR\sc.exe" delete "${SERVICE_NAME}"'
  Pop $0
  Sleep 1000

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
  ; Register Windows Service (Automatic start)
  ;------------------------------------------------------------------
  DetailPrint "Registering Windows service..."
  nsExec::ExecToLog '"$SYSDIR\sc.exe" create "${SERVICE_NAME}" binPath= "$INSTDIR\HorstMFG.Bridge.exe" DisplayName= "${SERVICE_DISPLAY}" start= auto'
  Pop $0
  ${If} $0 != 0
    MessageBox MB_OK|MB_ICONEXCLAMATION \
      "Service registration failed (error $0).$\n$\nYou can register it manually:$\n  sc.exe create ${SERVICE_NAME} binPath= $\"$INSTDIR\HorstMFG.Bridge.exe$\" DisplayName= $\"${SERVICE_DISPLAY}$\" start= auto"
  ${Else}
    Sleep 500
    DetailPrint "Starting service..."
    nsExec::ExecToLog '"$SYSDIR\sc.exe" start "${SERVICE_NAME}"'
    Pop $0
    ${If} $0 != 0
      MessageBox MB_ICONINFORMATION|MB_OK \
        "Service was registered but could not be started (error $0).$\nYou can start it from the Windows Services console."
    ${EndIf}
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
; Uninstall Section
;====================================================================
Section "Uninstall"

  DetailPrint "Stopping service..."
  nsExec::ExecToLog '"$SYSDIR\sc.exe" stop "${SERVICE_NAME}"'
  Pop $0
  Sleep 3000

  DetailPrint "Deleting service..."
  nsExec::ExecToLog '"$SYSDIR\sc.exe" delete "${SERVICE_NAME}"'
  Pop $0
  Sleep 1000

  DetailPrint "Removing files..."
  RMDir /r "$INSTDIR"

  DeleteRegKey HKLM "${UNINST_KEY}"

SectionEnd
