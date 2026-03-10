using Fluxion.Core.Interfaces;
using Fluxion.Core.Models;

namespace Fluxion.Core.Data;

/// <summary>
/// Seeds the Knowledge Graph with a sample "Learn C# Programming" curriculum.
/// ~15 nodes forming a realistic prerequisite chain.
/// </summary>
public static class GraphSeeder
{
    public static async Task SeedAsync(IKnowledgeGraphRepository repo)
    {
        // ── Define Nodes ────────────────────────────────────

        var intro = new KnowledgeNode
        {
            Title = "Introduction to C#",
            Description = "What is C#, .NET ecosystem, development environment setup.",
            DifficultyLevel = 1, DefaultFormat = ContentFormat.Text,
            MasteryThreshold = 0.6, EstimatedMinutes = 10,
            Tags = ["csharp", "fundamentals"]
        };

        var variables = new KnowledgeNode
        {
            Title = "Variables & Data Types",
            Description = "Primitive types, strings, type conversion, var keyword.",
            DifficultyLevel = 2, DefaultFormat = ContentFormat.Interactive,
            MasteryThreshold = 0.7, EstimatedMinutes = 15,
            Tags = ["csharp", "fundamentals"]
        };

        var operators = new KnowledgeNode
        {
            Title = "Operators & Expressions",
            Description = "Arithmetic, comparison, logical, and ternary operators.",
            DifficultyLevel = 2, DefaultFormat = ContentFormat.Text,
            MasteryThreshold = 0.7, EstimatedMinutes = 12,
            Tags = ["csharp", "fundamentals"]
        };

        var controlFlow = new KnowledgeNode
        {
            Title = "Control Flow",
            Description = "If/else, switch, for, while, foreach loops.",
            DifficultyLevel = 3, DefaultFormat = ContentFormat.Interactive,
            MasteryThreshold = 0.7, EstimatedMinutes = 20,
            Tags = ["csharp", "fundamentals"]
        };

        var methods = new KnowledgeNode
        {
            Title = "Methods & Parameters",
            Description = "Defining methods, return types, ref/out parameters, overloading.",
            DifficultyLevel = 3, DefaultFormat = ContentFormat.Text,
            MasteryThreshold = 0.7, EstimatedMinutes = 18,
            Tags = ["csharp", "fundamentals"]
        };

        var arrays = new KnowledgeNode
        {
            Title = "Arrays & Collections",
            Description = "Arrays, List<T>, Dictionary<K,V>, LINQ basics.",
            DifficultyLevel = 4, DefaultFormat = ContentFormat.Interactive,
            MasteryThreshold = 0.7, EstimatedMinutes = 20,
            Tags = ["csharp", "collections"]
        };

        var oopBasics = new KnowledgeNode
        {
            Title = "OOP – Classes & Objects",
            Description = "Classes, constructors, properties, access modifiers.",
            DifficultyLevel = 4, DefaultFormat = ContentFormat.Visual,
            MasteryThreshold = 0.7, EstimatedMinutes = 25,
            Tags = ["csharp", "oop"]
        };

        var inheritance = new KnowledgeNode
        {
            Title = "Inheritance & Polymorphism",
            Description = "Base classes, virtual/override, abstract classes, interfaces.",
            DifficultyLevel = 5, DefaultFormat = ContentFormat.Visual,
            MasteryThreshold = 0.7, EstimatedMinutes = 25,
            Tags = ["csharp", "oop"]
        };

        var exceptions = new KnowledgeNode
        {
            Title = "Exception Handling",
            Description = "Try/catch/finally, custom exceptions, best practices.",
            DifficultyLevel = 5, DefaultFormat = ContentFormat.Text,
            MasteryThreshold = 0.7, EstimatedMinutes = 15,
            Tags = ["csharp", "error-handling"]
        };

        var generics = new KnowledgeNode
        {
            Title = "Generics",
            Description = "Generic classes, methods, constraints, covariance/contravariance.",
            DifficultyLevel = 6, DefaultFormat = ContentFormat.Text,
            MasteryThreshold = 0.7, EstimatedMinutes = 20,
            Tags = ["csharp", "advanced"]
        };

        var delegates = new KnowledgeNode
        {
            Title = "Delegates & Events",
            Description = "Delegates, multicast delegates, events, Func/Action.",
            DifficultyLevel = 6, DefaultFormat = ContentFormat.Interactive,
            MasteryThreshold = 0.7, EstimatedMinutes = 20,
            Tags = ["csharp", "advanced"]
        };

        var linq = new KnowledgeNode
        {
            Title = "LINQ Deep Dive",
            Description = "Query syntax, method syntax, deferred execution, custom operators.",
            DifficultyLevel = 7, DefaultFormat = ContentFormat.Interactive,
            MasteryThreshold = 0.7, EstimatedMinutes = 25,
            Tags = ["csharp", "linq"]
        };

        var asyncAwait = new KnowledgeNode
        {
            Title = "Async & Await",
            Description = "Task-based asynchronous pattern, async streams, cancellation.",
            DifficultyLevel = 8, DefaultFormat = ContentFormat.Visual,
            MasteryThreshold = 0.75, EstimatedMinutes = 30,
            Tags = ["csharp", "async"]
        };

        var patterns = new KnowledgeNode
        {
            Title = "Design Patterns in C#",
            Description = "Strategy, Observer, Factory, Dependency Injection patterns.",
            DifficultyLevel = 9, DefaultFormat = ContentFormat.Visual,
            MasteryThreshold = 0.8, EstimatedMinutes = 35,
            Tags = ["csharp", "patterns", "advanced"]
        };

        var capstone = new KnowledgeNode
        {
            Title = "Capstone: Build a REST API",
            Description = "Apply all concepts to build a full ASP.NET Core Web API.",
            DifficultyLevel = 10, DefaultFormat = ContentFormat.Interactive,
            MasteryThreshold = 0.8, EstimatedMinutes = 60,
            Tags = ["csharp", "project", "aspnet"]
        };

        // ── Add All Nodes ───────────────────────────────────

        var nodes = new[]
        {
            intro, variables, operators, controlFlow, methods,
            arrays, oopBasics, inheritance, exceptions, generics,
            delegates, linq, asyncAwait, patterns, capstone
        };

        foreach (var node in nodes)
            await repo.AddNodeAsync(node);

        // ── Define Prerequisite Edges ───────────────────────
        // Edge direction: Source IS prerequisite OF Target

        var edges = new (KnowledgeNode src, KnowledgeNode tgt)[]
        {
            (intro, variables),
            (intro, operators),
            (variables, controlFlow),
            (operators, controlFlow),
            (controlFlow, methods),
            (variables, arrays),
            (methods, oopBasics),
            (arrays, oopBasics),
            (oopBasics, inheritance),
            (methods, exceptions),
            (inheritance, generics),
            (arrays, generics),
            (inheritance, delegates),
            (generics, linq),
            (delegates, linq),
            (linq, asyncAwait),
            (asyncAwait, patterns),
            (inheritance, patterns),
            (patterns, capstone),
            (asyncAwait, capstone),
        };

        foreach (var (src, tgt) in edges)
        {
            await repo.AddEdgeAsync(new KnowledgeEdge
            {
                SourceNodeId = src.Id,
                TargetNodeId = tgt.Id,
                Relation = EdgeRelation.PrerequisiteOf,
                Weight = 1.0
            });
        }

        // ── Add some "RelatedTo" cross-links ────────────────

        await repo.AddEdgeAsync(new KnowledgeEdge
        {
            SourceNodeId = delegates.Id, TargetNodeId = asyncAwait.Id,
            Relation = EdgeRelation.RelatedTo, Weight = 0.6
        });
        await repo.AddEdgeAsync(new KnowledgeEdge
        {
            SourceNodeId = exceptions.Id, TargetNodeId = asyncAwait.Id,
            Relation = EdgeRelation.RelatedTo, Weight = 0.5
        });
    }
}
