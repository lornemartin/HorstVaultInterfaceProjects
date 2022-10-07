############################################################################################
#      NSIS Installation Script created by NSIS Quick Setup Script Generator v1.09.18
#               Entirely Edited with NullSoft Scriptable Installation System                
#              by Vlasis K. Barkas aka Red Wine red_wine@freemail.gr Sep 2006               
############################################################################################

!define APP_NAME "DirectView 2023"
!define COMP_NAME "Lorne Martin"
!define VERSION "01.50.00.00"
!define COPYRIGHT "Author  © 2023"
!define DESCRIPTION "Vault Viewer"
!define INSTALLER_NAME "M:\Installers\Custom Code 2023 Deployment Files\DirectView\DirectView Setup.exe"
!define INSTALL_TYPE "SetShellVarContext all"
!define REG_ROOT "HKLM"
!define UNINSTALL_PATH "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

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
InstallDirRegKey "${REG_ROOT}" "${UNINSTALL_PATH}" "UninstallString"
InstallDir "$APPDATA\Autodesk\Vault 2023\Extensions\DirectView"

######################################################################


######################################################################

Section -Additional
	
SectionEnd


######################################################################

!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

!insertmacro MUI_PAGE_WELCOME

!ifdef LICENSE_TXT
!insertmacro MUI_PAGE_LICENSE "${LICENSE_TXT}"
!endif

!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

######################################################################

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
			File "C:\Users\lorne\source\repos\HorstVaultInterfaceProjects\VaultItemProcessor\VaultItemProcessor\AppSettings.xml"
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



		; This is important to have $APPDATA variable
		; point to ProgramData folder
		; instead of current user's Roaming folder
		SetShellVarContext all
		StrCpy $INSTDIR "$APPDATA\Autodesk\Vault 2023\Extensions\DirectView"
		
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

Function .onInstSuccess
        ExecShell "open" "$INSTDIR\ShowPDF.exe"
FunctionEnd




Section -MainProgram
${INSTALL_TYPE}
SetOverwrite ifnewer
SetOutPath "$INSTDIR"
File "C:\Users\lorne\source\repos\HorstVaultInterfaceProjects\DirectView2016\bin\Release\*.*"
File "C:\Users\lorne\source\repos\HorstVaultInterfaceProjects\AutoHotKey\ShowPDF\ShowPDF.exe"

SectionEnd

######################################################################

Section -Icons_Reg
SetOutPath "$INSTDIR"
WriteUninstaller "$INSTDIR\uninstall.exe"

WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayName" "${APP_NAME}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "UninstallString" "$INSTDIR\uninstall.exe"
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

DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
DeleteRegValue HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "DirectView"
SectionEnd

######################################################################

