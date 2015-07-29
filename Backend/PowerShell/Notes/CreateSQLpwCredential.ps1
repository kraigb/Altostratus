<#
    .SYNOPSIS #    One time set up of credentials file for SQL Server DB
    .PARAMETER  $SQLsrvPW #  The SQL Server PW we want to persist on disk securly.
    .EXAMPLE -- see  http://www.powershellcookbook.com/recipe/PukO/securely-store-credentials-on-disk
	.\CreateSQLpwCredential.ps1 -SQLsrvPW "bogus"
#> 
param(
    [CmdletBinding( SupportsShouldProcess=$true)]        # https://technet.microsoft.com/en-us/magazine/ff677563.aspx
    [Parameter(Mandatory = $true)]
    [String]$SQLsrvPW,
	 [Parameter(Mandatory = $true)]
    [String]$user
	 )

$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop" 
$credPath = $PSCommandPath + ".credential"

$PWord = ConvertTo-SecureString –String $SQLsrvPW –AsPlainText -Force 
$Credential = New-Object –TypeName System.Management.Automation.PSCredential –ArgumentList $user, $PWord
# Create the persisted username and password file. View in browser to see XML and <S N="UserName">user1</S>
$credential | Export-CliXml $credPath
