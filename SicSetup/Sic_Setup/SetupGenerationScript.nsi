;NSIS Modern User Interface
;Basic Example Script
;Written by Joost Verburg

;--------------------------------
;Include Modern UI

  !include "MUI2.nsh"

;--------------------------------
;General

  !getdllversion "sicui\sicui.exe" ver

  ;Name and file
  Name "Sic ${ver1}.${ver2}.${ver3}.${ver4}"
  OutFile "Sic04_Setup_v${ver1}.${ver2}.${ver3}.${ver4}.exe"

  ;Default installation folder
  ;InstallDir "$LOCALAPPDATA\Modern UI Test"
  InstallDir "C:\Sic"
  
  ;Get installation folder from registry if available
  InstallDirRegKey HKCU "Software\Sicentury\CVD" ""

  ;Request application privileges for Windows Vista
  RequestExecutionLevel user

;--------------------------------
;Interface Settings

  !define MUI_ABORTWARNING

;--------------------------------
;Pages

  !insertmacro MUI_PAGE_LICENSE "License.txt"
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  
;--------------------------------
;Languages
 
  !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "All" SecAll

  SetOutPath "$INSTDIR"
  
  ;ADD YOUR OWN FILES HERE...
  SetOutPath "$INSTDIR\SicUI"
  File /r "SicUI\*"

  SetOutPath "$INSTDIR\SicRT"
  File /r "SicRT\*"

  ;Store installation folder
  WriteRegStr HKCU "Software\Sicentury\CVD" "" $INSTDIR
  
  ;Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateShortCut "$DESKTOP\SicRTIPConfig.lnk" "$INSTDIR\SicUI\SicUI.exe" "--is-set-rt-ip" "$INSTDIR\SicUI\RtIpAddressSetting.ico" 0
  CreateShortCut "$DESKTOP\SicUI.lnk" "$INSTDIR\SicUI\SicUI.exe"

SectionEnd

;--------------------------------
;Descriptions

  ;Language strings
  LangString DESC_SecAll ${LANG_ENGLISH} "Install SicRT and SicUI."

  ;Assign language strings to sections
  !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${SecAll} $(DESC_SecAll)
  !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"

  ;ADD YOUR OWN FILES HERE...

  Delete "$INSTDIR\Uninstall.exe"
  Delete "$DESKTOP\SicRTIPConfig.lnk"
  Delete "$DESKTOP\SicUI.lnk"

  RMDir /r "$INSTDIR\SicUI"
  RMDir /r "$INSTDIR\SicRT"

  DeleteRegKey /ifempty HKCU "Software\Sicentury\CVD"

SectionEnd
