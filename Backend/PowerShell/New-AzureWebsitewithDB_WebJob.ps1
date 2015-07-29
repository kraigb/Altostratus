<#
.SYNOPSIS 
	This script is for unattended backup, therefore you must pass in secrets via ".credential files". 
    Creates a Windows Azure Website and links to a SQL Azure DB and a storage account.  
.DESCRIPTION 
   Creates a new website and a new SQL Azure server and database. If you don't specify a DB server, one
	will be created. If the storage account  specified doesn't exist, it will create a new storage account.

   When the SQL Azure database server is created, a firewall rule is added for the
   ClientIPAddress and also for Azure services (to connect to from the WebSite).

   The user is prompted for administrator credentials to be used when creating 
   the login for the new SQL Azure database.
.EXAMPLE
	# Creates everything except the DB Server which you pass in
	# TODO remove keys in this sample
   .\New-AzureWebsitewithDB_WebJob.ps1  -FB_AppId "1412198239001409" -TwitterConsumerKey "vv8IXu3poCB0nN5zWE9OuFOs5" `
		-GoogClientID "877288051312-51m2kbunflpjk4f80g6bkm66a72viicm.apps.googleusercontent.com" -DbServerName "rickandwest"

#>
param(
    [CmdletBinding( SupportsShouldProcess=$true)]
         
    # The webSite Name you want to create
    [string]$WebSiteName,
        
    # The Azure Data center Location
    [string]$Location="West US",
    
	 [Parameter(Mandatory = $true)] 
	 [string]$FB_AppId,

	 [Parameter(Mandatory = $true)] 
	 [string]$TwitterConsumerKey,

	 [Parameter(Mandatory = $true)] 
	 [string]$GoogClientID,

	 # If you specify a DB Server, the app DB will be created on the specified server and a new server will not be created.
    [Parameter(Mandatory = $true)]
    [String]$DbServerName
	 )
	
<#
    .SYNOPSIS     Creates an environment xml file to deploy the website.
            
    .DESCRIPTION    The New-EnvironmentXml function creates and saves to disk a website-environment.xml file. 
    New-EnvironmentXml requires a website-environment.template  file in the script directory. This file is packaged with     this  script. 
	            
    .PARAMETER  WebsiteName   
    .PARAMETER  Storage #    Specifies a hashtable of values about a Windows Azure storage account. 

    .PARAMETER  Sql
    Specifies a hashtable of values about an Azure database server and the member and application   databasaes.

    .INPUTS
    System.String
    System.Collections.Hashtable

    .OUTPUTS
    None. This function creates and saves a  website-environment.xml file to disk in the  script directory.   
#>
Function New-EnvironmentXml
{
    Param(
        [String]$WebsiteName,
        [String]$StorageAccountNameP,
        [String]$StorageAccessKeyP,
        [String]$StorageConnStrP,
        [String]$DatabaseServerNameP,
        [String]$UserNameP,
        [String]$PasswordP,
        [String]$DbConnectionStringP    
    )

   $scriptPath = Split-Path -parent $PSCommandPath

    [String]$template = Get-Content $scriptPath\website-environment.template

    $envName = $WebSiteName + "env"    
    
    $xml = $template -f $envName, $WebsiteName, `
                        $StorageAccountNameP, $StorageAccessKeyP, $StorageConnStrP, `
                      $DatabaseServerNameP, $UserNameP, $PasswordP, $DbConnectionStringP
    
    $xml | Out-File -Encoding utf8 -FilePath $scriptPath\website-environment.xml
}

<#
    .SYNOPSIS #   Creates the pubxml file that's used to deploy the website.

    .DESCRIPTION #   The New-PublishXml function creates and saves  to disk a <website_name>.pubxml file. The file includes values from the publishsettings file for the website. 
	Windows  New-PublishXml requires a pubxml.template file in the script directory.      

    .OUTPUTS
    None. This function creates and saves a  <WebsiteName>.pubxml file to disk in the    script directory.
   
#>
Function New-PublishXml
{
    Param(
        [Parameter(Mandatory = $true)]
        [String]$WebsiteName
    )
    
    $s = Get-AzureSubscription -Current
    if (!$s) {throw "Cannot get Windows Azure subscription. Failure in Get-AzureSubscription in New-PublishXml in New-AzureWebsiteEnv.ps1"}

    #$thumbprint = $s.Certificate.Thumbprint  # This property has been moved
	 $thumbprint = $s.Accounts[1].id
    if (!$thumbprint) {throw "Cannot get subscription cert thumbprint. Failure in Get-AzureSubscription in New-PublishXml in New-AzureWebsiteEnv.ps1"}
    
    # Get the certificate of the current subscription from your local cert store
	 # $thumbprint needs to be the GUID looking ID (3557C91530A4773419A81E56580FF900B8618D11), not your email.  
	 # If you have multiple subscriptions active you may need
	 # to use $thumbprint = $s.Accounts[0].id
    $cert = Get-ChildItem Cert:\CurrentUser\My\$thumbprint
    if (!$cert) {throw "Cannot find subscription cert see http://michaelwasham.com/windows-azure-powershell-reference-guide/getting-started-with-windows-azure-powershell/"}

    $website = Get-AzureWebsite -Name $WebsiteName
    if (!$website) {throw "thumbprint =  $thumbprint Cannot get Windows Azure website: $WebsiteName. Failure in Get-AzureWebsite in New-PublishXml in New-AzureWebsiteEnv.ps1"}
    
    # Compose the REST API URI from which you will get the publish settings info
    $uri = "https://management.core.windows.net:8443/{0}/services/WebSpaces/{1}/sites/{2}/publishxml" -f `
        $s.SubscriptionId, $website.WebSpace, $Website.Name

    # Get the publish settings info from the REST API
    $publishSettings = Invoke-RestMethod -Uri $uri -Certificate $cert -Headers @{"x-ms-version" = "2013-06-01"}
    if (!$publishSettings) {throw "Cannot get Windows Azure website publishSettings. Failure in Invoke-RestMethod in New-PublishXml in New-AzureWebsiteEnv.ps1"}

    # Save the publish settings info into a .publishsettings file and read the content as xml
    $publishSettings.InnerXml > $scriptPath\$WebsiteName.publishsettings
    [Xml]$xml = Get-Content $scriptPath\$WebsiteName.publishsettings
    if (!$xml) {throw "Cannot get website publishSettings XML for $WebsiteName website. Failure in Get-Content in New-PublishXml in New-AzureWebsiteEnv.ps1"}

    # Get the publish xml template and generate the .pubxml file
	 # Another F*ing global from the old code base 
	 $scriptPath = Split-Path -parent $PSCommandPath
    [String]$template = Get-Content $scriptPath\pubxml.template
    ($template -f $website.HostNames[0], $xml.publishData.publishProfile.publishUrl.Get(0), $WebsiteName) `
        | Out-File -Encoding utf8 ("{0}\{1}.pubxml" -f $scriptPath, $WebsiteName)
}

# Generate connection string of a given SQL Azure database
Function Get-SQLAzureDatabaseConnectionString
{
    Param(
        [String]$DatabaseServerNameP,
        [String]$DatabaseNameP,
        [String]$UserNameP,
        [String]$PasswordP
    )

    Return "Server=tcp:$DatabaseServerNameP.database.windows.net,1433;Database=$DatabaseNameP;User ID=$UserNameP@$DatabaseServerNameP;Password=$PasswordP;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;"
}
   
<#
.SYNOPSIS
    creates a sql db server and sets server firewall rule.
.DESCRIPTION
   This function creates a database server, sets up server firewall rules  
    `
.EXAMPLE
    $db = CreateDbServerAndFireWallRules -Location "West US" -Credential cred 

.TODO replace with start/end  IP address. See FixIt for example
#>

function CreateDbServerAndFireWallRules($Location, $Credential, $ClientIP)
{
	     # Create Database Server
		 Write-Verbose "Creating SQL Azure Database Server."
		 $databaseServer = New-AzureSqlDatabaseServer -AdministratorLogin $Credential.UserName `
			  -AdministratorLoginPassword $Credential.GetNetworkCredential().Password -Location $Location
		 
		 $dbSrvNm = $databaseServer.ServerName
		 Write-Verbose ("SQL Azure Database Server '" + $dbSrvNm + "' created.") 
    
		 # Apply Firewall Rules
       $clientFirewallRuleName = "ClientIPAddress_" + (Get-Random).ToString() 
		 Write-Verbose "Creating client firewall rule '$clientFirewallRuleName'."
		 New-AzureSqlDatabaseServerFirewallRule -ServerName $dbSrvNm `
			  -RuleName $clientFirewallRuleName -StartIpAddress $ClientIP -EndIpAddress $ClientIP | Out-Null  

         $azureFirewallRuleName = "AzureServices" 
		 Write-Verbose "Creating Azure Services firewall rule '$azureFirewallRuleName'."
		 New-AzureSqlDatabaseServerFirewallRule -ServerName $dbSrvNm `
        -RuleName $azureFirewallRuleName -StartIpAddress "0.0.0.0" -EndIpAddress "0.0.0.0"	| Out-Null  
     
    return $dbSrvNm;
}

<#
.SYNOPSIS
    creates a sql db given a server name.
.DESCRIPTION
   This function creates a SQL DB given a server name.
    `
.EXAMPLE
    $db = CreateDatabase -DbServerName "myDbServer" -Location "West US" -Credential cred 
#>
function CreateDatabase($DbServerName, $Location, $AppDatabaseName, $Credential)
{
    $context = New-AzureSqlDatabaseServerContext -ServerName $DbServerName -Credential $Credential
    Write-Verbose "Creating database '$AppDatabaseName' in database server $DbServerName."
    New-AzureSqlDatabase -DatabaseName $AppDatabaseName -Context $context -Edition "Basic"
}

function Get-MissingFiles
{
    $Path = Split-Path $MyInvocation.PSCommandPath
    $files = dir $Path | foreach {$_.Name}
	 # 'CreateAzureCredential.ps1.credential',
    $required= 'New-AzureWebsitewithDB_WebJob.ps1',
				'Publish-AzureWebsite.ps1',
               'pubxml.template',
					'CreateSQLpwCredential.ps1.credential',					
					'CreateFBCredential.ps1.credential',
					'CreateGoogCredential.ps1.credential',
					'CreateTwitterCredential.ps1.credential',
               'website-environment.template'

    foreach ($r in $required)
    {            
        if ($r -notin $files)
        {
            [PSCustomObject]@{"Name"=$r; "Error"="Missing"}
        }
    }
}

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

#-- SCRIPT starts here zz -------------------------

	
$VerbosePreference = "Continue"   # Set the output level to verbose and make the script stop on error
$ErrorActionPreference = "Stop"
$startTime = Get-Date  # Get the time that script execution starts

$scriptPath = Split-Path -parent $PSCommandPath
$missingFiles = Get-MissingFiles
if ($missingFiles) {$missingFiles; 
	throw "Required files missing from WebSite subdirectory. Review build guide."}

# If we just want to test this script, generate random strings so creation will probably be successful
if ($website -eq $null) {
	$randomString =  "rikand" + $(Get-Date -Format ('ddhhmm'))
	$randomString =  "rikand" + $(Get-Date -Format ('ddmm'))
    $WebSiteName =  $randomString + "web"
}
$StorageAccountName = $randomString + "stor"


# Create the website 
$website = Get-AzureWebsite | Where-Object {$_.Name -eq $WebSiteName }
if ($website -eq $null) 
{   
    Write-Verbose "Creating website '$WebSiteName'." 
    $website = New-AzureWebsite -Name $WebSiteName -Location $Location 
}
else 
{
    throw "Website already exists.  Please try a different website name."
}

# Create storage account if it does not already exist.
$storageAccount = Get-AzureStorageAccount | Where-Object { $_.StorageAccountName -eq $StorageAccountName }
if($storageAccount -eq $null) 
{
    Write-Verbose "Creating storage account '$StorageAccountName'."
    $storage = New-AzureStorageAccount -StorageAccountName $StorageAccountName -Location $Location 
}

# Construct a storage account app settings hashtable.
$storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
$WJstorVal = "DefaultEndpointsProtocol=https;AccountName=" + $StorageAccountName + ";AccountKey=" + $storageAccountKey.Primary
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

# Create the SQL DB Server if no Server name is passed 
# Create SQL DB not supported

$dbServerCreated =  $false
if( !$DbServerName )
{
    $DbServerName = CreateDbServerAndFireWallRules -Location $Location -Credential $credential -ClientIP "24.16.65.126"
    $dbServerCreated = $true
}

$SQLcredential = GetAllCredentialFromFile CreateSQLpwCredential.ps1.credential

$AppDatabaseName = $WebSiteName + "_db"
Write-Verbose "Creating database '$AppDatabaseName'."
CreateDatabase -Location $Location -AppDatabaseName $AppDatabaseName `
              -Credential $SQLcredential  -dbs $DbServerName

				  
# Create a connection string for the database.
$appDBConnStr  = "Server=tcp:{0}.database.windows.net,1433;Database={1};" 
$appDBConnStr += "User ID={2}@{0};Password={3};Trusted_Connection=False;Encrypt=True;Connection Timeout=30;"
$appDBConnStr = $appDBConnStr -f `
                    $DbServerName, $AppDatabaseName, `
                    $SQLcredential.GetNetworkCredential().username, `
                    $SQLcredential.GetNetworkCredential().Password

# Instantiate a ConnStringInfo object to add connection string infomation to website.
$appDBConnStrInfo = New-Object Microsoft.WindowsAzure.Commands.Utilities.Websites.Services.WebEntities.ConnStringInfo;
#$appDBConnStrInfo.Name=$AppDatabaseName;
$appDBConnStrInfo.Name="DefaultConnection";
$appDBConnStrInfo.ConnectionString=$appDBConnStr;
$appDBConnStrInfo.Type =[Microsoft.WindowsAzure.Commands.Utilities.Websites.Services.WebEntities.DatabaseType]::SQLAzure


# Add new ConnStringInfo objecto list of connection strings for website.
$connStrSettings = (Get-AzureWebsite $WebSiteName).ConnectionStrings;
$connStrSettings.Add($appDBConnStrInfo);

# Link the website to the storage account and SQL Azure database.
Write-Verbose "Linking storage account '$StorageAccountName' and SQL Azure Database '$AppDatabaseName' to website '$WebSiteName'."
Set-AzureWebsite -Name $WebSiteName -AppSettings $storageSettings -ConnectionStrings $connStrSettings


# Write the environment info to an xml file so that the deploy script can consume
Write-Verbose "[Begin] writing environment info to website-environment.xml"
New-EnvironmentXml -WebsiteName $WebSiteName -StorageAccountNameP $StorageAccountName -StorageAccessKeyP $storageAccountKey.Primary `
                   -StorageConnStrP $WebJobsStr -DatabaseServerNameP $DbServerName -UserNameP $SQLcredential.UserName `
                   -PasswordP $SQLcredential.GetNetworkCredential().Password `
                    -DbConnectionStringP $appDBConnStr 


if (!(Test-path $scriptPath\website-environment.xml))
{
    throw "The script did not generate a website-environment.xml file that is required to deploy the website. Try to rerun the New-EnvironmentXml function in the New-AzureWebisteEnv.ps1 script."
}
else 
{
    Write-Verbose "$scriptPath\website-environment.xml"
    Write-Verbose "[Finish] writing environment info to website-environment.xml"
}

# Generate the .pubxml file which will be used by webdeploy later
$Name = $WebSiteName
Write-Verbose "[Begin] generating $Name.pubxml file"
New-PublishXml -Website $Name
if (!(Test-path $scriptPath\$Name.pubxml))
{
    throw "The script did not generate a $Name.pubxml file that is required for deployment. Try to rerun the New-PublishXml function in the New-AzureWebisteEnv.ps1 script."
}
else 
{
    Write-Verbose "$scriptPath\$Name.pubxml"
    Write-Verbose "[Finish] generating $Name.pubxml file"
}


Write-Verbose "Script is complete."
$finishTime = Get-Date
$TotalTime = ($finishTime - $startTime).TotalSeconds
Write-Output "Total time used (seconds): $TotalTime"