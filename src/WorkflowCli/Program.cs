using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotnetRelease.Graph;

namespace WorkflowCli;

class Program
{
    private const string BaseUrl = "https://raw.githubusercontent.com/dotnet/core/release-index/release-notes";
    private const string LlmsUrl = $"{BaseUrl}/llms.json";

    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0];
        var workflowFile = args[1];

        if (!File.Exists(workflowFile))
        {
            Console.Error.WriteLine($"Error: File not found: {workflowFile}");
            return 1;
        }

        var workflows = await LoadWorkflowsAsync(workflowFile);
        if (workflows == null || workflows.Count == 0)
        {
            Console.Error.WriteLine("Error: No workflows found in file");
            return 1;
        }

        return command switch
        {
            "list" => ListWorkflows(workflows),
            "show" when args.Length >= 3 => ShowWorkflow(workflows, args[2]),
            "script" when args.Length >= 3 => GenerateScript(workflows, args[2], args.Skip(3).ToArray()),
            _ => PrintUsage()
        };
    }

    static int PrintUsage()
    {
        Console.WriteLine("""
            Usage: workflow-cli <command> <workflow-file> [options]

            Commands:
              list                         List all workflows with descriptions
              show <name>                  Show workflow details
              script <name> [--key=value]  Generate bash script

            Examples:
              workflow-cli list skills/_workflows.json
              workflow-cli show skills/_workflows.json latest-cve-disclosures
              workflow-cli script skills/_workflows.json cves-by-month --year=2024 --month=10
            """);
        return 1;
    }

    static async Task<Dictionary<string, SourceWorkflow>?> LoadWorkflowsAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var file = JsonSerializer.Deserialize<SourceWorkflowsFile>(json, LlmsIndexSerializerContext.Default.SourceWorkflowsFile);
        return file?.Embedded?.Workflows;
    }

    static int ListWorkflows(Dictionary<string, SourceWorkflow> workflows)
    {
        var maxNameLen = workflows.Keys.Max(k => k.Length);

        foreach (var (name, workflow) in workflows.OrderBy(kv => kv.Key))
        {
            var templated = workflow.Templated == true ? " [templated]" : "";
            var description = workflow.Description ?? "(no description)";
            Console.WriteLine($"  {name.PadRight(maxNameLen)}  {description}{templated}");
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {workflows.Count} workflows");
        return 0;
    }

    static int ShowWorkflow(Dictionary<string, SourceWorkflow> workflows, string name)
    {
        if (!workflows.TryGetValue(name, out var workflow))
        {
            Console.Error.WriteLine($"Error: Workflow not found: {name}");
            Console.Error.WriteLine("Available workflows:");
            foreach (var key in workflows.Keys.OrderBy(k => k))
            {
                Console.Error.WriteLine($"  {key}");
            }
            return 1;
        }

        Console.WriteLine($"Workflow: {name}");
        Console.WriteLine($"Description: {workflow.Description ?? "(none)"}");

        if (workflow.FollowPath != null)
        {
            Console.WriteLine($"Follow path: {string.Join(" -> ", workflow.FollowPath)}");
        }

        if (workflow.DestinationKind != null)
        {
            Console.WriteLine($"Destination: {workflow.DestinationKind}");
        }

        if (workflow.SelectEmbedded != null)
        {
            Console.WriteLine($"Select embedded: {string.Join(", ", workflow.SelectEmbedded)}");
        }

        if (workflow.SelectProperty != null)
        {
            Console.WriteLine($"Select properties: {string.Join(", ", workflow.SelectProperty)}");
        }

        if (workflow.Yields != null)
        {
            Console.WriteLine($"Yields: {workflow.Yields}");
        }

        if (workflow.Templated == true)
        {
            Console.WriteLine("Templated: yes");
            var templates = ExtractTemplateVariables(workflow);
            if (templates.Count > 0)
            {
                Console.WriteLine($"Variables: {string.Join(", ", templates)}");
            }
        }

        if (workflow.QueryHints?.Count > 0)
        {
            Console.WriteLine("Query hints:");
            foreach (var hint in workflow.QueryHints)
            {
                Console.WriteLine($"  - {hint}");
            }
        }

        return 0;
    }

    static int GenerateScript(Dictionary<string, SourceWorkflow> workflows, string name, string[] args)
    {
        if (!workflows.TryGetValue(name, out var workflow))
        {
            Console.Error.WriteLine($"Error: Workflow not found: {name}");
            return 1;
        }

        if (workflow.FollowPath == null || workflow.FollowPath.Count == 0)
        {
            Console.Error.WriteLine($"Error: Workflow '{name}' has no follow_path");
            return 1;
        }

        // Parse --key=value arguments
        var parameters = new Dictionary<string, string>();
        foreach (var arg in args)
        {
            if (arg.StartsWith("--") && arg.Contains('='))
            {
                var parts = arg[2..].Split('=', 2);
                parameters[parts[0]] = parts[1];
            }
        }

        // Check for missing template variables
        var requiredVars = ExtractTemplateVariables(workflow);
        var missingVars = requiredVars.Where(v => !parameters.ContainsKey(v)).ToList();
        if (missingVars.Count > 0)
        {
            Console.Error.WriteLine($"Error: Missing required parameters: {string.Join(", ", missingVars.Select(v => $"--{v}=<value>"))}");
            return 1;
        }

        var script = GenerateBashScript(name, workflow, parameters);
        Console.WriteLine(script);
        return 0;
    }

    static HashSet<string> ExtractTemplateVariables(SourceWorkflow workflow)
    {
        var variables = new HashSet<string>();

        if (workflow.FollowPath != null)
        {
            foreach (var step in workflow.FollowPath)
            {
                ExtractVariablesFromString(step, variables);
            }
        }

        return variables;
    }

    static void ExtractVariablesFromString(string s, HashSet<string> variables)
    {
        var start = 0;
        while ((start = s.IndexOf('{', start)) >= 0)
        {
            var end = s.IndexOf('}', start);
            if (end > start)
            {
                var varName = s[(start + 1)..end];
                variables.Add(varName);
                start = end + 1;
            }
            else
            {
                break;
            }
        }
    }

    static string GenerateBashScript(string name, SourceWorkflow workflow, Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#!/bin/bash");
        sb.AppendLine($"# Workflow: {name}");
        sb.AppendLine($"# Description: {workflow.Description ?? "(none)"}");
        sb.AppendLine($"# Generated by workflow-cli");
        sb.AppendLine();
        sb.AppendLine("set -euo pipefail");
        sb.AppendLine();

        // Add parameter variables if any
        if (parameters.Count > 0)
        {
            sb.AppendLine("# Parameters");
            foreach (var (key, value) in parameters)
            {
                sb.AppendLine($"{key.ToUpperInvariant()}=\"{value}\"");
            }
            sb.AppendLine();
        }

        var stepNumber = 1;

        foreach (var step in workflow.FollowPath!)
        {
            var expandedStep = ExpandTemplates(step, parameters);

            if (step.StartsWith("kind:"))
            {
                // Entry point
                var kind = step[5..];
                if (kind == "llms")
                {
                    sb.AppendLine($"# Step {stepNumber}: Start at llms.json");
                    sb.AppendLine($"URL=\"{LlmsUrl}\"");
                    sb.AppendLine($"echo \"Fetching: $URL\" >&2");
                    sb.AppendLine($"DOC=$(curl -sf \"$URL\")");
                }
                else
                {
                    sb.AppendLine($"# Step {stepNumber}: Start at {kind} (unsupported entry point)");
                    sb.AppendLine($"echo \"Error: Unsupported entry point: {kind}\" >&2");
                    sb.AppendLine("exit 1");
                }
            }
            else if (step.Contains('.') && step.StartsWith("patches."))
            {
                // Dictionary access: patches.{version}
                var keyPart = step.Split('.', 2)[1];
                var expandedKey = ExpandTemplates(keyPart, parameters);

                sb.AppendLine($"# Step {stepNumber}: Access patches[\"{expandedKey}\"]");
                sb.AppendLine($"DOC=$(echo \"$DOC\" | jq '._embedded.patches[\"{expandedKey}\"]')");
                sb.AppendLine($"if [ \"$DOC\" = \"null\" ]; then");
                sb.AppendLine($"  echo \"Error: No patch found for version {expandedKey}\" >&2");
                sb.AppendLine($"  exit 1");
                sb.AppendLine($"fi");
            }
            else
            {
                // Link relation
                sb.AppendLine($"# Step {stepNumber}: Follow link \"{expandedStep}\"");
                sb.AppendLine($"URL=$(echo \"$DOC\" | jq -r '._links[\"{expandedStep}\"].href // empty')");
                sb.AppendLine($"if [ -z \"$URL\" ]; then");
                sb.AppendLine($"  echo \"Error: Link '{expandedStep}' not found\" >&2");
                sb.AppendLine($"  exit 1");
                sb.AppendLine($"fi");
                sb.AppendLine($"echo \"Fetching: $URL\" >&2");
                sb.AppendLine($"DOC=$(curl -sf \"$URL\")");
            }

            sb.AppendLine();
            stepNumber++;
        }

        // Apply selection
        if (workflow.SelectEmbedded?.Count > 0)
        {
            var selections = string.Join(", ", workflow.SelectEmbedded.Select(s => $"._embedded.{s}"));
            if (workflow.SelectEmbedded.Count == 1)
            {
                sb.AppendLine($"# Extract: _embedded.{workflow.SelectEmbedded[0]}");
                sb.AppendLine($"echo \"$DOC\" | jq '._embedded.{workflow.SelectEmbedded[0]}'");
            }
            else
            {
                sb.AppendLine($"# Extract: {selections}");
                var jqExpr = "{" + string.Join(", ", workflow.SelectEmbedded.Select(s => $"\"{s}\": ._embedded.{s}")) + "}";
                sb.AppendLine($"echo \"$DOC\" | jq '{jqExpr}'");
            }
        }
        else if (workflow.SelectProperty?.Count > 0)
        {
            if (workflow.SelectProperty.Count == 1)
            {
                sb.AppendLine($"# Extract: .{workflow.SelectProperty[0]}");
                sb.AppendLine($"echo \"$DOC\" | jq '.{workflow.SelectProperty[0]}'");
            }
            else
            {
                var jqExpr = "{" + string.Join(", ", workflow.SelectProperty.Select(s => $"\"{s}\": .{s}")) + "}";
                sb.AppendLine($"# Extract properties: {string.Join(", ", workflow.SelectProperty)}");
                sb.AppendLine($"echo \"$DOC\" | jq '{jqExpr}'");
            }
        }
        else
        {
            sb.AppendLine("# Output full document");
            sb.AppendLine("echo \"$DOC\" | jq '.'");
        }

        return sb.ToString();
    }

    static string ExpandTemplates(string s, Dictionary<string, string> parameters)
    {
        foreach (var (key, value) in parameters)
        {
            s = s.Replace($"{{{key}}}", value);
        }
        // For bash, convert remaining {var} to $VAR
        foreach (var match in Regex.Matches(s, @"\{(\w+)\}").Cast<Match>())
        {
            var varName = match.Groups[1].Value;
            s = s.Replace(match.Value, $"${{{varName.ToUpperInvariant()}}}");
        }
        return s;
    }
}
