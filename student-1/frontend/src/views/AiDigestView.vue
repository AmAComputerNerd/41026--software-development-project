<script setup lang="ts">
import { computed, onMounted } from 'vue'
import DigestCard from '@/components/notifications/DigestCard.vue'
import DigestHistoryList from '@/components/notifications/DigestHistoryList.vue'
import { useAiDigest } from '@/composables/useAiDigest'

const { digests, generating, error, fetchDigests, generate } = useAiDigest()

const latestDigest = computed(() => digests.value[0] ?? null)

onMounted(fetchDigests)
</script>

<template>
  <div class="nb-digest">
    <h1 class="nb-digest__title">AI DIGEST</h1>

    <p v-if="error" class="nb-digest__error nb-mono">{{ error }}</p>

    <DigestCard :latest-digest="latestDigest" :generating="generating" @generate="generate" />

    <DigestHistoryList :digests="digests" />
  </div>
</template>

<style scoped>
.nb-digest__title {
  font-size: 28px;
  font-weight: 700;
  margin-bottom: 16px;
}

.nb-digest__error {
  padding: 16px 0;
  color: var(--nb-color-muted);
}
</style>
