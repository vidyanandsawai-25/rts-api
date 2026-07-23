using System.Text.Json;
using System.Text.Json.Serialization;

namespace NtisPlatform.Application.DTOs.Rules.RuleEngine
{
    public class CreateRuleEngineDtoJsonConverter : JsonConverter<CreateRuleEngineDto>
    {
        public override CreateRuleEngineDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            var dto = new CreateRuleEngineDto();

            string? GetStringProp(JsonElement elem, string name)
            {
                if (elem.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                        return prop.GetString();
                    if (prop.ValueKind == JsonValueKind.Null)
                        return null;
                    return prop.GetRawText();
                }
                return null;
            }

            // Map RuleCode / id
            dto.RuleCode = GetStringProp(root, "ruleCode") ?? GetStringProp(root, "RuleCode") ?? GetStringProp(root, "id") ?? GetStringProp(root, "Id");

            // Map RuleName
            dto.RuleName = GetStringProp(root, "ruleName") ?? GetStringProp(root, "RuleName") ?? GetStringProp(root, "description") ?? GetStringProp(root, "Description") ?? string.Empty;

            // Map Description
            dto.Description = GetStringProp(root, "description") ?? GetStringProp(root, "Description");

            // Map RuleCategory
            dto.RuleCategory = GetStringProp(root, "ruleCategory") ?? GetStringProp(root, "RuleCategory");

            // Map RuleJson
            dto.RuleJson = GetStringProp(root, "ruleJson") ?? GetStringProp(root, "RuleJson");

            // Map ConditionsJson / conditions
            if (root.TryGetProperty("conditions", out var condProp))
            {
                dto.ConditionsJson = condProp.ValueKind == JsonValueKind.String
                    ? condProp.GetString()
                    : condProp.GetRawText();
            }
            else if (root.TryGetProperty("conditionsJson", out var condJsonProp))
            {
                dto.ConditionsJson = condJsonProp.ValueKind == JsonValueKind.String
                    ? condJsonProp.GetString()
                    : condJsonProp.GetRawText();
            }
            else if (root.TryGetProperty("ConditionsJson", out var condJsonPropCaps))
            {
                dto.ConditionsJson = condJsonPropCaps.ValueKind == JsonValueKind.String
                    ? condJsonPropCaps.GetString()
                    : condJsonPropCaps.GetRawText();
            }

            // Map EffectJson / effect / effects
            if (root.TryGetProperty("effects", out var effsProp))
            {
                dto.EffectJson = effsProp.ValueKind == JsonValueKind.String
                    ? effsProp.GetString()
                    : effsProp.GetRawText();
            }
            else if (root.TryGetProperty("effect", out var effProp))
            {
                dto.EffectJson = effProp.ValueKind == JsonValueKind.String
                    ? effProp.GetString()
                    : effProp.GetRawText();
            }
            else if (root.TryGetProperty("effectJson", out var effJsonProp))
            {
                dto.EffectJson = effJsonProp.ValueKind == JsonValueKind.String
                    ? effJsonProp.GetString()
                    : effJsonProp.GetRawText();
            }
            else if (root.TryGetProperty("EffectJson", out var effJsonPropCaps))
            {
                dto.EffectJson = effJsonPropCaps.ValueKind == JsonValueKind.String
                    ? effJsonPropCaps.GetString()
                    : effJsonPropCaps.GetRawText();
            }

            // Map TargetFiltersJson / targetFilters
            if (root.TryGetProperty("targetFilters", out var tfProp))
            {
                dto.TargetFiltersJson = tfProp.ValueKind == JsonValueKind.String
                    ? tfProp.GetString()
                    : tfProp.GetRawText();
            }
            else if (root.TryGetProperty("targetFiltersJson", out var tfJsonProp))
            {
                dto.TargetFiltersJson = tfJsonProp.ValueKind == JsonValueKind.String
                    ? tfJsonProp.GetString()
                    : tfJsonProp.GetRawText();
            }
            else if (root.TryGetProperty("TargetFiltersJson", out var tfJsonPropCaps))
            {
                dto.TargetFiltersJson = tfJsonPropCaps.ValueKind == JsonValueKind.String
                    ? tfJsonPropCaps.GetString()
                    : tfJsonPropCaps.GetRawText();
            }

            // Map Priority
            if (root.TryGetProperty("priority", out var prioProp) || root.TryGetProperty("Priority", out prioProp))
            {
                if (prioProp.ValueKind == JsonValueKind.Number && prioProp.TryGetInt32(out var pVal))
                    dto.Priority = pVal;
            }

            // Map IsEnabled
            if (root.TryGetProperty("isEnabled", out var isEnabledProp) || root.TryGetProperty("IsEnabled", out isEnabledProp))
            {
                if (isEnabledProp.ValueKind == JsonValueKind.True) dto.IsEnabled = true;
                else if (isEnabledProp.ValueKind == JsonValueKind.False) dto.IsEnabled = false;
            }

            // Map StopProcessing
            if (root.TryGetProperty("stopProcessing", out var stopProp) || root.TryGetProperty("StopProcessing", out stopProp))
            {
                if (stopProp.ValueKind == JsonValueKind.True) dto.StopProcessing = true;
                else if (stopProp.ValueKind == JsonValueKind.False) dto.StopProcessing = false;
            }

            // Map RuleScopeId
            if (root.TryGetProperty("ruleScopeId", out var scopeProp) || root.TryGetProperty("RuleScopeId", out scopeProp))
            {
                if (scopeProp.ValueKind == JsonValueKind.Number && scopeProp.TryGetInt32(out var scopeVal))
                    dto.RuleScopeId = scopeVal;
                else if (scopeProp.ValueKind == JsonValueKind.Null)
                    dto.RuleScopeId = null;
            }



            // Map ChangeReason
            dto.ChangeReason = GetStringProp(root, "changeReason") ?? GetStringProp(root, "ChangeReason");

            // Map IsActive (from CreateBaseDtos)
            if (root.TryGetProperty("isActive", out var actProp) || root.TryGetProperty("IsActive", out actProp))
            {
                if (actProp.ValueKind == JsonValueKind.True) dto.IsActive = true;
                else if (actProp.ValueKind == JsonValueKind.False) dto.IsActive = false;
            }

            // Map CreatedBy (from CreateBaseDtos)
            if (root.TryGetProperty("createdBy", out var cbProp) || root.TryGetProperty("CreatedBy", out cbProp))
            {
                if (cbProp.ValueKind == JsonValueKind.Number && cbProp.TryGetInt32(out var cbVal))
                    dto.CreatedBy = cbVal;
            }

            return dto;
        }

        public override void Write(Utf8JsonWriter writer, CreateRuleEngineDto value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("ruleCode", value.RuleCode);
            writer.WriteString("ruleName", value.RuleName);
            writer.WriteString("description", value.Description);
            writer.WriteString("ruleCategory", value.RuleCategory);
            writer.WriteString("ruleJson", value.RuleJson);
            writer.WriteString("conditionsJson", value.ConditionsJson);
            writer.WriteString("effectJson", value.EffectJson);
            writer.WriteString("targetFiltersJson", value.TargetFiltersJson);
            writer.WriteNumber("priority", value.Priority);
            writer.WriteBoolean("isEnabled", value.IsEnabled);
            writer.WriteBoolean("stopProcessing", value.StopProcessing);
            if (value.RuleScopeId.HasValue)
                writer.WriteNumber("ruleScopeId", value.RuleScopeId.Value);
            else
                writer.WriteNull("ruleScopeId");
            writer.WriteBoolean("isActive", value.IsActive);
            if (value.CreatedBy.HasValue)
                writer.WriteNumber("createdBy", value.CreatedBy.Value);
            else
                writer.WriteNull("createdBy");


            writer.WriteString("changeReason", value.ChangeReason);
            writer.WriteEndObject();
        }
    }
}
