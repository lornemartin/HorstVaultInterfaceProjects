############################################################################################
#      NSIS Installation Script created by NSIS Quick Setup Script Generator v1.09.18
#               Entirely Edited with NullSoft Scriptable Installation System                
#              by Vlasis K. Barkas aka Red Wine red_wine@freemail.gr Sep 2006               
############################################################################################

!define APP_NAME "Vault Item Processor 2021.1"
!define COMP_NAME "Horst Welding"
!define VERSION "2021.01.00.00"
!define COPYRIGHT "Lorne Martin  © 2021"
!define DESCRIPTION ""
!define INSTALLER_NAME "M:\Custom Code 2021 Deployment Files\VaultItemProcessor\Vault Item Processor Setup 2021.1.exe"
!define MAIN_APP_EXE "Vault Item Processor 2021.1.exe"
!define INSTALL_TYPE "SetShellVarContext all"
!define REG_ROOT "HKLM"
!define REG_APP_PATH "Software\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
!define UNINSTALL_PATH "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_UNFINISHPAGE_NOAUTOCLOSE

######################################################################

VIProductVersion  "${VERSION}"
VIAddVersionKey "ProductName"  "${APP_NAME}"
VIAddVersionKey "CompanyName"  "${COMP_NAME}"
VIAddVersionKey "LegalCopyright"  "${COPYRIGHT}"
VIAddVersionKey "FileDescription"  "${DESCRIPTION}"
VIAddVersionKey "FileVersion"  "${VERSION}"

######################################################################

SetCompressor ZLIB
Name "${APP_NAME}"
Caption "${APP_NAME}"
OutFile "${INSTALLER_NAME}"
BrandingText "${APP_NAME}"
XPStyle on
InstallDirRegKey "${REG_ROOT}" "${REG_APP_PATH}" ""
InstallDir "$PROGRAMFILES\Vault Item Processor 2021.1"

######################################################################

!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

!insertmacro MUI_PAGE_WELCOME

!ifdef LICENSE_TXT
!insertmacro MUI_PAGE_LICENSE "${LICENSE_TXT}"
!endif

!ifdef REG_START_MENU
!define MUI_STARTMENUPAGE_NODISABLE
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "Vault Item Processor 2021.1"
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "${REG_ROOT}"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "${UNINSTALL_PATH}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "${REG_START_MENU}"
!insertmacro MUI_PAGE_STARTMENU Application $SM_Folder
!endif

!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$INSTDIR\${MAIN_APP_EXE}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Function .onInit
		; This is important to have $APPDATA variable
		; point to ProgramData folder
		; instead of current user's Roaming folder
		SetShellVarContext all
		SetOutPath "$APPDATA\VaultExtensions"
		StrCpy $INSTDIR "$APPDATA\VaultExtensions"
		IfFileExists $APPDATA\VaultExtensions\AppSettings.xml 0 config_file_not_found
			DetailPrint "Skipping config file because it already exists"
			goto end_of_test

		config_file_not_found:
			DetailPrint "Writing config file"
			File "C:\Users\lorne\OneDrive - Horst Welding\Documents\source\repos\Vault Interface Projects\VaultItemProcessor\AppSettings.xml"
		end_of_test:
		
		# Make the directory "$INSTDIR" read write accessible by all users
		CreateDirectory $INSTDIR
		AccessControl::GrantOnFile "$INSTDIR" "(BU)" "GenericRead + GenericWrite"
		Pop $R0
		${If} $R0 == error
			Pop $R0
			MessageBox MB_OK `AccessControl error: $R0, $\r$\n Installation will be aborted.`
			Quit
		${EndIf}
FunctionEnd


######################################################################

Section -MainProgram
${INSTALL_TYPE}
SetOverwrite ifnewer

StrCpy $INSTDIR "$PROGRAMFILES\Vault Item Processor 2021.1"
SetOutPath "$INSTDIR"

File "C:\Users\lorne\OneDrive - Horst Welding\Documents\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\*.*"
File "C:\Users\lorne\OneDrive - Horst Welding\Documents\source\repos\Vault Interface Projects\Vault 2021 SDK Binaries\AdskLicensingSDK_3.dll"

SetOutPath "$APPDATA\Autodesk\Vault 2021\Extensions\ItemExport"
File "C:\Users\lorne\OneDrive - Horst Welding\Documents\source\repos\Vault Interface Projects\ItemExport\bin\Debug\*.*"

SetShellVarContext all
SetOutPath "$APPDATA\Autodesk\Vault 2021\Extensions\DirectView"
File "C:\Users\lorne\OneDrive - Horst Welding\Documents\source\repos\Vault Interface Projects\AutoHotKey\ShowPDF\ShowPDF.exe"

SectionEnd

######################################################################




######################################################################

Section -Icons_Reg
SetOutPath "$INSTDIR"
WriteUninstaller "$INSTDIR\uninstall.exe"

!ifdef REG_START_MENU
!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
CreateDirectory "$SMPROGRAMS\$SM_Folder"
CreateShortCut "$SMPROGRAMS\$SM_Folder\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\$SM_Folder\Uninstall ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\$SM_Folder\${APP_NAME} Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!insertmacro MUI_STARTMENU_WRITE_END
!endif

!ifndef REG_START_MENU
CreateDirectory "$SMPROGRAMS\Vault Item Processor 2021.1"
CreateShortCut "$SMPROGRAMS\Vault Item Processor 2021.1\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\Vault Item Processor 2021.1\Uninstall ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\Vault Item Processor 2021.1\${APP_NAME} Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!endif

WriteRegStr ${REG_ROOT} "${REG_APP_PATH}" "" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayName" "${APP_NAME}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "UninstallString" "$INSTDIR\uninstall.exe"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayIcon" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayVersion" "${VERSION}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "Publisher" "${COMP_NAME}"

WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "DirectView" "$INSTDIR\ShowPDF.exe"

!ifdef WEB_SITE
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "URLInfoAbout" "${WEB_SITE}"
!endif
SectionEnd

######################################################################

Section Uninstall
${INSTALL_TYPE}


Delete "$INSTDIR\*.*"

!ifdef WEB_SITE
Delete "$INSTDIR\${APP_NAME} website.url"
!endif

RmDir "$INSTDIR"

;!ifndef NEVER_UNINSTALL
;Delete "$APPDATA\${APP_NAME}\AppSettings.xml"
 
;RmDir "$APPDATA\${APP_NAME}"
;!endif

!ifdef REG_START_MENU
!insertmacro MUI_STARTMENU_GETFOLDER "Application" $SM_Folder
Delete "$SMPROGRAMS\$SM_Folder\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\$SM_Folder\Uninstall ${APP_NAME}.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\$SM_Folder\${APP_NAME} Website.lnk"
!endif
RmDir "$SMPROGRAMS\$SM_Folder"
!endif

!ifndef REG_START_MENU
Delete "$SMPROGRAMS\Vault Item Processor 2021.1\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\Vault Item Processor 2021.1\Uninstall ${APP_NAME}.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\Vault Item Processor 2021\${APP_NAME} Website.lnk"
!endif
RmDir "$SMPROGRAMS\Vault Item Processor 2021.1"
!endif

DeleteRegKey ${REG_ROOT} "${REG_APP_PATH}"
DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
SectionEnd

######################################################################

