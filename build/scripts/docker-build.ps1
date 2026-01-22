param(
  [ValidateSet("local","dev","prod")]
  [string]$Environment = "local",

  [string]$Registry = "ghcr.io/agrosolutions",

  [string]$Tag = ""
)

$ErrorActionPreference = "Stop"

function Get-GitSha {
  try {
    return (git rev-parse --short HEAD).Trim()
  } catch {
    return "nogit"
  }
}

if ([string]::IsNullOrWhiteSpace($Tag)) {
  switch ($Environment) {
    "local" { $Tag = "local" }
    "dev"   { $Tag = "dev" }
    "prod"  { $Tag = "1.0.0" }  # Em produção, prefira informar -Tag explicitamente
    default { $Tag = "local" }
  }
}

$services = @(
  @{ Name = "analise";  Dockerfile = "src/services/AgroSolutions.Analise/AgroSolutions.Analise.WebApi/Dockerfile" },
  @{ Name = "propriedades";  Dockerfile = "src/services/AgroSolutions.Propriedades/AgroSolutions.Propriedades.WebApi/Dockerfile" },
  @{ Name = "ingestao"; Dockerfile = "src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.WebApi/Dockerfile" },
  @{ Name = "usuarios";  Dockerfile = "src/services/AgroSolutions.Usuarios/AgroSolutions.Usuarios.WebApi/Dockerfile" }
)

Write-Host "Building images for env=$Environment tag=$Tag registry=$Registry"

foreach ($svc in $services) {
  $image = "$Registry/$($svc.Name):$Tag"
  Write-Host "==> Building $image"

  docker build `
    -f $svc.Dockerfile `
    -t $image `
    --build-arg DOTNET_VERSION=8.0 `
    .

  if ($LASTEXITCODE -ne 0) { throw "Docker build failed for $($svc.Name)" }
}

Write-Host "Done."
Write-Host "Images tagged with: $Tag"
