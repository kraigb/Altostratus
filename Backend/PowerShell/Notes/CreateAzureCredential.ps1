<#
    .SYNOPSIS #    One time set up of credentials file for Azure
    .PARAMETER  $AzurePW #  The SQL Server PW we want to persist on disk securly.
    .EXAMPLE -- see  http://www.powershellcookbook.com/recipe/PukO/securely-store-credentials-on-disk
	.\CreateAzureCredential.ps1 -AzureAccount "joe@example.com" cd -AzurePW "bogus"
#> 
param(
    [CmdletBinding( SupportsShouldProcess=$true)]        # https://technet.microsoft.com/en-us/magazine/ff677563.aspx
    [Parameter(Mandatory = $true)]
    [String]$AzurePW,
	 [Parameter(Mandatory = $true)]
    [String]$AzureAccount)

$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop" 
$credPath = $PSCommandPath + ".credential"

$PWord = ConvertTo-SecureString –String $AzurePW –AsPlainText -Force 
$Credential = New-Object –TypeName System.Management.Automation.PSCredential –ArgumentList $AzureAccount, $PWord
# Create the persisted username and password file. View in browser to see XML and <S N="UserName">user1</S>
$credential | Export-CliXml $credPath
