<script setup lang="ts">
import { computed } from 'vue'
import { NOTIFICATION_TYPES, tagClassForType } from '@/composables/useNotifications'

const props = defineProps<{ type: string }>()

const label = computed(
  () => NOTIFICATION_TYPES.find((t) => t.value === props.type)?.label ?? props.type.toUpperCase(),
)
</script>

<template>
  <span class="nb-tag" :class="tagClassForType(type)">{{ label }}</span>
</template>
