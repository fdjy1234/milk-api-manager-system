Param(
  [string]$BaseUrl = "http://localhost:55894"
)

$env:BASE_URL = $BaseUrl
Push-Location $PSScriptRoot\.. 

if (-not (Test-Path node_modules)) {
  npm install
}

npx playwright install
npm test

Pop-Location
