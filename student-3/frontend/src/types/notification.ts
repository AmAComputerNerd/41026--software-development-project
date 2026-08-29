export interface NotificationItem {
  id: string
  studentId: string
  type: string
  sourceMicroservice: string
  message: string
  isRead: boolean
  createdAtUtc: string
}
