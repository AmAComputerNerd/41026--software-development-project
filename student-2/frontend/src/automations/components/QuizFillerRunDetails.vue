<script setup lang="ts">
import type { QuizFillerAutomationRun } from '@/types/automation'

defineProps<{
  run: QuizFillerAutomationRun
}>()
</script>

<template>
  <dl class="nb-run-fields">
    <div>
      <dt>Quiz</dt>
      <dd>{{ run.quizTitle }}</dd>
    </div>
    <div>
      <dt>Quiz ID</dt>
      <dd>{{ run.quizId }}</dd>
    </div>
    <div>
      <dt>Course ID</dt>
      <dd>{{ run.courseId }}</dd>
    </div>
    <div>
      <dt>Questions</dt>
      <dd>{{ run.questionCount }}</dd>
    </div>
  </dl>
</template>
