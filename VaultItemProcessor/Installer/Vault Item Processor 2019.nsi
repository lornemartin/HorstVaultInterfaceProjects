############################################################################################
#      NSIS Installation Script created by NSIS Quick Setup Script Generator v1.09.18
#               Entirely Edited with NullSoft Scriptable Installation System                
#              by Vlasis K. Barkas aka Red Wine red_wine@freemail.gr Sep 2006               
############################################################################################

!define APP_NAME "Vault Item Processor 2019"
!define COMP_NAME "Horst Welding"
!define VERSION "3.00.00.00"
!define COPYRIGHT "Lorne Martin  © 2019"
!define DESCRIPTION ""
!define INSTALLER_NAME "M:\temp\VaultItemProcessor\Vault Item Processor Setup.exe"
!define MAIN_APP_EXE "Vault Item Processor 2019.exe"
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
InstallDir "$PROGRAMFILES\Vault Item Processor 2019"

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
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "Vault Item Processor 2019"
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

######################################################################

Section -MainProgram
${INSTALL_TYPE}
SetOverwrite ifnewer
SetOutPath "$INSTDIR"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.Connectivity.Extensibility.Framework.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.Connectivity.Extensibility.Framework.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.Connectivity.WebServices.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.Connectivity.WebServices.Interop.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.Connectivity.WebServices.WCF.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.Connectivity.WebServices.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.Forms.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.Forms.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.Forms.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.Forms.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Autodesk.DataManagement.Client.Framework.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Data.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Data.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Data.v17.2.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Images.v17.2.dll"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Office.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Office.v17.2.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Office.v17.2.Core.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Pdf.v16.1.Core.dll"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Pdf.v16.1.Drawing.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Pdf.v17.2.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Pdf.v17.2.Core.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Pdf.v17.2.Drawing.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Pdf.v17.2.Drawing.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Printing.v16.1.Core.dll"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Printing.v17.2.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Printing.v17.2.Core.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.RichEdit.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.RichEdit.v17.2.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.RichEdit.v17.2.Core.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.RichEdit.v17.2.Export.dll"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Sparkline.v16.1.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Sparkline.v17.2.Core.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Sparkline.v17.2.Core.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Utils.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Utils.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.Utils.v17.2.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraBars.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraBars.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraBars.v17.2.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraEditors.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraEditors.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraEditors.v17.2.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraGrid.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraGrid.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraGrid.v17.2.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraLayout.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraLayout.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraLayout.v17.2.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraPdfViewer.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraPdfViewer.v17.2.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraPrinting.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraPrinting.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraPrinting.v17.2.xml"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraTreeList.v16.1.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraTreeList.v17.2.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\DevExpress.XtraTreeList.v17.2.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\log4net.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\log4net.dll"
;File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\log4net.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\MigraDoc.DocumentObjectModel.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\MigraDoc.DocumentObjectModel.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\MigraDoc.Rendering.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\MigraDoc.Rendering.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\MigraDoc.RtfRendering.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\MigraDoc.RtfRendering.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Npgsql.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Npgsql.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\PdfSharp.Charting.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\PdfSharp.Charting.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\PdfSharp.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\PdfSharp.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\PrintPDF.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\PrintPDF.pdb"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\ProcessPDF.cs"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\System.Threading.Tasks.Extensions.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\System.Threading.Tasks.Extensions.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Vault Item Processor 2019.exe"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Vault Item Processor 2019.exe.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\Vault Item Processor 2019.pdb"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\VaultAccess.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\VaultAccess.dll.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\VaultItemProcessor\bin\Debug\VaultAccess.pdb"
File "C:\Users\lorne\source\repos\Vault Interface Projects\Vault 2019 SDK Binaries\x64\clmloader.dll"

SetOutPath "$APPDATA\Autodesk\Vault 2019\Extensions\ItemExport"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\AppSettings.xml"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.Connectivity.Explorer.Extensibility.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.Connectivity.Explorer.ExtensibilityTools.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.Connectivity.Extensibility.Framework.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.Connectivity.WebServices.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.Connectivity.WebServices.Interop.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.DataManagement.Client.Framework.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.DataManagement.Client.Framework.Forms.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\Autodesk.DataManagement.Client.Framework.Vault.Forms.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\ItemExport.dll"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\ItemExport.dll.config"
File "C:\Users\lorne\source\repos\Vault Interface Projects\ItemExport\bin\Debug\ItemExport.vcet.config"

SectionEnd

######################################################################

Section -Additional
SetShellVarContext all
SetOutPath "$APPDATA\VaultExtensions"

IfFileExists $INSTDIR\VaultExtensions\AppSettings.xml 0 config_file_not_found
     DetailPrint "Skipping config file because it already exists"
     goto end_of_test

config_file_not_found:
	DetailPrint "Writing config file"
     File "C:\Users\lorne\source\repos\Vault Interface Projects\VaultItemProcessor\AppSettings.xml"
end_of_test:

;SetOutPath "$APPDATA\Autodesk\Vault 2019\Extensions\ItemExport"

SetShellVarContext all
SetOutPath "$APPDATA\Autodesk\Vault 2019\Extensions\DirectView"
File "C:\Users\lorne\source\repos\Vault Interface Projects\AutoHotKey\ShowPDF\ShowPDF.exe"
SectionEnd

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
CreateDirectory "$SMPROGRAMS\Vault Item Processor 2019"
CreateShortCut "$SMPROGRAMS\Vault Item Processor 2019\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\Vault Item Processor 2019\Uninstall ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\Vault Item Processor 2019\${APP_NAME} Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!endif

WriteRegStr ${REG_ROOT} "${REG_APP_PATH}" "" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayName" "${APP_NAME}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "UninstallString" "$INSTDIR\uninstall.exe"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayIcon" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayVersion" "${VERSION}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "Publisher" "${COMP_NAME}"

!ifdef WEB_SITE
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "URLInfoAbout" "${WEB_SITE}"
!endif
SectionEnd

######################################################################

Section Uninstall
${INSTALL_TYPE}
Delete "$INSTDIR\Autodesk.Connectivity.Extensibility.Framework.dll"
Delete "$INSTDIR\Autodesk.Connectivity.Extensibility.Framework.xml"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.dll"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.Interop.dll"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.WCF.dll"
Delete "$INSTDIR\Autodesk.Connectivity.WebServices.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Forms.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Forms.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.Forms.dll"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.Forms.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.Vault.xml"
Delete "$INSTDIR\Autodesk.DataManagement.Client.Framework.xml"
Delete "$INSTDIR\DevExpress.Data.v16.1.dll"
Delete "$INSTDIR\DevExpress.Data.v17.2.dll"
Delete "$INSTDIR\DevExpress.Data.v17.2.xml"
Delete "$INSTDIR\DevExpress.Images.v17.2.dll"
Delete "$INSTDIR\DevExpress.Office.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Office.v17.2.Core.dll"
Delete "$INSTDIR\DevExpress.Office.v17.2.Core.xml"
Delete "$INSTDIR\DevExpress.Pdf.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Pdf.v16.1.Drawing.dll"
Delete "$INSTDIR\DevExpress.Pdf.v17.2.Core.dll"
Delete "$INSTDIR\DevExpress.Pdf.v17.2.Core.xml"
Delete "$INSTDIR\DevExpress.Pdf.v17.2.Drawing.dll"
Delete "$INSTDIR\DevExpress.Pdf.v17.2.Drawing.xml"
Delete "$INSTDIR\DevExpress.Printing.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Printing.v17.2.Core.dll"
Delete "$INSTDIR\DevExpress.Printing.v17.2.Core.xml"
Delete "$INSTDIR\DevExpress.RichEdit.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.RichEdit.v17.2.Core.dll"
Delete "$INSTDIR\DevExpress.RichEdit.v17.2.Core.xml"
Delete "$INSTDIR\DevExpress.RichEdit.v17.2.Export.dll"
Delete "$INSTDIR\DevExpress.Sparkline.v16.1.Core.dll"
Delete "$INSTDIR\DevExpress.Sparkline.v17.2.Core.dll"
Delete "$INSTDIR\DevExpress.Sparkline.v17.2.Core.xml"
Delete "$INSTDIR\DevExpress.Utils.v16.1.dll"
Delete "$INSTDIR\DevExpress.Utils.v17.2.dll"
Delete "$INSTDIR\DevExpress.Utils.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraBars.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraBars.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraBars.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraEditors.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraEditors.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraEditors.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraGrid.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraGrid.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraGrid.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraLayout.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraLayout.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraLayout.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraPdfViewer.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraPdfViewer.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraPrinting.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraPrinting.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraPrinting.v17.2.xml"
Delete "$INSTDIR\DevExpress.XtraTreeList.v16.1.dll"
Delete "$INSTDIR\DevExpress.XtraTreeList.v17.2.dll"
Delete "$INSTDIR\DevExpress.XtraTreeList.v17.2.xml"
Delete "$INSTDIR\log4net.config"
Delete "$INSTDIR\log4net.dll"
Delete "$INSTDIR\log4net.xml"
Delete "$INSTDIR\MigraDoc.DocumentObjectModel.dll"
Delete "$INSTDIR\MigraDoc.DocumentObjectModel.xml"
Delete "$INSTDIR\MigraDoc.Rendering.dll"
Delete "$INSTDIR\MigraDoc.Rendering.xml"
Delete "$INSTDIR\MigraDoc.RtfRendering.dll"
Delete "$INSTDIR\MigraDoc.RtfRendering.xml"
Delete "$INSTDIR\Npgsql.dll"
Delete "$INSTDIR\Npgsql.xml"
Delete "$INSTDIR\PdfSharp.Charting.dll"
Delete "$INSTDIR\PdfSharp.Charting.xml"
Delete "$INSTDIR\PdfSharp.dll"
Delete "$INSTDIR\PdfSharp.xml"
Delete "$INSTDIR\PrintPDF.dll"
Delete "$INSTDIR\PrintPDF.pdb"
Delete "$INSTDIR\ProcessPDF.cs"
Delete "$INSTDIR\System.Threading.Tasks.Extensions.dll"
Delete "$INSTDIR\System.Threading.Tasks.Extensions.xml"
Delete "$INSTDIR\Vault Item Processor 2019.exe"
Delete "$INSTDIR\Vault Item Processor 2019.exe.config"
Delete "$INSTDIR\Vault Item Processor 2019.pdb"
Delete "$INSTDIR\VaultAccess.dll"
Delete "$INSTDIR\VaultAccess.dll.config"
Delete "$INSTDIR\VaultAccess.pdb"
Delete "$INSTDIR\uninstall.exe"
Delete "$INSTDIR\clmloader.dll"
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
Delete "$SMPROGRAMS\Vault Item Processor 2019\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\Vault Item Processor 2019\Uninstall ${APP_NAME}.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\Vault Item Processor 2019\${APP_NAME} Website.lnk"
!endif
RmDir "$SMPROGRAMS\Vault Item Processor 2019"
!endif

DeleteRegKey ${REG_ROOT} "${REG_APP_PATH}"
DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
SectionEnd

######################################################################

