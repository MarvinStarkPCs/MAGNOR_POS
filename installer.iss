; Script de instalación para MAGNOR POS
; Compilar con Inno Setup: https://jrsoftware.org/isinfo.php

#define MyAppName "MAGNOR POS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "MAGNOR"
#define MyAppExeName "MAGNOR_POS.exe"

[Setup]
; NOTE: El valor de AppId identifica de forma única esta aplicación.
AppId={{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\MAGNOR_POS
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=.\Installer
OutputBaseFilename=MAGNOR_POS_Setup
SetupIconFile=MAGNOR_POS\Assets\Images\favicon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Archivos de la aplicación
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: No usar "Flags: ignoreversion" en archivos compartidos del sistema

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\MAGNOR_POS"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
