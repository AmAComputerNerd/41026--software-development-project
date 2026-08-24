import { ref } from 'vue'
import { generateDigest, getDigests } from '@/api/digest'
import { CURRENT_STUDENT_ID } from '@/config'

export interface AiDigestDto {
  id: string
  studentId: string
  summary: string
  generatedAtUtc: string
}

export function useAiDigest() {
  const digests = ref<AiDigestDto[]>([])
  const loading = ref(false)
  const generating = ref(false)
  const error = ref<string | null>(null)

  async function fetchDigests() {
    loading.value = true
    error.value = null
    try {
      digests.value = await getDigests(CURRENT_STUDENT_ID)
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load digests'
    } finally {
      loading.value = false
    }
  }

  async function generate() {
    generating.value = true
    error.value = null
    try {
      const digest = await generateDigest(CURRENT_STUDENT_ID)
      digests.value = [digest, ...digests.value]
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to generate digest'
    } finally {
      generating.value = false
    }
  }

  return {
    digests,
    loading,
    generating,
    error,
    fetchDigests,
    generate,
  }
}
