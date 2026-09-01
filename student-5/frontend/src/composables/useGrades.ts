import { computed, ref } from 'vue'
import {
  gradesApi,
  type Assignment,
  type Course,
  type Student,
  type StudentAssignment,
} from '@/api/grades'
import { CONFIGURED_STUDENT_ID } from '@/config'

const student = ref<Student | null>(null)
const courses = ref<Course[]>([])
const assignments = ref<Assignment[]>([])
const marks = ref<StudentAssignment[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const loaded = ref(false)

function messageFrom(errorValue: unknown) {
  return errorValue instanceof Error ? errorValue.message : 'Unable to load grades.'
}

function markFor(assignmentId: string) {
  return marks.value.find((mark) => mark.assignmentId === assignmentId)
}

function earnedPercent(assignment: Assignment, useTemporary: boolean) {
  const mark = markFor(assignment.assignmentId)
  const score = mark?.finalMark ?? (useTemporary ? mark?.tempMark : null)
  if (score == null || !assignment.maxMark) return null
  return Math.min(100, Math.max(0, (score / assignment.maxMark) * 100))
}

export function calculateCourseMark(courseId: string, useTemporary = false) {
  const relevant = assignments.value.filter((assignment) => assignment.courseId === courseId)
  let weightedScore = 0
  let includedWeight = 0

  for (const assignment of relevant) {
    const percent = earnedPercent(assignment, useTemporary)
    if (percent == null) continue
    const weight = assignment.weight ?? 0
    weightedScore += percent * weight
    includedWeight += weight
  }

  return includedWeight > 0 ? weightedScore / includedWeight : null
}

export function courseStats(courseId: string) {
  const relevant = assignments.value.filter((assignment) => assignment.courseId === courseId)
  return {
    assignmentCount: relevant.length,
    gradedCount: relevant.filter((assignment) => markFor(assignment.assignmentId)?.finalMark != null)
      .length,
  }
}

export function useGrades() {
  const overallCurrentMark = computed(() => {
    const available = courses.value
      .map((course) => calculateCourseMark(course.courseId))
      .filter((mark): mark is number => mark != null)
    return available.length ? available.reduce((sum, mark) => sum + mark, 0) / available.length : null
  })

  const overallProjectedMark = computed(() => {
    const available = courses.value
      .map((course) => calculateCourseMark(course.courseId, true))
      .filter((mark): mark is number => mark != null)
    return available.length ? available.reduce((sum, mark) => sum + mark, 0) / available.length : null
  })

  async function load() {
    if (loaded.value || loading.value) return
    loading.value = true
    error.value = null

    try {
      const allStudents = await gradesApi.getStudents()
      const selected = CONFIGURED_STUDENT_ID
        ? allStudents.find((item) => item.studentId === CONFIGURED_STUDENT_ID)
        : allStudents[0]

      if (!selected) throw new Error('No student grade profile is available.')
      student.value = selected

      const [allCourses, studentAssignments, studentMarks] = await Promise.all([
        gradesApi.getCourses(),
        gradesApi.getStudentAssignments(selected.studentId),
        gradesApi.getStudentMarks(selected.studentId),
      ])

      const courseIds = new Set(studentAssignments.map((assignment) => assignment.courseId))
      const relevantCourses = allCourses.filter((course) => courseIds.has(course.courseId))
      courses.value = relevantCourses.length ? relevantCourses : allCourses
      assignments.value = studentAssignments
      marks.value = studentMarks
      loaded.value = true
    } catch (loadError) {
      error.value = messageFrom(loadError)
    } finally {
      loading.value = false
    }
  }

  async function ensureCourseAssignments(courseId: string) {
    await load()
    if (assignments.value.some((assignment) => assignment.courseId === courseId)) return

    try {
      const courseAssignments = await gradesApi.getCourseAssignments(courseId)
      const existingIds = new Set(assignments.value.map((assignment) => assignment.assignmentId))
      assignments.value.push(
        ...courseAssignments.filter((assignment) => !existingIds.has(assignment.assignmentId)),
      )
    } catch (loadError) {
      error.value = messageFrom(loadError)
    }
  }

  async function updateIdealMark(value: number) {
    if (!student.value) return
    const updated = await gradesApi.setIdealMark(
      student.value.studentId,
      value,
      student.value.idealMark != null,
    )
    student.value = updated
  }

  async function updateTemporaryMark(assignmentId: string, value: number) {
    if (!student.value) return
    const existing = markFor(assignmentId)
    const updated = await gradesApi.setTemporaryMark(
      student.value.studentId,
      assignmentId,
      value,
      existing?.tempMark != null,
    )

    const index = marks.value.findIndex((mark) => mark.assignmentId === assignmentId)
    if (index >= 0) marks.value[index] = updated
    else marks.value.push(updated)
  }

  return {
    student,
    courses,
    assignments,
    marks,
    loading,
    error,
    overallCurrentMark,
    overallProjectedMark,
    load,
    ensureCourseAssignments,
    updateIdealMark,
    updateTemporaryMark,
    markFor,
    calculateCourseMark,
    courseStats,
  }
}
