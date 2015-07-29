<#
.SYNOPSIS #    creates a sql db given a server name.
.EXAMPLE
    $db = CreateDatabase -DbServerName "myDbServer" -Location "West US" -Credential cred 
#>
function CreateDatabase($DbServerName, $Location, $AppDatabaseName, $Credential)
{
    $context = New-AzureSqlDatabaseServerContext -ServerName $DbServerName -Credential $Credential
    Write-Verbose "Creating database '$AppDatabaseName' in database server $DbServerName."
    New-AzureSqlDatabase -DatabaseName $AppDatabaseName -Context $context -Edition "Basic"
}

$credPath = Join-Path (Split-Path -parent $PSCommandPath) CreateSQLpwCredential.ps1.credential
$SQLcredential = Import-CliXml $credPath

$AppDatabaseName = "rickand" + $(Get-Date -Format ('ddMMhhmm'))
$Location = "West US"
$DbServerName = "rickandwest" 
$db = CreateDatabase -Location $Location -AppDatabaseName $AppDatabaseName `
              -Credential $SQLcredential  -dbs $DbServerName

$context = New-AzureSqlDatabaseServerContext -ServerName $DbServerName -Credential $SQLcredential
Remove-AzureSqlDatabase -ConnectionContext $context -DatabaseName $AppDatabaseName  -Force