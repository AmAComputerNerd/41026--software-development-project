import AssignmentExtensionFields from '../components/AssignmentExtensionFields.vue'
import AssignmentExtensionRunDetails from '../components/AssignmentExtensionRunDetails.vue'
import { defineAutomationType } from '../definition'
import { getAssignmentExtensionReasonLabel } from '../reasons/assignmentExtension'

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
  automationTitle: (automation) => getAssignmentExtensionReasonLabel(automation.reason),
  automationDetail: (automation) =>
    `${automation.subjectId ? `SUBJECT ${automation.subjectId}` : 'ANY SUBJECT'} · ${automation.bufferMinutes} MIN BUFFER`,
  runTitle: (run) => `Assignment ${run.assignmentId}`,
  runDetail: () => 'ASSIGNMENT EXTENSION',
})