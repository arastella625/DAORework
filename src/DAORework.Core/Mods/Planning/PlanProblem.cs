namespace DAORework.Core.Mods.Planning;

/// <summary>What stops a plan, as the window says it: what is wrong, then what to do about it (VOICE.md).</summary>
public sealed record PlanProblem(string Title, string WhatToDo);
