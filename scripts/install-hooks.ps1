# Install pre-commit hook
# Run from project root: .\scripts\install-hooks.ps1

$hookSource = Join-Path $PSScriptRoot "pre-commit"
$hookDest = Join-Path $PSScriptRoot "..\\.git\\hooks\\pre-commit"

if (-not (Test-Path (Split-Path $hookDest))) {
    Write-Host "Creating hooks directory..."
    New-Item -ItemType Directory -Path (Split-Path $hookDest) -Force | Out-Null
}

Copy-Item $hookSource $hookDest -Force
Write-Host "✅ Pre-commit hook installed to .git/hooks/pre-commit" -ForegroundColor Green
Write-Host ""
Write-Host "The hook will run 'dotnet test' before each commit."
Write-Host "To bypass (not recommended): git commit --no-verify"
