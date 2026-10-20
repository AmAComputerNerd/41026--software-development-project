using System.Text.Json;
using Microsoft.Extensions.Options;
using MultiAgentServer.Configuration;
using MultiAgentServer.Models;

namespace MultiAgentServer.Services;

public sealed class WorkflowStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly Lock _syncRoot = new();
    private readonly string _filePath;
    private readonly List<MultiAgentWorkflowRecord> _records = [];

    public WorkflowStore(IOptions<MultiAgentOptions> options, IWebHostEnvironment env)
    {
        var configuredPath = options.Value.DataFilePath;
        _filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(env.ContentRootPath, configuredPath);

        LoadFromDisk();
    }

    public IReadOnlyList<MultiAgentWorkflowRecord> List(string? owner = null, Guid? studentId = null)
    {
        lock (_syncRoot)
        {
            IEnumerable<MultiAgentWorkflowRecord> query = _records;
            if (!string.IsNullOrWhiteSpace(owner))
            {
                query = query.Where(r => string.Equals(r.Owner, owner.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (studentId.HasValue && studentId.Value != Guid.Empty)
            {
                query = query.Where(r => r.StudentId == studentId.Value);
            }

            return query
                .OrderByDescending(r => r.UpdatedAtUtc)
                .ToList();
        }
    }

    public MultiAgentWorkflowRecord? GetById(Guid id)
    {
        lock (_syncRoot)
        {
            return _records.FirstOrDefault(r => r.Id == id);
        }
    }

    public MultiAgentWorkflowRecord Save(MultiAgentWorkflowRecord record)
    {
        lock (_syncRoot)
        {
            var records = _records.ToList();
            var existingIndex = records.FindIndex(r => r.Id == record.Id);
            if (existingIndex >= 0)
            {
                records[existingIndex] = record;
            }
            else
            {
                records.Add(record);
            }

            PersistToDisk(records);
            _records.Clear();
            _records.AddRange(records);
            return record;
        }
    }

    public MultiAgentWorkflowRecord Update(
        MultiAgentWorkflowRecord expected, MultiAgentWorkflowRecord updated)
    {
        lock (_syncRoot)
        {
            var current = _records.FirstOrDefault(record => record.Id == expected.Id);
            if (!ReferenceEquals(current, expected))
            {
                throw new WorkflowConflictException("The workflow changed while this request was running. Reload the current proposal.");
            }
            return Save(updated);
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var loaded = JsonSerializer.Deserialize<List<MultiAgentWorkflowRecord>>(json, JsonOptions);
            if (loaded is not null)
            {
                _records.Clear();
                _records.AddRange(loaded);
            }
        }
        catch (Exception)
        {
            // Keep empty in-memory store if existing file is unreadable.
        }
    }

    private void PersistToDisk(IReadOnlyList<MultiAgentWorkflowRecord> records)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(records, JsonOptions);
            var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
        catch (Exception ex)
        {
            // Disk persistence error should not fail workflow execution; state remains in-memory.
            System.Diagnostics.Trace.TraceWarning($"WorkflowStore: Failed to persist workflows to disk: {ex.Message}");
        }
    }
}
