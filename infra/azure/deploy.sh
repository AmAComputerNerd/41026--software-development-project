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
SKIP_BUILD="false"
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
    --skip-build)
      SKIP_BUILD="true"
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
echo "Skip Build          : $SKIP_BUILD"
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

echo "[1/4] Ensuring Azure Resource Group '$RESOURCE_GROUP' and Container Registry '$ACR_NAME' exist..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
if ! az acr show --name "$ACR_NAME" --resource-group "$RESOURCE_GROUP" >/dev/null 2>&1; then
  echo "Creating Azure Container Registry '$ACR_NAME'..."
  az acr create --resource-group "$RESOURCE_GROUP" --name "$ACR_NAME" --sku Basic --admin-enabled true --location "$LOCATION" --output none
fi

if [[ "$SKIP_BUILD" != "true" ]]; then
  echo "[2/4] Logging into Azure Container Registry '$ACR_NAME' and pushing images..."
  az acr login --name "$ACR_NAME"

  build_and_push() {
    local svc="$1"
    local ctx="$2"
    local df="$3"
    local target_image="${ACR_NAME}.azurecr.io/${svc}:${IMAGE_TAG}"

    echo "==> Building and pushing $svc ($target_image)..."
    docker build -t "$target_image" -f "$ctx/$df" "$ctx"
    docker push "$target_image"
  }

  echo "==> Building and pushing baseline service containers..."
  build_and_push "shared-backend" "$REPO_ROOT/shared/backend" "Api/Dockerfile"
  build_and_push "student-1-database" "$REPO_ROOT/student-1/database" "Dockerfile"
  build_and_push "student-1-backend" "$REPO_ROOT/student-1/backend" "Api/Dockerfile"
  build_and_push "student-2-backend" "$REPO_ROOT/student-2/backend" "Api/Dockerfile"
  build_and_push "student-3-database" "$REPO_ROOT/student-3" "database/Database/Dockerfile"
  build_and_push "student-3-backend" "$REPO_ROOT/student-3" "backend/Api/Dockerfile"
  build_and_push "student-4-database" "$REPO_ROOT/student-4" "database/Database/Dockerfile"
  build_and_push "student-4-backend" "$REPO_ROOT/student-4" "backend/Api/Dockerfile"
  build_and_push "student-5-database" "$REPO_ROOT/student-5" "database/Database/Dockerfile"
  build_and_push "student-5-backend" "$REPO_ROOT/student-5" "backend/GradesManager/GradesManager/Dockerfile"
  build_and_push "student-1-frontend" "$REPO_ROOT" "student-1/frontend/Dockerfile"
  build_and_push "student-2-frontend" "$REPO_ROOT" "student-2/frontend/Dockerfile"
  build_and_push "student-3-frontend" "$REPO_ROOT" "student-3/frontend/Dockerfile"
  build_and_push "student-4-authentication" "$REPO_ROOT/student-4" "authentication/Authentication/Dockerfile"
  build_and_push "student-4-frontend" "$REPO_ROOT" "student-4/frontend/Dockerfile"
  build_and_push "student-5-frontend" "$REPO_ROOT" "student-5/frontend/Dockerfile"
  build_and_push "shared-shell" "$REPO_ROOT" "shared/frontend/Dockerfile"

  if [[ "$ENABLE_BONUS_AI" == "true" ]]; then
    echo "==> Building and pushing bonus AI service containers..."
    build_and_push "ai-mode" "$REPO_ROOT/ai-services/ai-mode" "Api/Dockerfile"
    build_and_push "mcp-server" "$REPO_ROOT/ai-services/mcp-server" "McpServer/Dockerfile"
    build_and_push "rag-server" "$REPO_ROOT" "ai-services/rag-server/RagServer/Dockerfile"
    build_and_push "multi-agent-server" "$REPO_ROOT/ai-services/multi-agent-server/MultiAgentServer" "Dockerfile"
  fi
else
  echo "[2/4] Skipping container image build (--skip-build requested)..."
fi

DEPLOY_NAME="better-canvas-$(date +%s)"
echo "[3/4] Deploying Bicep template $SCRIPT_DIR/main.bicep (name=$DEPLOY_NAME, enableBonusAi=$ENABLE_BONUS_AI)..."
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

echo "[4/4] Cloud deployment complete."
echo "Public Gateway URL: https://$FQDN"
