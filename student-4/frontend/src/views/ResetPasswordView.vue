<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { resetPassword } from '@/api/users'
import { ApiError } from '@/api/http'

const router = useRouter()

const token = ref<string | null>(null)
const newPassword = ref('')
const confirmPassword = ref('')
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const loading = ref(false)

const isFormValid = computed(() => {
  return (
    !!token.value &&
    newPassword.value.length >= 8 &&
    newPassword.value === confirmPassword.value
  )
})

onMounted(() => {
  // Read the token from the query string. If there's no token we
  // show an error and don't let the user submit — the link is bad.
  const params = new URLSearchParams(window.location.search)
  token.value = params.get('token')
  if (!token.value) {
    error.value =
      'This reset link is missing a token. Please request a new one from the login page.'
  }
})

async function handleSubmit() {
  error.value = null
  success.value = null

  if (!token.value) {
    error.value =
      'This reset link is missing a token. Please request a new one from the login page.'
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = 'Passwords do not match.'
    return
  }

  loading.value = true
  try {
    const result = await resetPassword(token.value, newPassword.value)
    success.value =
      result.message ?? 'Password reset successfully. Redirecting to login…'

    // Give the user a moment to read the success message, then
    // redirect to the login page.
    setTimeout(() => {
      router.push('/')
    }, 1500)
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
      // The server's "this link is invalid or has expired" message
      // is already user-friendly; surface it verbatim.
      error.value =
        (await err.body) ||
        'This reset link is invalid or has expired. Please request a new one.'
    } else {
      error.value = err instanceof Error ? err.message : 'Failed to reset password.'
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="nb-auth-page">
    <div class="nb-panel nb-auth__card">
      <div class="nb-auth__header">
        <h1 class="nb-auth__title nb-mono">RESET PASSWORD</h1>
        <p class="nb-auth__subtitle">
          Choose a new password for your Student 4 account.
        </p>
      </div>

      <div v-if="error" class="nb-auth__error nb-panel nb-mono">{{ error }}</div>
      <div v-if="success" class="nb-auth__success nb-panel nb-mono">{{ success }}</div>

      <form
        v-if="!success && token"
        @submit.prevent="handleSubmit"
        class="nb-auth__form"
      >
        <div class="nb-auth__section">
          <h2 class="nb-auth__section-title nb-mono">NEW PASSWORD</h2>

          <div class="nb-form-group">
            <label for="newPassword" class="nb-form-label nb-mono">PASSWORD</label>
            <input
              id="newPassword"
              type="password"
              v-model="newPassword"
              class="nb-input"
              :disabled="loading"
              required
              minlength="8"
              autocomplete="new-password"
            />
            <p class="nb-form-hint nb-mono">AT LEAST 8 CHARACTERS</p>
          </div>

          <div class="nb-form-group">
            <label for="confirmPassword" class="nb-form-label nb-mono">CONFIRM PASSWORD</label>
            <input
              id="confirmPassword"
              type="password"
              v-model="confirmPassword"
              class="nb-input"
              :disabled="loading"
              required
              autocomplete="new-password"
            />
            <p
              v-if="confirmPassword && newPassword !== confirmPassword"
              class="nb-form-error nb-mono"
            >
              PASSWORDS DO NOT MATCH
            </p>
          </div>
        </div>

        <div class="nb-auth__actions">
          <button
            type="submit"
            class="nb-btn"
            :disabled="loading || !isFormValid"
          >
            {{ loading ? 'RESETTING…' : 'RESET PASSWORD' }}
          </button>
        </div>
      </form>

      <div class="nb-auth__footer">
        <p class="nb-mono">
          <RouterLink to="/" class="nb-link">BACK TO LOG IN</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.nb-auth-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - 140px);
  padding: 24px;
}

.nb-auth__card {
  width: 100%;
  max-width: 480px;
  padding: 20px;
}

.nb-auth__header {
  text-align: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: var(--nb-border-width-md) solid var(--nb-color-ink);
}

.nb-auth__title {
  font-size: 24px;
  font-weight: 700;
  margin: 0 0 8px;
}

.nb-auth__subtitle {
  font-size: 14px;
  color: var(--nb-color-muted);
  margin: 0;
}

.nb-auth__error {
  background: var(--nb-color-white);
  color: var(--nb-color-accent-orange);
  border-color: var(--nb-color-accent-orange);
  padding: 12px 16px;
  margin-bottom: 16px;
  text-align: center;
}

.nb-auth__success {
  background: var(--nb-color-white);
  color: var(--nb-color-ink);
  border-color: var(--nb-color-ink);
  padding: 12px 16px;
  margin-bottom: 16px;
  text-align: center;
}

.nb-auth__section {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.nb-auth__section:last-of-type {
  border-bottom: none;
  margin-bottom: 16px;
  padding-bottom: 0;
}

.nb-auth__section-title {
  font-size: 12px;
  font-weight: var(--nb-font-weight-bold);
  margin: 0 0 16px;
  color: var(--nb-color-muted);
}

.nb-form-group {
  margin-bottom: 16px;
}

.nb-form-label {
  display: block;
  font-size: 11px;
  font-weight: var(--nb-font-weight-semibold);
  letter-spacing: 0.5px;
  text-transform: uppercase;
  margin-bottom: 6px;
  color: var(--nb-color-ink);
}

.nb-input,
.nb-select {
  width: 100%;
  border: var(--nb-border-width-md) solid var(--nb-color-ink);
  background: var(--nb-color-bg);
  color: var(--nb-color-ink);
  font-family: var(--nb-font-display);
  font-size: 14px;
  padding: 10px 12px;
  box-shadow: var(--nb-shadow);
}

.nb-input:focus,
.nb-select:focus {
  outline: none;
  background: var(--nb-color-white);
}

.nb-input:disabled,
.nb-select:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.nb-form-hint {
  margin: 6px 0 0;
  font-size: 11px;
  color: var(--nb-color-muted);
}

.nb-form-error {
  margin: 6px 0 0;
  font-size: 11px;
  color: var(--nb-color-accent-orange);
}

.nb-auth__actions {
  margin-top: 24px;
}

.nb-auth__actions .nb-btn {
  width: 100%;
  padding: 14px 24px;
  font-size: 13px;
}

.nb-auth__footer {
  margin-top: 24px;
  padding-top: 16px;
  border-top: var(--nb-border-width-md) solid var(--nb-color-ink);
  text-align: center;
}

.nb-auth__footer p {
  margin: 8px 0;
  font-size: 12px;
}

.nb-link {
  color: var(--nb-color-ink);
  text-decoration: underline;
  text-decoration-thickness: 2px;
  text-underline-offset: 4px;
}
</style>
