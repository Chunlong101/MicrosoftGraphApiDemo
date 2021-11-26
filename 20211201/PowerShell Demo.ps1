# ----- 
#PowerShell code sample to implement Authorization Code Auth flow
# ----- 

#Define Client Variables Here
#############################
$TenantId="ed3c1c81-f3be-495c-8028-f11b7ad6415a"
$clientId = "87202bda-1a2a-4b6c-917b-df77c98c640d"
$clientSecret = "xxx"
$scope = "https://graph.microsoft.com/.default"
$redirectUri = "https://localhost" #this can be set to any URL
#$resource = "https://graph.microsoft.com"

#UrlEncode variables for special characters
###########################################
Add-Type -AssemblyName System.Web
$clientSecretEncoded = [System.Web.HttpUtility]::UrlEncode($clientSecret)
$redirectUriEncoded =  [System.Web.HttpUtility]::UrlEncode($redirectUri)
$scopeEncoded = [System.Web.HttpUtility]::UrlEncode($scope)

#Obtain Authorization Code
##########################
Add-Type -AssemblyName System.Windows.Forms
$form = New-Object -TypeName System.Windows.Forms.Form -Property @{Width=440;Height=640}
$web  = New-Object -TypeName System.Windows.Forms.WebBrowser -Property @{Width=420;Height=600;Url=$url}
$url = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/authorize?response_type=code&redirect_uri=$redirectUriEncoded&client_id=$clientId&scope=$scopeEncoded&prompt=admin_consent"
$DocComp  = {
        $Global:uri = $web.Url.AbsoluteUri        
        if ($Global:uri -match "error=[^&]*|code=[^&]*") {$form.Close() }
    }
$web.ScriptErrorsSuppressed = $true
$web.Add_DocumentCompleted($DocComp)
$form.Controls.Add($web)
$form.Add_Shown({$form.Activate()})
$form.ShowDialog() | Out-Null
$queryOutput = [System.Web.HttpUtility]::ParseQueryString($web.Url.Query)
$output = @{}
foreach($key in $queryOutput.Keys){
    $output["$key"] = $queryOutput[$key]
}
$regex = '(?<=code=)(.*)(?=&)'
$authCode  = ($uri | Select-string -pattern $regex).Matches[0].Value

#Get Access Token with obtained Auth Code
#########################################
$body = "grant_type=authorization_code&redirect_uri=$redirectUri&client_id=$clientId&client_secret=$clientSecretEncoded&code=$authCode&resource=$resource"
$authUri = "https://login.microsoftonline.com/common/oauth2/token"
$tokenResponse = Invoke-RestMethod -Uri $authUri -Method Post -Body $body -ErrorAction STOP

#Call Graph API to download a file
#########################################
#$DownloadUri=https://graph.microsoft.com/v1.0/me/drive/root:/ee - Copy.xlsx:/content
$DownloadUri = "https://graph.microsoft.com/v1.0/sites/ec884a3f-7f7e-460a-900b-39c61f8195be/drive/items/01DFEMAO44C7FIP2APCBEZFLF67OL6JAIZ/content"
$destinationFilePath = "C:\Users\menxia\Desktop\Files\Graph Test\Test.pdf"
$header =@{
    'Authorization' = "Bearer $($tokenResponse.access_token)"
}

$results = Invoke-RestMethod -Uri $DownloadUri -Headers $header -Method Get -OutFile $destinationFilePath

# ----- 
# PowerShell code sample to implement client credential grant flow
# ----- 

#Define Client Variables Here
#############################
$TenantId='ed3c1c81-f3be-495c-8028-f11b7ad6415a'
$ClientId='87202bda-1a2a-4b6c-917b-df77c98c640d'
$ClientSecret='xxx'

$Body = @{
    'tenant' = $TenantId
    'client_id' = $ClientId
    'scope' = 'https://graph.microsoft.com/.default'
    'client_secret' = $ClientSecret
    'grant_type' = 'client_credentials'
}

$Params = @{
    'Uri' = https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token   #tenant name is also fine here, like xia053.onmicrosoft.com  
    'Method' = 'Post'
    'Body' = $Body
    'ContentType' = 'application/x-www-form-urlencoded'
}

#Get Access Token
##########################
$AuthResponse = Invoke-RestMethod @Params

#Call Graph API to add a new folder 
####################################
$FolderName ='New Folder Name'
$SiteId = 'ec884a3f-7f7e-460a-900b-39c61f8195be'
$Uri=https://graph.microsoft.com/v1.0/sites/$SiteId/drive/root/children
$post = @"
{
        "name":  "$FolderName",
        "folder":  { }
}
"@

$header =@{
    'Authorization' = "Bearer $($AuthResponse.access_token)"
    'Content-Type' = 'application/json'
}

$results = Invoke-WebRequest -Uri $Uri -Headers $header -Method Post -Body $post