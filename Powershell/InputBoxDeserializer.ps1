<#
.SYNOPSIS
- This script will prompt for a json string, deserialize it, and open it for viewing in a new VS Code window
#>

##############################################################################################################
# Onesky Helpers
Get-Module | Where-Object {$_.Name -eq 'OneSkyPowershellHelpers'} | Remove-Module
Import-Module -Name $PSScriptRoot\OneSkyPowershellHelpers
. $PSScriptRoot\OneSkyPowershellHelpers\OneSkyPowershellVariables.ps1
##############################################################################################################
try {
    $clipboard = Get-Clipboard -Raw

    # If the clipboard data is JSON, use that as the default text
    if ($null -ne $clipboard) {
        try {
            $clipboardJson = ConvertFrom-Json $clipboard
            $defaultText = $clipboardJson | ConvertTo-Json -Depth 5
            $message = "Copied Text from Clipboard!"
        }
        catch {
            Write-Debug "Using demo TripAggregate"
            $defaultText = $demoTripAggregate
            $message = "Please enter some text. It can be multiple lines"
        }
    }

    # Get input from the user
    $multiLineText = Read-MultiLineInputBoxDialog -Message $message -WindowTitle "Multi Line Input" -DefaultText $defaultText
    if ($null -eq $multiLineText) 
    { 
        Write-Debug "You clicked Cancel"
        exit
    }

    # Convert the input to JSON
    $json = ConvertFrom-Json $multiLineText

    if ($json.Message -notmatch '^\{.*\}$') {
        Write-Debug "The message property is NOT a JSON string. Attempting to decompress"
        $decompressedMessage = DecompressGzipString $json.Message
        $json.Message = ConvertFrom-Json $decompressedMessage
    } 

    # Escape the entire message
    UnescapeJson $json

    # Get temp directory
    $tempDir = [System.IO.Path]::GetTempPath()

    if ($json.Title && $json.MessageGuid) {
        $fileName = $json.Title + '_' + $json.MessageGuid + '_' + $(Get-Date -Format "yyyy-MM-dd_HH-mm-ss") + '.json'
    }
    else {
        $fileName = $(Get-Date -Format "yyyy-MM-dd_HH-mm-ss") + '.json'
    }

    # Combine the temp directory and the file name
    $filePath = Join-Path $tempDir $fileName

    $json | ConvertTo-Json -Depth 25 | Out-File $filePath
    & code -n $filePath
}
catch {
    Write-Error $_
    exit
}
finally{
    $DebugPreference = $origDebugPreference
    Set-Location $origWorkingDirectory
}