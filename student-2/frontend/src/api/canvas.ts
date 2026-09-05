import { request } from './http'
import type { CanvasCourse, CanvasRecipient } from '@/types/canvas'

export const getCanvasCourses = () => request<CanvasCourse[]>('/canvas/courses')

export const getCanvasRecipients = (courseId: number) =>
  request<CanvasRecipient[]>(`/canvas/courses/${courseId}/recipients`)