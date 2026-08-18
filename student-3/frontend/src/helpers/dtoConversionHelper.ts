import type { CourseDto, CreateTaskRequestDto, TaskDto } from '../models/dto'
import type { Course } from '../models/course'
import type { Task } from '@/models/task'

export function getCourseFromDto(courseDto: CourseDto): Course {
  // Simple conversion from CourseDto to Course. Since the properties are the same, we can directly map them.
  // But as always... despite it being the same currently, that may not always be the case. Good to keep separate types.
  return {
    id: courseDto.id,
    code: courseDto.code,
    name: courseDto.name,
  }
}

export function convertCourseToDto(course: Course): CourseDto {
  // Simple conversion from Course to CourseDto. Since the properties are the same, we can directly map them.
  // But as always... despite it being the same currently, that may not always be the case. Good to keep separate types.
  return {
    id: course.id,
    code: course.code,
    name: course.name,
  }
}

export function getTaskFromDto(taskDto: TaskDto, fetchRelatedData: boolean = false): Task {
  let course: Course | null = null,
    parentTask: Task | null = null
  if (!fetchRelatedData) {
    course = {
      id: '',
      code: '',
      name: taskDto.courseName || '',
    }
    parentTask = {
      id: '',
      title: taskDto.parentTaskTitle || '',
      description: null,
      status: taskDto.status,
      priority: taskDto.priority,
      dueDateUtc: null,
      course: null,
      parentTask: null,
    }
  }

  return {
    id: taskDto.id,
    title: taskDto.title,
    description: taskDto.description,
    status: taskDto.status,
    priority: taskDto.priority,
    dueDateUtc: taskDto.dueDate ? new Date(taskDto.dueDate) : null,
    course: course,
    parentTask: parentTask,
  }
}

export function convertTaskToDto(task: Task): TaskDto {
  return {
    id: task.id,
    title: task.title,
    description: task.description,
    status: task.status,
    priority: task.priority,
    dueDate: task.dueDateUtc ? task.dueDateUtc.toISOString() : null,
    courseId: task.course ? task.course.id : null,
    courseName: task.course ? task.course.name : null,
    parentTaskId: task.parentTask ? task.parentTask.id : null,
    parentTaskTitle: task.parentTask ? task.parentTask.title : null,
  }
}

export function convertTaskToCreateTaskRequestDto(task: Task): CreateTaskRequestDto {
  return {
    title: task.title,
    description: task.description,
    priority: task.priority,
    dueDate: task.dueDateUtc ? task.dueDateUtc.toISOString() : null,
    courseId: task.course ? task.course.id : null,
    parentTaskId: task.parentTask ? task.parentTask.id : null,
  }
}
