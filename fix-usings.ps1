#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Auto-fix missing 'using System;' and 'using System.Collections.Generic;' statements
    
.DESCRIPTION
    This script scans all C# files and adds missing using directives automatically
    
.EXAMPLE
    .\fix-usings.ps1
#>

param(
    [string]$ProjectPath = "c:\Users\dung0\source\repos\QLCC\ApartmentManager"
)

Write-Host "🔧 Starting Auto-Fix for Missing Using Statements..." -ForegroundColor Green
Write-Host "📁 Project Path: $ProjectPath" -ForegroundColor Cyan
Write-Host ""

# Get all C# files
$csFiles = Get-ChildItem -Path $ProjectPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue

$fixedCount = 0
$checkedCount = 0

# Required using statements
$requiredUsings = @(
    "using System;",
    "using System.Collections.Generic;",
    "using System.Windows.Forms;",
    "using System.Configuration;"
)

foreach ($file in $csFiles) {
    $checkedCount++
    $content = Get-Content -Path $file.FullName -Raw
    $originalContent = $content
    
    # Skip generated files
    if ($content -like '*Auto-generated*' -or $file.Name -like '*.Designer.cs') {
        continue
    }
    
    # Add missing System using
    if ($content -notmatch 'using System;' -and -not $content.StartsWith('using System;')) {
        # Find first using statement or namespace
        if ($content -match 'using \w+') {
            $content = $content -replace '(using \w+)', "using System;`n`$1"
        } elseif ($content -match 'namespace') {
            $content = $content -replace '(namespace )', "using System;`n`n`$1"
        } else {
            $content = "using System;`n`n" + $content
        }
        Write-Host "✅ Added 'using System;' to: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
    
    # Add missing System.Collections.Generic if List<> or Dictionary< found
    if (($content -like '*List<*' -or $content -like '*Dictionary<*' -or $content -like '*IEnumerable<*') -and 
        $content -notmatch 'using System.Collections.Generic;') {
        if ($content -match 'using \w+') {
            $content = $content -replace '(using System;)', "`$1`nusing System.Collections.Generic;"
        } else {
            $content = "using System.Collections.Generic;`n" + $content
        }
        Write-Host "✅ Added 'using System.Collections.Generic;' to: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
    
    # Add System.Windows.Forms for Form derived classes
    if ($content -like '*: Form*' -and $content -notmatch 'using System.Windows.Forms;') {
        if ($content -match 'using') {
            $content = $content -replace '(using [^\n]+;)', "`$1`nusing System.Windows.Forms;"
        } else {
            $content = "using System.Windows.Forms;`n" + $content
        }
        Write-Host "✅ Added 'using System.Windows.Forms;' to: $($file.Name)" -ForegroundColor Green
        $fixedCount++
    }
    
    # Only write if changes were made
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
    }
}

Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor Yellow
Write-Host "   Total files checked: $checkedCount" -ForegroundColor White
Write-Host "   Files fixed: $fixedCount" -ForegroundColor Green
Write-Host ""

# Fix SessionManager static class issue
Write-Host "🔧 Fixing SessionManager instantiation issues..." -ForegroundColor Green

$formFiles = @(
    "FrmFeeTypeManagement.cs",
    "FrmNotificationManagement.cs",
    "FrmVisitorManagement.cs",
    "FrmContractManagement.cs"
)

foreach ($formFile in $formFiles) {
    $path = Get-ChildItem -Path $ProjectPath -Recurse -Filter $formFile -ErrorAction SilentlyContinue
    
    if ($path) {
        $content = Get-Content -Path $path.FullName -Raw
        
        # Fix: SessionManager session = SessionManager.GetSession();
        if ($content -like '*SessionManager session = SessionManager.GetSession()*') {
            $content = $content -replace 'SessionManager session = SessionManager\.GetSession\(\);', 'var session = SessionManager.GetSession();'
            Set-Content -Path $path.FullName -Value $content -Encoding UTF8
            Write-Host "✅ Fixed SessionManager in: $($path.Name)" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "✨ Auto-fix complete! Now run: dotnet build" -ForegroundColor Green
Write-Host ""
