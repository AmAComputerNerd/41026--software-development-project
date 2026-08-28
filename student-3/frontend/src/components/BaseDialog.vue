<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    modelValue: boolean
    labelledBy: string
    width?: string
  }>(),
  {
    width: '650px',
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  opened: []
}>()

const dialog = ref<HTMLDialogElement | null>(null)

watch(
  () => props.modelValue,
  async (open) => {
    await nextTick()
    if (open && !dialog.value?.open) {
      dialog.value?.showModal()
      emit('opened')
    } else if (!open && dialog.value?.open) {
      dialog.value.close()
    }
  },
  { immediate: true },
)

function requestClose() {
  emit('update:modelValue', false)
}

function handleBackdropClick(event: MouseEvent) {
  if (event.target === dialog.value) {
    requestClose()
  }
}
</script>

<template>
  <dialog
    ref="dialog"
    class="nb-modal"
    :aria-labelledby="labelledBy"
    :style="{ '--nb-dialog-width': width }"
    @cancel.prevent="requestClose"
    @close="requestClose"
    @click="handleBackdropClick"
  >
    <div class="nb-dialog">
      <slot />
    </div>
  </dialog>
</template>
