using GitHubWorkflowManager.Models;
using GitHubWorkflowManager.Services.Interfaces;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitHubWorkflowManager.Services.Implementations;

public class WorkflowParsingService : IWorkflowParsingService
{
    private readonly ILogger<WorkflowParsingService> _logger;
    private readonly IDeserializer _deserializer;

    public WorkflowParsingService(ILogger<WorkflowParsingService> logger)
    {
        _logger = logger;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public WorkflowDefinition ParseWorkflowFile(string fileName, string content)
    {
        try
        {
            var workflow = new WorkflowDefinition
            {
                FileName = fileName,
                Content = content,
                LastModified = DateTime.UtcNow
            };

            if (!IsValidWorkflowFile(content))
            {
                workflow.Name = Path.GetFileNameWithoutExtension(fileName);
                workflow.Description = "Invalid workflow file";
                workflow.IsEnabled = false;
                return workflow;
            }

            var yamlObject = _deserializer.Deserialize<dynamic>(content);
            
            if (yamlObject is Dictionary<object, object> yamlDict)
            {
                // Extract name
                if (yamlDict.TryGetValue("name", out var nameObj))
                {
                    workflow.Name = nameObj?.ToString() ?? Path.GetFileNameWithoutExtension(fileName);
                }
                else
                {
                    workflow.Name = Path.GetFileNameWithoutExtension(fileName);
                }

                // Extract description
                if (yamlDict.TryGetValue("description", out var descObj))
                {
                    workflow.Description = descObj?.ToString() ?? "";
                }

                // Extract triggers
                workflow.On = ExtractTriggers(content);

                // Extract inputs if workflow_dispatch is present
                if (workflow.On.WorkflowDispatch)
                {
                    workflow.Inputs = ExtractInputs(content);
                }

                // Extract metadata from comments
                workflow.Metadata = ExtractMetadata(content);
            }

            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing workflow file {FileName}", fileName);
            return new WorkflowDefinition
            {
                FileName = fileName,
                Name = Path.GetFileNameWithoutExtension(fileName),
                Description = $"Error parsing workflow: {ex.Message}",
                Content = content,
                IsEnabled = false,
                LastModified = DateTime.UtcNow
            };
        }
    }

    public bool IsValidWorkflowFile(string content)
    {
        try
        {
            var yamlObject = _deserializer.Deserialize<dynamic>(content);
            
            if (yamlObject is Dictionary<object, object> yamlDict)
            {
                return yamlDict.ContainsKey("on") && yamlDict.ContainsKey("jobs");
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public Dictionary<string, WorkflowInput> ExtractInputs(string content)
    {
        var inputs = new Dictionary<string, WorkflowInput>();

        try
        {
            var yamlObject = _deserializer.Deserialize<dynamic>(content);
            
            if (yamlObject is Dictionary<object, object> yamlDict &&
                yamlDict.TryGetValue("on", out var onObj) &&
                onObj is Dictionary<object, object> onDict &&
                onDict.TryGetValue("workflow_dispatch", out var dispatchObj) &&
                dispatchObj is Dictionary<object, object> dispatchDict &&
                dispatchDict.TryGetValue("inputs", out var inputsObj) &&
                inputsObj is Dictionary<object, object> inputsDict)
            {
                foreach (var inputPair in inputsDict)
                {
                    var inputName = inputPair.Key.ToString()!;
                    var input = new WorkflowInput();

                    if (inputPair.Value is Dictionary<object, object> inputDef)
                    {
                        if (inputDef.TryGetValue("description", out var desc))
                            input.Description = desc?.ToString() ?? "";

                        if (inputDef.TryGetValue("type", out var type))
                            input.Type = type?.ToString() ?? "string";

                        if (inputDef.TryGetValue("required", out var required))
                            input.Required = Convert.ToBoolean(required);

                        if (inputDef.TryGetValue("default", out var defaultVal))
                            input.Default = defaultVal;

                        if (inputDef.TryGetValue("options", out var options) && options is List<object> optionsList)
                        {
                            input.Options = optionsList.Select(o => o?.ToString() ?? "").ToList();
                        }
                    }

                    inputs[inputName] = input;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting inputs from workflow");
        }

        return inputs;
    }

    public WorkflowTriggers ExtractTriggers(string content)
    {
        var triggers = new WorkflowTriggers();

        try
        {
            var yamlObject = _deserializer.Deserialize<dynamic>(content);
            
            if (yamlObject is Dictionary<object, object> yamlDict &&
                yamlDict.TryGetValue("on", out var onObj))
            {
                if (onObj is string singleTrigger)
                {
                    SetTriggerFlag(triggers, singleTrigger);
                }
                else if (onObj is List<object> triggerList)
                {
                    foreach (var trigger in triggerList)
                    {
                        if (trigger?.ToString() is string triggerName)
                        {
                            SetTriggerFlag(triggers, triggerName);
                        }
                    }
                }
                else if (onObj is Dictionary<object, object> triggerDict)
                {
                    foreach (var triggerPair in triggerDict)
                    {
                        if (triggerPair.Key?.ToString() is string triggerName)
                        {
                            SetTriggerFlag(triggers, triggerName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting triggers from workflow");
        }

        return triggers;
    }

    private static void SetTriggerFlag(WorkflowTriggers triggers, string triggerName)
    {
        switch (triggerName.ToLowerInvariant())
        {
            case "push":
                triggers.Push = true;
                break;
            case "pull_request":
                triggers.PullRequest = true;
                break;
            case "workflow_dispatch":
                triggers.WorkflowDispatch = true;
                break;
            case "schedule":
                triggers.Schedule = true;
                break;
            default:
                if (!triggers.Other.Contains(triggerName))
                    triggers.Other.Add(triggerName);
                break;
        }
    }

    private WorkflowMetadata? ExtractMetadata(string content)
    {
        try
        {
            var lines = content.Split('\n');
            var metadata = new WorkflowMetadata();
            bool inMetadataBlock = false;
            string currentSection = "";
            var currentItems = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Check for start of metadata block
                if (trimmedLine.Contains("============================================================================="))
                {
                    inMetadataBlock = !inMetadataBlock;
                    if (!inMetadataBlock)
                    {
                        // End of metadata block, process any remaining items
                        ProcessMetadataSection(metadata, currentSection, currentItems);
                        break;
                    }
                    continue;
                }

                if (!inMetadataBlock || !trimmedLine.StartsWith("#"))
                    continue;

                // Remove leading # and whitespace
                var content_line = trimmedLine.TrimStart('#').Trim();
                
                if (string.IsNullOrWhiteSpace(content_line))
                    continue;

                // Check for section headers
                if (content_line.StartsWith("WORKFLOW:"))
                {
                    // Extract workflow title (different from name)
                    continue;
                }
                else if (content_line.StartsWith("PURPOSE:"))
                {
                    ProcessMetadataSection(metadata, currentSection, currentItems);
                    currentSection = "PURPOSE";
                    currentItems.Clear();
                    var purpose = content_line.Substring("PURPOSE:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(purpose))
                        currentItems.Add(purpose);
                }
                else if (content_line.StartsWith("TRIGGER:"))
                {
                    ProcessMetadataSection(metadata, currentSection, currentItems);
                    currentSection = "TRIGGER";
                    currentItems.Clear();
                    var trigger = content_line.Substring("TRIGGER:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(trigger))
                        currentItems.Add(trigger);
                }
                else if (content_line.StartsWith("SCOPE:"))
                {
                    ProcessMetadataSection(metadata, currentSection, currentItems);
                    currentSection = "SCOPE";
                    currentItems.Clear();
                    var scope = content_line.Substring("SCOPE:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(scope))
                        currentItems.Add(scope);
                }
                else if (content_line.StartsWith("ACTIONS:"))
                {
                    ProcessMetadataSection(metadata, currentSection, currentItems);
                    currentSection = "ACTIONS";
                    currentItems.Clear();
                }
                else if (content_line.StartsWith("INPUTS:"))
                {
                    ProcessMetadataSection(metadata, currentSection, currentItems);
                    currentSection = "INPUTS";
                    currentItems.Clear();
                }
                else if (content_line.StartsWith("-"))
                {
                    // List item
                    var item = content_line.Substring(1).Trim();
                    if (!string.IsNullOrWhiteSpace(item))
                        currentItems.Add(item);
                }
                else if (!string.IsNullOrWhiteSpace(currentSection))
                {
                    // Continuation of current section
                    currentItems.Add(content_line);
                }
            }

            // Process final section
            ProcessMetadataSection(metadata, currentSection, currentItems);

            // Return null if no metadata was found
            if (string.IsNullOrWhiteSpace(metadata.Purpose) && 
                string.IsNullOrWhiteSpace(metadata.Trigger) && 
                string.IsNullOrWhiteSpace(metadata.Scope) && 
                metadata.Actions.Count == 0 && 
                metadata.Inputs.Count == 0)
            {
                return null;
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting metadata from workflow");
            return null;
        }
    }

    private static void ProcessMetadataSection(WorkflowMetadata metadata, string section, List<string> items)
    {
        if (string.IsNullOrWhiteSpace(section) || items.Count == 0)
            return;

        switch (section.ToUpperInvariant())
        {
            case "PURPOSE":
                metadata.Purpose = string.Join(" ", items);
                break;
            case "TRIGGER":
                metadata.Trigger = string.Join(" ", items);
                break;
            case "SCOPE":
                metadata.Scope = string.Join(" ", items);
                break;
            case "ACTIONS":
                metadata.Actions.AddRange(items);
                break;
            case "INPUTS":
                metadata.Inputs.AddRange(items);
                break;
        }
    }
}
