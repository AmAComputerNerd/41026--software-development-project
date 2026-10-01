<script setup lang="ts">
import { computed, ref } from 'vue'
import { askAccountHelp, type RagAnswer } from '@/api/rag'

const question = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const result = ref<RagAnswer | null>(null)
const submittedQuestion = ref('')
const valid = computed(() => question.value.trim().length >= 3 && question.value.trim().length <= 500)

async function ask() {
  if (loading.value || !valid.value) return
  loading.value = true
  error.value = null
  result.value = null
  submittedQuestion.value = question.value.trim()
  try {
    result.value = await askAccountHelp(submittedQuestion.value)
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Could not answer your account question.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="nb-panel nb-profile__section" aria-labelledby="rag-help-heading" :aria-busy="loading">
    <h2 id="rag-help-heading" class="nb-profile__section-title nb-mono">ACCOUNT HELP / RAG</h2>
    <p>Ask about profile setup, roles, login, or password resets. Answers use account documentation, not your private account data.</p>
    <form @submit.prevent="ask">
      <label for="rag-account-question" class="nb-form-label nb-mono">YOUR QUESTION</label>
      <textarea id="rag-account-question" v-model="question" class="nb-input nb-textarea"
        rows="3" minlength="3" maxlength="500" required :disabled="loading"
        placeholder="How do I reset my password if I have forgotten it?" aria-describedby="rag-account-warning" />
      <p id="rag-account-warning" class="nb-mono">Do not include passwords, reset tokens, or API keys.</p>
      <button type="submit" class="nb-btn" :disabled="loading || !valid">
        {{ loading ? 'SEARCHING ACCOUNT DOCUMENTATION...' : 'ASK ACCOUNT HELP' }}
      </button>
    </form>
    <p v-if="error" role="alert" class="nb-mono">{{ error }}</p>
    <div v-if="result" class="nb-rag-help__result" aria-live="polite">
      <p class="nb-mono">{{ submittedQuestion }}</p>
      <p class="nb-mono">
        {{ result.status === 'insufficient_context' ? 'INSUFFICIENT CONTEXT' : 'GROUNDED ANSWER' }} /
        CONFIDENCE: {{ result.confidence.toUpperCase() }}
      </p>
      <p class="nb-rag-help__answer">{{ result.answer }}</p>
      <p v-if="result.status === 'insufficient_context'">Try a question about a documented account feature. For live account setup, use the MCP readiness check above.</p>
      <div v-if="result.citations.length">
        <h3 class="nb-mono">SOURCES</h3>
        <ul class="nb-rag-help__sources">
          <li v-for="citation in result.citations" :key="`${citation.sourceId}:${citation.heading}`">
            <strong>{{ citation.title }}</strong> / {{ citation.heading }}
            <br /><span class="nb-mono">{{ citation.sourceId }}</span>
          </li>
        </ul>
      </div>
      <p class="nb-mono">{{ result.retrieval.matchedChunks }} relevant excerpts retrieved. Confidence reflects retrieval strength, not a guarantee.</p>
    </div>
  </section>
</template>

<style scoped lang="scss">
.nb-rag-help__result {
  margin-top: var(--nb-space-5);
  border-top: var(--nb-border-width-md) solid var(--nb-color-ink);
  padding-top: var(--nb-space-4);
  overflow-wrap: anywhere;
}

.nb-rag-help__answer {
  white-space: pre-wrap;
}

.nb-rag-help__sources {
  padding-left: var(--nb-space-5);

  li {
    margin-bottom: var(--nb-space-3);
  }
}
</style>
