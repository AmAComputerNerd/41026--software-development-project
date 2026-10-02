<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import McpReadinessPanel from '@/components/McpReadinessPanel.vue'
import RagHelpPanel from '@/components/RagHelpPanel.vue'

const router = useRouter()
const { currentUser, loading, error, fetchUser } = useAuth()

async function loadAccount() {
  const userId = localStorage.getItem('userId')
  if (!userId) {
    await router.replace({ name: 'login' })
    return
  }

  try {
    await fetchUser(userId)
  } catch {
    if (!localStorage.getItem('userId')) {
      await router.replace({ name: 'login' })
    }
  }
}

onMounted(loadAccount)

function editProfile() {
  router.push({ name: 'profile', query: { edit: '1' } })
}
</script>

<template>
  <div class="nb-knowledge-page">
    <div v-if="loading" class="nb-profile__loading nb-mono">LOADING ACCOUNT...</div>
    <div v-else-if="error" class="nb-profile__error nb-panel">
      <p role="alert" class="nb-mono">{{ error }}</p>
      <button type="button" class="nb-btn" @click="loadAccount">RETRY</button>
    </div>
    <template v-else-if="currentUser">
      <header class="nb-panel nb-profile__header">
        <div class="nb-profile__info">
          <h1 class="nb-profile__name nb-mono">ACCOUNT KNOWLEDGE</h1>
          <p>Check your account setup with MCP or ask documentation-grounded questions with RAG. Manage account details in 02 PROFILE.</p>
        </div>
      </header>
      <McpReadinessPanel :user-id="currentUser.id" @edit-profile="editProfile" />
      <RagHelpPanel />
    </template>
  </div>
</template>
