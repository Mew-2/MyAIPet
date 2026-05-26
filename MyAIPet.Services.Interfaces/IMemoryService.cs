using System;
using System.Collections.Generic;

namespace MyAIPet.Services.Interfaces;

public interface IMemoryService
{
    void AddMemory(MemoryEntry entry);
    void RemoveMemory(string id);
    List<MemoryEntry> GetAllMemories();
    MemoryEntry? ExtractFromText(string input);
    string FormatForContext();
    bool ShouldRemember(string input);
}

public class MemoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Date { get; set; } = DateTime.Now;
    public string Content { get; set; } = string.Empty;
    public MemoryType Type { get; set; } = MemoryType.Context;
    public int Priority { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
}

public enum MemoryType
{
    UserProfile = 0,
    Identity = 1,
    Preference = 2,
    Fact = 3,
    Command = 4,
    Context = 5
}
