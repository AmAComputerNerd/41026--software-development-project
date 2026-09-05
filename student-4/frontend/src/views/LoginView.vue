<script setup lang="ts">
import { ref, computed } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { createUser, login, type CreateUserRequest } from '@/api/users'
import { ApiError } from '@/api/http'

const { fetchUser } = useAuth()

// Form state
const isLoginMode = ref(true)
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const firstName = ref('')
const lastName = ref('')
const middleNames = ref('')
const gender = ref<'Male' | 'Female' | 'NonBinary'>('Male')
const dateOfBirth = ref('')
const userType = ref<'Student' | 'Teacher' | 'Admin'>('Student')
const studentCourseStatus = ref<'FullTime' | 'PartTime' | 'Inactive'>('FullTime')
const studentIsInternational = ref(false)
const studentCanvasApiKey = ref('')
const teacherEmploymentStatus = ref<'FullTime' | 'PartTime' | 'Inactive'>('FullTime')
const teacherCanvasApiKey = ref('')

// Error/success messages
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const loading = ref(false)

// Validation
const isFormValid = computed(() => {
  if (isLoginMode.value) {
    return email.value && password.value
  }
  return (
    email.value &&
    password.value &&
    confirmPassword.value &&
    password.value === confirmPassword.value &&
    firstName.value &&
    lastName.value &&
    dateOfBirth.value &&
    userType.value // Ensure account type is selected
  )
})

function toggleMode() {
  isLoginMode.value = !isLoginMode.value
  error.value = null
  success.value = null
}

async function handleSubmit() {
  error.value = null
  success.value = null
  loading.value = true

  try {
    if (isLoginMode.value) {
      // Call the backend's /api/auth/login endpoint, which validates
      // email + password and returns the UserDto on success or 401
      // on a mismatch.
      const user = await login(email.value, password.value)

      // Hydrate the auth store and persist the user ID so the
      // profile page (and any other page) knows who is signed in.
      await fetchUser(user.id)
      localStorage.setItem('userId', user.id)

      success.value = 'Login successful! Redirecting...'

      // Redirect to profile after a short delay
      setTimeout(() => {
        window.location.href = '/account/profile'
      }, 1000)
    } else {
      // Sign up
      const userData: CreateUserRequest = {
        email: email.value,
        passwordHash: password.value, // In real app, hash this on backend
        firstName: firstName.value,
        middleNames: middleNames.value || undefined,
        lastName: lastName.value,
        gender: gender.value,
        dateOfBirth: dateOfBirth.value,
        userType: userType.value,
      }

      if (userType.value === 'Student') {
        userData.studentDto = {
          userId: '00000000-0000-0000-0000-000000000000', // Will be set by backend
          courseStatus: studentCourseStatus.value,
          isInternational: studentIsInternational.value,
          canvasApiKey: studentCanvasApiKey.value,
        }
      } else if (userType.value === 'Teacher') {
        userData.teacherDto = {
          userId: '00000000-0000-0000-0000-000000000000',
          employmentStatus: teacherEmploymentStatus.value,
          canvasApiKey: teacherCanvasApiKey.value,
        }
      }

      const newUser = await createUser(userData)
      
      // Persist the new user's ID so the profile page picks them up
      // automatically — no second login round-trip required.
      localStorage.setItem('userId', newUser.id)
      
      success.value = 'Account created successfully! Redirecting to your profile...'
      
      // Redirect straight to the profile after a short delay
      setTimeout(() => {
        window.location.href = '/account/profile'
      }, 1000)
    }
  } catch (err) {
    // Map the 401 from the auth endpoint to a friendlier message —
    // we don't want to surface "401 Unauthorized" to the user.
    if (isLoginMode.value && err instanceof ApiError && err.status === 401) {
      error.value = 'Invalid email or password.'
    } else if (isLoginMode.value && err instanceof Error && err.message.includes('401')) {
      // Fallback if a non-ApiError somehow surfaces (defensive).
      error.value = 'Invalid email or password.'
    } else {
      error.value = err instanceof Error ? err.message : 'An error occurred'
    }
  } finally {
    loading.value = false
  }
}

function handleForgotPassword() {
  // In a real app, this would trigger a password reset email
  alert('Password reset functionality would be implemented here. This would send a reset link to your email.')
}
</script>

<template>
  <div class="nb-auth-page">
    <div class="nb-panel nb-auth__card">
      <div class="nb-auth__header">
        <h1 class="nb-auth__title nb-mono">{{ isLoginMode ? 'LOG IN' : 'CREATE ACCOUNT' }}</h1>
        <p class="nb-auth__subtitle">
          {{ isLoginMode ? 'Enter your credentials to access your account' : 'Fill in your details to create a new account' }}
        </p>
      </div>

      <div v-if="error" class="nb-auth__error nb-panel nb-mono">{{ error }}</div>
      <div v-if="success" class="nb-auth__success nb-panel nb-mono">{{ success }}</div>

      <form @submit.prevent="handleSubmit" class="nb-auth__form">
        <div v-if="!isLoginMode" class="nb-auth__section">
          <h2 class="nb-auth__section-title nb-mono">ACCOUNT TYPE <span class="nb-form-required">*</span></h2>
          <p class="nb-auth__hint nb-mono">SELECT THE TYPE THAT MATCHES YOUR ROLE</p>
          <div class="nb-auth__radio-group">
            <label class="nb-auth__radio">
              <input type="radio" value="Student" v-model="userType" required />
              <span class="nb-auth__radio-label">STUDENT</span>
            </label>
            <label class="nb-auth__radio">
              <input type="radio" value="Teacher" v-model="userType" />
              <span class="nb-auth__radio-label">TEACHER</span>
            </label>
            <label class="nb-auth__radio">
              <input type="radio" value="Admin" v-model="userType" />
              <span class="nb-auth__radio-label">ADMIN</span>
            </label>
          </div>
        </div>

        <div class="nb-auth__section">
          <h2 class="nb-auth__section-title nb-mono">{{ isLoginMode ? 'LOGIN DETAILS' : 'PERSONAL DETAILS' }}</h2>
          
          <div class="nb-form-group">
            <label for="email" class="nb-form-label nb-mono">EMAIL</label>
            <input
              id="email"
              type="email"
              v-model="email"
              class="nb-input"
              :disabled="loading"
              required
              autocomplete="email"
            />
          </div>

          <div class="nb-form-group">
            <label :for="isLoginMode ? 'password' : 'newPassword'" class="nb-form-label nb-mono">
              {{ isLoginMode ? 'PASSWORD' : 'PASSWORD' }}
            </label>
            <input
              :id="isLoginMode ? 'password' : 'newPassword'"
              type="password"
              v-model="password"
              class="nb-input"
              :disabled="loading"
              required
              :autocomplete="isLoginMode ? 'current-password' : 'new-password'"
            />
          </div>

          <div v-if="!isLoginMode" class="nb-form-group">
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
            <p v-if="confirmPassword && password !== confirmPassword" class="nb-form-error nb-mono">
              PASSWORDS DO NOT MATCH
            </p>
          </div>

          <div v-if="!isLoginMode" class="nb-form-row">
            <div class="nb-form-group">
              <label for="firstName" class="nb-form-label nb-mono">FIRST NAME</label>
              <input
                id="firstName"
                type="text"
                v-model="firstName"
                class="nb-input"
                :disabled="loading"
                required
              />
            </div>
            <div class="nb-form-group">
              <label for="lastName" class="nb-form-label nb-mono">LAST NAME</label>
              <input
                id="lastName"
                type="text"
                v-model="lastName"
                class="nb-input"
                :disabled="loading"
                required
              />
            </div>
          </div>

          <div v-if="!isLoginMode" class="nb-form-group">
            <label for="middleNames" class="nb-form-label nb-mono">MIDDLE NAMES (OPTIONAL)</label>
            <input
              id="middleNames"
              type="text"
              v-model="middleNames"
              class="nb-input"
              :disabled="loading"
            />
          </div>

          <div v-if="!isLoginMode" class="nb-form-row">
            <div class="nb-form-group">
              <label for="gender" class="nb-form-label nb-mono">GENDER</label>
              <select
                id="gender"
                v-model="gender"
                class="nb-input nb-select"
                :disabled="loading"
                required
              >
                <option value="Male">MALE</option>
                <option value="Female">FEMALE</option>
                <option value="NonBinary">NON-BINARY</option>
              </select>
            </div>
            <div class="nb-form-group">
              <label for="dateOfBirth" class="nb-form-label nb-mono">DATE OF BIRTH</label>
              <input
                id="dateOfBirth"
                type="date"
                v-model="dateOfBirth"
                class="nb-input"
                :disabled="loading"
                required
              />
            </div>
          </div>
        </div>

        <!-- Student-specific fields -->
        <div v-if="!isLoginMode && userType === 'Student'" class="nb-auth__section">
          <h2 class="nb-auth__section-title nb-mono">STUDENT DETAILS</h2>
          <div class="nb-form-group">
            <label for="studentCourseStatus" class="nb-form-label nb-mono">COURSE STATUS</label>
            <select
              id="studentCourseStatus"
              v-model="studentCourseStatus"
              class="nb-input nb-select"
              :disabled="loading"
            >
              <option value="FullTime">FULL TIME</option>
              <option value="PartTime">PART TIME</option>
              <option value="Inactive">INACTIVE</option>
            </select>
          </div>
          <div class="nb-form-group nb-checkbox-group">
            <input
              id="studentIsInternational"
              type="checkbox"
              v-model="studentIsInternational"
              class="nb-checkbox"
              :disabled="loading"
            />
            <label for="studentIsInternational" class="nb-form-label nb-mono">INTERNATIONAL STUDENT</label>
          </div>
          <div class="nb-form-group">
            <label for="studentCanvasApiKey" class="nb-form-label nb-mono">CANVAS API KEY (OPTIONAL)</label>
            <input
              id="studentCanvasApiKey"
              type="text"
              v-model="studentCanvasApiKey"
              class="nb-input"
              :disabled="loading"
            />
          </div>
        </div>

        <!-- Teacher-specific fields -->
        <div v-if="!isLoginMode && userType === 'Teacher'" class="nb-auth__section">
          <h2 class="nb-auth__section-title nb-mono">TEACHER DETAILS</h2>
          <div class="nb-form-group">
            <label for="teacherEmploymentStatus" class="nb-form-label nb-mono">EMPLOYMENT STATUS</label>
            <select
              id="teacherEmploymentStatus"
              v-model="teacherEmploymentStatus"
              class="nb-input nb-select"
              :disabled="loading"
            >
              <option value="FullTime">FULL TIME</option>
              <option value="PartTime">PART TIME</option>
              <option value="Inactive">INACTIVE</option>
            </select>
          </div>
          <div class="nb-form-group">
            <label for="teacherCanvasApiKey" class="nb-form-label nb-mono">CANVAS API KEY (OPTIONAL)</label>
            <input
              id="teacherCanvasApiKey"
              type="text"
              v-model="teacherCanvasApiKey"
              class="nb-input"
              :disabled="loading"
            />
          </div>
        </div>

        <div class="nb-auth__actions">
          <button
            type="submit"
            class="nb-btn"
            :disabled="loading || !isFormValid"
          >
            {{ loading ? 'PROCESSING...' : (isLoginMode ? 'LOG IN' : 'CREATE ACCOUNT') }}
          </button>
        </div>
      </form>

      <div class="nb-auth__footer">
        <p class="nb-mono">
          {{ isLoginMode ? "DON'T HAVE AN ACCOUNT?" : 'ALREADY HAVE AN ACCOUNT?' }}
          <button type="button" class="nb-btn nb-btn--outline nb-auth__toggle" @click="toggleMode">
            {{ isLoginMode ? 'SIGN UP' : 'LOG IN' }}
          </button>
        </p>
        
        <p v-if="isLoginMode" class="nb-auth__forgot nb-mono">
          <button type="button" class="nb-btn nb-btn--outline" @click="handleForgotPassword">
            FORGOT PASSWORD?
          </button>
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

.nb-form-required {
  color: var(--nb-color-accent-orange);
  margin-left: 4px;
}

.nb-auth__hint {
  font-size: 11px;
  color: var(--nb-color-muted);
  margin: 0 0 12px;
}

.nb-auth__radio-group {
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.nb-auth__radio {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}

.nb-auth__radio input[type="radio"] {
  width: 16px;
  height: 16px;
  accent-color: var(--nb-color-accent-orange);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
}

.nb-auth__radio-label {
  font-family: var(--nb-font-mono);
  font-size: 12px;
  font-weight: var(--nb-font-weight-semibold);
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.nb-form-group {
  margin-bottom: 16px;
}

.nb-form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
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

.nb-checkbox-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

.nb-checkbox {
  width: 18px;
  height: 18px;
  accent-color: var(--nb-color-accent-orange);
  border: var(--nb-border-width-sm) solid var(--nb-color-ink);
  flex-shrink: 0;
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

.nb-auth__toggle {
  margin-left: 12px;
  padding: 6px 12px;
  font-size: 11px;
}

.nb-auth__forgot .nb-btn {
  padding: 6px 12px;
  font-size: 11px;
}
</style>