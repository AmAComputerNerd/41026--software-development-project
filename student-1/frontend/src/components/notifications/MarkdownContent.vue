<script setup lang="ts">
import { computed } from 'vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'

const props = defineProps<{ source: string }>()

marked.setOptions({ breaks: true, gfm: true })

const html = computed(() => DOMPurify.sanitize(marked.parse(props.source, { async: false }) as string))
</script>

<template>
  <div class="nb-markdown" v-html="html" />
</template>

<style scoped>
.nb-markdown {
  white-space: normal;
  word-break: break-word;
}

.nb-markdown :deep(p) {
  margin: 0 0 8px;
  line-height: 1.5;
}

.nb-markdown :deep(p:last-child) {
  margin-bottom: 0;
}

.nb-markdown :deep(ul),
.nb-markdown :deep(ol) {
  margin: 4px 0 8px;
  padding-left: 20px;
}

.nb-markdown :deep(li) {
  margin-bottom: 4px;
  line-height: 1.45;
}

.nb-markdown :deep(li:last-child) {
  margin-bottom: 0;
}

.nb-markdown :deep(h1),
.nb-markdown :deep(h2),
.nb-markdown :deep(h3),
.nb-markdown :deep(h4) {
  font-size: 14px;
  font-weight: 700;
  margin: 12px 0 4px;
  color: var(--nb-color-ink);
}

.nb-markdown :deep(h1:first-child),
.nb-markdown :deep(h2:first-child),
.nb-markdown :deep(h3:first-child),
.nb-markdown :deep(h4:first-child) {
  margin-top: 0;
}

.nb-markdown :deep(strong) {
  font-weight: 700;
  color: var(--nb-color-ink);
}

.nb-markdown :deep(em) {
  font-style: italic;
}

.nb-markdown :deep(code) {
  font-family: var(--nb-font-mono, monospace);
  background: rgba(0, 0, 0, 0.07);
  border: 1px solid rgba(0, 0, 0, 0.12);
  padding: 1px 5px;
  font-size: 12px;
}

.nb-markdown :deep(pre) {
  background: var(--nb-color-ink);
  color: var(--nb-color-bg);
  padding: var(--nb-space-3);
  margin: 8px 0;
  overflow-x: auto;
  border-radius: 0;
}

.nb-markdown :deep(pre code) {
  background: transparent;
  border: none;
  color: inherit;
  padding: 0;
}

.nb-markdown :deep(blockquote) {
  border-left: 3px solid var(--nb-color-ink);
  padding-left: 12px;
  margin: 8px 0;
  color: var(--nb-color-muted);
}
</style>
