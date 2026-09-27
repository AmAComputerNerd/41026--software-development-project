import { ref, computed } from 'vue'
import {
  getChatSessions,
  createChatSession,
  getChatSessionDetail,
  deleteChatSession,
  generateInitialDigest,
  sendChatMessage,
  type ChatSessionHeader,
  type ChatSessionDetail,
  type ChatMessageDetail,
} from '@/api/chat'
import { CURRENT_STUDENT_ID } from '@/config'

export function useChatAssistant() {
  const sessions = ref<ChatSessionHeader[]>([])
  const activeSessionId = ref<string | null>(null)
  const activeSession = ref<ChatSessionDetail | null>(null)
  const loadingSessions = ref(false)
  const loadingMessages = ref(false)
  const sendingMessage = ref(false)
  const generatingOpeningDigest = ref(false)
  const drawerOpen = ref(false)
  const error = ref<string | null>(null)

  const messages = computed<ChatMessageDetail[]>(() => activeSession.value?.messages ?? [])

  async function fetchSessions() {
    loadingSessions.value = true
    error.value = null
    try {
      sessions.value = await getChatSessions(CURRENT_STUDENT_ID)
      if (sessions.value.length > 0 && !activeSessionId.value) {
        await selectSession(sessions.value[0].id)
      } else if (sessions.value.length === 0) {
        await createNewSession('Welcome Chat')
      }
    } catch (err: any) {
      error.value = err?.message || 'Failed to load chat history.'
    } finally {
      loadingSessions.value = false
    }
  }

  async function selectSession(id: string) {
    if (activeSessionId.value === id && activeSession.value) {
      drawerOpen.value = false
      return
    }

    activeSessionId.value = id
    loadingMessages.value = true
    error.value = null
    drawerOpen.value = false

    try {
      activeSession.value = await getChatSessionDetail(id)
      if (activeSession.value.messages.length === 0) {
        await triggerOpeningDigest(id)
      }
    } catch (err: any) {
      error.value = err?.message || 'Failed to load conversation messages.'
    } finally {
      loadingMessages.value = false
    }
  }

  async function triggerOpeningDigest(sessionId: string) {
    generatingOpeningDigest.value = true
    try {
      const digestMsg = await generateInitialDigest(sessionId, CURRENT_STUDENT_ID)
      if (activeSession.value && activeSession.value.id === sessionId) {
        activeSession.value.messages.push(digestMsg)
      }
    } catch (err: any) {
      console.warn('Failed to generate initial digest:', err)
    } finally {
      generatingOpeningDigest.value = false
    }
  }

  async function createNewSession(customTitle?: string) {
    loadingMessages.value = true
    error.value = null
    drawerOpen.value = false

    try {
      const title = customTitle || `Chat ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
      const newHeader = await createChatSession(CURRENT_STUDENT_ID, title)
      sessions.value.unshift(newHeader)
      activeSessionId.value = newHeader.id
      activeSession.value = {
        id: newHeader.id,
        studentId: newHeader.studentId,
        title: newHeader.title,
        createdAtUtc: newHeader.createdAtUtc,
        updatedAtUtc: newHeader.updatedAtUtc,
        messages: [],
      }

      await triggerOpeningDigest(newHeader.id)
    } catch (err: any) {
      error.value = err?.message || 'Failed to create new chat session.'
    } finally {
      loadingMessages.value = false
    }
  }

  async function removeSession(id: string) {
    try {
      await deleteChatSession(id)
      sessions.value = sessions.value.filter((s) => s.id !== id)

      if (activeSessionId.value === id) {
        activeSessionId.value = null
        activeSession.value = null
        if (sessions.value.length > 0) {
          await selectSession(sessions.value[0].id)
        } else {
          await createNewSession('New Chat')
        }
      }
    } catch (err: any) {
      error.value = err?.message || 'Failed to delete chat session.'
    }
  }

  async function sendMessage(content: string, includeRag = true) {
    if (!content.trim() || !activeSessionId.value || sendingMessage.value) return

    const trimmed = content.trim()
    const sessionId = activeSessionId.value

    // Optimistic user message
    const tempUserMsg: ChatMessageDetail = {
      id: `temp-${Date.now()}`,
      chatSessionId: sessionId,
      role: 'user',
      content: trimmed,
      createdAtUtc: new Date().toISOString(),
    }

    if (activeSession.value) {
      activeSession.value.messages.push(tempUserMsg)
    }

    sendingMessage.value = true
    error.value = null

    try {
      const assistantMsg = await sendChatMessage(sessionId, trimmed, includeRag, CURRENT_STUDENT_ID)
      if (activeSession.value && activeSession.value.id === sessionId) {
        activeSession.value.messages.push(assistantMsg)
      }

      // Update snippet in sidebar
      const currentHeader = sessions.value.find((s) => s.id === sessionId)
      if (currentHeader) {
        currentHeader.lastMessage = assistantMsg.content
        currentHeader.updatedAtUtc = assistantMsg.createdAtUtc
        currentHeader.messageCount += 2
      }
    } catch (err: any) {
      error.value = err?.message || 'Failed to send message.'
    } finally {
      sendingMessage.value = false
    }
  }

  function toggleDrawer() {
    drawerOpen.value = !drawerOpen.value
  }

  return {
    sessions,
    activeSessionId,
    activeSession,
    messages,
    loadingSessions,
    loadingMessages,
    sendingMessage,
    generatingOpeningDigest,
    drawerOpen,
    error,
    fetchSessions,
    selectSession,
    createNewSession,
    removeSession,
    sendMessage,
    toggleDrawer,
  }
}
