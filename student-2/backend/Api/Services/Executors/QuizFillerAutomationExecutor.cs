using Api.DTOs;
using Api.Models;

namespace Api.Services.Executors;

public sealed class QuizFillerAutomationExecutor(
    ISharedCanvasClient canvas,
    IAiQuizAnswerService ai,
    QuizAnswerReviewService review) : AutomationExecutor<QuizFillerAutomation>
{
    private const string MultipleChoiceQuestionType = "multiple_choice_question";
    private const string TrueFalseQuestionType = "true_false_question";
    private const string ShortAnswerQuestionType = "short_answer_question";

    protected override async Task<IReadOnlyList<IAutomationExecutionCandidate>>
        GetDueExecutionsAsync(
            QuizFillerAutomation automation,
            DateTime utcNow,
            CancellationToken cancellationToken)
    {
        var questionTypes = GetQuestionTypes(automation);
        if (questionTypes.Count == 0)
        {
            return [];
        }

        long[] courseIds;
        try
        {
            courseIds = automation.SubjectId is { } subjectId
                ? [subjectId]
                : [.. (await canvas.GetCoursesAsync(cancellationToken)).Select(course => course.Id)];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return [new QuizDiscoveryFailureCandidate(automation.Id, 0, exception)];
        }

        var candidates = new List<IAutomationExecutionCandidate>();
        foreach (var courseId in courseIds)
        {
            try
            {
                var quizzes = await canvas.GetQuizzesAsync(courseId, cancellationToken);
                candidates.AddRange(quizzes
                    .Where(quiz => IsEligible(quiz, automation))
                    .Select(quiz => new QuizFillerExecutionCandidate(
                        canvas,
                        ai,
                        automation.RequireHumanReview ? review : null,
                        automation.Id,
                        automation.StudentId,
                        quiz,
                        questionTypes)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                candidates.Add(new QuizDiscoveryFailureCandidate(
                    automation.Id,
                    courseId,
                    exception));
            }
        }

        return candidates;
    }

    private static bool IsEligible(SharedCanvasQuizDto quiz, QuizFillerAutomation automation)
    {
        return quiz.Published &&
            !quiz.LockedForUser &&
            !quiz.HasSubmitted &&
            quiz.QuestionCount > 0 &&
            (HasEnoughAttempts(quiz.AllowedAttempts, automation.NumberOfAttemptsRequired) ||
                (automation.AllowForNoTimeLimit && quiz.TimeLimit is null));
    }

    private static bool HasEnoughAttempts(int allowedAttempts, int attemptsRequired)
    {
        return allowedAttempts < 0 || allowedAttempts >= attemptsRequired;
    }

    private static HashSet<string> GetQuestionTypes(QuizFillerAutomation automation)
    {
        var questionTypes = new HashSet<string>(StringComparer.Ordinal);
        if (automation.MultipleChoice)
        {
            questionTypes.Add(MultipleChoiceQuestionType);
            questionTypes.Add(TrueFalseQuestionType);
        }

        if (automation.ShortAnswer)
        {
            questionTypes.Add(ShortAnswerQuestionType);
        }

        return questionTypes;
    }

    private sealed class QuizDiscoveryFailureCandidate(
        Guid automationId,
        long courseId,
        Exception exception) : IAutomationExecutionCandidate
    {
        public string ExecutionKey { get; } = $"quiz-filler-discovery:v1:{courseId}";

        public bool MatchesRun(AutomationRun run)
        {
            return run.ExecutionKey == ExecutionKey;
        }

        public AutomationRun CreateRun(DateTime startedAtUtc)
        {
            return new QuizFillerAutomationRun
            {
                AutomationId = automationId,
                ExecutionKey = ExecutionKey,
                ExecutionTimeStamp = startedAtUtc,
                Result = AutomationRunResult.Running,
                CourseId = courseId,
                QuizId = 0,
                QuizTitle = $"Quiz discovery failed for course {courseId}",
                QuestionCount = 0
            };
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromException(exception);
        }
    }

    private sealed class QuizFillerExecutionCandidate(
        ISharedCanvasClient canvas,
        IAiQuizAnswerService ai,
        QuizAnswerReviewService? review,
        Guid automationId,
        Guid studentId,
        SharedCanvasQuizDto quiz,
        HashSet<string> questionTypes) : IAutomationExecutionCandidate
    {
        private QuizFillerAutomationRun? _run;

        public string ExecutionKey { get; } = $"quiz-filler:v1:{quiz.Id}";

        public bool MatchesRun(AutomationRun run)
        {
            return run is QuizFillerAutomationRun quizRun && quizRun.QuizId == quiz.Id;
        }

        public AutomationRun CreateRun(DateTime startedAtUtc)
        {
            _run = new QuizFillerAutomationRun
            {
                AutomationId = automationId,
                ExecutionKey = ExecutionKey,
                ExecutionTimeStamp = startedAtUtc,
                Result = AutomationRunResult.Running,
                CourseId = quiz.CourseId,
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                QuestionCount = quiz.QuestionCount
            };
            return _run;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (review is { IsMultiAgentEnabled: false })
            {
                throw new MultiAgentIntegrationDisabledException();
            }

            var submission = await canvas.StartQuizSubmissionAsync(
                quiz.CourseId,
                quiz.Id,
                cancellationToken);
            var questions = await canvas.GetQuizSubmissionQuestionsAsync(
                submission.Id,
                cancellationToken);
            var targetedQuestions = questions
                .Where(question => questionTypes.Contains(question.QuestionType))
                .ToArray();

            if (targetedQuestions.Length == 0)
            {
                throw new InvalidOperationException(
                    "The quiz does not contain any selected supported question types.");
            }

            if (review is not null && _run is not null)
            {
                var draft = await review.ProposeAsync(
                    studentId,
                    _run.Id,
                    quiz.Title,
                    targetedQuestions,
                    cancellationToken);
                _run.SubmissionId = submission.Id;
                _run.SubmissionAttempt = submission.Attempt;
                _run.ValidationToken = submission.ValidationToken;
                _run.WorkflowId = draft.WorkflowId;
                _run.ProposalJson = QuizAnswerReviewService.SerializeProposal(draft.Proposal);
                _run.Result = AutomationRunResult.AwaitingReview;
                return;
            }

            var answers = await ai.AnswerQuestionsAsync(
                new AiQuizContext(quiz.Title, ToAiQuestions(targetedQuestions)),
                cancellationToken);

            await canvas.AnswerQuizSubmissionQuestionsAsync(
                submission.Id,
                new AnswerSharedCanvasQuizQuestionsDto(
                    submission.Attempt,
                    submission.ValidationToken,
                    [.. answers.Select(answer => new SharedCanvasQuizAnswerDto(
                        answer.QuestionId,
                        answer.AnswerId,
                        answer.Text))]),
                cancellationToken);
        }

        private static AiQuizQuestion[] ToAiQuestions(
            IReadOnlyList<SharedCanvasQuizQuestionDto> questions)
        {
            return [.. questions.Select(question => new AiQuizQuestion(
                question.Id,
                question.QuestionText,
                [.. question.Answers.Select(answer =>
                    new AiQuizAnswerOption(answer.Id, answer.Text))]))];
        }
    }
}
