#!/usr/bin/env pwsh

Write-Host "🔧 Fixing missing using statements and removing duplicates..." -ForegroundColor Green

$csFiles = Get-ChildItem -Path "c:\Users\dung0\source\repos\QLCC\ApartmentManager" -Recurse -Filter "*.cs" | 
    Where-Object { $_.Name -notlike '*Designer.cs' }

$requiredUsings = @(
    'using System.Drawing;',
    'using System.Linq;',
    'using System.Configuration;'
)

$fixed = 0

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    $modified = $false
    
    # Remove duplicate using statements first
    $lines = $content -split "`n"
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
    
    # Add missing using statements
    foreach ($usingStatement in $requiredUsings) {
        $usingName = $usingStatement -replace 'using ', '' -replace ';', ''
        if ($content -notmatch "using $usingName;") {
            # Find the first namespace or other using
            if ($content -match 'namespace') {
                $content = $content -replace '(namespace )', "$usingStatement`n`1"
                $modified = $true
            }
        }
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
        $fixed++
        Write-Host "✅ $($file.Name)" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "✨ Fixed $fixed files with missing usings and duplicates!" -ForegroundColor Green
