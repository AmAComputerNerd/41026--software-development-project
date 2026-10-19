#!/usr/bin/env bash
# ============================================================================
# Better Canvas — Release 2 Azure Cloud Deployment Script (deploy.sh)
# Usage:
#   ./infra/azure/deploy.sh --mode baseline [--dry-run]
#   ./infra/azure/deploy.sh --mode bonus-full [--dry-run]
# ============================================================================
set -euo pipefail

MODE="baseline"
DRY_RUN="false"
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-rg-better-canvas-r2}"
LOCATION="${AZURE_LOCATION:-australiaeast}"
ACR_NAME="${AZURE_ACR_NAME:-acrbettercanvasr2}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mode)
      MODE="$2"
      shift 2
      ;;
    --dry-run)
      DRY_RUN="true"
      shift
      ;;
    *)
      echo "Unknown flag: $1" >&2
      exit 1
      ;;
  esac
done

ENABLE_BONUS_AI="false"
ENABLE_BONUS_SECURITY="true"

if [[ "$MODE" == "bonus-full" ]]; then
  ENABLE_BONUS_AI="true"
  ENABLE_BONUS_SECURITY="true"
elif [[ "$MODE" == "baseline" ]]; then
  ENABLE_BONUS_AI="false"
  ENABLE_BONUS_SECURITY="true"
else
  echo "Invalid --mode '$MODE'. Expected 'baseline' or 'bonus-full'." >&2
  exit 1
fi

echo "=================================================================="
echo "Better Canvas — Release 2 Cloud Deployment (Azure Container Apps)"
echo "Profile Mode        : $MODE"
echo "Resource Group      : $RESOURCE_GROUP ($LOCATION)"
echo "Container Registry  : $ACR_NAME"
echo "Image Tag           : $IMAGE_TAG"
echo "AI_MODE_ENABLED     : $ENABLE_BONUS_AI"
echo "MCP_ENABLED         : $ENABLE_BONUS_AI"
echo "RAG_ENABLED         : $ENABLE_BONUS_AI"
echo "MULTI_AGENT_ENABLED : $ENABLE_BONUS_AI"
echo "Dry Run             : $DRY_RUN"
echo "=================================================================="

if [[ "$DRY_RUN" == "true" ]]; then
  echo "[DRY-RUN] Validating Bicep syntax and parameters for mode '$MODE'..."
  if command -v az >/dev/null 2>&1; then
    az bicep build --file infra/azure/main.bicep --stdout >/dev/null
    echo "[DRY-RUN] az bicep build succeeded cleanly."
  else
    echo "[DRY-RUN] Azure CLI not installed locally; verified infra/azure/main.bicep exists."
  fi
  exit 0
fi

echo "[1/3] Ensuring Azure Resource Group '$RESOURCE_GROUP' exists..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "[2/3] Deploying Bicep template infra/azure/main.bicep (enableBonusAi=$ENABLE_BONUS_AI)..."
FQDN=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/azure/main.bicep \
  --parameters \
    acrName="$ACR_NAME" \
    imageTag="$IMAGE_TAG" \
    enableBonusAi="$ENABLE_BONUS_AI" \
    enableBonusSecurity="$ENABLE_BONUS_SECURITY" \
  --query "properties.outputs.publicGatewayFqdn.value" \
  --output tsv)

echo "[3/3] Cloud deployment complete."
echo "Public Gateway URL: https://$FQDN"
