<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuth } from '@/composables/useAuth'
import { useRouter } from 'vue-router'
import { generateProfileSummary } from '@/api/users'

const { currentUser, currentStudent, currentTeacher, isStudent, isTeacher, loading, error, fetchUser, updateProfile, updateStudentProfile, updateTeacherProfile, changeUserPassword, deleteUserAccount, logout } = useAuth()
const router = useRouter()

const isEditing = ref(false)
const formData = ref({
  email: '',
  firstName: '',
  middleNames: '',
  lastName: '',
  gender: 'Male' as 'Male' | 'Female' | 'NonBinary',
  dateOfBirth: '',
  userProfile: '',
})

const studentFormData = ref({
  courseStatus: 'FullTime' as 'FullTime' | 'PartTime' | 'Inactive',
  isInternational: false,
  canvasApiKey: '',
})

const teacherFormData = ref({
  employmentStatus: 'FullTime' as 'FullTime' | 'PartTime' | 'Inactive',
  canvasApiKey: '',
})

const saveLoading = ref(false)
const saveError = ref<string | null>(null)
const saveSuccess = ref<string | null>(null)

const showChangePassword = ref(false)
const changePasswordData = ref({
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
})
const changePasswordLoading = ref(false)
const changePasswordError = ref<string | null>(null)

const showDeleteAccount = ref(false)
const deleteAccountPassword = ref('')
const deleteAccountLoading = ref(false)
const deleteAccountError = ref<string | null>(null)

const generateSummaryLoading = ref(false)
const generateSummaryError = ref<string | null>(null)
const generatedOldSummary = ref<string | null>(null)
const generatedNewSummary = ref<string | null>(null)

onMounted(async () => {
  const userId = localStorage.getItem('userId')
  if (!userId) {
    router.push('/')
    return
  }
  try {
    await fetchUser(userId)
    populateForm()
  } catch (err) {
    console.error('Failed to load profile:', err)
    if (!currentUser.value) {
      router.push('/')
    }
  }
})

function populateForm() {
  if (currentUser.value) {
    formData.value = {
      email: currentUser.value.email,
      firstName: currentUser.value.firstName,
      middleNames: currentUser.value.middleNames || '',
      lastName: currentUser.value.lastName,
      gender: currentUser.value.gender,
      dateOfBirth: currentUser.value.dateOfBirth.split('T')[0],
      userProfile: currentUser.value.userProfile || '',
    }
  }
  
  if (currentStudent.value) {
    studentFormData.value = {
      courseStatus: currentStudent.value.courseStatus,
      isInternational: currentStudent.value.isInternational,
      canvasApiKey: currentStudent.value.canvasApiKey,
    }
  }
  
  if (currentTeacher.value) {
    teacherFormData.value = {
      employmentStatus: currentTeacher.value.employmentStatus,
      canvasApiKey: currentTeacher.value.canvasApiKey,
    }
  }
}

function startEditing() {
  isEditing.value = true
  saveError.value = null
  saveSuccess.value = null
}

function cancelEditing() {
  isEditing.value = false
  populateForm()
  generatedOldSummary.value = null
  generatedNewSummary.value = null
  saveError.value = null
  saveSuccess.value = null
}

function chooseGeneratedSummary(summary: string) {
  formData.value.userProfile = summary
  isEditing.value = true
  saveSuccess.value = 'Summary selected. Save your profile to apply it.'
}

async function handleSave() {
  saveLoading.value = true
  saveError.value = null
  saveSuccess.value = null

  try {
    await updateProfile({
      email: formData.value.email,
      firstName: formData.value.firstName,
      middleNames: formData.value.middleNames || undefined,
      lastName: formData.value.lastName,
      gender: formData.value.gender,
      dateOfBirth: formData.value.dateOfBirth,
      userProfile: formData.value.userProfile || undefined,
    })

    if (isStudent.value) {
      await updateStudentProfile({
        courseStatus: studentFormData.value.courseStatus,
        isInternational: studentFormData.value.isInternational,
        canvasApiKey: studentFormData.value.canvasApiKey,
      })
    } else if (isTeacher.value) {
      await updateTeacherProfile({
        employmentStatus: teacherFormData.value.employmentStatus,
        canvasApiKey: teacherFormData.value.canvasApiKey,
      })
    }

    saveSuccess.value = 'Profile updated successfully!'
    isEditing.value = false
    generatedOldSummary.value = null
    generatedNewSummary.value = null
  } catch (err) {
    saveError.value = err instanceof Error ? err.message : 'Failed to update profile'
  } finally {
    saveLoading.value = false
  }
}

function openChangePassword() {
  showChangePassword.value = true
  changePasswordData.value = { currentPassword: '', newPassword: '', confirmNewPassword: '' }
  changePasswordError.value = null
}

function closeChangePassword() {
  showChangePassword.value = false
}

async function handleChangePassword() {
  changePasswordError.value = null
  if (changePasswordData.value.newPassword !== changePasswordData.value.confirmNewPassword) {
    changePasswordError.value = 'NEW PASSWORDS DO NOT MATCH'
    return
  }
  if (changePasswordData.value.newPassword.length < 8) {
    changePasswordError.value = 'PASSWORD MUST BE AT LEAST 8 CHARACTERS'
    return
  }
  changePasswordLoading.value = true
  try {
    await changeUserPassword(changePasswordData.value.currentPassword, changePasswordData.value.newPassword)
    closeChangePassword()
    saveSuccess.value = 'Password changed successfully!'
  } catch (err) {
    if (err instanceof Error && err.message.includes('401')) {
      changePasswordError.value = 'CURRENT PASSWORD IS INCORRECT'
    } else {
      changePasswordError.value = err instanceof Error ? err.message : 'Failed to change password'
    }
  } finally {
    changePasswordLoading.value = false
  }
}

function openDeleteAccount() {
  showDeleteAccount.value = true
  deleteAccountPassword.value = ''
  deleteAccountError.value = null
}

function closeDeleteAccount() {
  showDeleteAccount.value = false
}

async function handleDeleteAccount() {
  deleteAccountError.value = null
  deleteAccountLoading.value = true
  try {
    await deleteUserAccount(deleteAccountPassword.value)
    router.push('/')
  } catch (err) {
    if (err instanceof Error && err.message.includes('401')) {
      deleteAccountError.value = 'PASSWORD IS INCORRECT'
    } else {
      deleteAccountError.value = err instanceof Error ? err.message : 'Failed to delete account'
    }
  } finally {
    deleteAccountLoading.value = false
  }
}

async function handleGenerateSummary() {
  if (!currentUser.value) return
  generateSummaryLoading.value = true
  generateSummaryError.value = null
  try {
    const { oldSummary, newSummary } = await generateProfileSummary(currentUser.value.id)
    generatedOldSummary.value = oldSummary
    generatedNewSummary.value = newSummary
    saveSuccess.value = 'Choose which summary to use, then save your profile.'
  } catch (err) {
    generateSummaryError.value = err instanceof Error ? err.message : 'Failed to generate summary'
  } finally {
    generateSummaryLoading.value = false
  }
}

function formatUserType(type: string) {
  return type.toUpperCase()
}

function formatGender(gender: string) {
  return gender.toUpperCase()
}

function formatDate(dateStr: string) {
  const date = new Date(dateStr)
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' }).toUpperCase()
}
</script>

<template>
  <div class="nb-profile-page">
    <div v-if="loading" class="nb-profile__loading nb-mono">LOADING PROFILE...</div>
    <div v-else-if="error" class="nb-profile__error nb-panel nb-mono">{{ error }}</div>
    <div v-else-if="currentUser" class="nb-profile__content">
      <!-- Profile Header -->
      <div class="nb-panel nb-profile__header">
        <div class="nb-profile__avatar">
          <span class="nb-mono">{{ currentUser.firstName.charAt(0) }}{{ currentUser.lastName.charAt(0) }}</span>
        </div>
        <div class="nb-profile__info">
          <h1 class="nb-profile__name">{{ currentUser.firstName }} {{ currentUser.lastName }}</h1>
          <div class="nb-profile__badges">
            <span class="nb-tag nb-tag--account nb-mono">{{ formatUserType(currentUser.userType) }}</span>
            <span v-if="currentUser.userType === 'Student'" class="nb-tag nb-tag--deadline nb-mono">STUDENT</span>
            <span v-if="currentUser.userType === 'Teacher'" class="nb-tag nb-tag--grade nb-mono">TEACHER</span>
            <span v-if="currentUser.userType === 'Admin'" class="nb-tag nb-tag--ai nb-mono">ADMIN</span>
          </div>
        </div>
        <div class="nb-profile__actions">
          <button
            v-if="!isEditing"
            type="button"
            class="nb-btn nb-btn--outline"
            @click="startEditing"
          >
            EDIT PROFILE
          </button>
          <div v-else class="nb-profile__edit-actions">
            <button type="button" class="nb-btn nb-btn--outline" @click="cancelEditing">
              CANCEL
            </button>
            <button type="button" class="nb-btn" :disabled="saveLoading" @click="handleSave">
              {{ saveLoading ? 'SAVING...' : 'SAVE CHANGES' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Messages -->
      <div v-if="saveError" class="nb-profile__message nb-panel nb-mono" style="border-color: var(--nb-color-accent-orange); color: var(--nb-color-accent-orange);">
        {{ saveError }}
      </div>
      <div v-if="saveSuccess" class="nb-profile__message nb-panel nb-mono">
        {{ saveSuccess }}
      </div>

      <!-- Personal Details Section -->
      <div class="nb-panel nb-profile__section">
        <h2 class="nb-profile__section-title nb-mono">PERSONAL DETAILS</h2>
        
        <div class="nb-profile__grid">
          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">EMAIL</label>
            <div v-if="!isEditing" class="nb-profile__field-value">{{ currentUser.email }}</div>
            <input
              v-else
              type="email"
              v-model="formData.email"
              class="nb-input"
              :disabled="saveLoading"
            />
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">FIRST NAME</label>
            <div v-if="!isEditing" class="nb-profile__field-value">{{ currentUser.firstName }}</div>
            <input
              v-else
              type="text"
              v-model="formData.firstName"
              class="nb-input"
              :disabled="saveLoading"
              required
            />
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">MIDDLE NAMES</label>
            <div v-if="!isEditing" class="nb-profile__field-value">{{ currentUser.middleNames || '-' }}</div>
            <input
              v-else
              type="text"
              v-model="formData.middleNames"
              class="nb-input"
              :disabled="saveLoading"
            />
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">LAST NAME</label>
            <div v-if="!isEditing" class="nb-profile__field-value">{{ currentUser.lastName }}</div>
            <input
              v-else
              type="text"
              v-model="formData.lastName"
              class="nb-input"
              :disabled="saveLoading"
              required
            />
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">GENDER</label>
            <div v-if="!isEditing" class="nb-profile__field-value">{{ formatGender(currentUser.gender) }}</div>
            <select
              v-else
              v-model="formData.gender"
              class="nb-input nb-select"
              :disabled="saveLoading"
            >
              <option value="Male">MALE</option>
              <option value="Female">FEMALE</option>
              <option value="NonBinary">NON-BINARY</option>
            </select>
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">DATE OF BIRTH</label>
            <div v-if="!isEditing" class="nb-profile__field-value">{{ formatDate(currentUser.dateOfBirth) }}</div>
            <input
              v-else
              type="date"
              v-model="formData.dateOfBirth"
              class="nb-input"
              :disabled="saveLoading"
              required
            />
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">USER ID</label>
            <div class="nb-profile__field-value nb-mono">{{ currentUser.id }}</div>
          </div>

          <div class="nb-profile__field nb-profile__field--full">
            <label class="nb-profile__field-label nb-mono">PROFILE SUMMARY</label>
            <div v-if="!isEditing" class="nb-profile__field-value nb-profile__summary">
              {{ currentUser.userProfile || 'No summary yet. Click "GENERATE AI SUMMARY" below to create one.' }}
            </div>
            <textarea
              v-else
              v-model="formData.userProfile"
              class="nb-input nb-textarea"
              :disabled="saveLoading"
              rows="4"
              placeholder="A short summary about you â€” generated by AI or written by you."
            ></textarea>
          </div>
        </div>
      </div>

      <!-- Student Details Section -->
      <div v-if="currentUser?.userType === 'Student'" class="nb-panel nb-profile__section">
        <h2 class="nb-profile__section-title nb-mono">STUDENT DETAILS</h2>
        
        <div class="nb-profile__grid">
          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">COURSE STATUS</label>
            <div v-if="!isEditing && currentStudent" class="nb-profile__field-value nb-tag nb-tag--deadline nb-mono">{{ currentStudent.courseStatus }}</div>
            <div v-else-if="!isEditing" class="nb-profile__field-value nb-tag nb-tag--deadline nb-mono">NOT SET</div>
            <select
              v-else
              v-model="studentFormData.courseStatus"
              class="nb-input nb-select"
              :disabled="saveLoading"
            >
              <option value="FullTime">FULL TIME</option>
              <option value="PartTime">PART TIME</option>
              <option value="Inactive">INACTIVE</option>
            </select>
          </div>

          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">INTERNATIONAL STUDENT</label>
            <div v-if="!isEditing" class="nb-profile__field-value nb-mono">{{ currentStudent?.isInternational ? 'YES' : 'NO' }}</div>
            <div v-else class="nb-checkbox-group">
              <input
                type="checkbox"
                v-model="studentFormData.isInternational"
                class="nb-checkbox"
                :disabled="saveLoading"
              />
            </div>
          </div>

          <div class="nb-profile__field nb-profile__field--full">
            <label class="nb-profile__field-label nb-mono">CANVAS API KEY</label>
            <div v-if="!isEditing" class="nb-profile__field-value nb-mono">{{ currentStudent?.canvasApiKey || 'NOT SET' }}</div>
            <input
              v-else
              type="text"
              v-model="studentFormData.canvasApiKey"
              class="nb-input"
              :disabled="saveLoading"
              placeholder="ENTER CANVAS API KEY"
            />
          </div>
        </div>
      </div>

      <!-- Teacher Details Section -->
      <div v-if="currentUser?.userType === 'Teacher'" class="nb-panel nb-profile__section">
        <h2 class="nb-profile__section-title nb-mono">TEACHER DETAILS</h2>
        
        <div class="nb-profile__grid">
          <div class="nb-profile__field">
            <label class="nb-profile__field-label nb-mono">EMPLOYMENT STATUS</label>
            <div v-if="!isEditing && currentTeacher" class="nb-profile__field-value nb-tag nb-tag--grade nb-mono">{{ currentTeacher.employmentStatus }}</div>
            <div v-else-if="!isEditing" class="nb-profile__field-value nb-tag nb-tag--grade nb-mono">NOT SET</div>
            <select
              v-else
              v-model="teacherFormData.employmentStatus"
              class="nb-input nb-select"
              :disabled="saveLoading"
            >
              <option value="FullTime">FULL TIME</option>
              <option value="PartTime">PART TIME</option>
              <option value="Inactive">INACTIVE</option>
            </select>
          </div>

          <div class="nb-profile__field nb-profile__field--full">
            <label class="nb-profile__field-label nb-mono">CANVAS API KEY</label>
            <div v-if="!isEditing" class="nb-profile__field-value nb-mono">{{ currentTeacher?.canvasApiKey || 'NOT SET' }}</div>
            <input
              v-else
              type="text"
              v-model="teacherFormData.canvasApiKey"
              class="nb-input"
              :disabled="saveLoading"
              placeholder="ENTER CANVAS API KEY"
            />
          </div>
        </div>
      </div>

      <!-- Account Actions -->
      <div class="nb-panel nb-profile__section">
        <h2 class="nb-profile__section-title nb-mono">ACCOUNT ACTIONS</h2>
        <div class="nb-profile__actions-list">
          <button
            type="button"
            class="nb-profile__action nb-btn nb-btn--accent"
            :disabled="generateSummaryLoading"
            @click="handleGenerateSummary"
          >
            {{ generateSummaryLoading ? 'GENERATING...' : 'GENERATE AI SUMMARY' }}
          </button>
          <button
            type="button"
            class="nb-profile__action nb-btn nb-btn--outline"
            style="background: var(--nb-color-accent-orange); color: var(--nb-color-ink); border-color: var(--nb-color-ink);"
            @click="openChangePassword"
          >
            CHANGE PASSWORD
          </button>
          <button
            type="button"
            class="nb-profile__action nb-btn nb-btn--outline"
            style="background: var(--nb-color-white); color: var(--nb-color-ink); border-color: var(--nb-color-ink);"
            @click="openDeleteAccount"
          >
            DELETE ACCOUNT
          </button>
        </div>
        <div v-if="generatedNewSummary !== null" class="nb-panel nb-profile__summary-options">
          <h3 class="nb-profile__field-label nb-mono">CHOOSE PROFILE SUMMARY</h3>
          <div class="nb-profile__summary-option">
            <strong class="nb-mono">OLD SUMMARY</strong>
            <p>{{ generatedOldSummary || 'No existing summary.' }}</p>
            <button
              type="button"
              class="nb-btn nb-btn--outline"
              :disabled="generatedOldSummary === null"
              @click="chooseGeneratedSummary(generatedOldSummary || '')"
            >
              USE OLD SUMMARY
            </button>
          </div>
          <div class="nb-profile__summary-option">
            <strong class="nb-mono">NEW SUMMARY</strong>
            <p>{{ generatedNewSummary }}</p>
            <button
              type="button"
              class="nb-btn nb-btn--accent"
              @click="chooseGeneratedSummary(generatedNewSummary || '')"
            >
              USE NEW SUMMARY
            </button>
          </div>
        </div>
        <div v-if="generateSummaryError" class="nb-profile__message nb-panel nb-mono" style="border-color: var(--nb-color-accent-orange); color: var(--nb-color-accent-orange); margin-top: 12px;">
          {{ generateSummaryError }}
        </div>
      </div>
    </div>
  </div>

  <!-- Change Password Modal -->
  <div v-if="showChangePassword" class="nb-modal-overlay" @click.self="closeChangePassword">
    <div class="nb-modal nb-panel">
      <h2 class="nb-mono">CHANGE PASSWORD</h2>
      <div v-if="changePasswordError" class="nb-modal__error nb-mono">{{ changePasswordError }}</div>
      <div class="nb-form-group">
        <label for="currentPassword" class="nb-form-label nb-mono">CURRENT PASSWORD</label>
        <input
          id="currentPassword"
          type="password"
          v-model="changePasswordData.currentPassword"
          class="nb-input"
          :disabled="changePasswordLoading"
          required
          autocomplete="current-password"
        />
      </div>
      <div class="nb-form-group">
        <label for="newPassword" class="nb-form-label nb-mono">NEW PASSWORD</label>
        <input
          id="newPassword"
          type="password"
          v-model="changePasswordData.newPassword"
          class="nb-input"
          :disabled="changePasswordLoading"
          required
          autocomplete="new-password"
        />
      </div>
      <div class="nb-form-group">
        <label for="confirmNewPassword" class="nb-form-label nb-mono">CONFIRM NEW PASSWORD</label>
        <input
          id="confirmNewPassword"
          type="password"
          v-model="changePasswordData.confirmNewPassword"
          class="nb-input"
          :disabled="changePasswordLoading"
          required
          autocomplete="new-password"
        />
      </div>
      <div class="nb-modal__actions">
        <button type="button" class="nb-btn nb-btn--outline" @click="closeChangePassword" :disabled="changePasswordLoading">
          CANCEL
        </button>
        <button type="button" class="nb-btn" @click="handleChangePassword" :disabled="changePasswordLoading">
          {{ changePasswordLoading ? 'CHANGING...' : 'CHANGE PASSWORD' }}
        </button>
      </div>
    </div>
  </div>

  <!-- Delete Account Modal -->
  <div v-if="showDeleteAccount" class="nb-modal-overlay" @click.self="closeDeleteAccount">
    <div class="nb-modal nb-panel" style="max-width: 400px;">
      <h2 class="nb-mono" style="color: var(--nb-color-accent-orange);">DELETE ACCOUNT</h2>
      <p class="nb-modal__warning nb-mono">THIS ACTION CANNOT BE UNDONE. ALL YOUR DATA WILL BE PERMANENTLY REMOVED.</p>
      <div v-if="deleteAccountError" class="nb-modal__error nb-mono">{{ deleteAccountError }}</div>
      <div class="nb-form-group">
        <label for="deleteAccountPassword" class="nb-form-label nb-mono">TYPE YOUR PASSWORD TO CONFIRM</label>
        <input
          id="deleteAccountPassword"
          type="password"
          v-model="deleteAccountPassword"
          class="nb-input"
          :disabled="deleteAccountLoading"
          required
          autocomplete="current-password"
        />
      </div>
      <div class="nb-modal__actions">
        <button type="button" class="nb-btn nb-btn--outline" @click="closeDeleteAccount" :disabled="deleteAccountLoading">
          CANCEL
        </button>
        <button
          type="button"
          class="nb-btn"
          style="background: var(--nb-color-accent-orange); color: var(--nb-color-ink); border-color: var(--nb-color-ink);"
          @click="handleDeleteAccount"
          :disabled="deleteAccountLoading"
        >
          {{ deleteAccountLoading ? 'DELETING...' : 'DELETE ACCOUNT' }}
        </button>
      </div>
    </div>
  </div>
</template>

