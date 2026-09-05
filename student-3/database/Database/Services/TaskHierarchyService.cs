using Database.Data;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Database.Models.TaskStatus;

namespace Database.Services;

public sealed class TaskHierarchyService(AppDbContext db)
{
    public async Task CompleteDescendantsAsync(
        Guid parentId,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        var parentIds = new[] { parentId };

        while (parentIds.Length > 0)
        {
            var children = await db.Tasks
                .Where(task =>
                    task.ParentTaskId.HasValue &&
                    parentIds.Contains(task.ParentTaskId.Value))
                .ToListAsync(cancellationToken);

            foreach (var child in children.Where(child => child.Status != TaskStatus.Completed))
            {
                child.Status = TaskStatus.Completed;
                child.UpdatedAt = updatedAt;
            }

            parentIds = children.Select(child => child.Id).ToArray();
        }
    }
}
