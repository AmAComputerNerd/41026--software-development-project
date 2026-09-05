import QuizFillerFields from '../components/QuizFillerFields.vue'
import QuizFillerRunDetails from '../components/QuizFillerRunDetails.vue'
import { defineAutomationType } from '../definition'

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
  automationTitle: (automation) =>
    [
      automation.multipleChoice ? 'Multiple choice + true/false' : null,
      automation.shortAnswer ? 'Short answer' : null,
    ]
      .filter(Boolean)
      .join(' + ') || 'No question types',
  automationDetail: (automation) =>
    `${automation.subjectId ? `SUBJECT ${automation.subjectId}` : 'ANY SUBJECT'} · ${automation.numberOfAttemptsRequired} ATTEMPTS${automation.allowForNoTimeLimit ? ' OR NO TIME LIMIT' : ''}`,
  runTitle: (run) => run.quizTitle,
  runDetail: (run) => `QUIZ ${run.quizId} · ${run.questionCount} QUESTIONS`,
})
