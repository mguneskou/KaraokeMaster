<#
.SYNOPSIS
One-time setup of the Python virtual environment used for vocal separation (Demucs + PyTorch CPU).

Run this manually if you'd rather not use the in-app "Set Up Vocal Separation" wizard, or to
pre-warm the environment before first use. Safe to re-run — it skips steps that are already done.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$venvRoot = Join-Path $repoRoot "data\pyenv"
$venvPython = Join-Path $venvRoot "Scripts\python.exe"

function Find-SystemPython {
    foreach ($version in @("3.12", "3.11", "3.10", "3.13")) {
        try {
            $path = & py "-$version" -c "import sys; print(sys.executable)" 2>$null
            if ($LASTEXITCODE -eq 0 -and (Test-Path $path)) { return $path }
        } catch {}
    }
    try {
        $path = & py -3 -c "import sys; print(sys.executable)" 2>$null
        if ($LASTEXITCODE -eq 0 -and (Test-Path $path)) { return $path }
    } catch {}
    throw "No Python 3 installation found via the 'py' launcher. Install Python 3.10+ from python.org."
}

if (Test-Path $venvPython) {
    Write-Host "Virtual environment already exists at $venvRoot, reusing it."
} else {
    $systemPython = Find-SystemPython
    Write-Host "Using system Python: $systemPython"
    Write-Host "Creating virtual environment at $venvRoot..."
    & $systemPython -m venv $venvRoot
}

Write-Host "Upgrading pip..."
& $venvPython -m pip install --upgrade pip

Write-Host "Installing PyTorch (CPU build) - this is a large download and may take a while..."
& $venvPython -m pip install torch --index-url https://download.pytorch.org/whl/cpu

Write-Host "Installing demucs..."
& $venvPython -m pip install demucs

Write-Host "Setup complete. Vocal separation is ready to use."
