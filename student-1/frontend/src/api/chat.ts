import { request, buildQuery } from './http'

export interface ChatSessionHeader {
  id: string
  studentId: string
  title: string
  createdAtUtc: string
  updatedAtUtc: string
  messageCount: number
  lastMessage: string | null
}

export interface ChatMessageDetail {
  id: string
  chatSessionId: string
  role: 'user' | 'assistant'
  content: string
  createdAtUtc: string
  citationsJson?: string | null
  confidence?: string | null
}

export interface ChatSessionDetail {
  id: string
  studentId: string
  title: string
  createdAtUtc: string
  updatedAtUtc: string
  messages: ChatMessageDetail[]
}

export async function getChatSessions(studentId: string): Promise<ChatSessionHeader[]> {
  const query = buildQuery({ studentId })
  return request(`/api/chat/sessions${query}`)
}

export async function createChatSession(studentId: string, title?: string): Promise<ChatSessionHeader> {
  return request('/api/chat/sessions', {
    method: 'POST',
    body: JSON.stringify({ studentId, title }),
  })
}

export async function getChatSessionDetail(id: string): Promise<ChatSessionDetail> {
  return request(`/api/chat/sessions/${id}`)
}

export async function deleteChatSession(id: string): Promise<void> {
  return request(`/api/chat/sessions/${id}`, {
    method: 'DELETE',
  })
}

export async function generateInitialDigest(sessionId: string, studentId: string): Promise<ChatMessageDetail> {
  const query = buildQuery({ studentId })
  return request(`/api/chat/sessions/${sessionId}/initial-digest${query}`, {
    method: 'POST',
  })
}

export async function sendChatMessage(
  sessionId: string,
  content: string,
  includeRag = true,
  studentId?: string,
): Promise<ChatMessageDetail> {
  const query = buildQuery({ studentId })
  return request(`/api/chat/sessions/${sessionId}/messages${query}`, {
    method: 'POST',
    body: JSON.stringify({ content, includeRag }),
  })
}
