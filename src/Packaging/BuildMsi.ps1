param(
    [string]$SourceExe = "C:\Users\DiJay\.gemini\antigravity\scratch\TitanCalc\bin\TitanCalc.exe",
    [string]$OutputMsi = "C:\Users\DiJay\.gemini\antigravity\scratch\TitanCalc\bin\TitanCalc.msi"
)

Write-Host "Creating enterprise MSI package via native Windows Installer Automation..." -ForegroundColor Cyan

if (-not (Test-Path $SourceExe)) {
    Write-Error "Source binary not found: $SourceExe"
    exit 1
}

$fileInfo = Get-Item $SourceExe
$fileSize = $fileInfo.Length

if (Test-Path $OutputMsi) {
    Remove-Item $OutputMsi -Force
}

try {
    $wi = New-Object -ComObject WindowsInstaller.Installer
    # Mode 1 = msiOpenDatabaseModeCreateDirect
    $db = $wi.OpenDatabase($OutputMsi, 1)

    function Exec-Sql($query) {
        $view = $db.OpenView($query)
        $view.Execute()
        $view.Close()
    }

    # 1. Create Property Table
    Exec-Sql "CREATE TABLE \`Property\` (\`Property\` CHAR(72) NOT NULL, \`Value\` CHAR(0) NOT NULL PRIMARY KEY \`Property\`)"
    Exec-Sql "INSERT INTO \`Property\` (\`Property\`, \`Value\`) VALUES ('ProductCode', '{5B8C248A-8519-4C92-9426-3C6BD7F7A250}')"
    Exec-Sql "INSERT INTO \`Property\` (\`Property\`, \`Value\`) VALUES ('ProductName', 'TitanCalc Scientific Desktop Calculator')"
    Exec-Sql "INSERT INTO \`Property\` (\`Property\`, \`Value\`) VALUES ('ProductVersion', '1.0.0')"
    Exec-Sql "INSERT INTO \`Property\` (\`Property\`, \`Value\`) VALUES ('Manufacturer', 'Titan Systems Architecture')"
    Exec-Sql "INSERT INTO \`Property\` (\`Property\`, \`Value\`) VALUES ('ProductLanguage', '1033')"
    Exec-Sql "INSERT INTO \`Property\` (\`Property\`, \`Value\`) VALUES ('ALLUSERS', '1')"

    # 2. Create Directory Table
    Exec-Sql "CREATE TABLE \`Directory\` (\`Directory\` CHAR(72) NOT NULL, \`Directory_Parent\` CHAR(72), \`DefaultDir\` CHAR(255) NOT NULL PRIMARY KEY \`Directory\`)"
    Exec-Sql "INSERT INTO \`Directory\` (\`Directory\`, \`Directory_Parent\`, \`DefaultDir\`) VALUES ('TARGETDIR', null, 'SourceDir')"
    Exec-Sql "INSERT INTO \`Directory\` (\`Directory\`, \`Directory_Parent\`, \`DefaultDir\`) VALUES ('ProgramFiles64Folder', 'TARGETDIR', 'PFiles64')"
    Exec-Sql "INSERT INTO \`Directory\` (\`Directory\`, \`Directory_Parent\`, \`DefaultDir\`) VALUES ('INSTALLFOLDER', 'ProgramFiles64Folder', 'TitanCalc')"

    # 3. Create Component Table
    Exec-Sql "CREATE TABLE \`Component\` (\`Component\` CHAR(72) NOT NULL, \`ComponentId\` CHAR(38), \`Directory_\` CHAR(72) NOT NULL, \`Attributes\` SHORT NOT NULL, \`Condition\` CHAR(255), \`KeyPath\` CHAR(72) PRIMARY KEY \`Component\`)"
    Exec-Sql "INSERT INTO \`Component\` (\`Component\`, \`ComponentId\`, \`Directory_\`, \`Attributes\`, \`KeyPath\`) VALUES ('TitanCalcComp', '{3A9B2E8C-1456-427A-9A8C-2D881B9A5E12}', 'INSTALLFOLDER', 256, 'TitanCalcFile')"

    # 4. Create Feature Table
    Exec-Sql "CREATE TABLE \`Feature\` (\`Feature\` CHAR(38) NOT NULL, \`Feature_Parent\` CHAR(38), \`Title\` CHAR(64), \`Description\` CHAR(255), \`Display\` SHORT NOT NULL, \`Level\` SHORT NOT NULL, \`Directory_\` CHAR(72), \`Attributes\` SHORT NOT NULL PRIMARY KEY \`Feature\`)"
    Exec-Sql "INSERT INTO \`Feature\` (\`Feature\`, \`Title\`, \`Display\`, \`Level\`, \`Directory_\`, \`Attributes\`) VALUES ('Complete', 'TitanCalc Application', 1, 1, 'INSTALLFOLDER', 0)"

    # 5. Create FeatureComponents Table
    Exec-Sql "CREATE TABLE \`FeatureComponents\` (\`Feature_\` CHAR(38) NOT NULL, \`Component_\` CHAR(72) NOT NULL PRIMARY KEY \`Feature_\`, \`Component_\`)"
    Exec-Sql "INSERT INTO \`FeatureComponents\` (\`Feature_\`, \`Component_\`) VALUES ('Complete', 'TitanCalcComp')"

    # 6. Create File Table
    Exec-Sql "CREATE TABLE \`File\` (\`File\` CHAR(72) NOT NULL, \`Component_\` CHAR(72) NOT NULL, \`FileName\` CHAR(255) NOT NULL, \`FileSize\` LONG NOT NULL, \`Version\` CHAR(72), \`Language\` CHAR(20), \`Attributes\` SHORT, \`Sequence\` SHORT NOT NULL PRIMARY KEY \`File\`)"
    Exec-Sql "INSERT INTO \`File\` (\`File\`, \`Component_\`, \`FileName\`, \`FileSize\`, \`Version\`, \`Language\`, \`Attributes\`, \`Sequence\`) VALUES ('TitanCalcFile', 'TitanCalcComp', 'TitanCalc.exe', $fileSize, '1.0.0.0', '1033', 512, 1)"

    # 7. Create Media Table
    Exec-Sql "CREATE TABLE \`Media\` (\`DiskId\` SHORT NOT NULL, \`LastSequence\` SHORT NOT NULL, \`DiskPrompt\` CHAR(64), \`Cabinet\` CHAR(255), \`VolumeLabel\` CHAR(32), \`Source\` CHAR(72) PRIMARY KEY \`DiskId\`)"
    Exec-Sql "INSERT INTO \`Media\` (\`DiskId\`, \`LastSequence\`, \`Cabinet\`) VALUES (1, 1, '#Data1.cab')"

    # 8. Create InstallExecuteSequence Table
    Exec-Sql "CREATE TABLE \`InstallExecuteSequence\` (\`Action\` CHAR(72) NOT NULL, \`Condition\` CHAR(255), \`Sequence\` SHORT PRIMARY KEY \`Action\`)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('CostInitialize', 800)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('FileCost', 900)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('CostFinalize', 1000)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('InstallValidate', 1400)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('InstallInitialize', 1500)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('ProcessComponents', 1600)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('InstallFiles', 4000)"
    Exec-Sql "INSERT INTO \`InstallExecuteSequence\` (\`Action\`, \`Sequence\`) VALUES ('InstallFinalize', 6600)"

    $db.Commit()

    Write-Host "Successfully generated production MSI installer: $OutputMsi" -ForegroundColor Green
    $msiItem = Get-Item $OutputMsi
    Write-Host "MSI Package Size: $([math]::Round($msiItem.Length / 1024, 2)) KB" -ForegroundColor Green
}
catch {
    Write-Error "Failed to build MSI: $_"
}
