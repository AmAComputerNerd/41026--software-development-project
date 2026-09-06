import AssignmentExtensionFields from '../components/AssignmentExtensionFields.vue'
import AssignmentExtensionRunDetails from '../components/AssignmentExtensionRunDetails.vue'
import { defineAutomationType } from '../definition'
import { getAssignmentExtensionReasonLabel } from '../reasons/assignmentExtension'

function formatTitle(
  automation: { subjectId: number | null; reason: Parameters<typeof getAssignmentExtensionReasonLabel>[0] },
  subjectLabel?: string,
) {
  const subject = subjectLabel ?? (automation.subjectId ? `Course ${automation.subjectId}` : 'Any course')
  return `${subject} · ${getAssignmentExtensionReasonLabel(automation.reason)}`
}

export default defineAutomationType({
  discriminator: 'assignmentExtension',
  label: 'Assignment extension',
  pluralLabel: 'Extensions',
  tagClass: 'extension',
  formComponent: AssignmentExtensionFields,
  runDetailsComponent: AssignmentExtensionRunDetails,
  createForm: () => ({
    $type: 'assignmentExtension',
    enabled: true,
    subjectId: null,
    bufferMinutes: 60,
    reason: 'UNW',
    furtherDetails: '',
  }),
  loadForm: (automation) => ({
    $type: automation.$type,
    enabled: automation.enabled,
    subjectId: automation.subjectId,
    bufferMinutes: automation.bufferMinutes,
    reason: automation.reason,
    furtherDetails: automation.furtherDetails,
  }),
  buildInput: (form, studentId) => ({
    $type: form.$type,
    studentId,
    enabled: form.enabled,
    subjectId: form.subjectId,
    bufferMinutes: form.bufferMinutes,
    reason: form.reason,
    furtherDetails: form.furtherDetails,
  }),
  buildUpdateInput: (automation, enabled) => ({
    $type: automation.$type,
    studentId: automation.studentId,
    enabled,
    subjectId: automation.subjectId,
    bufferMinutes: automation.bufferMinutes,
    reason: automation.reason,
    furtherDetails: automation.furtherDetails,
  }),
  automationTitle: (automation, subjectLabel) => formatTitle(automation, subjectLabel),
  automationDetail: (automation) => `${automation.bufferMinutes} MIN BUFFER`,
  runTitle: (run, automation, subjectLabel) =>
    `${automation ? formatTitle(automation, subjectLabel) : subjectLabel || 'Course unavailable'} · ${run.assignmentTitle || 'Assignment unavailable'}`,
  runDetail: () => 'DETAILS UNAVAILABLE',
})