import type { Component } from 'vue'
import type {
  Automation,
  AutomationDiscriminator,
  AutomationFormData,
  AutomationRun,
  SaveAutomationInput,
} from '@/types/automation'

type AutomationFor<T extends AutomationDiscriminator> = Extract<Automation, { $type: T }>
type AutomationRunFor<T extends AutomationDiscriminator> = Extract<AutomationRun, { $type: T }>
type AutomationFormDataFor<T extends AutomationDiscriminator> = Extract<AutomationFormData, { $type: T }>
type SaveAutomationInputFor<T extends AutomationDiscriminator> = Extract<SaveAutomationInput, { $type: T }>

interface TypedAutomationDefinition<T extends AutomationDiscriminator> {
  discriminator: T
  label: string
  pluralLabel: string
  tagClass: string
  formComponent: Component
  runDetailsComponent: Component
  createForm: () => AutomationFormDataFor<T>
  loadForm: (automation: AutomationFor<T>) => AutomationFormDataFor<T>
  buildInput: (
    form: AutomationFormDataFor<T>,
    studentId: string,
  ) => SaveAutomationInputFor<T>
  buildUpdateInput: (
    automation: AutomationFor<T>,
    enabled: boolean,
  ) => SaveAutomationInputFor<T>
  automationTitle: (automation: AutomationFor<T>) => string
  automationDetail: (automation: AutomationFor<T>) => string
  runTitle: (run: AutomationRunFor<T>) => string
  runDetail: (run: AutomationRunFor<T>) => string
}

export interface AutomationDefinition {
  discriminator: AutomationDiscriminator
  label: string
  pluralLabel: string
  tagClass: string
  formComponent: Component
  runDetailsComponent: Component
  createForm: () => AutomationFormData
  loadForm: (automation: Automation) => AutomationFormData
  buildInput: (form: AutomationFormData, studentId: string) => SaveAutomationInput
  buildUpdateInput: (automation: Automation, enabled: boolean) => SaveAutomationInput
  automationTitle: (automation: Automation) => string
  automationDetail: (automation: Automation) => string
  runTitle: (run: AutomationRun) => string
  runDetail: (run: AutomationRun) => string
}

export function defineAutomationType<T extends AutomationDiscriminator>(
  definition: TypedAutomationDefinition<T>,
): AutomationDefinition {
  return {
    ...definition,
    loadForm: (automation) => definition.loadForm(automation as AutomationFor<T>),
    buildInput: (form, studentId) =>
      definition.buildInput(form as AutomationFormDataFor<T>, studentId),
    buildUpdateInput: (automation, enabled) =>
      definition.buildUpdateInput(automation as AutomationFor<T>, enabled),
    automationTitle: (automation) => definition.automationTitle(automation as AutomationFor<T>),
    automationDetail: (automation) => definition.automationDetail(automation as AutomationFor<T>),
    runTitle: (run) => definition.runTitle(run as AutomationRunFor<T>),
    runDetail: (run) => definition.runDetail(run as AutomationRunFor<T>),
  }
}