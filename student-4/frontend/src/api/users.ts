import { request, buildQuery } from './http'

export interface UserDto {
  id: string
  email: string
  firstName: string
  middleNames: string | null
  lastName: string
  gender: 'Male' | 'Female' | 'NonBinary'
  dateOfBirth: string
  userType: 'Student' | 'Teacher' | 'Admin'
  userProfile: string | null
}

export interface StudentDto {
  userId: string
  courseStatus: 'FullTime' | 'PartTime' | 'Inactive'
  isInternational: boolean
  canvasApiKey: string
}

export interface TeacherDto {
  userId: string
  employmentStatus: 'FullTime' | 'PartTime' | 'Inactive'
  canvasApiKey: string
}

export interface CreateUserRequest {
  email: string
  passwordHash: string
  firstName: string
  middleNames?: string
  lastName: string
  gender: 'Male' | 'Female' | 'NonBinary'
  dateOfBirth: string
  userType: 'Student' | 'Teacher' | 'Admin'
  studentDto?: StudentDto
  teacherDto?: TeacherDto
}

export interface UpdateUserRequest {
  email: string
  firstName: string
  middleNames?: string
  lastName: string
  gender: 'Male' | 'Female' | 'NonBinary'
  dateOfBirth: string
  userProfile?: string | null
}

// POST /api/auth/login — returns the matching UserDto on success,
// throws (with the 401 message) if the email/password don't match.
export async function login(email: string, password: string): Promise<UserDto> {
  return request('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export async function getUsers(): Promise<UserDto[]> {
  return request('/users')
}

export async function getUser(userId: string): Promise<UserDto> {
  return request(`/users/${userId}`)
}

export async function createUser(userData: CreateUserRequest): Promise<UserDto> {
  return request('/users', {
    method: 'POST',
    body: JSON.stringify(userData),
  })
}

export async function updateUser(userId: string, userData: UpdateUserRequest): Promise<UserDto> {
  return request(`/users/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(userData),
  })
}

export async function deleteUser(userId: string): Promise<void> {
  return request(`/users/${userId}`, {
    method: 'DELETE',
  })
}

export async function getStudent(userId: string): Promise<StudentDto> {
  return request(`/students/${userId}`)
}

export async function updateStudent(userId: string, studentData: Partial<StudentDto>): Promise<StudentDto> {
  return request(`/students/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(studentData),
  })
}

export async function getTeacher(userId: string): Promise<TeacherDto> {
  return request(`/teachers/${userId}`)
}

export async function updateTeacher(userId: string, teacherData: Partial<TeacherDto>): Promise<TeacherDto> {
  return request(`/teachers/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(teacherData),
  })
}

// POST /api/auth/change-password — changes the user's password after
// verifying the current one. Returns { message } on success.
export async function changePassword(
  email: string,
  currentPassword: string,
  newPassword: string
): Promise<{ message: string }> {
  return request('/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ email, currentPassword, newPassword }),
  })
}

// DELETE /api/auth/delete-account — deletes the user's account after
// verifying the password. Returns { message } on success.
export async function deleteAccount(
  email: string,
  password: string
): Promise<{ message: string }> {
  return request('/auth/delete-account', {
    method: 'DELETE',
    body: JSON.stringify({ email, password }),
  })
}

// POST /api/users/{userId}/profile-summary — generates an AI profile
// summary for the user and persists it. Returns { summary }.
export async function generateProfileSummary(userId: string): Promise<{ summary: string }> {
  return request(`/users/${userId}/profile-summary`, {
    method: 'POST',
  })
}