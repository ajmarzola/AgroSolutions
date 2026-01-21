#!/usr/bin/env bash
set -euo pipefail

ENVIRONMENT="${1:-local}"     # local|dev|prod
REGISTRY="${2:-ghcr.io/sua-org}"
TAG="${3:-}"

git_sha() {
  if command -v git >/dev/null 2>&1; then
    git rev-parse --short HEAD 2>/dev/null || echo "nogit"
  else
    echo "nogit"
  fi
}

if [[ -z "${TAG}" ]]; then
  if [[ "${ENVIRONMENT}" == "prod" ]]; then
    TAG="1.0.0"
  else
    TAG="$(git_sha)"
  fi
fi

declare -a SERVICES=(
  "analise|src/services/AgroSolutions.Analise/AgroSolutions.Analise.WebApi/Dockerfile"
  "propriedades|src/services/AgroSolutions.Propriedades/AgroSolutions.Propriedades.WebApi/Dockerfile"
  "ingestao|src/services/AgroSolutions.Ingestao/AgroSolutions.Ingestao.WebApi/Dockerfile"
  "usuarios|src/services/AgroSolutions.Usuarios/AgroSolutions.Usuarios.WebApi/Dockerfile"
)

echo "Building images for env=${ENVIRONMENT} tag=${TAG} registry=${REGISTRY}"

for item in "${SERVICES[@]}"; do
  IFS="|" read -r name dockerfile <<< "${item}"
  image="${REGISTRY}/${name}:${TAG}"

  echo "==> Building ${image}"
  docker build \
    -f "${dockerfile}" \
    -t "${image}" \
    --build-arg DOTNET_VERSION=8.0 \
    .
done

echo "Done."
echo "Images tagged with: ${TAG}"
