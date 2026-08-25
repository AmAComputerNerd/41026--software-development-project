<script setup lang="ts">
import { computed } from 'vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'

const props = defineProps<{ source: string }>()

marked.setOptions({ breaks: true })

const html = computed(() => DOMPurify.sanitize(marked.parse(props.source, { async: false }) as string))
</script>

<template>
  <div class="nb-markdown" v-html="html" />
</template>

<style scoped>
.nb-markdown :deep(p) {
  margin: 0 0 8px;
}

.nb-markdown :deep(p:last-child) {
  margin-bottom: 0;
}

.nb-markdown :deep(ul),
.nb-markdown :deep(ol) {
  margin: 0 0 8px;
  padding-left: 20px;
}

.nb-markdown :deep(h1),
.nb-markdown :deep(h2),
.nb-markdown :deep(h3) {
  font-size: 1em;
  font-weight: 700;
  margin: 12px 0 4px;
}

.nb-markdown :deep(h1:first-child),
.nb-markdown :deep(h2:first-child),
.nb-markdown :deep(h3:first-child) {
  margin-top: 0;
}

.nb-markdown :deep(strong) {
  font-weight: 700;
}

.nb-markdown :deep(code) {
  font-family: var(--nb-font-mono, monospace);
  background: rgba(0, 0, 0, 0.06);
  padding: 1px 4px;
}
</style>
