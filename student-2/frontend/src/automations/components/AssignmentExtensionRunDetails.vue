<script setup lang="ts">
import type { AssignmentExtensionAutomationRun } from '@/types/automation'

defineProps<{
  run: AssignmentExtensionAutomationRun
}>()
</script>

<template>
  <dl class="nb-run-fields">
    <div>
      <dt>Assignment ID</dt>
      <dd>{{ run.assignmentId }}</dd>
    </div>
  </dl>
</template>