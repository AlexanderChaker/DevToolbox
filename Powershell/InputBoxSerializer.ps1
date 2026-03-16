<#
.SYNOPSIS
- This script will prompt for json object, serialize it, print it, and copy it to the clipboard
#>

##############################################################################################################
# Onesky Helpers
Get-Module | Where-Object {$_.Name -eq 'OneSkyPowershellHelpers'} | Remove-Module
Import-Module -Name $PSScriptRoot\OneSkyPowershellHelpers
. $PSScriptRoot\OneSkyPowershellHelpers\OneSkyPowershellVariables.ps1
##############################################################################################################

$json = Read-MultiLineInputBoxDialog -Message "Please enter some text. It can be multiple lines" -WindowTitle "Multi Line Example" -DefaultText $demoAircraft

$serialized = SerializeJson $json

Write-Debug "The JSON has been serialized!"
Write-Debug "The serialized JSON is:"
Write-Debug $serialized
Write-Debug ""

# Copy $json to the clipboard
$serialized | Set-Clipboard
Write-Debug "The JSON has been copied to the clipboard!"

$DebugPreference = $origDebugPreference
Set-Location $origWorkingDirectory