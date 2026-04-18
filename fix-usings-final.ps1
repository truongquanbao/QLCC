#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Fix missing using statements and remove duplicates
#>

Write-Host "🔧 Fixing missing using statements..." -ForegroundColor Green

$requiredUsings = @{
    'System.Drawing' = 'using System.Drawing;'
    'System.Linq' = 'using System.Linq;'
    'System.Configuration' = 'using System.Configuration;'
}

$csFiles = Get-ChildItem -Path "c:\Users\dung0\source\repos\QLCC\ApartmentManager" -Recurse -Filter "*.cs" | 
    Where-Object { $_.Name -notlike '*Designer.cs' }

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $modified = $false
    
    # Add missing using statements
    foreach ($using in $requiredUsings.Keys) {
        if ($content -notmatch "using $using;") {
            # Find first using or namespace
            if ($content -match 'namespace') {
                $content = $content -replace '(namespace )', "using $using;`n`n`$1"
                $modified = $true
            }
        }
    }
    
    # Remove duplicate using statements
    $lines = $content -split "`n"
    $seenUsings = @{}
    $newLines = @()
    
    foreach ($line in $lines) {
        if ($line -match '^using ') {
            if (-not $seenUsings.ContainsKey($line)) {
                $seenUsings[$line] = $true
                $newLines += $line
                $modified = $true
            }
        } else {
            $newLines += $line
        }
    }
    
    if ($modified) {
        $newContent = $newLines -join "`n"
        Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
        Write-Host "✅ Fixed: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "✨ Done! Fixed missing usings and removed duplicates" -ForegroundColor Green
