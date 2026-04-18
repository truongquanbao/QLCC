#!/usr/bin/env pwsh

Write-Host "Starting fix..." -ForegroundColor Green

$csFiles = Get-ChildItem -Path "c:\Users\dung0\source\repos\QLCC\ApartmentManager" -Recurse -Filter "*.cs" | 
    Where-Object { $_.Name -notlike '*Designer.cs' }

$fixed = 0

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    
    # Remove duplicate using directives
    $lines = @($content -split "`n")
    $seenUsings = @{}
    $newLines = @()
    
    foreach ($line in $lines) {
        if ($line -match '^using ') {
            $trimmedLine = $line.Trim()
            if (-not $seenUsings.ContainsKey($trimmedLine)) {
                $seenUsings[$trimmedLine] = $true
                $newLines += $line
            }
        } else {
            $newLines += $line
        }
    }
    
    $content = $newLines -join "`n"
    
    # Add System.Drawing if using Color/Font/Point/Size and not already present
    if (($content -match 'Color\.|Font\.|Point\.|Size\.') -and ($content -notmatch 'using System\.Drawing;')) {
        $content = $content -replace '(namespace )', "using System.Drawing;`n`$1"
    }
    
    # Add System.Linq if using LINQ methods and not already present
    if (($content -match '\.(Where|Select|FirstOrDefault|Sum|Count|Any|Take|Skip|OrderBy|GroupBy)\(') -and ($content -notmatch 'using System\.Linq;')) {
        $content = $content -replace '(namespace )', "using System.Linq;`n`$1"
    }
    
    # Add System.Configuration if using ConfigurationManager and not already present
    if (($content -match 'ConfigurationManager\.' -or $content -match 'Configuration\.') -and ($content -notmatch 'using System\.Configuration;')) {
        $content = $content -replace '(namespace )', "using System.Configuration;`n`$1"
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        $fixed++
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "Completed! Fixed $fixed files." -ForegroundColor Green
