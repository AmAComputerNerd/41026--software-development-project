<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    value: number | null
    target?: number | null
    label: string
    accent?: 'orange' | 'yellow' | 'ink'
  }>(),
  { target: null, accent: 'orange' },
)

const width = computed(() => `${Math.min(100, Math.max(0, props.value ?? 0))}%`)
const targetPosition = computed(() => `${Math.min(100, Math.max(0, props.target ?? 0))}%`)
</script>

<template>
  <div class="mark-meter">
    <div class="mark-meter__heading">
      <span class="nb-mono">{{ label }}</span>
      <strong>{{ value == null ? '—' : `${value.toFixed(1)}%` }}</strong>
    </div>
    <div class="mark-meter__track" :aria-label="`${label}: ${value ?? 'not available'}`">
      <div class="mark-meter__fill" :class="`mark-meter__fill--${accent}`" :style="{ width }" />
      <span
        v-if="target != null"
        class="mark-meter__target"
        :style="{ left: targetPosition }"
        :title="`Desired mark: ${target}%`"
      />
    </div>
    <div v-if="target != null" class="mark-meter__legend nb-mono">
      <span>0</span>
      <span>◆ TARGET {{ target.toFixed(1) }}</span>
      <span>100</span>
    </div>
  </div>
</template>
