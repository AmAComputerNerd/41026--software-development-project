import { computed, ref } from 'vue'
import {
  gradesApi,
  type Assignment,
  type AssignmentGroup,
  type Course,
  type Student,
  type StudentAssignment,
} from '@/api/grades'
import { CONFIGURED_STUDENT_ID } from '@/config'

const student = ref<Student | null>(null)
const courses = ref<Course[]>([])
const assignments = ref<Assignment[]>([])
const groups = ref<AssignmentGroup[]>([])
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
  const relevantGroups = groups.value.filter((group) => group.courseId === courseId)
  const relevantAssignments = assignments.value.filter(
    (assignment) => assignment.courseId === courseId,
  )

  // Group-weighted rollup: each group's mean percent is multiplied by the
  // group's weight. Falls back to per-assignment weighting if groups haven't
  // been loaded yet so the overview still has something to display.
  if (relevantGroups.length > 0) {
    let weightedScore = 0
    let includedWeight = 0

    for (const group of relevantGroups) {
      const groupAssignments = relevantAssignments.filter(
        (assignment) => assignment.groupId === group.groupId,
      )
      if (groupAssignments.length === 0) continue

      let groupPercentSum = 0
      let groupPercentCount = 0
      for (const assignment of groupAssignments) {
        const percent = earnedPercent(assignment, useTemporary)
        if (percent == null) continue
        groupPercentSum += percent
        groupPercentCount += 1
      }
      if (groupPercentCount === 0) continue

      const groupMean = groupPercentSum / groupPercentCount
      const weight = group.weight ?? 0
      weightedScore += groupMean * weight
      includedWeight += weight
    }

    return includedWeight > 0 ? weightedScore / includedWeight : null
  }

  let weightedScore = 0
  let includedWeight = 0

  for (const assignment of relevantAssignments) {
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

  async function ensureCourseGroups(courseId: string) {
    await load()
    if (groups.value.some((group) => group.courseId === courseId)) return

    try {
      const courseGroups = await gradesApi.getAssignmentGroups(courseId)
      const newGroups = courseGroups.filter(
        (group) => !groups.value.some((existing) => existing.groupId === group.groupId),
      )
      groups.value.push(...newGroups)

      // Fetch per-group assignments so the detail view can render nested rows.
      const groupAssignments = (
        await Promise.all(
          newGroups.map((group) =>
            gradesApi
              .getGroupAssignments(group.groupId)
              .catch(() => [] as Assignment[]),
          ),
        )
      ).flat()
      const existingAssignmentIds = new Set(
        assignments.value.map((assignment) => assignment.assignmentId),
      )
      assignments.value.push(
        ...groupAssignments.filter(
          (assignment) => !existingAssignmentIds.has(assignment.assignmentId),
        ),
      )
    } catch (loadError) {
      error.value = messageFrom(loadError)
    }
  }

  function groupsForCourse(courseId: string) {
    return groups.value.filter((group) => group.courseId === courseId)
  }

  function assignmentsForGroup(groupId: string) {
    return assignments.value.filter((assignment) => assignment.groupId === groupId)
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

  async function deleteTemporaryMark(assignmentId: string) {
    if (!student.value) return
    await gradesApi.deleteTemporaryMark(student.value.studentId, assignmentId)
    const existing = markFor(assignmentId)
    if (existing) existing.tempMark = null
  }

  return {
    student,
    courses,
    assignments,
    groups,
    marks,
    loading,
    error,
    overallCurrentMark,
    overallProjectedMark,
    load,
    ensureCourseAssignments,
    ensureCourseGroups,
    updateIdealMark,
    updateTemporaryMark,
    deleteTemporaryMark,
    markFor,
    calculateCourseMark,
    courseStats,
    groupsForCourse,
    assignmentsForGroup,
  }
}
