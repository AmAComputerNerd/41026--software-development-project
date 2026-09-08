<script setup lang="ts">
import { ref, computed } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import { createUser, login, forgotPassword, type CreateUserRequest } from '@/api/users'
import { ApiError } from '@/api/http'

const { fetchUser } = useAuth()

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

const showForgot = ref(false)
const forgotEmail = ref('')
const forgotLoading = ref(false)
const forgotError = ref<string | null>(null)
const forgotSuccess = ref<string | null>(null)

const error = ref<string | null>(null)
const success = ref<string | null>(null)
const loading = ref(false)

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
    userType.value
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
      const user = await login(email.value, password.value)

      await fetchUser(user.id)
      localStorage.setItem('userId', user.id)

      success.value = 'Login successful! Redirecting...'

      setTimeout(() => {
        window.location.href = '/account/profile'
      }, 1000)
    } else {
      const userData: CreateUserRequest = {
        email: email.value,
        passwordHash: password.value,
        firstName: firstName.value,
        middleNames: middleNames.value || undefined,
        lastName: lastName.value,
        gender: gender.value,
        dateOfBirth: dateOfBirth.value,
        userType: userType.value,
      }

      if (userType.value === 'Student') {
        userData.studentDto = {
          userId: '00000000-0000-0000-0000-000000000000',
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

      localStorage.setItem('userId', newUser.id)

      success.value = 'Account created successfully! Redirecting to your profile...'

      setTimeout(() => {
        window.location.href = '/account/profile'
      }, 1000)
    }
  } catch (err) {
    if (isLoginMode.value && err instanceof ApiError && err.status === 401) {
      error.value = 'Invalid email or password.'
    } else if (isLoginMode.value && err instanceof Error && err.message.includes('401')) {
      error.value = 'Invalid email or password.'
    } else {
      error.value = err instanceof Error ? err.message : 'An error occurred'
    }
  } finally {
    loading.value = false
  }
}

function openForgotPassword() {
  showForgot.value = true
  forgotEmail.value = email.value
  forgotError.value = null
  forgotSuccess.value = null
}

function cancelForgotPassword() {
  showForgot.value = false
  forgotError.value = null
  forgotSuccess.value = null
}

async function handleForgotPassword() {
  forgotError.value = null
  forgotSuccess.value = null

  if (!forgotEmail.value) {
    forgotError.value = 'Please enter your email address.'
    return
  }

  forgotLoading.value = true
  try {
    const result = await forgotPassword(forgotEmail.value)
    forgotSuccess.value =
      result.message ??
      'If that email is registered, a reset link has been sent. Check your inbox.'
  } catch (err) {
    if (err instanceof ApiError && err.status === 503) {
      forgotError.value =
        'The account service is temporarily unavailable. Please try again shortly.'
    } else {
      forgotError.value = err instanceof Error ? err.message : 'Failed to send reset email.'
    }
  } finally {
    forgotLoading.value = false
  }
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

      <form v-if="!showForgot" @submit.prevent="handleSubmit" class="nb-auth__form">
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

      <div v-if="!showForgot" class="nb-auth__footer">
        <p class="nb-mono">
          {{ isLoginMode ? "DON'T HAVE AN ACCOUNT?" : 'ALREADY HAVE AN ACCOUNT?' }}
          <button type="button" class="nb-btn nb-btn--outline nb-auth__toggle" @click="toggleMode">
            {{ isLoginMode ? 'SIGN UP' : 'LOG IN' }}
          </button>
        </p>

        <p v-if="isLoginMode" class="nb-auth__forgot nb-mono">
          <button type="button" class="nb-btn nb-btn--outline" @click="openForgotPassword">
            FORGOT PASSWORD?
          </button>
        </p>
      </div>

      <!-- Inline "forgot password" form. Replaces the main form when
           the user clicks the FORGOT PASSWORD button. After the
           reset email is sent, the user sees a confirmation and a
           "back to login" link. -->
      <div v-else class="nb-auth__forgot-panel">
        <div class="nb-auth__header">
          <h1 class="nb-auth__title nb-mono">RESET PASSWORD</h1>
          <p class="nb-auth__subtitle">
            Enter your email and we'll send you a link to choose a new password.
          </p>
        </div>

        <div v-if="forgotError" class="nb-auth__error nb-panel nb-mono">{{ forgotError }}</div>
        <div v-if="forgotSuccess" class="nb-auth__success nb-panel nb-mono">{{ forgotSuccess }}</div>

        <form
          v-if="!forgotSuccess"
          @submit.prevent="handleForgotPassword"
          class="nb-auth__form"
        >
          <div class="nb-auth__section">
            <h2 class="nb-auth__section-title nb-mono">YOUR EMAIL</h2>
            <div class="nb-form-group">
              <label for="forgotEmail" class="nb-form-label nb-mono">EMAIL</label>
              <input
                id="forgotEmail"
                type="email"
                v-model="forgotEmail"
                class="nb-input"
                :disabled="forgotLoading"
                required
                autocomplete="email"
              />
            </div>
          </div>

          <div class="nb-auth__actions">
            <button
              type="submit"
              class="nb-btn"
              :disabled="forgotLoading || !forgotEmail"
            >
              {{ forgotLoading ? 'SENDINGâ€¦' : 'SEND RESET LINK' }}
            </button>
          </div>
        </form>

        <div class="nb-auth__footer">
          <p class="nb-mono">
            <button type="button" class="nb-btn nb-btn--outline" @click="cancelForgotPassword">
              BACK TO LOG IN
            </button>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

