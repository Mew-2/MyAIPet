using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;
using MyAIPet.Services.Interfaces;
using Serilog;

namespace MyAIPet.Services;

public class MemoryService : IMemoryService
{
    private readonly string _connectionString;
    private static readonly string[] TriggerWords = { "记住", "记一下", "别忘了", "以后", "称呼我", "我是", "我叫", "喜欢", "讨厌", "不喜欢", "过敏", "不能吃", "在", "住在", "工作" };

    public MemoryService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyAIPet");

        Directory.CreateDirectory(appDataPath);

        var dbPath = Path.Combine(appDataPath, "memories.db");
        _connectionString = $"Data Source={dbPath}";

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Memories (
                Id TEXT PRIMARY KEY,
                Date TEXT NOT NULL,
                Content TEXT NOT NULL,
                Type INTEGER NOT NULL,
                Priority INTEGER NOT NULL,
                Tags TEXT
            )");
    }

    public void AddMemory(MemoryEntry entry)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            connection.Execute(@"
                INSERT INTO Memories (Id, Date, Content, Type, Priority, Tags)
                VALUES (@Id, @Date, @Content, @Type, @Priority, @Tags)",
                new
                {
                    entry.Id,
                    Date = entry.Date.ToString("o"),
                    entry.Content,
                    Type = (int)entry.Type,
                    entry.Priority,
                    Tags = JsonSerializer.Serialize(entry.Tags)
                });

            Log.Information("记忆已保存: {Content}", entry.Content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存记忆失败");
        }
    }

    public void RemoveMemory(string id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            connection.Execute("DELETE FROM Memories WHERE Id = @Id", new { Id = id });
            Log.Information("记忆已删除: {Id}", id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除记忆失败");
        }
    }

    public List<MemoryEntry> GetAllMemories()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var results = connection.Query<MemoryRow>(
                "SELECT * FROM Memories ORDER BY Priority DESC, Date DESC");

            return results.Select(row => new MemoryEntry
            {
                Id = row.Id,
                Date = DateTime.Parse(row.Date),
                Content = row.Content,
                Type = (MemoryType)row.Type,
                Priority = row.Priority,
                Tags = string.IsNullOrEmpty(row.Tags)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(row.Tags) ?? new List<string>()
            }).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取记忆失败");
            return new List<MemoryEntry>();
        }
    }

    public bool ShouldRemember(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return TriggerWords.Any(t => input.Contains(t));
    }

    public MemoryEntry? ExtractFromText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        if (Regex.IsMatch(input, @"^记住"))
        {
            input = input.Substring(2).Trim();
        }

        var memoryContent = ExtractMemoryContent(input);
        if (string.IsNullOrWhiteSpace(memoryContent))
            return null;

        var (type, priority) = DetermineTypeAndPriority(input);

        return new MemoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Date = DateTime.Now,
            Content = memoryContent,
            Type = type,
            Priority = priority
        };
    }

    private string ExtractMemoryContent(string input)
    {
        if (Regex.IsMatch(input, @"(?:我|叫我|称呼我)(?:叫|做|是)(\S{2,6})"))
        {
            var match = Regex.Match(input, @"(?:我|叫我|称呼我)(?:叫|做|是)(\S{2,6})");
            if (match.Success)
                return $"用户名为{match.Groups[1].Value}";
        }

        if (Regex.IsMatch(input, @"称呼我为?(\S{2,6})"))
        {
            var match = Regex.Match(input, @"称呼我为?(\S{2,6})");
            if (match.Success)
                return $"用户希望被称呼为{match.Groups[1].Value}";
        }

        if (Regex.IsMatch(input, @"喜欢(?!被叫)"))
        {
            var match = Regex.Match(input, @"喜欢([^，。,.！!？?]+)");
            if (match.Success)
                return $"用户喜欢{match.Groups[1].Value}";
        }

        if (Regex.IsMatch(input, @"(?:讨厌|不喜欢)([^，。,.！!？?]+)"))
        {
            var match = Regex.Match(input, @"(?:讨厌|不喜欢)([^，。,.！!？?]+)");
            if (match.Success)
                return $"用户讨厌或不喜欢{match.Groups[1].Value}";
        }

        if (Regex.IsMatch(input, @"在([^，,。.]+)工作"))
        {
            var match = Regex.Match(input, @"在([^，,。.]+)工作");
            if (match.Success)
                return $"用户在{match.Groups[1].Value}工作";
        }

        if (Regex.IsMatch(input, @"住在([^，。,.！!？?]+)"))
        {
            var match = Regex.Match(input, @"住在([^，。,.！!？?]+)");
            if (match.Success)
                return $"用户住在{match.Groups[1].Value}";
        }

        if (Regex.IsMatch(input, @"(?:过敏|不能吃)([^，。,.！!？?]+)"))
        {
            var match = Regex.Match(input, @"(?:过敏|不能吃)([^，。,.！!？?]+)");
            if (match.Success)
                return $"用户过敏或不能吃{match.Groups[1].Value}";
        }

        if (input.Length <= 50)
            return input;

        return input.Length > 50 ? input.Substring(0, 50) + "..." : input;
    }

    private (MemoryType type, int priority) DetermineTypeAndPriority(string input)
    {
        if (Regex.IsMatch(input, @"(?:称呼|叫我|我叫|我是)"))
        {
            return (MemoryType.Command, 9);
        }

        if (Regex.IsMatch(input, @"喜欢|讨厌|不喜欢"))
        {
            return (MemoryType.Preference, 7);
        }

        if (Regex.IsMatch(input, @"(?:工作|住在|过敏|不能吃)"))
        {
            return (MemoryType.Fact, 5);
        }

        return (MemoryType.Context, 1);
    }

    public string FormatForContext()
    {
        var memories = GetAllMemories();
        if (memories.Count == 0)
            return string.Empty;

        var grouped = memories
            .GroupBy(m => m.Type)
            .OrderBy(g => GetTypePriority(g.Key));

        var lines = new List<string> { "\n\n【长期记忆】" };

        foreach (var group in grouped)
        {
            var typeName = group.Key switch
            {
                MemoryType.Command => "称呼规则",
                MemoryType.Identity => "身份信息",
                MemoryType.Preference => "用户偏好",
                MemoryType.Fact => "事实信息",
                MemoryType.UserProfile => "用户档案",
                _ => "其他"
            };

            lines.Add($"- {typeName}：{string.Join("；", group.Select(m => m.Content))}");
        }

        return string.Join("\n", lines);
    }

    private int GetTypePriority(MemoryType type)
    {
        return type switch
        {
            MemoryType.Identity => 10,
            MemoryType.Command => 9,
            MemoryType.Preference => 7,
            MemoryType.Fact => 5,
            MemoryType.UserProfile => 10,
            _ => 1
        };
    }

    private class MemoryRow
    {
        public string Id { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Priority { get; set; }
        public string? Tags { get; set; }
    }
}
