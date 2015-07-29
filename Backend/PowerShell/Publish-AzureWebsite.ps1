<#
    .SYNOPSIS #    Publishes a Windows Azure website project
    .PARAMETER  ProjectFile #    Specifies the .csproj file of the project that you    want to deploy. 
    .EXAMPLE -- web site publishes fine, WJ doesn't 
	./Publish-AzureWebsite.ps1 -ProjectFile "..\Altostratus.Website\Altostratus.Website.csproj"  
    ./Publish-AzureWebsite.ps1 -ProjectFile "..\Altostratus.WebJob\Altostratus.WebJob.csproj"
#> 
Param(
    [Parameter(Mandatory = $true)]
    [String]$ProjectFile,
    [Switch]$Launch
)

# Set the output level to verbose and make the script stop on error
$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop" 
$scriptPath = Split-Path -parent $PSCommandPath

# Read from website-environment.xml to get the environment name
[Xml]$envXml = Get-Content $scriptPath\website-environment.xml
if (!$envXml) {throw "Error: Cannot find the website-environment.xml in $scriptPath"}
$websiteName = $envXml.environment.website.name
# Build and publish the project via web deploy package using msbuild.exe 
Write-Verbose ("[Start] deploying to Windows Azure website {0}" -f $websiteName)

# Read from the publish settings file to get the deploy password
$publishXmlFile = Join-Path $scriptPath -ChildPath ($websiteName + ".pubxml")
[Xml]$xml = Get-Content $scriptPath\$websiteName.publishsettings
if (!$xml) {throw "Error: Cannot find a publishsettings file for the $website web site in $scriptPath."}
$password = $xml.publishData.publishProfile.userPWD[0]

# Run MSBuild to publish the project - the & (ampersand) in Powershell it is a way of executing a string
# $env:windir is usually c:\windows
& "$env:windir\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" $ProjectFile `
    /p:VisualStudioVersion=12.0 `
    /p:DeployOnBuild=true `
    /p:PublishProfile=$publishXmlFile `
    /p:Password=$password

Write-Verbose "[Finish] deploying to Windows Azure website $websiteName"

Show-AzureWebsite -Name $websiteName