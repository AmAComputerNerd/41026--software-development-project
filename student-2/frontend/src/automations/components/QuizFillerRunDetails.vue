<script setup lang="ts">
import type { QuizFillerAutomationRun } from '@/types/automation'

defineProps<{
  run: QuizFillerAutomationRun
  courseLabel: string
}>()
</script>

<template>
  <dl class="nb-run-fields">
    <div>
      <dt>Quiz</dt>
      <dd>{{ run.quizTitle }}</dd>
    </div>
    <div>
      <dt>Course</dt>
      <dd>{{ courseLabel }}</dd>
    </div>
    <div>
      <dt>Questions</dt>
      <dd>{{ run.questionCount }}</dd>
    </div>
  </dl>
</template>
