<script setup lang="ts">
import type { ChatSessionHeader } from '@/api/chat'

defineProps<{
  open: boolean
  sessions: ChatSessionHeader[]
  activeSessionId: string | null
  loading: boolean
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'select', id: string): void
  (e: 'new-chat'): void
  (e: 'delete', id: string): void
}>()

function formatDate(dateString: string) {
  const date = new Date(dateString)
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>

<template>
  <div v-if="open" class="nb-drawer-backdrop" @click="emit('close')" />

  <aside class="nb-drawer" :class="{ 'nb-drawer--open': open }" aria-label="Past Chat Conversations">
    <div class="nb-drawer__header">
      <div class="nb-drawer__title-row">
        <h2 class="nb-drawer__title">PAST CHATS</h2>
        <button class="nb-drawer__close-btn" aria-label="Close past chats drawer" @click="emit('close')">
          ✕
        </button>
      </div>

      <button class="nb-btn nb-btn--primary nb-drawer__new-btn" @click="emit('new-chat')">
        + NEW CHAT
      </button>
    </div>

    <div class="nb-drawer__body">
      <p v-if="loading && sessions.length === 0" class="nb-drawer__empty">
        Loading conversations...
      </p>

      <p v-else-if="sessions.length === 0" class="nb-drawer__empty">
        No past chats found. Click "+ NEW CHAT" to start one!
      </p>

      <ul v-else class="nb-drawer__list">
        <li
          v-for="session in sessions"
          :key="session.id"
          class="nb-drawer__item"
          :class="{ 'nb-drawer__item--active': session.id === activeSessionId }"
          @click="emit('select', session.id)"
        >
          <div class="nb-drawer__item-main">
            <span class="nb-drawer__item-title">{{ session.title }}</span>
            <span class="nb-drawer__item-time">{{ formatDate(session.updatedAtUtc) }}</span>
            <span v-if="session.lastMessage" class="nb-drawer__item-preview">
              {{ session.lastMessage }}
            </span>
          </div>

          <button
            class="nb-drawer__delete-btn"
            title="Delete chat session"
            aria-label="Delete chat"
            @click.stop="emit('delete', session.id)"
          >
            🗑
          </button>
        </li>
      </ul>
    </div>
  </aside>
</template>

<style scoped>
.nb-drawer-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 900;
  backdrop-filter: blur(2px);
}

.nb-drawer {
  position: fixed;
  top: 0;
  left: 0;
  bottom: 0;
  width: 320px;
  max-width: 85vw;
  background: var(--nb-color-bg, #fff);
  border-right: var(--nb-border-width-md, 3px) solid var(--nb-color-ink, #000);
  box-shadow: 6px 0 0 rgba(0, 0, 0, 0.9);
  z-index: 1000;
  transform: translateX(-100%);
  transition: transform 220ms ease-out;
  display: flex;
  flex-direction: column;
}

.nb-drawer--open {
  transform: translateX(0);
}

.nb-drawer__header {
  padding: 16px;
  border-bottom: var(--nb-border-width-md, 3px) solid var(--nb-color-ink, #000);
  background: var(--nb-color-accent-yellow, #ffe600);
}

.nb-drawer__title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.nb-drawer__title {
  font-size: 18px;
  font-weight: 800;
  margin: 0;
  letter-spacing: 0.5px;
}

.nb-drawer__close-btn {
  background: var(--nb-color-bg, #fff);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  font-weight: 800;
  font-size: 16px;
  width: 32px;
  height: 32px;
  cursor: pointer;
  box-shadow: 2px 2px 0 var(--nb-color-ink, #000);
}

.nb-drawer__close-btn:hover {
  background: #ff5252;
  color: #fff;
}

.nb-drawer__new-btn {
  width: 100%;
  font-weight: 800;
  font-size: 14px;
  background: var(--nb-color-accent-green, #00e676);
  color: var(--nb-color-ink, #000);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  padding: 10px;
  box-shadow: 3px 3px 0 var(--nb-color-ink, #000);
  cursor: pointer;
}

.nb-drawer__new-btn:hover {
  transform: translate(-1px, -1px);
  box-shadow: 4px 4px 0 var(--nb-color-ink, #000);
}

.nb-drawer__body {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
}

.nb-drawer__empty {
  padding: 24px 8px;
  font-size: 13px;
  color: var(--nb-color-muted, #555);
  text-align: center;
}

.nb-drawer__list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.nb-drawer__item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px;
  background: var(--nb-color-card-bg, #fbfbfb);
  border: var(--nb-border-width-sm, 2px) solid var(--nb-color-ink, #000);
  box-shadow: 2px 2px 0 var(--nb-color-ink, #000);
  cursor: pointer;
  transition: all 120ms ease-out;
}

.nb-drawer__item:hover {
  background: #f0f0f0;
  transform: translate(-1px, -1px);
  box-shadow: 3px 3px 0 var(--nb-color-ink, #000);
}

.nb-drawer__item--active {
  background: #e3f2fd;
  border-left: 6px solid var(--nb-color-ink, #000);
}

.nb-drawer__item-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.nb-drawer__item-title {
  font-weight: 700;
  font-size: 13px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.nb-drawer__item-time {
  font-size: 11px;
  color: var(--nb-color-muted, #666);
}

.nb-drawer__item-preview {
  font-size: 11px;
  color: var(--nb-color-muted, #444);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.nb-drawer__delete-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 6px;
  font-size: 14px;
  opacity: 0.6;
}

.nb-drawer__delete-btn:hover {
  opacity: 1;
  transform: scale(1.15);
}
</style>
