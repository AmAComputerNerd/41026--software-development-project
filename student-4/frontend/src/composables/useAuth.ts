import { computed, ref } from 'vue'
import {
  getUser,
  updateUser,
  getStudent,
  updateStudent,
  getTeacher,
  updateTeacher,
  changePassword,
  deleteAccount,
  type UserDto,
  type StudentDto,
  type TeacherDto,
} from '@/api/users'
import { ApiError } from '@/api/http'

const currentUser = ref<UserDto | null>(null)
const currentStudent = ref<StudentDto | null>(null)
const currentTeacher = ref<TeacherDto | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

export function useAuth() {
  const isAuthenticated = computed(() => currentUser.value !== null)
  const userType = computed(() => currentUser.value?.userType ?? null)
  const isStudent = computed(() => userType.value?.toLowerCase() === 'student')
  const isTeacher = computed(() => userType.value?.toLowerCase() === 'teacher')
  const isAdmin = computed(() => userType.value?.toLowerCase() === 'admin')

  async function fetchUser(userId: string) {
    loading.value = true
    error.value = null
    try {
      try {
        currentUser.value = await getUser(userId)
      } catch (err) {
        if (err instanceof ApiError && err.status === 404) {
          localStorage.removeItem('userId')
          logout()
          error.value = 'User not found. Please log in again.'
        } else {
          error.value = err instanceof Error ? err.message : 'Failed to load user'
        }
        throw err
      }

      currentStudent.value = null
      currentTeacher.value = null

      if (currentUser.value?.userType === 'Student') {
        try {
          currentStudent.value = await getStudent(userId)
        } catch (err) {
          if (err instanceof ApiError && err.status === 404) {
            console.info('No student record yet for user', userId)
          } else {
            console.warn('Failed to load student record', userId, err)
          }
        }
      } else if (currentUser.value?.userType === 'Teacher') {
        try {
          currentTeacher.value = await getTeacher(userId)
        } catch (err) {
          if (err instanceof ApiError && err.status === 404) {
            console.info('No teacher record yet for user', userId)
          } else {
            console.warn('Failed to load teacher record', userId, err)
          }
        }
      }
    } catch (err) {
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateProfile(userData: Partial<UserDto>) {
    if (!currentUser.value) throw new Error('No user logged in')
    
    loading.value = true
    error.value = null
    try {
      const updated = await updateUser(currentUser.value.id, {
        email: userData.email ?? currentUser.value.email,
        firstName: userData.firstName ?? currentUser.value.firstName,
        middleNames: (userData.middleNames ?? currentUser.value.middleNames) ?? undefined,
        lastName: userData.lastName ?? currentUser.value.lastName,
        gender: userData.gender ?? currentUser.value.gender,
        dateOfBirth: userData.dateOfBirth ?? currentUser.value.dateOfBirth,
      })
      currentUser.value = updated
      return updated
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update profile'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateStudentProfile(studentData: Partial<StudentDto>) {
    if (!currentUser.value || !currentStudent.value) throw new Error('No student profile')
    
    loading.value = true
    error.value = null
    try {
      const updated = await updateStudent(currentUser.value.id, studentData)
      currentStudent.value = updated
      return updated
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update student profile'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updateTeacherProfile(teacherData: Partial<TeacherDto>) {
    if (!currentUser.value) throw new Error('No user logged in')
    
    loading.value = true
    error.value = null
    try {
      const updated = await updateTeacher(currentUser.value.id, teacherData)
      currentTeacher.value = updated
      return updated
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update teacher profile'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function changeUserPassword(currentPassword: string, newPassword: string) {
    if (!currentUser.value) throw new Error('No user logged in')
    
    loading.value = true
    error.value = null
    try {
      const result = await changePassword(currentUser.value.email, currentPassword, newPassword)
      return result
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to change password'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deleteUserAccount(password: string) {
    if (!currentUser.value) throw new Error('No user logged in')
    
    loading.value = true
    error.value = null
    try {
      const result = await deleteAccount(currentUser.value.email, password)
      logout()
      return result
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to delete account'
      throw err
    } finally {
      loading.value = false
    }
  }

  function logout() {
    currentUser.value = null
    currentStudent.value = null
    currentTeacher.value = null
    localStorage.removeItem('userId')
  }

  return {
    currentUser,
    currentStudent,
    currentTeacher,
    loading,
    error,
    isAuthenticated,
    userType,
    isStudent,
    isTeacher,
    isAdmin,
    fetchUser,
    updateProfile,
    updateStudentProfile,
    updateTeacherProfile,
    changeUserPassword,
    deleteUserAccount,
    logout,
  }
}