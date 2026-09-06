import QuizFillerFields from '../components/QuizFillerFields.vue'
import QuizFillerRunDetails from '../components/QuizFillerRunDetails.vue'
import { defineAutomationType } from '../definition'

function formatSubject(subjectId: number | null, subjectLabel?: string) {
  return subjectLabel ?? (subjectId ? `Course ${subjectId}` : 'Any course')
}

export default defineAutomationType({
  discriminator: 'quizFiller',
  label: 'Quiz filler',
  pluralLabel: 'Quiz fillers',
  tagClass: 'quiz',
  formComponent: QuizFillerFields,
  runDetailsComponent: QuizFillerRunDetails,
  createForm: () => ({
    $type: 'quizFiller',
    enabled: true,
    subjectId: null,
    multipleChoice: true,
    shortAnswer: false,
    numberOfAttemptsRequired: 2,
    allowForNoTimeLimit: false,
  }),
  loadForm: (automation) => ({
    $type: automation.$type,
    enabled: automation.enabled,
    subjectId: automation.subjectId,
    multipleChoice: automation.multipleChoice,
    shortAnswer: automation.shortAnswer,
    numberOfAttemptsRequired: automation.numberOfAttemptsRequired,
    allowForNoTimeLimit: automation.allowForNoTimeLimit,
  }),
  buildInput: (form, studentId) => ({
    $type: form.$type,
    studentId,
    enabled: form.enabled,
    subjectId: form.subjectId,
    multipleChoice: form.multipleChoice,
    shortAnswer: form.shortAnswer,
    numberOfAttemptsRequired: form.numberOfAttemptsRequired,
    allowForNoTimeLimit: form.allowForNoTimeLimit,
  }),
  buildUpdateInput: (automation, enabled) => ({
    $type: automation.$type,
    studentId: automation.studentId,
    enabled,
    subjectId: automation.subjectId,
    multipleChoice: automation.multipleChoice,
    shortAnswer: automation.shortAnswer,
    numberOfAttemptsRequired: automation.numberOfAttemptsRequired,
    allowForNoTimeLimit: automation.allowForNoTimeLimit,
  }),
  automationTitle: (automation, subjectLabel) => formatSubject(automation.subjectId, subjectLabel),
  automationDetail: (automation) =>
    `${automation.numberOfAttemptsRequired} ATTEMPTS${automation.allowForNoTimeLimit ? ' OR NO TIME LIMIT' : ''}`,
  runTitle: (run, automation, subjectLabel) =>
    `${subjectLabel || (automation ? formatSubject(automation.subjectId) : `Course ${run.courseId}`)} · ${run.quizId === 0 ? 'Quiz discovery failed' : run.quizTitle || 'Quiz unavailable'}`,
  runDetail: (run) => `${run.questionCount} QUESTIONS`,
})
