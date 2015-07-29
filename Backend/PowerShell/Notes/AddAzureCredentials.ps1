<#
.SYNOPSIS #    Adds Azure credentials from a file
#>

$credPath = Join-Path (Split-Path -parent $PSCommandPath) CreateAzureCredential.ps1.credential
$cred = Import-CliXml $credPath
Add-AzureAccount -Credential $cred

