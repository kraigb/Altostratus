<#
    .SYNOPSIS #    deletes Azure resources
    # WARNING, the ultra-bizarre nature of PS is such that if you
    ./MyRemoveScript -Detlet $true -nameToMatch "Joe", it will set nameToMatch to True
    #switch works by converting the arg switch to true or false, that is -Delete  # makes it true
    #and if you don't pass -Delete, it's false

#> 
Param(
    [switch]$Delete,
    [String]$nameToMatch = 'stratus'
)

#see http://stackoverflow.com/questions/1485215/powershell-how-to-grep-command-output
# <azure command> | Get-Member 

# Create a PSCrendential object from plain text password.
# The PS Credential object will be used to create a database context, which will be used to create database.
Function New-PSCredentialFromPlainText
{
    Param(
        [String]$UserName,
        [String]$Password
    )

    $securePassword = ConvertTo-SecureString -String $Password -AsPlainText -Force

    Return New-Object System.Management.Automation.PSCredential($UserName, $securePassword)
}

$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop" 


$ws = Get-AzureWebsite | ?{$_.Name.StartsWith($nameToMatch ) }
if ($ws){
    foreach ($var in $ws) {
        $wsName = $var.Name
        Write-Verbose "removing $wsName"
        if($Delete) {  Remove-AzureWebsite -Force $wsName }
     }
}

<#
$sqlServerRM = Get-AzureSqlDatabaseServer | ?{$_.AdministratorLogin -match 'rickand'}
# since I'm not create DB servers anymore, this shouldn't happen

if ($sqlServerRM){
    $sqlName99 = $sqlServerRM.ServerName
    Write-Verbose "removing $sqlName99 "
    if($Delete){ Remove-AzureSqlDatabaseServer -Force -ServerName  $sqlServerRM.ServerName }
}
#>

$asa = Get-AzureStorageAccount | ?{$_.Label.StartsWith($nameToMatch ) }

if ($asa){
    foreach ($var2 in $asa) {
        $storName99 = $var2.Label
        Write-Verbose "removing StorageAccountName $storName99 "
       if($Delete) {  Remove-AzureStorageAccount -StorageAccountName $storName99 }
   }
}

$credential = New-PSCredentialFromPlainText -UserName 'rickand' -Password 'Pa$$w0rd'
$context = New-AzureSqlDatabaseServerContext -ServerName "rickand" -Credential $Credential
$db2rm = Get-AzureSqlDatabase -ConnectionContext $context |  ?{$_.Name -match $nameToMatch }

# This workss #Remove-AzureSqlDatabase $context –DatabaseName 'appdb'  -Force

if($db2rm ){
    foreach ($var3 in $db2rm) {
    $dbNameX = $var3.Name
    Write-Verbose "removing $dbNameX"
     if($Delete){ Remove-AzureSqlDatabase -ConnectionContext $context -DatabaseName $dbNameX  -Force }
    }
}
