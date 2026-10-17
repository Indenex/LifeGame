using System;
using System.Collections.Generic;

namespace LifeGame;

// What a bug report needs from the last few minutes: the notes the HUD showed and the
// errors the server answered with. Hud.Note and the reducer error handler add to it.
public static class BugLog
{
    const int Keep = 20;
    static readonly Queue<string> notes = new(), errors = new();

    public static void Note(string text) => Add(notes, text);
    public static void Error(string text) => Add(errors, text);

    public static IReadOnlyCollection<string> Notes => notes;
    public static IReadOnlyCollection<string> Errors => errors;

    static void Add(Queue<string> q, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        q.Enqueue($"{DateTime.Now:HH:mm:ss} {text}");
        while (q.Count > Keep) q.Dequeue();
    }
}
