<script setup lang="ts">
import { computed } from 'vue'
import AssignmentRow from './AssignmentRow.vue'
import type { Assignment, AssignmentGroup, StudentAssignment } from '@/api/grades'

const props = defineProps<{
  group: AssignmentGroup
  assignments: Assignment[]
  markFor: (assignmentId: string) => StudentAssignment | undefined
  saveTemporaryMark: (assignmentId: string, mark: number) => Promise<void>
  removeTemporaryMark: (assignmentId: string) => Promise<void>
}>()

const weightPercent = computed(() =>
  props.group.weight == null ? null : (props.group.weight * 100).toFixed(0),
)

const groupMark = computed(() => {
  if (!props.assignments.length) return null
  let sum = 0
  let count = 0
  for (const assignment of props.assignments) {
    const mark = props.markFor(assignment.assignmentId)
    const score = mark?.finalMark ?? mark?.tempMark ?? null
    if (score == null || !assignment.maxMark) continue
    sum += (score / assignment.maxMark) * 100
    count += 1
  }
  return count > 0 ? sum / count : null
})

const gradedCount = computed(
  () =>
    props.assignments.filter(
      (assignment) => props.markFor(assignment.assignmentId)?.finalMark != null,
    ).length,
)
</script>

<template>
  <article class="group-card nb-panel">
    <header class="group-card__header">
      <div class="group-card__title">
        <h3>{{ group.name ?? 'UNTITLED GROUP' }}</h3>
        <p class="nb-mono group-card__meta">
          {{ gradedCount }}/{{ assignments.length }} MARKED
        </p>
      </div>
      <div class="group-card__chips">
        <span v-if="weightPercent != null" class="group-card__weight nb-mono">
          WEIGHT {{ weightPercent }}%
        </span>
        <span class="group-card__average nb-mono">
          {{ groupMark == null ? '—' : `${groupMark.toFixed(1)}%` }}
        </span>
      </div>
    </header>

    <div v-if="assignments.length" class="group-card__assignments">
      <AssignmentRow
        v-for="assignment in assignments"
        :key="assignment.assignmentId"
        :assignment="assignment"
        :mark="markFor(assignment.assignmentId)"
        :save-temporary-mark="saveTemporaryMark"
        :remove-temporary-mark="removeTemporaryMark"
      />
    </div>
    <p v-else class="group-card__empty nb-mono">NO ASSIGNMENTS IN THIS GROUP.</p>
  </article>
</template>