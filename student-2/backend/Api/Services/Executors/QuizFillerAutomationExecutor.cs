using Api.DTOs;
using Api.Models;

namespace Api.Services.Executors;

public sealed class QuizFillerAutomationExecutor(
    ISharedCanvasClient canvas,
    IAiQuizAnswerService ai) : AutomationExecutor<QuizFillerAutomation>
{
    private const string MultipleChoiceQuestionType = "multiple_choice_question";
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

        long[] courseIds = automation.SubjectId is { } subjectId
            ? [subjectId]
            : [.. (await canvas.GetCoursesAsync(cancellationToken)).Select(course => course.Id)];

        var candidates = new List<IAutomationExecutionCandidate>();
        foreach (var courseId in courseIds)
        {
            var quizzes = await canvas.GetQuizzesAsync(courseId, cancellationToken);
            candidates.AddRange(quizzes
                .Where(quiz => IsEligible(quiz, automation))
                .Select(quiz => new QuizFillerExecutionCandidate(
                    canvas,
                    ai,
                    automation.Id,
                    quiz,
                    questionTypes)));
        }

        return candidates;
    }

    private static bool IsEligible(SharedCanvasQuizDto quiz, QuizFillerAutomation automation)
    {
        return quiz.Published &&
            !quiz.LockedForUser &&
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
        }

        if (automation.ShortAnswer)
        {
            questionTypes.Add(ShortAnswerQuestionType);
        }

        return questionTypes;
    }

    private sealed class QuizFillerExecutionCandidate(
        ISharedCanvasClient canvas,
        IAiQuizAnswerService ai,
        Guid automationId,
        SharedCanvasQuizDto quiz,
        HashSet<string> questionTypes) : IAutomationExecutionCandidate
    {
        public string ExecutionKey { get; } = $"quiz-filler:v1:{quiz.Id}";

        public bool MatchesRun(AutomationRun run)
        {
            return run is QuizFillerAutomationRun quizRun && quizRun.QuizId == quiz.Id;
        }

        public AutomationRun CreateRun(DateTime startedAtUtc)
        {
            return new QuizFillerAutomationRun
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
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
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
