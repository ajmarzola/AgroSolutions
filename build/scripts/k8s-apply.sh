#!/usr/bin/env bash
set -euo pipefail

ENVIRONMENT="${1:-local}"     # local|dev|prod
OVERLAY_DIR="infra/k8s/overlays/${ENVIRONMENT}"

if [[ ! -d "${OVERLAY_DIR}" ]]; then
  echo "Overlay directory not found: ${OVERLAY_DIR}"
  exit 1
fi

# Namespace é definido no kustomization.yaml; ainda assim, é comum garantir que exista.
NAMESPACE="$(grep -E '^namespace:' "${OVERLAY_DIR}/kustomization.yaml" | awk '{print $2}' || true)"

if [[ -n "${NAMESPACE}" ]]; then
  echo "Ensuring namespace exists: ${NAMESPACE}"
  kubectl get namespace "${NAMESPACE}" >/dev/null 2>&1 || kubectl create namespace "${NAMESPACE}"
fi

echo "Applying Kustomize overlay: ${OVERLAY_DIR}"
kubectl apply -k "${OVERLAY_DIR}"

echo "Done."
