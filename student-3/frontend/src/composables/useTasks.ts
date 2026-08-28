import { computed, ref } from 'vue'
import * as taskApi from '@/api/tasks'
import type {
  CanvasSyncResult,
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
let loaded = false

export function useTasks() {
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
    const created = await taskApi.generateTaskBreakdown(id, input)
    tasks.value.push(...created)
    return created
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
    load,
    add,
    update,
    remove,
    sync,
    generateBreakdown,
    generateDescription,
  }
}
