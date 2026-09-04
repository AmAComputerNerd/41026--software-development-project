import type { AutomationDiscriminator } from '@/types/automation'
import type { AutomationDefinition } from './definition'

const modules = import.meta.glob<AutomationDefinition>('./definitions/*.ts', {
  eager: true,
  import: 'default',
})

export const automationDefinitions = Object.values(modules)
const definitionsByType = new Map(
  automationDefinitions.map((definition) => [definition.discriminator, definition]),
)

export function getAutomationDefinition(type: AutomationDiscriminator): AutomationDefinition {
  const definition = definitionsByType.get(type)
  if (!definition) {
    throw new Error(`No automation definition is registered for ${type}.`)
  }
  return definition
}