import { computed, ref } from 'vue'
import * as taskApi from '@/api/tasks'
import type {
  CanvasSyncResult,
  BreakdownReviewInput,
  BreakdownRevisionInput,
  Course,
  CreateTaskInput,
  GenerateTaskBreakdownInput,
  GenerateTaskDescriptionInput,
  TaskItem,
  UpdateTaskInput,
} from '@/types/task'

const tasks = ref<TaskItem[]>([])
const courses = ref<Course[]>([])
const loading = ref(false)
const error = ref('')
const aiModeEnabled = ref(false)
const multiAgentEnabled = ref(false)
const aiBreakdownEnabled = computed(() => multiAgentEnabled.value || aiModeEnabled.value)
const aiModeError = ref('')
let loaded = false

export function useTasks() {
  async function loadAiMode() {
    aiModeEnabled.value = false
    multiAgentEnabled.value = false
    aiModeError.value = ''
    try {
      const capabilities = await taskApi.getAiModeCapabilities()
      if (typeof capabilities.aiModeEnabled !== 'boolean' ||
          typeof capabilities.multiAgentEnabled !== 'boolean') {
        throw new Error('The Deadline Tracker API returned invalid AI Mode capabilities.')
      }
      aiModeEnabled.value = capabilities.aiModeEnabled
      multiAgentEnabled.value = capabilities.multiAgentEnabled
    } catch (reason) {
      aiModeError.value =
        reason instanceof Error ? reason.message : 'Unable to load AI Mode availability.'
    }
  }

  const upcoming = computed(() =>
    tasks.value
      .filter((task) => task.status !== 'Completed' && task.dueDate)
      .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime()),
  )

  async function load(force = false) {
    if (loaded && !force) return

    loading.value = true
    error.value = ''
    try {
      const [taskItems, courseItems] = await Promise.all([
        taskApi.getTasks(),
        taskApi.getCourses(),
      ])
      tasks.value = taskItems
      courses.value = courseItems
      loaded = true
    } catch (reason) {
      error.value = reason instanceof Error ? reason.message : 'Unable to load tasks.'
      throw reason
    } finally {
      loading.value = false
    }
  }

  async function add(input: CreateTaskInput) {
    const created = await taskApi.createTask(input)
    tasks.value.push(created)
    return created
  }

  async function update(id: string, input: UpdateTaskInput) {
    const updated = await taskApi.updateTask(id, input)
    const index = tasks.value.findIndex((task) => task.id === id)
    if (index !== -1) {
      const current = tasks.value[index]!
      tasks.value[index] = {
        ...current,
        ...updated,
        courseName: updated.courseName ?? current.courseName,
        parentTaskTitle: updated.parentTaskTitle ?? current.parentTaskTitle,
      }
    }

    if (input.newStatus === 'Completed') {
      const completedIds = new Set([id])
      let foundDescendant = true
      while (foundDescendant) {
        foundDescendant = false
        for (const task of tasks.value) {
          if (
            task.parentTaskId &&
            completedIds.has(task.parentTaskId) &&
            !completedIds.has(task.id)
          ) {
            completedIds.add(task.id)
            foundDescendant = true
          }
        }
      }
      tasks.value = tasks.value.map((task) =>
        completedIds.has(task.id) ? { ...task, status: 'Completed' } : task,
      )
    }

    return updated
  }

  async function remove(id: string) {
    await taskApi.deleteTask(id)
    const removedIds = new Set([id])
    let foundDescendant = true
    while (foundDescendant) {
      foundDescendant = false
      for (const task of tasks.value) {
        if (task.parentTaskId && removedIds.has(task.parentTaskId) && !removedIds.has(task.id)) {
          removedIds.add(task.id)
          foundDescendant = true
        }
      }
    }
    tasks.value = tasks.value.filter((task) => !removedIds.has(task.id))
  }

  async function sync(): Promise<CanvasSyncResult> {
    const result = await taskApi.syncCanvas()
    await load(true)
    return result
  }

  async function generateBreakdown(id: string, input: GenerateTaskBreakdownInput) {
    const result = await taskApi.generateTaskBreakdown(id, input)
    if (Array.isArray(result)) tasks.value.push(...result)
    return result
  }

  async function reviewBreakdown(id: string, input: BreakdownReviewInput) {
    const result = await taskApi.reviewTaskBreakdown(id, input)
    const existingIds = new Set(tasks.value.map((task) => task.id))
    tasks.value.push(...result.tasks.filter((task) => !existingIds.has(task.id)))
    return result
  }

  async function reviseBreakdown(id: string, input: BreakdownRevisionInput) {
    return taskApi.reviseTaskBreakdown(id, input)
  }

  async function generateDescription(input: GenerateTaskDescriptionInput) {
    const result = await taskApi.generateTaskDescription(input)
    return result.description
  }

  return {
    tasks,
    courses,
    upcoming,
    loading,
    error,
    aiModeEnabled,
    aiBreakdownEnabled,
    multiAgentEnabled,
    aiModeError,
    loadAiMode,
    load,
    add,
    update,
    remove,
    sync,
    generateBreakdown,
    reviewBreakdown,
    reviseBreakdown,
    generateDescription,
  }
}
