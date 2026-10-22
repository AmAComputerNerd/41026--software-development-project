<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { useGrades } from "@/composables/useGrades";
import type {
  McpResponse,
  RagAnswerResponse,
  MultiAgentWorkflow,
  MultiAgentReviewRequest,
} from "@/api/grades";

const {
  generateRecommendation,
  getMcpWeightings,
  getRagAnswer,
  runMultiAgentWorkflow,
} = useGrades();

defineProps<{
  open: boolean;
}>();

const emit = defineEmits<{
  close: [];
}>();

const activeTab = ref<"ai" | "mcp" | "rag" | "multi-agent">("ai");

const aiLoading = ref(false);
const aiRecommendation = ref<string | null>(null);
const aiError = ref<string | null>(null);

const mcpLoading = ref(false);
const mcpWeight = ref(40);
const mcpLimit = ref(5);
const mcpResult = ref<McpResponse | null>(null);
const mcpError = ref<string | null>(null);

const ragLoading = ref(false);
const ragQuestion = ref("");
const ragResult = ref<RagAnswerResponse | null>(null);
const ragError = ref<string | null>(null);

const maLoading = ref(false);
const maWorkflow = ref<MultiAgentWorkflow | null>(null);
const maError = ref<string | null>(null);
const maReviewMode = ref(false);
const maReviewDecision = ref<"accept" | "deny">("accept");
const maReviewerName = ref("");
const maComments = ref("");
const maCorrectedOutput = ref("");

watch(
  () => activeTab.value,
  () => {
    aiRecommendation.value = null;
    aiError.value = null;
    mcpResult.value = null;
    mcpError.value = null;
    ragResult.value = null;
    ragError.value = null;
    maWorkflow.value = null;
    maError.value = null;
    maReviewMode.value = false;
    maReviewDecision.value = "accept";
    maReviewerName.value = "";
    maComments.value = "";
    maCorrectedOutput.value = "";
  },
);

async function handleAiRecommendation() {
  aiLoading.value = true;
  aiError.value = null;
  aiRecommendation.value = null;
  try {
    const response = await generateRecommendation();
    aiRecommendation.value = response.recommendation;
  } catch (err) {
    aiError.value =
      err instanceof Error
        ? err.message
        : "The AI coach is unavailable right now.";
  } finally {
    aiLoading.value = false;
  }
}

async function handleMcpWeightings() {
  mcpLoading.value = true;
  mcpError.value = null;
  mcpResult.value = null;
  try {
    const response = await getMcpWeightings(
      mcpWeight.value / 100,
      mcpLimit.value,
    );
    mcpResult.value = response;
  } catch (err) {
    mcpError.value =
      err instanceof Error ? err.message : "MCP integration failed.";
  } finally {
    mcpLoading.value = false;
  }
}

async function handleRagAnswer() {
  const question = ragQuestion.value.trim();
  if (question.length < 3 || question.length > 500) {
    ragError.value = "Question must be between 3 and 500 characters.";
    return;
  }
  ragLoading.value = true;
  ragError.value = null;
  ragResult.value = null;
  try {
    const response = await getRagAnswer(question);
    ragResult.value = response;
  } catch (err) {
    ragError.value =
      err instanceof Error ? err.message : "RAG integration failed.";
  } finally {
    ragLoading.value = false;
  }
}

async function handleMultiAgentWorkflow() {
  maLoading.value = true;
  maError.value = null;
  maWorkflow.value = null;
  try {
    const workflow = await runMultiAgentWorkflow();
    maWorkflow.value = workflow;
  } catch (err) {
    maError.value =
      err instanceof Error ? err.message : "Multi-Agent workflow failed.";
  } finally {
    maLoading.value = false;
  }
}

function startReview() {
  maReviewMode.value = true;
}

async function submitReview() {
  if (!maWorkflow.value) return;

  const request: MultiAgentReviewRequest = {
    decision: maReviewDecision.value === "accept" ? "Approved" : "Rejected",
    reviewerName: maReviewerName.value.trim() || "Student",
    comments: maComments.value.trim(),
  };

  if (maReviewDecision.value === "deny" && maCorrectedOutput.value.trim()) {
    request.correctedOutput = maCorrectedOutput.value.trim();
  }

  // For now, we just store the review locally since the backend doesn't have a review endpoint yet
  // In a full implementation, this would call the backend review endpoint
  maWorkflow.value = {
    ...maWorkflow.value,
    humanReview: {
      decision: request.decision,
      reviewerName: request.reviewerName,
      comments: request.comments,
      finalOutput:
        request.correctedOutput || maWorkflow.value.worker.draftOutput,
      reviewedAtUtc: new Date().toISOString(),
    },
    status: request.decision === "Approved" ? "Completed" : "Rejected",
  };

  maReviewMode.value = false;
  maReviewerName.value = "";
  maComments.value = "";
  maCorrectedOutput.value = "";
}

function closeDialog() {
  emit("close");
  activeTab.value = "ai";
}

const tabs = [
  { id: "ai", label: "AI COACH", icon: "✦" },
  { id: "mcp", label: "MCP TOOLS", icon: "⚙" },
  { id: "rag", label: "RAG QUERY", icon: "📖" },
  { id: "multi-agent", label: "MULTI AGENT", icon: "🤖" },
] as const;
</script>

<template>
  <Transition name="integration-modal">
    <div
      v-if="open"
      class="integration-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="integration-title"
      @click.self="closeDialog"
    >
      <article class="integration-modal__panel nb-panel">
        <header class="integration-modal__header">
          <div>
            <p class="integration-modal__eyebrow nb-mono">AI INTEGRATIONS</p>
            <h2 id="integration-title">AI TOOLS</h2>
          </div>
          <button
            type="button"
            class="nb-btn nb-btn--outline integration-modal__close"
            aria-label="Close AI integrations"
            @click="closeDialog"
          >
            ✕
          </button>
        </header>

        <nav
          class="integration-modal__tabs nb-mono"
          role="tablist"
          aria-label="AI integration tabs"
        >
          <button
            v-for="tab in tabs"
            :key="tab.id"
            :class="[
              'integration-modal__tab',
              activeTab === tab.id ? 'integration-modal__tab--active' : '',
            ]"
            :aria-selected="activeTab === tab.id"
            :aria-controls="`panel-${tab.id}`"
            :id="`tab-${tab.id}`"
            role="tab"
            @click="activeTab = tab.id"
          >
            <span class="integration-modal__tab-icon" aria-hidden="true">{{
              tab.icon
            }}</span>
            {{ tab.label }}
          </button>
        </nav>

        <div class="integration-modal__body">
          <div
            v-if="activeTab === 'ai'"
            id="panel-ai"
            role="tabpanel"
            aria-labelledby="tab-ai"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p
                v-if="aiLoading"
                class="integration-modal__placeholder nb-mono"
              >
                ASKING THE AI COACH FOR GUIDANCE...
              </p>
              <p
                v-else-if="aiError"
                class="integration-modal__error nb-mono"
                role="alert"
              >
                {{ aiError }}
              </p>
              <p v-else-if="aiRecommendation" class="integration-modal__text">
                {{ aiRecommendation }}
              </p>
              <p v-else class="integration-modal__placeholder nb-mono">
                CLICK THE BUTTON BELOW TO GET A FOCUS RECOMMENDATION BASED ON
                YOUR INCOMPLETE ASSIGNMENTS.
              </p>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="aiLoading"
                @click="handleAiRecommendation"
              >
                {{ aiLoading ? "ASKING..." : "GET AI RECOMMENDATION" }}
              </button>
            </div>
          </div>

          <div
            v-if="activeTab === 'mcp'"
            id="panel-mcp"
            role="tabpanel"
            aria-labelledby="tab-mcp"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p class="integration-modal__description nb-mono">
                RETRIEVE HIGH-WEIGHT ASSIGNMENTS VIA MCP TOOL.
              </p>

              <div class="integration-form">
                <div class="form-group">
                  <label for="mcp-weight" class="nb-mono"
                    >MINIMUM WEIGHT %</label
                  >
                  <input
                    id="mcp-weight"
                    type="number"
                    v-model.number="mcpWeight"
                    class="nb-input"
                    min="1"
                    max="100"
                    step="1"
                  />
                </div>
                <div class="form-group">
                  <label for="mcp-limit" class="nb-mono">MAX RESULTS</label>
                  <input
                    id="mcp-limit"
                    type="number"
                    v-model.number="mcpLimit"
                    class="nb-input"
                    min="1"
                    max="10"
                    step="1"
                  />
                </div>
              </div>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="mcpLoading"
                @click="handleMcpWeightings"
              >
                {{ mcpLoading ? "QUERYING MCP..." : "RUN MCP TOOL" }}
              </button>

              <div
                v-if="mcpError"
                class="integration-modal__error nb-mono"
                role="alert"
              >
                {{ mcpError }}
              </div>

              <div v-if="mcpResult" class="mcp-results">
                <div class="mcp-results__header nb-mono">
                  <span>TOOL: {{ mcpResult.tool }}</span>
                  <span>STATUS: {{ mcpResult.status.toUpperCase() }}</span>
                </div>
                <div v-if="mcpResult.data" class="mcp-results__data">
                  <p class="nb-mono">
                    WEIGHT THRESHOLD:
                    {{ (mcpResult.data.weight * 100).toFixed(0) }}%
                  </p>
                  <p class="nb-mono">ITEMS FOUND: {{ mcpResult.data.count }}</p>
                  <table class="mcp-results__table">
                    <thead>
                      <tr>
                        <th class="nb-mono">ASSIGNMENT</th>
                        <th class="nb-mono">WEIGHT %</th>
                        <th class="nb-mono">MAX MARK</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr
                        v-for="item in mcpResult.data.items"
                        :key="item.assignmentId"
                      >
                        <td>{{ item.name }}</td>
                        <td class="nb-mono">
                          {{
                            item.weight != null
                              ? (item.weight * 100).toFixed(0)
                              : "—"
                          }}
                        </td>
                        <td class="nb-mono">{{ item.maxMark ?? "—" }}</td>
                      </tr>
                      <tr v-if="mcpResult.data.items.length === 0">
                        <td colspan="3" class="nb-mono">
                          NO ASSIGNMENTS MATCH CRITERIA
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <div
                  v-else-if="mcpResult.error"
                  class="mcp-results__error nb-mono"
                >
                  ERROR: {{ mcpResult.error.message }} ({{
                    mcpResult.error.code
                  }})
                </div>
              </div>
            </div>
          </div>

          <div
            v-if="activeTab === 'rag'"
            id="panel-rag"
            role="tabpanel"
            aria-labelledby="tab-rag"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p class="integration-modal__description nb-mono">
                QUERY PROJECT DOCUMENTATION USING RAG SERVICE.
              </p>

              <div class="integration-form">
                <div class="form-group">
                  <label for="rag-question" class="nb-mono"
                    >YOUR QUESTION</label
                  >
                  <textarea
                    id="rag-question"
                    v-model="ragQuestion"
                    class="nb-input"
                    rows="4"
                    placeholder="Ask about grading policies, course requirements, academic procedures..."
                    :disabled="ragLoading"
                    @keydown.enter.exact="handleRagAnswer"
                  ></textarea>
                </div>
              </div>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="ragLoading || !ragQuestion.trim()"
                @click="handleRagAnswer"
              >
                {{ ragLoading ? "QUERYING RAG..." : "ASK RAG SERVICE" }}
              </button>

              <div
                v-if="ragError"
                class="integration-modal__error nb-mono"
                role="alert"
              >
                {{ ragError }}
              </div>

              <div v-if="ragResult" class="rag-results">
                <div class="rag-results__header nb-mono">
                  <span>STATUS: {{ ragResult.status.toUpperCase() }}</span>
                  <span>CONFIDENCE: {{ ragResult.confidence }}</span>
                </div>
                <p class="rag-results__answer">{{ ragResult.answer }}</p>

                <div
                  v-if="ragResult.citations.length"
                  class="rag-results__citations"
                >
                  <h4 class="nb-mono">SOURCES</h4>
                  <ul>
                    <li
                      v-for="citation in ragResult.citations"
                      :key="citation.sourceId"
                    >
                      <strong>{{ citation.title }}</strong> —
                      {{ citation.heading }}
                      <span class="nb-mono"
                        >(score: {{ citation.score.toFixed(2) }})</span
                      >
                    </li>
                  </ul>
                </div>

                <div class="rag-results__retrieval nb-mono">
                  MATCHED CHUNKS: {{ ragResult.retrieval.matchedChunks }} /
                  {{ ragResult.retrieval.consideredChunks }} CONSIDERED
                </div>
              </div>
            </div>
          </div>

          <div
            v-if="activeTab === 'multi-agent'"
            id="panel-multi-agent"
            role="tabpanel"
            aria-labelledby="tab-multi-agent"
            class="integration-modal__panel-content"
          >
            <div class="integration-section">
              <p class="integration-modal__description nb-mono">
                RUN MULTI-AGENT WORKFLOW FOR GRADE FOCUS RECOMMENDATIONS.
              </p>

              <button
                type="button"
                class="nb-btn nb-btn--accent integration-modal__action"
                :disabled="maLoading"
                @click="handleMultiAgentWorkflow"
              >
                {{
                  maLoading
                    ? "RUNNING MULTI-AGENT WORKFLOW..."
                    : "RUN MULTI-AGENT WORKFLOW"
                }}
              </button>

              <div
                v-if="maError"
                class="integration-modal__error nb-mono"
                role="alert"
              >
                {{ maError }}
              </div>

              <div v-if="maWorkflow" class="ma-results">
                <div class="ma-results__header nb-mono">
                  <span>GOAL: {{ maWorkflow.goal }}</span>
                  <span>STATUS: {{ maWorkflow.status.toUpperCase() }}</span>
                </div>

                <div class="ma-results__stage">
                  <h4 class="nb-mono">PLANNER</h4>
                  <p class="ma-results__text">
                    {{ maWorkflow.planner.objective }}
                  </p>
                  <div
                    v-if="maWorkflow.planner.steps.length"
                    class="ma-results__steps"
                  >
                    <div
                      v-for="(step, index) in maWorkflow.planner.steps"
                      :key="index"
                      class="ma-results__step nb-mono"
                    >
                      {{ index + 1 }}. {{ step }}
                    </div>
                  </div>
                  <div
                    v-if="maWorkflow.planner.requiredEvidence.length"
                    class="ma-results__evidence"
                  >
                    <strong class="nb-mono">REQUIRED EVIDENCE:</strong>
                    <ul>
                      <li
                        v-for="(evidence, index) in maWorkflow.planner
                          .requiredEvidence"
                        :key="index"
                        class="nb-mono"
                      >
                        {{ evidence }}
                      </li>
                    </ul>
                  </div>
                </div>

                <div class="ma-results__stage">
                  <h4 class="nb-mono">WORKER</h4>
                  <p class="ma-results__text">
                    {{ maWorkflow.worker.evidenceSummary }}
                  </p>
                  <p class="ma-results__draft">
                    {{ maWorkflow.worker.draftOutput }}
                  </p>
                  <div
                    v-if="maWorkflow.worker.recommendedActions.length"
                    class="ma-results__actions"
                  >
                    <strong class="nb-mono">RECOMMENDED ACTIONS:</strong>
                    <ul>
                      <li
                        v-for="(action, index) in maWorkflow.worker
                          .recommendedActions"
                        :key="index"
                        class="nb-mono"
                      >
                        {{ action }}
                      </li>
                    </ul>
                  </div>
                </div>

                <div class="ma-results__stage">
                  <h4 class="nb-mono">REVIEWER</h4>
                  <p class="ma-results__text">
                    <strong class="nb-mono">VERDICT:</strong>
                    {{ maWorkflow.reviewer.verdict }}
                  </p>
                  <p class="ma-results__text">
                    <strong class="nb-mono">CONFIDENCE:</strong>
                    {{ maWorkflow.reviewer.confidence }}
                  </p>
                  <p class="ma-results__text">
                    {{ maWorkflow.reviewer.critique }}
                  </p>
                  <div
                    v-if="maWorkflow.reviewer.identifiedRisks.length"
                    class="ma-results__risks"
                  >
                    <strong class="nb-mono">IDENTIFIED RISKS:</strong>
                    <ul>
                      <li
                        v-for="(risk, index) in maWorkflow.reviewer
                          .identifiedRisks"
                        :key="index"
                        class="nb-mono"
                      >
                        {{ risk }}
                      </li>
                    </ul>
                  </div>
                  <div
                    v-if="maWorkflow.reviewer.recommendations.length"
                    class="ma-results__recommendations"
                  >
                    <strong class="nb-mono">RECOMMENDATIONS:</strong>
                    <ul>
                      <li
                        v-for="(rec, index) in maWorkflow.reviewer
                          .recommendations"
                        :key="index"
                        class="nb-mono"
                      >
                        {{ rec }}
                      </li>
                    </ul>
                  </div>
                </div>

                <div
                  v-if="!maReviewMode && !maWorkflow.humanReview"
                  class="ma-review-prompt"
                >
                  <button
                    type="button"
                    class="nb-btn nb-btn--accent integration-modal__action"
                    @click="startReview"
                  >
                    REVIEW RECOMMENDATION
                  </button>
                </div>

                <div v-if="maReviewMode" class="ma-review-form">
                  <h4 class="nb-mono">HUMAN REVIEW</h4>
                  <div class="integration-form">
                    <div class="form-group">
                      <label class="nb-mono">DECISION</label>
                      <div class="ma-decision-buttons">
                        <button
                          type="button"
                          class="nb-btn"
                          :class="
                            maReviewDecision === 'accept'
                              ? 'nb-btn--accent'
                              : 'nb-btn--outline'
                          "
                          @click="maReviewDecision = 'accept'"
                        >
                          ACCEPT
                        </button>
                        <button
                          type="button"
                          class="nb-btn"
                          :class="
                            maReviewDecision === 'deny'
                              ? 'nb-btn--accent'
                              : 'nb-btn--outline'
                          "
                          @click="maReviewDecision = 'deny'"
                        >
                          DENY
                        </button>
                      </div>
                    </div>
                    <div class="form-group">
                      <label for="ma-reviewer" class="nb-mono">YOUR NAME</label>
                      <input
                        id="ma-reviewer"
                        type="text"
                        v-model="maReviewerName"
                        class="nb-input"
                        placeholder="Enter your name"
                      />
                    </div>
                    <div class="form-group" style="flex-basis: 100%">
                      <label for="ma-comments" class="nb-mono">COMMENTS</label>
                      <textarea
                        id="ma-comments"
                        v-model="maComments"
                        class="nb-input"
                        rows="3"
                        placeholder="Add your comments..."
                      ></textarea>
                    </div>
                    <div
                      v-if="maReviewDecision === 'deny'"
                      class="form-group"
                      style="flex-basis: 100%"
                    >
                      <label for="ma-corrected" class="nb-mono"
                        >CORRECTED OUTPUT (OPTIONAL)</label
                      >
                      <textarea
                        id="ma-corrected"
                        v-model="maCorrectedOutput"
                        class="nb-input"
                        rows="3"
                        placeholder="Provide corrected recommendation..."
                      ></textarea>
                    </div>
                  </div>
                  <div class="ma-review-actions">
                    <button
                      type="button"
                      class="nb-btn nb-btn--outline"
                      @click="maReviewMode = false"
                    >
                      CANCEL
                    </button>
                    <button
                      type="button"
                      class="nb-btn nb-btn--accent"
                      :disabled="
                        !maReviewerName.trim() ||
                        (maReviewDecision === 'deny' &&
                          !maCorrectedOutput.trim())
                      "
                      @click="submitReview"
                    >
                      SUBMIT REVIEW
                    </button>
                  </div>
                </div>

                <div v-if="maWorkflow.humanReview" class="ma-human-review">
                  <div class="ma-human-review__header nb-mono">
                    <span
                      >DECISION:
                      {{ maWorkflow.humanReview.decision.toUpperCase() }}</span
                    >
                    <span
                      >REVIEWER: {{ maWorkflow.humanReview.reviewerName }}</span
                    >
                  </div>
                  <p class="ma-human-review__comments">
                    <strong class="nb-mono">COMMENTS:</strong>
                    {{ maWorkflow.humanReview.comments }}
                  </p>
                  <p class="ma-human-review__final">
                    <strong class="nb-mono">FINAL OUTPUT:</strong>
                    {{ maWorkflow.humanReview.finalOutput }}
                  </p>
                  <p class="ma-human-review__date nb-mono">
                    REVIEWED:
                    {{
                      new Date(
                        maWorkflow.humanReview.reviewedAtUtc,
                      ).toLocaleString()
                    }}
                  </p>
                </div>

                <div class="ma-audit">
                  <h4 class="nb-mono">AUDIT LOG</h4>
                  <ul>
                    <li
                      v-for="(entry, index) in maWorkflow.auditLog"
                      :key="index"
                      class="ma-audit__entry nb-mono"
                    >
                      <span class="ma-audit__time">{{
                        new Date(entry.timestampUtc).toLocaleTimeString()
                      }}</span>
                      <span class="ma-audit__agent">[{{ entry.agent }}]</span>
                      <span class="ma-audit__action">{{ entry.action }}</span>
                      <span class="ma-audit__summary">{{ entry.summary }}</span>
                    </li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>

        <footer class="integration-modal__footer nb-mono">
          AI-GENERATED CONTENT • VERIFY BEFORE MAKING ACADEMIC DECISIONS
        </footer>
      </article>
    </div>
  </Transition>
</template>

<style scoped>
.integration-modal {
  position: fixed;
  inset: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--nb-space-5);
  background: rgba(0, 0, 0, 0.55);
}

.integration-modal__panel {
  width: min(720px, 100%);
  max-height: min(85vh, 800px);
  display: flex;
  flex-direction: column;
  background: var(--nb-color-white);
}

.integration-modal__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--nb-space-4);
  padding: var(--nb-space-5) var(--nb-space-5) var(--nb-space-3);
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.integration-modal__eyebrow {
  margin: 0 0 var(--nb-space-1);
  color: var(--nb-color-muted);
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
}

.integration-modal__header h2 {
  margin: 0;
  font-size: 26px;
  letter-spacing: -1px;
  text-transform: uppercase;
}

.integration-modal__close {
  background: var(--nb-color-accent-orange);
}

.integration-modal__tabs {
  display: flex;
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.integration-modal__tab {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--nb-space-2);
  padding: var(--nb-space-3) var(--nb-space-4);
  border: none;
  background: transparent;
  color: var(--nb-color-muted);
  font-size: 11px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  cursor: pointer;
  transition:
    background 120ms ease,
    color 120ms ease;
  border-bottom: var(--nb-border-width-md) solid transparent;
  margin-bottom: calc(var(--nb-border-width-sm) * -1);
}

.integration-modal__tab:hover:not(.integration-modal__tab--active) {
  color: var(--nb-color-ink);
  background: var(--nb-color-white);
}

.integration-modal__tab--active {
  color: var(--nb-color-ink);
  background: var(--nb-color-white);
  border-bottom-color: var(--nb-color-accent-orange);
}

.integration-modal__tab-icon {
  font-size: 13px;
}

.integration-modal__body {
  padding: var(--nb-space-5);
  overflow-y: auto;
  flex: 1;
}

.integration-section {
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-4);
}

.integration-modal__description {
  margin: 0;
  font-size: 11px;
  color: var(--nb-color-muted);
}

.integration-form {
  display: flex;
  gap: var(--nb-space-4);
  flex-wrap: wrap;
}

.form-group {
  flex: 1;
  min-width: 150px;
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-1);
}

.form-group label {
  font-size: 10px;
  color: var(--nb-color-muted);
}

.integration-modal__action {
  width: 100%;
  margin-top: var(--nb-space-2);
}

.integration-modal__placeholder {
  margin: 0;
  font-size: 13px;
  color: var(--nb-color-muted);
}

.integration-modal__error {
  margin: 0;
  font-size: 12px;
  color: var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  padding: var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.integration-modal__text {
  margin: 0;
  font-size: 15px;
  line-height: 1.5;
}

.mcp-results {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.mcp-results__header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--nb-space-3);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.mcp-results__data p {
  margin: var(--nb-space-1) 0;
  font-size: 12px;
}

.mcp-results__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.mcp-results__table th,
.mcp-results__table td {
  padding: var(--nb-space-2);
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
  text-align: left;
}

.mcp-results__table th {
  color: var(--nb-color-muted);
  font-weight: var(--nb-font-weight-bold);
}

.mcp-results__table tr:last-child td {
  border-bottom: none;
}

.mcp-results__error {
  color: var(--nb-color-ink);
  background: var(--nb-color-accent-yellow);
  padding: var(--nb-space-3);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.rag-results {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.rag-results__header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--nb-space-3);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.rag-results__answer {
  margin: 0 0 var(--nb-space-4);
  font-size: 14px;
  line-height: 1.6;
}

.rag-results__citations {
  margin-top: var(--nb-space-4);
  padding-top: var(--nb-space-3);
  border-top: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.rag-results__citations h4 {
  margin: 0 0 var(--nb-space-2);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.rag-results__citations ul {
  margin: 0;
  padding-left: var(--nb-space-4);
  font-size: 12px;
}

.rag-results__citations li {
  margin-bottom: var(--nb-space-2);
  display: flex;
  flex-direction: column;
  gap: var(--nb-space-1);
}

.rag-results__citations span {
  font-size: 10px;
  color: var(--nb-color-muted);
}

.rag-results__retrieval {
  margin-top: var(--nb-space-3);
  padding-top: var(--nb-space-3);
  border-top: var(--nb-border-width-sm) solid var(--nb-color-ink);
  font-size: 10px;
  color: var(--nb-color-muted);
}

.integration-modal__footer {
  padding: var(--nb-space-3) var(--nb-space-5);
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  font-size: 10px;
  font-weight: var(--nb-font-weight-bold);
  letter-spacing: 0.5px;
  text-align: center;
}

.integration-enter-active,
.integration-leave-active {
  transition: opacity 160ms ease;
}

.integration-enter-active .integration-modal__panel,
.integration-leave-active .integration-modal__panel {
  transition:
    transform 200ms ease,
    opacity 200ms ease;
}

.integration-enter-from,
.integration-leave-to {
  opacity: 0;
}

.integration-enter-from .integration-modal__panel,
.integration-leave-to .integration-modal__panel {
  transform: translateY(20px);
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .integration-enter-active,
  .integration-leave-active,
  .integration-enter-active .integration-modal__panel,
  .integration-leave-active .integration-modal__panel {
    transition: none;
  }
}

.ma-results {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.ma-results__header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--nb-space-4);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.ma-results__stage {
  margin-bottom: var(--nb-space-5);
  padding-bottom: var(--nb-space-4);
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.ma-results__stage:last-of-type {
  border-bottom: none;
  margin-bottom: 0;
  padding-bottom: 0;
}

.ma-results__stage h4 {
  margin: 0 0 var(--nb-space-3);
  font-size: 12px;
  color: var(--nb-color-ink);
}

.ma-results__text {
  margin: 0 0 var(--nb-space-2);
  font-size: 13px;
  line-height: 1.5;
}

.ma-results__draft {
  margin: 0;
  padding: var(--nb-space-3);
  background: var(--nb-color-white);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  font-size: 14px;
  line-height: 1.6;
  font-style: italic;
}

.ma-results__steps,
.ma-results__evidence,
.ma-results__actions,
.ma-results__risks,
.ma-results__recommendations {
  margin-top: var(--nb-space-3);
}

.ma-results__steps ul,
.ma-results__evidence ul,
.ma-results__actions ul,
.ma-results__risks ul,
.ma-results__recommendations ul {
  margin: var(--nb-space-2) 0 0;
  padding-left: var(--nb-space-4);
  font-size: 12px;
}

.ma-results__steps li,
.ma-results__evidence li,
.ma-results__actions li,
.ma-results__risks li,
.ma-results__recommendations li {
  margin-bottom: var(--nb-space-1);
}

.ma-results__step {
  margin: var(--nb-space-1) 0;
  padding-left: var(--nb-space-3);
  border-left: var(--nb-border-width-sm) solid var(--nb-color-accent-orange);
}

.ma-review-prompt {
  text-align: center;
  margin: var(--nb-space-4) 0;
}

.ma-review-form {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-md) solid var(--nb-color-accent-orange);
  background: var(--nb-color-white);
}

.ma-decision-buttons {
  display: flex;
  gap: var(--nb-space-3);
}

.ma-decision-buttons .nb-btn {
  flex: 1;
  font-weight: var(--nb-font-weight-bold);
}

.ma-review-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--nb-space-3);
  margin-top: var(--nb-space-4);
  padding-top: var(--nb-space-3);
  border-top: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.ma-human-review {
  margin-top: var(--nb-space-4);
  padding: var(--nb-space-4);
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
}

.ma-human-review__header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--nb-space-3);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.ma-human-review__comments,
.ma-human-review__final,
.ma-human-review__date {
  margin: var(--nb-space-2) 0;
  font-size: 13px;
}

.ma-human-review__final {
  font-style: italic;
}

.ma-audit {
  margin-top: var(--nb-space-4);
  padding-top: var(--nb-space-3);
  border-top: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.ma-audit h4 {
  margin: 0 0 var(--nb-space-3);
  font-size: 11px;
  color: var(--nb-color-muted);
}

.ma-audit ul {
  margin: 0;
  padding: 0;
  list-style: none;
}

.ma-audit__entry {
  display: flex;
  gap: var(--nb-space-2);
  margin-bottom: var(--nb-space-2);
  padding: var(--nb-space-2);
  font-size: 10px;
  background: var(--nb-color-white);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.ma-audit__time {
  color: var(--nb-color-muted);
  min-width: 70px;
}

.ma-audit__agent {
  color: var(--nb-color-accent-orange);
  min-width: 80px;
  font-weight: var(--nb-font-weight-bold);
}

.ma-audit__action {
  color: var(--nb-color-ink);
  min-width: 100px;
  font-weight: var(--nb-font-weight-bold);
}

.ma-audit__summary {
  color: var(--nb-color-ink);
  flex: 1;
}
</style>
