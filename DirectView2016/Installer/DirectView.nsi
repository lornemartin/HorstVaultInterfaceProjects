############################################################################################
#      NSIS Installation Script created by NSIS Quick Setup Script Generator v1.09.18
#               Entirely Edited with NullSoft Scriptable Installation System                
#              by Vlasis K. Barkas aka Red Wine red_wine@freemail.gr Sep 2006               
############################################################################################

!define APP_NAME "DirectView"
!define COMP_NAME "Lorne Martin"
!define VERSION "01.05.00.00"
!define COPYRIGHT "Author  © 2019"
!define DESCRIPTION "Vault Viewer"
!define INSTALLER_NAME "M:\temp\DirectView Deployment\DirectView Setup.exe"
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
InstallDir "$APPDATA\Autodesk\Vault 2019\Extensions\DirectView"

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
			File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\AppSettings.xml"
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
		StrCpy $INSTDIR "$APPDATA\Autodesk\Vault 2019\Extensions\DirectView"
		
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
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.Explorer.Extensibility.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.Explorer.Extensibility.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.Explorer.ExtensibilityTools.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.Explorer.ExtensibilityTools.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.Extensibility.Framework.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.Extensibility.Framework.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.WebServices.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.WebServices.Interop.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.Connectivity.WebServices.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.Forms.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.Forms.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.Forms.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.Forms.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Autodesk.DataManagement.Client.Framework.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Data.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Data.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Office.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Pdf.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Pdf.v16.1.Drawing.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Printing.v15.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Printing.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.RichEdit.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Sparkline.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Utils.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.Utils.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraBars.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraBars.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraEditors.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraEditors.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraGrid.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraGrid.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraLayout.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraLayout.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraPrinting.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraPrinting.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraTreeList.v15.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\DevExpress.XtraTreeList.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\log4net.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\log4net.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\Microsoft.Web.Services3.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\PdfSharp.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\PrintPDF.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\PrintPDF.pdb"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultAccess.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultAccess.dll.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultAccess.pdb"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultView.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultView.dll.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultView.pdb"
File "C:\Users\lorne\source\repos\Vault Interface Projects\DirectView2016\bin\Debug\VaultView.vcet.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\AutoHotKey\ShowPDF\ShowPDF.exe"

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
Delete "$INSTDIR\AppSettings.xml"
Delete "$INSTDIR\Autodesk.Connectivity.Explorer.Extensibility.dll"
Delete "$INSTDIR\Autodesk.Connectivity.Explorer.Extensibility.xml"
Delete "$INSTDIR\Autodesk.Connectivity.Explorer.ExtensibilityTools.dll"
Delete "$INSTDIR\Autodesk.Connectivity.Explorer.ExtensibilityTools.xml"
Delete "$INSTDIR\Autodesk.Connectivity.Extensibility.Framework.dll"
Delete "$INSTDIR\Autodesk.Connectivity.Extensibility.Framework.xml"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.dll"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.Interop.dll"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Forms.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Forms.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.Forms.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.Forms.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.xml"
Delete "$INSTDIR\DevExpress.Data.v15.1.dll"
Delete "$INSTDIR\DevExpress.Data.v16.1.dll"
Delete "$INSTDIR\DevExpress.Office.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Pdf.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Pdf.v16.1.Drawing.dll"
Delete "$INSTDIR\DevExpress.Printing.v15.1.Core.dll"
Delete "$INSTDIR\DevExpress.Printing.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.RichEdit.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Sparkline.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Utils.v15.1.dll"
Delete "$INSTDIR\DevExpress.Utils.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraBars.v15.1.dll"
Delete "$INSTDIR\DevExpress.XtraBars.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraEditors.v15.1.dll"
Delete "$INSTDIR\DevExpress.XtraEditors.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraGrid.v15.1.dll"
Delete "$INSTDIR\DevExpress.XtraGrid.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraLayout.v15.1.dll"
Delete "$INSTDIR\DevExpress.XtraLayout.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraPrinting.v15.1.dll"
Delete "$INSTDIR\DevExpress.XtraPrinting.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraTreeList.v15.1.dll"
Delete "$INSTDIR\DevExpress.XtraTreeList.v16.1.dll"
Delete "$INSTDIR\log4net.config"
Delete "$INSTDIR\log4net.dll"
Delete "$INSTDIR\Microsoft.Web.Services3.dll"
Delete "$INSTDIR\PdfSharp.dll"
Delete "$INSTDIR\PrintPDF.dll"
Delete "$INSTDIR\PrintPDF.pdb"
Delete "$INSTDIR\VaultAccess.dll"
Delete "$INSTDIR\VaultAccess.dll.config"
Delete "$INSTDIR\VaultAccess.pdb"
Delete "$INSTDIR\VaultView.dll"
Delete "$INSTDIR\VaultView.dll.config"
Delete "$INSTDIR\VaultView.pdb"
Delete "$INSTDIR\VaultView.vcet.config"
Delete "$INSTDIR\ShowPDF.exe"
 
 
Delete "$INSTDIR\uninstall.exe"
!ifdef WEB_SITE
Delete "$INSTDIR\${APP_NAME} website.url"
!endif

RmDir "$INSTDIR"

DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
DeleteRegValue HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "DirectView"
SectionEnd

######################################################################

