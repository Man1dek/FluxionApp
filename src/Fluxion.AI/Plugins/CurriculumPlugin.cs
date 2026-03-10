using System.ComponentModel;
using Fluxion.Core.Models;
using Microsoft.SemanticKernel;

namespace Fluxion.AI.Plugins;

/// <summary>
/// Semantic Kernel native plugin exposing curriculum-intelligence functions
/// that are callable by the AI orchestrator.
/// </summary>
public class CurriculumPlugin
{
    /// <summary>
    /// Evaluates a student's response to a learning module and produces
    /// a mastery score and detailed feedback.
    /// </summary>
    [KernelFunction("evaluate_student_response")]
    [Description("Evaluates a student's answer to a learning exercise and returns a mastery score (0.0-1.0) with detailed feedback.")]
    public async Task<string> EvaluateStudentResponseAsync(
        Kernel kernel,
        [Description("The title of the knowledge node being assessed")] string nodeTitle,
        [Description("The difficulty level of the current module (1-10)")] int difficulty,
        [Description("The student's submitted response or answer")] string studentResponse,
        [Description("The expected correct answer or key concepts")] string expectedAnswer)
    {
        var prompt = $$"""
            You are an expert educator assessing a student's understanding.

            ## Module: {{nodeTitle}}
            ## Difficulty Level: {{difficulty}}/10

            ## Expected Key Concepts:
            {{expectedAnswer}}

            ## Student's Response:
            {{studentResponse}}

            ## Your Task:
            1. Evaluate how well the student demonstrates understanding
            2. Assign a mastery score between 0.0 (no understanding) and 1.0 (complete mastery)
            3. Identify specific knowledge gaps
            4. Provide actionable feedback

            Respond in this exact JSON format:
            {
                "masteryScore": <float>,
                "knowledgeGaps": ["<gap1>", "<gap2>"],
                "feedback": "<constructive feedback>",
                "suggestedReview": ["<topic to review>"]
            }
            """;

        var result = await kernel.InvokePromptAsync(prompt);
        return result.GetValue<string>() ?? "{}";
    }

    /// <summary>
    /// Recommends the optimal next nodes for a learner based on their
    /// mastery state and the knowledge graph topology.
    /// </summary>
    [KernelFunction("recommend_next_nodes")]
    [Description("Recommends the best next learning modules based on a learner's current mastery and available candidates.")]
    public async Task<string> RecommendNextNodesAsync(
        Kernel kernel,
        [Description("JSON array of available candidate node titles with their difficulty levels")] string candidateNodesJson,
        [Description("JSON object mapping node titles to current mastery scores")] string currentMasteryJson,
        [Description("The learner's current cognitive load index (0.0-1.0)")] double cognitiveLoad)
    {
        var prompt = $$"""
            You are an AI curriculum advisor optimizing a personalized learning path.

            ## Available Next Modules:
            {{candidateNodesJson}}

            ## Learner's Current Mastery:
            {{currentMasteryJson}}

            ## Cognitive Load Index: {{cognitiveLoad:F2}} (0=relaxed, 1=overloaded)

            ## Instructions:
            - If cognitive load is HIGH (>0.7): recommend easier or review modules
            - If cognitive load is LOW (<0.3): recommend challenging modules to prevent boredom
            - If cognitive load is MODERATE: recommend the next natural progression
            - Consider mastery scores to avoid gaps
            - Recommend exactly 3 modules in priority order

            Respond in this exact JSON format:
            {
                "recommendations": [
                    {
                        "nodeTitle": "<title>",
                        "reason": "<why this module next>",
                        "suggestedDifficultyAdjustment": <int offset, e.g. -1, 0, +1>
                    }
                ]
            }
            """;

        var result = await kernel.InvokePromptAsync(prompt);
        return result.GetValue<string>() ?? "{}";
    }

    /// <summary>
    /// Generates learning content for a node at a specific difficulty
    /// and format, adapted to the learner's state.
    /// </summary>
    [KernelFunction("generate_module_content")]
    [Description("Generates personalized learning content for a specific knowledge node at the given difficulty and format.")]
    public async Task<string> GenerateModuleContentAsync(
        Kernel kernel,
        [Description("The title of the knowledge node")] string nodeTitle,
        [Description("Detailed description of the concept to teach")] string nodeDescription,
        [Description("Adjusted difficulty level (1-10)")] int difficulty,
        [Description("Content format: Text, Visual, Interactive, Video, Mixed")] string contentFormat,
        [Description("Known knowledge gaps to address")] string knowledgeGaps)
    {
        var formatInstructions = contentFormat switch
        {
            "Visual" => "Use diagrams described in ASCII art, flowcharts, comparison tables, and visual metaphors. Minimize dense text blocks.",
            "Interactive" => "Structure as a series of progressive exercises with hints. Include fill-in-the-blank code, fix-the-bug challenges, and mini-projects.",
            "Video" => "Write a detailed script outline with scene descriptions, key talking points, and visual cues for video production.",
            "Mixed" => "Blend explanatory text with visual diagrams and hands-on exercises in a balanced approach.",
            _ => "Present as clear, well-structured text with code examples, explanations, and key takeaways."
        };

        var prompt = $$"""
            You are an expert C# instructor creating a personalized learning module.

            ## Topic: {{nodeTitle}}
            ## Description: {{nodeDescription}}
            ## Difficulty: {{difficulty}}/10
            ## Format Style: {{contentFormat}}

            ## Format Instructions:
            {{formatInstructions}}

            ## Known Knowledge Gaps to Address:
            {{knowledgeGaps}}

            ## Requirements:
            1. Content MUST be at difficulty level {{difficulty}}/10
            2. Include a clear learning objective at the top
            3. Provide at least 2 code examples (if applicable)
            4. End with 3 practice questions of increasing difficulty
            5. Use markdown formatting throughout
            6. Keep explanations concise but thorough

            Generate the complete module content:
            """;

        var result = await kernel.InvokePromptAsync(prompt);
        return result.GetValue<string>() ?? "No content generated.";
    }
}
