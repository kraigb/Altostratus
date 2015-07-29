<#
    .SYNOPSIS #    One time set up of credentials file for Goog
    .EXAMPLE -- see  http://www.powershellcookbook.com/recipe/PukO/securely-store-credentials-on-disk
#> 
param(
    [CmdletBinding( SupportsShouldProcess=$true)]        # https://technet.microsoft.com/en-us/magazine/ff677563.aspx
    [Parameter(Mandatory = $true)]
    [String]$Password)

$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop" 
$credPath = $PSCommandPath + ".credential"

$PWord = ConvertTo-SecureString –String $Password –AsPlainText -Force 
$Credential = New-Object –TypeName System.Management.Automation.PSCredential –ArgumentList "GoogClientSecret", $PWord
# Create the persisted username and password file. View in browser to see XML
$credential | Export-CliXml $credPath
