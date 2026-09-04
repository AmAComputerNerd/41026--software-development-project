import type { AssignmentExtensionReason } from '@/types/automation'

interface AssignmentExtensionReasonOption {
  code: AssignmentExtensionReason
  label: string
}

export const assignmentExtensionReasons: AssignmentExtensionReasonOption[] = [
  { code: 'UNW', label: 'I’m unwell' },
  { code: 'ACL', label: 'I have assignment clashes' },
  { code: 'NMT', label: 'I need more time to complete my assignment task' },
  { code: 'FAM', label: 'I have family commitments/responsibilities' },
  { code: 'CAR', label: 'I have had unexpected carer responsibilities' },
  { code: 'REL', label: 'I have religious commitments' },
  { code: 'WRK', label: 'I have to prioritise work' },
  { code: 'TEC', label: 'I have encountered a technical problem trying to submit my assignment' },
  { code: 'BRV', label: 'I have suffered a loss or bereavement' },
  { code: 'OTH', label: 'Other/Prefer not to say' },
]

const reasonLabels = new Map(assignmentExtensionReasons.map((reason) => [reason.code, reason.label]))

export function getAssignmentExtensionReasonLabel(reason: AssignmentExtensionReason) {
  return reasonLabels.get(reason) ?? reason
}