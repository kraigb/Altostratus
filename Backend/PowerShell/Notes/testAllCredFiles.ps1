<#  .\testAllCredFiles.ps1 -FB_AppId "1412198239001409" -TwitterConsumerKey "vv8IXu3poCB0nN5zWE9OuFOs5" `
   -GoogClientID "877288051312-51m2kbunflpjk4f80g6bkm66a72viicm.apps.googleusercontent.com"
   #>
param(
    [CmdletBinding( SupportsShouldProcess=$true)]
    [switch]$ResetKeys,

	 [Parameter(Mandatory = $true)] 
	 [string]$FB_AppId,

	 [Parameter(Mandatory = $true)] 
	 [string]$TwitterConsumerKey,

	 [Parameter(Mandatory = $true)] 
	 [string]$GoogClientID,
    [string]$TestName
        )


Function GetAllCredentialFromFile
{
    Param( [String]$CredFile )

	 $credPath = Join-Path (Split-Path -parent $PSCommandPath) $CredFile
	 $Credential = Import-CliXml $credPath
   Return $Credential
}

Function GetCredentialFromFile
{
    Param( [String]$CredFile )

	 $Credential = GetAllCredentialFromFile $CredFile
	 $PW = $Credential.GetNetworkCredential().Password  
	 $user = $Credential.GetNetworkCredential().username # not using this but we could, also good for debug
   Return $PW
}

$VerbosePreference = "Continue" 
if (!$TestName) {
	$TestName =  "rikand" + $(Get-Date -Format ('ddhhmm'))
}
$WebSiteName =  $TestName + "web"
$StorageAccountName = $TestName + 'stor'
$Location = "West US"


# Add-AzureAccount only works with Azure AD
#$cred =  GetAllCredentialFromFile CreateAzureCredential.ps1.credential
# Add-AzureAccount -Credential $cred

if(!$ResetKeys) { # no need to do this if we are just resetting  the storage keys, we know key names and values will be set to "reset"
	$storageAccount = Get-AzureStorageAccount | Where-Object { $_.StorageAccountName -eq $StorageAccountName }
	if($storageAccount -eq $null) 
	{
		 Write-Verbose "Creating storage account '$StorageAccountName'."
		 $storage = New-AzureStorageAccount -StorageAccountName $StorageAccountName -Location $Location 
	}
}

if(!$ResetKeys) { 
	$website = Get-AzureWebsite | Where-Object {$_.Name -eq $WebSiteName }
	if ($website -eq $null) 
	{   
		 Write-Verbose "Creating website '$WebSiteName'." 
		 $website = New-AzureWebsite -Name $WebSiteName -Location $Location 
	}
}

# Construct a storage account app settings hashtable.
if(!$ResetKeys) {
	$storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
	$WJstorVal = "DefaultEndpointsProtocol=https;AccountName=" + $StorageAccountName + "AccountKey=" + $storageAccountKey.Primary
	}
	else
{
	$WJstorVal = "Dummy_AzureWebJobs_Val"
}
	
$storageSettings = @{
	"AzureWebJobsDashboard" = $WJstorVal; 
	"AzureWebJobsStorage"   = $WJstorVal;
	"FB_AppId" = $FB_AppId;
	"FB_AppSecret" = 	GetCredentialFromFile "CreateFBCredential.ps1.credential" ; 
	"GoogClientID" = 	$GoogClientID; 
	"GoogClientSecret" = GetCredentialFromFile "CreateGoogCredential.ps1.credential" ; 
	"TwitterSecret" = 	GetCredentialFromFile "CreateTwitterCredential.ps1.credential" ; 
	"TwitterConsumerKey" = $TwitterConsumerKey
}

 Write-Verbose "storageSettings '$storageSettings'"

#write out the values
$storageSettings.Keys | % { "key = $_ , value = " + $storageSettings.Item($_) }

if($ResetKeys)
{
	foreach($key in $($storageSettings.keys)){
    $storageSettings[$key] = "reset"
	 }
}


Set-AzureWebsite -Name $WebSiteName -AppSettings $storageSettings


