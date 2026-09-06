<script setup lang="ts">
import type { AssignmentExtensionAutomationRun } from '@/types/automation'

defineProps<{
  run: AssignmentExtensionAutomationRun
  courseLabel: string
}>()
</script>

<template>
  <dl class="nb-run-fields">
    <div>
      <dt>Course</dt>
      <dd>{{ courseLabel }}</dd>
    </div>
    <div>
      <dt>Assignment</dt>
      <dd>{{ run.assignmentTitle || 'Assignment unavailable' }}</dd>
    </div>
  </dl>
</template>