# Pre-commit hook that runs all tests before allowing a commit.
# Install with: Copy-Item scripts\pre-commit.ps1 .git\hooks\pre-commit.ps1

Write-Host "🧪 Running tests before commit..." -ForegroundColor Cyan
Write-Host ""

# Load .env file if it exists
$envFile = Join-Path $PSScriptRoot "..\..\\.env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.*)$') {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2], 'Process')
        }
    }
}

# Run all tests
$result = dotnet test --verbosity minimal
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "❌ Tests failed. Commit aborted." -ForegroundColor Red
    Write-Host ""
    Write-Host "To skip this check (not recommended), use: git commit --no-verify" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "✅ All tests passed. Proceeding with commit." -ForegroundColor Green
exit 0
