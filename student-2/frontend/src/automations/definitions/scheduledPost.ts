import ScheduledPostFields from '../components/ScheduledPostFields.vue'
import ScheduledPostRunDetails from '../components/ScheduledPostRunDetails.vue'
import { defineAutomationType } from '../definition'

function toLocalDateTime(date: Date) {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export default defineAutomationType({
  discriminator: 'scheduledPost',
  label: 'Scheduled post',
  pluralLabel: 'Scheduled posts',
  tagClass: 'post',
  formComponent: ScheduledPostFields,
  runDetailsComponent: ScheduledPostRunDetails,
  createForm: () => ({
    $type: 'scheduledPost',
    enabled: true,
    postTime: toLocalDateTime(new Date(Date.now() + 24 * 60 * 60 * 1000)),
    contextCode: '',
    recipients: [],
    subject: '',
    body: '',
    groupConversation: true,
  }),
  loadForm: (automation) => ({
    $type: automation.$type,
    enabled: automation.enabled,
    postTime: toLocalDateTime(new Date(automation.postTime)),
    contextCode: automation.contextCode,
    recipients: automation.recipients,
    subject: automation.subject,
    body: automation.body,
    groupConversation: automation.groupConversation,
  }),
  buildInput: (form, studentId) => ({
    $type: form.$type,
    studentId,
    enabled: form.enabled,
    postTime: new Date(form.postTime).toISOString(),
    contextCode: form.contextCode,
    recipients: form.recipients,
    subject: form.subject,
    body: form.body,
    groupConversation: form.groupConversation,
  }),
  buildUpdateInput: (automation, enabled) => ({
    $type: automation.$type,
    studentId: automation.studentId,
    enabled,
    postTime: automation.postTime,
    contextCode: automation.contextCode,
    recipients: automation.recipients,
    subject: automation.subject,
    body: automation.body,
    groupConversation: automation.groupConversation,
  }),
  automationTitle: (automation) => automation.subject,
  automationDetail: (automation) =>
    `${formatDate(automation.postTime)} · ${automation.recipients.length} recipient${automation.recipients.length === 1 ? '' : 's'}`,
  runTitle: (run) => run.subject,
  runDetail: (run) =>
    `${run.recipients.length} recipient${run.recipients.length === 1 ? '' : 's'}`,
})