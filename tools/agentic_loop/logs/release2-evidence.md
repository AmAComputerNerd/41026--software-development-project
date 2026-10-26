# Release 2 historical evidence

These files preserve local execution records from 7–22 October 2026. Two `pragma: allowlist secret` comments were added to each cloud collector log for a Boolean deployment-toggle comparison and a literal `ci-placeholder` key; neither line contains a credential. Other recorded content is unchanged. They are historical observations, not fresh validation of the current application or proof that every review passed.

## Report evidence map

| Report section | Evidence | Interpretation |
| --- | --- | --- |
| 5.3 Workflow history and coordination/audit logs | [Workflow history snapshot](multi-agent/20261021_workflow-history.json) | Two additional Student 1 runs copied from the local workflow store, including Planner, Worker, Reviewer, human decisions, and timestamped audit entries. |
| 5.4 Multi-Agent Workflow Review | [22 October collector log](multi-agent/20261022T015615Z_multi-agent.log) | Includes successful readiness probes and an observed count of 10 persisted workflows. Both model response fields say `No response generated.` |
| Testing Report Review | [22 October testing collector log](testing/20261022T015701Z_testing.log) | Captures configuration and source inspection; both model response fields say `No response generated.` This does not prove that the referenced tests ran successfully. |
| Cloud Deployment Report Review | [First collector log](cloud-deployment/20261022T015807Z_cloud-deployment.log), [second collector log](cloud-deployment/20261022T021741Z_cloud-deployment.log) | The two files have identical contents. Both model response fields say `No response generated.` These are not successful deployment or completed model-review evidence. |
| Historical MCP checks | [MCP log](mcp/20261007T053758Z_mcp.log), [shared MCP log](shared/20261007T054047Z_mcp.log) | Record HTTP 200 for an empty deadline list and HTTP 400 for invalid input. Model text is incomplete and includes an obsolete container-network description; it is not the current architecture specification. |
| Historical RAG checks | [Partial RAG log](rag/20261007T054014Z_rag.log), [shared RAG observation](shared/20261007T054152Z_rag.log), [failed RAG run](shared/20261007T055409Z_rag.log) | Preserve a partial run, a successful grounded/insufficient-context observation with incomplete model text, and a later HTTP 502 failure. |

## Workflow snapshot limitations

The snapshot contains the two locally added runs, not the complete workflow store:

- `81a7d0fc-4efe-42e0-a262-635616cbe1c8`: Rejected. The human comment says “approved”, but the stored decision is `Reject`; use the stored decision when reporting the result.
- `1b0fa2af-b595-4aee-a6d7-6a5a7a59d817`: Approved. This is the latest workflow referenced by the 22 October Multi-Agent collector log.

Generated plans and reviewer assessments are retained verbatim and can contain inaccurate counts, dates, or recommendations. Human approval records a workflow decision; it does not independently verify every model claim. The snapshot does not demonstrate task writes, current database contents, or other students' workflow execution.

For the current architecture, use the repository documentation: Student 1 owns PostgreSQL 16; Students 2–5 own SQLite persistence; Canvas access uses shared-backend; local AI services run as host processes; all four AI integrations are disabled by default in the required cloud baseline.

Use a GitHub link pinned to this evidence commit when citing these files in the Release 2 Google Doc. Sections 5.3 and 5.4 need their corresponding evidence links added after publication.
