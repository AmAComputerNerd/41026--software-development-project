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
OPENROUTER_API_KEY="${OPENROUTER_API_KEY:-}"
CANVAS_API_TOKEN="${CANVAS_API_TOKEN:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# If running locally and .env exists, load OPENROUTER_API_KEY and CANVAS_API_TOKEN if not already set
if [[ -f "$REPO_ROOT/.env" ]]; then
  if [[ -z "$OPENROUTER_API_KEY" ]]; then
    OPENROUTER_API_KEY="$(grep -E '^OPENROUTER_API_KEY=' "$REPO_ROOT/.env" | cut -d '=' -f2- | tr -d '\"' | tr -d "'" || true)"
  fi
  if [[ -z "$CANVAS_API_TOKEN" ]]; then
    CANVAS_API_TOKEN="$(grep -E '^CANVAS_API_TOKEN=' "$REPO_ROOT/.env" | cut -d '=' -f2- | tr -d '\"' | tr -d "'" || true)"
  fi
fi

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
    --openrouter-key)
      OPENROUTER_API_KEY="$2"
      shift 2
      ;;
    --canvas-token)
      CANVAS_API_TOKEN="$2"
      shift 2
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
echo "OPENROUTER_KEY      : $(if [[ -n "$OPENROUTER_API_KEY" ]]; then echo "[CONFIGURED]"; else echo "[NOT CONFIGURED]"; fi)"
echo "CANVAS_TOKEN        : $(if [[ -n "$CANVAS_API_TOKEN" ]]; then echo "[CONFIGURED]"; else echo "[NOT CONFIGURED]"; fi)"
echo "Dry Run             : $DRY_RUN"
echo "=================================================================="

if [[ "$DRY_RUN" == "true" ]]; then
  echo "[DRY-RUN] Validating Bicep syntax and parameters for mode '$MODE'..."
  if command -v az >/dev/null 2>&1; then
    az bicep build --file "$SCRIPT_DIR/main.bicep" --stdout >/dev/null
    echo "[DRY-RUN] az bicep build succeeded cleanly."
  else
    echo "[DRY-RUN] Azure CLI not installed locally; verified $SCRIPT_DIR/main.bicep exists."
  fi
  exit 0
fi

echo "[1/3] Ensuring Azure Resource Group '$RESOURCE_GROUP' exists..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

DEPLOY_NAME="better-canvas-$(date +%s)"
echo "[2/3] Deploying Bicep template $SCRIPT_DIR/main.bicep (name=$DEPLOY_NAME, enableBonusAi=$ENABLE_BONUS_AI)..."
if ! FQDN=$(az deployment group create \
  --name "$DEPLOY_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters \
    acrName="$ACR_NAME" \
    imageTag="$IMAGE_TAG" \
    enableBonusAi="$ENABLE_BONUS_AI" \
    enableBonusSecurity="$ENABLE_BONUS_SECURITY" \
    openRouterApiKey="$OPENROUTER_API_KEY" \
    canvasApiToken="$CANVAS_API_TOKEN" \
  --query "properties.outputs.publicGatewayFqdn.value" \
  --output tsv); then
  echo "::error::Azure deployment failed. Listing failed resource operations..."
  az deployment operation group list \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOY_NAME" \
    --query "[?properties.provisioningState=='Failed'].{Resource:properties.targetResource.resourceName, Type:properties.targetResource.resourceType, Code:properties.statusMessage.error.code, Message:properties.statusMessage.error.message}" \
    --output table || true
  exit 1
fi

echo "[3/3] Cloud deployment complete."
echo "Public Gateway URL: https://$FQDN"
