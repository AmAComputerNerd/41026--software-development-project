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
      result.message ?? 'Password reset successfully. Redirecting to loginâ€¦'

    setTimeout(() => {
      router.push('/')
    }, 1500)
  } catch (err) {
    if (err instanceof ApiError && err.status === 400) {
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
            {{ loading ? 'RESETTINGâ€¦' : 'RESET PASSWORD' }}
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

