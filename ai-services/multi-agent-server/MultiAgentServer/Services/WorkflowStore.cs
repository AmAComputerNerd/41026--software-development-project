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
            var existingIndex = _records.FindIndex(r => r.Id == record.Id);
            if (existingIndex >= 0)
            {
                _records[existingIndex] = record;
            }
            else
            {
                _records.Add(record);
            }

            PersistToDisk();
            return record;
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

    private void PersistToDisk()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_records, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
