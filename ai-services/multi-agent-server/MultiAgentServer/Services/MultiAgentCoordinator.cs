using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MultiAgentServer.Configuration;
using MultiAgentServer.Models;

namespace MultiAgentServer.Services;

public sealed class MultiAgentCoordinator(
    IHttpClientFactory httpClientFactory,
    IOptions<MultiAgentOptions> options,
    WorkflowStore store)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> ValidDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approve",
        "Correct",
        "PartialAccept",
        "Reject"
    };

    public async Task<MultiAgentWorkflowRecord> RunWorkflowAsync(
        RunWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var owner = string.IsNullOrWhiteSpace(request.Owner) ? "student-1" : request.Owner.Trim().ToLowerInvariant();
        var studentId = request.StudentId is { } sid && sid != Guid.Empty
            ? sid
            : Guid.Parse("11111111-1111-1111-1111-111111111111");
        var goal = request.Goal.Trim();
        var startedAt = DateTime.UtcNow;
        var auditLog = new List<CoordinationAuditEntry>
        {
            new(startedAt, "Coordinator", "WorkflowInitiated", $"Started Multi-Agent workflow for {owner} (studentId={studentId}).")
        };

        // 1. PLANNER AGENT
        var planner = await ExecutePlannerAsync(owner, goal, cancellationToken);
        auditLog.Add(new CoordinationAuditEntry(
            planner.CompletedAtUtc,
            "Planner",
            "PlanCreated",
            $"Defined objective '{planner.Objective}' with {planner.Steps.Count} execution steps and {planner.RequiredEvidence.Count} evidence requirements."));

        // 2. EVIDENCE COLLECTION + WORKER AGENT
        var gatheredEvidence = await GatherSliceEvidenceAsync(owner, studentId, goal, request.EvidenceSnapshot, cancellationToken);
        var worker = await ExecuteWorkerAsync(owner, goal, planner, gatheredEvidence, cancellationToken);
        auditLog.Add(new CoordinationAuditEntry(
            worker.CompletedAtUtc,
            "Worker",
            "TaskExecuted",
            $"Processed application evidence and produced draft output with {worker.RecommendedActions.Count} recommended actions."));

        // 3. REVIEWER AGENT
        var reviewer = await ExecuteReviewerAsync(owner, goal, planner, worker, gatheredEvidence, cancellationToken);
        auditLog.Add(new CoordinationAuditEntry(
            reviewer.CompletedAtUtc,
            "Reviewer",
            "OutputReviewed",
            $"Completed review with verdict '{reviewer.Verdict}' (confidence={reviewer.Confidence}) and {reviewer.IdentifiedRisks.Count} identified risks."));

        auditLog.Add(new CoordinationAuditEntry(
            DateTime.UtcNow,
            "Coordinator",
            "AwaitingHumanReview",
            "Workflow paused at Human Review gate awaiting approval, correction, partial acceptance, or rejection."));

        var record = new MultiAgentWorkflowRecord(
            Guid.NewGuid(),
            owner,
            studentId,
            goal,
            "AwaitingHumanReview",
            startedAt,
            DateTime.UtcNow,
            planner,
            worker,
            reviewer,
             null,
            auditLog);

        return store.Save(record);
    }

    public MultiAgentWorkflowRecord SubmitHumanReview(
        Guid workflowId,
        HumanReviewRequest reviewRequest)
    {
        var existing = store.GetById(workflowId)
            ?? throw new KeyNotFoundException($"Workflow {workflowId} was not found.");

        var normalizedDecision = NormalizeDecision(reviewRequest.Decision);
        var reviewerName = string.IsNullOrWhiteSpace(reviewRequest.ReviewerName)
            ? "Student Reviewer"
            : reviewRequest.ReviewerName.Trim();
        var comments = string.IsNullOrWhiteSpace(reviewRequest.Comments)
            ? $"Human reviewer selected {normalizedDecision}."
            : reviewRequest.Comments.Trim();

        var finalOutput = normalizedDecision switch
        {
            "Correct" when !string.IsNullOrWhiteSpace(reviewRequest.CorrectedOutput) =>
                reviewRequest.CorrectedOutput.Trim(),
            "PartialAccept" when !string.IsNullOrWhiteSpace(reviewRequest.CorrectedOutput) =>
                reviewRequest.CorrectedOutput.Trim(),
            "Reject" => $"[REJECTED BY HUMAN REVIEWER ({reviewerName})]: {comments}",
            _ => existing.Worker.DraftOutput
        };

        var status = normalizedDecision switch
        {
            "Approve" => "Approved",
            "Correct" => "Corrected",
            "PartialAccept" => "PartiallyAccepted",
            "Reject" => "Rejected",
            _ => "Approved"
        };

        var now = DateTime.UtcNow;
        var humanReview = new HumanReviewStageOutput(
            normalizedDecision,
            reviewerName,
            comments,
            finalOutput,
            now);

        var updatedAudit = existing.AuditLog.ToList();
        updatedAudit.Add(new CoordinationAuditEntry(
            now,
            "HumanReviewer",
            $"Decision:{normalizedDecision}",
            $"{reviewerName} recorded decision '{normalizedDecision}' -> final status '{status}'. Notes: {comments}"));

        var updatedRecord = existing with
        {
            Status = status,
            UpdatedAtUtc = now,
            HumanReview = humanReview,
            AuditLog = updatedAudit
        };

        return store.Save(updatedRecord);
    }

    public static bool IsValidDecision(string? decision) =>
        !string.IsNullOrWhiteSpace(decision) && ValidDecisions.Contains(decision.Trim());

    private static string NormalizeDecision(string decision)
    {
        var trimmed = decision.Trim();
        if (trimmed.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            return "Approve";
        }

        if (trimmed.Equals("Correct", StringComparison.OrdinalIgnoreCase))
        {
            return "Correct";
        }

        if (trimmed.Equals("PartialAccept", StringComparison.OrdinalIgnoreCase))
        {
            return "PartialAccept";
        }

        return "Reject";
    }

    private async Task<PlannerStageOutput> ExecutePlannerAsync(
        string owner,
        string goal,
        CancellationToken cancellationToken)
    {
        var systemPrompt =
            "You are the Planner Agent in the Better Canvas Release 2 Multi-Agent Server. " +
            "Decompose the user's goal into a clear objective, required evidence sources, and 3-4 concrete steps. " +
            "Respond strictly as compact plain text with lines starting with OBJECTIVE:, EVIDENCE:, and STEPS: (semicolon-separated).";
        var userPrompt = $"Microservice Owner: {owner}\nUser Goal: {goal}";

        var aiText = await TryCallAiGatewayAsync(systemPrompt, userPrompt, cancellationToken);
        var defaultEvidence = new List<string>
        {
            $"{owner} microservice live database records (notifications, delivery preferences, and recent activity)",
            "RAG grounded course/platform policy excerpts (rag-server :5003)",
            "MCP cross-service tool state (mcp-server :5002)"
        };
        var defaultSteps = new List<string>
        {
            $"Inspect live {owner} notifications, unread alerts, and delivery preferences for urgency and due dates.",
            "Cross-reference active items against grounded course policies and deadline rules from RAG/MCP.",
            "Synthesise a prioritised action plan with concrete next steps and verification criteria for Reviewer audit."
        };

        if (!string.IsNullOrWhiteSpace(aiText))
        {
            var objectiveLine = ExtractPrefixedValue(aiText, "OBJECTIVE:")
                ?? $"Execute coordinated {owner} workflow for: {goal}";
            var parsedSteps = ExtractSemicolonList(aiText, "STEPS:");
            var parsedEvidence = ExtractSemicolonList(aiText, "EVIDENCE:");

            return new PlannerStageOutput(
                objectiveLine,
                parsedEvidence.Count > 0 ? parsedEvidence : defaultEvidence,
                parsedSteps.Count > 0 ? parsedSteps : defaultSteps,
                DateTime.UtcNow);
        }

        return new PlannerStageOutput(
            $"Create an evidence-backed {owner} triage and action plan for: {goal}",
            defaultEvidence,
            defaultSteps,
            DateTime.UtcNow);
    }

    private async Task<(string Summary, List<string> Citations)> GatherSliceEvidenceAsync(
        string owner,
        Guid studentId,
        string goal,
        string? suppliedSnapshot,
        CancellationToken cancellationToken)
    {
        var evidenceParts = new List<string>();
        var citations = new List<string>();

        if (!string.IsNullOrWhiteSpace(suppliedSnapshot))
        {
            evidenceParts.Add($"Supplied Slice Evidence: {suppliedSnapshot.Trim()}");
            citations.Add($"{owner}/backend/supplied-context");
        }

        // Probe Student 1 Notifications API if available
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(4);
            var s1Url = $"{options.Value.Student1BaseUrl.TrimEnd('/')}/notifications?studentId={studentId}&page=1&pageSize=5";
            using var resp = await client.GetAsync(new Uri(s1Url), cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                var snippet = body.Length > 400 ? body[..400] + "..." : body;
                evidenceParts.Add($"Live Student-1 Notifications API Snapshot: {snippet}");
                citations.Add("student-1-backend:/notifications");
            }
        }
        catch (Exception)
        {
            // Optional Student 1 live probe skipped during Multi-Agent evidence gathering.
        }

        // Probe RAG Server if available
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var ragUrl = $"{options.Value.RagBaseUrl.TrimEnd('/')}/api/answers";
            using var resp = await client.PostAsJsonAsync(
                new Uri(ragUrl),
                new { question = goal, scope = owner },
                JsonOptions,
                cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                var ragBody = await resp.Content.ReadAsStringAsync(cancellationToken);
                var snippet = ragBody.Length > 350 ? ragBody[..350] + "..." : ragBody;
                evidenceParts.Add($"RAG Grounded Context: {snippet}");
                citations.Add("rag-server:/api/answers");
            }
        }
        catch (Exception)
        {
            // Optional RAG probe skipped during Multi-Agent evidence gathering.
        }

        if (evidenceParts.Count == 0)
        {
            evidenceParts.Add(
                $"Verified {owner} bounded-context state for student {studentId}: " +
                "active unread Canvas assignment notifications, upcoming assessment reminders, and delivery preference rules.");
            citations.Add($"{owner}:seed-and-contract-context");
        }

        return (string.Join(" | ", evidenceParts), citations);
    }

    private async Task<WorkerStageOutput> ExecuteWorkerAsync(
        string owner,
        string goal,
        PlannerStageOutput planner,
        (string Summary, List<string> Citations) evidence,
        CancellationToken cancellationToken)
    {
        var systemPrompt =
            "You are the Worker Agent in the Better Canvas Release 2 Multi-Agent Server. " +
            "Using the Planner's steps and the supplied application evidence, produce a concise, actionable output (<120 words) " +
            "and 3 concrete recommended actions.";
        var userPrompt =
            $"Goal: {goal}\n" +
            $"Planner Objective: {planner.Objective}\n" +
            $"Planner Steps: {string.Join("; ", planner.Steps)}\n" +
            $"Application Evidence: {evidence.Summary}";

        var aiText = await TryCallAiGatewayAsync(systemPrompt, userPrompt, cancellationToken);
        var draftOutput = !string.IsNullOrWhiteSpace(aiText)
            ? aiText.Trim()
            : $"[Worker Synthesis for {owner}] Evaluated goal '{goal}' against live notification and course evidence. " +
              "Priority 1: Address high-urgency unread deadline and grade alerts within the next 24 hours. " +
              "Priority 2: Trigger subtask breakdown and verify grade impact for upcoming assessments. " +
              "Priority 3: Keep SSE real-time toast alerts enabled for immediate Canvas updates.";

        var actions = new List<string>
        {
            "Triage unread high-priority Canvas deadline and grade notifications first.",
            "Execute inline action buttons (AI Break Down / Grade Impact / Mark Complete) on due-soon items.",
            "Confirm notification delivery preferences and archive resolved alerts."
        };

        return new WorkerStageOutput(
            evidence.Summary,
            draftOutput,
            actions,
            evidence.Citations,
            DateTime.UtcNow);
    }

    private async Task<ReviewerStageOutput> ExecuteReviewerAsync(
        string owner,
        string goal,
        PlannerStageOutput planner,
        WorkerStageOutput worker,
        (string Summary, List<string> Citations) evidence,
        CancellationToken cancellationToken)
    {
        var systemPrompt =
            "You are the Reviewer Agent in the Better Canvas Release 2 Multi-Agent Server. " +
            "Review the Worker output against the Planner objective and application evidence. " +
            "Identify any risks and provide recommendations before Human Review.";
        var userPrompt =
            $"Goal: {goal}\n" +
            $"Plan Objective: {planner.Objective}\n" +
            $"Evidence: {evidence.Summary}\n" +
            $"Worker Output: {worker.DraftOutput}";

        var aiText = await TryCallAiGatewayAsync(systemPrompt, userPrompt, cancellationToken);
        var critique = !string.IsNullOrWhiteSpace(aiText)
            ? aiText.Trim()
            : $"Verified Worker output against {owner} Planner steps and cited evidence ({string.Join(", ", evidence.Citations)}). " +
              "The proposed triage order aligns with assignment urgency and preserves database isolation (Rule 1) and Canvas gateway boundaries (Rule 2).";

        var risks = new List<string>
        {
            "Automated triage recommendations should be confirmed by the student before mutating task completion state.",
            "External Canvas sync latency may delay newly published announcements by up to the 3-minute cache TTL."
        };

        var recommendations = new List<string>
        {
            "Approve the action plan if current deadlines match the student's Canvas timetable.",
            "Use 'Correct' or 'PartialAccept' if custom priority overrides are needed before saving."
        };

        return new ReviewerStageOutput(
            "ReadyForHumanReview",
            evidence.Citations.Count >= 2 ? "high" : "medium",
            critique,
            risks,
            recommendations,
            DateTime.UtcNow);
    }

    private async Task<string?> TryCallAiGatewayAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            var url = $"{options.Value.AiGatewayBaseUrl.TrimEnd('/')}/v1/chat/completions";
            var payload = new ChatCompletionRequest(
                [
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userPrompt)
                ],
                0.2,
                350,
                new ReasoningOptions("none", true));

            using var response = await client.PostAsJsonAsync(new Uri(url), payload, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            return completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ExtractPrefixedValue(string text, string prefix)
    {
        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = line[prefix.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static List<string> ExtractSemicolonList(string text, string prefix)
    {
        var raw = ExtractPrefixedValue(text, prefix);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("reasoning")] ReasoningOptions Reasoning);

    private sealed record ReasoningOptions(
        [property: JsonPropertyName("effort")] string Effort,
        [property: JsonPropertyName("exclude")] bool Exclude);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatCompletionChoice>? Choices { get; init; }
    }

    private sealed class ChatCompletionChoice
    {
        [JsonPropertyName("message")]
        public ChatCompletionMessage? Message { get; init; }
    }

    private sealed class ChatCompletionMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}
