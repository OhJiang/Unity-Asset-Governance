using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查模型导入器的 Scale Factor 是否符合项目配置值。
    /// </summary>
    public sealed class ModelScaleFactorRule : IAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-MODEL-001",
            "Model Scale Factor Must Match Configuration",
            "Model importer Scale Factor must match the configured value.",
            RuleSeverity.Warning,
            new[] { typeof(GameObject) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            if (!(context.Importer is ModelImporter))
            {
                return false;
            }

            GetExpectedScaleFactor(context);
            return true;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var modelImporter = (ModelImporter)context.Importer;
            var expectedScaleFactor = GetExpectedScaleFactor(context);
            if (Mathf.Approximately(modelImporter.globalScale, expectedScaleFactor))
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    $"Model Scale Factor is {modelImporter.globalScale}, " +
                    $"but the configured value is {expectedScaleFactor}.")
            };
        }

        private static float GetExpectedScaleFactor(AssetContext context)
        {
            if (context.GovernanceProfile == null ||
                !context.GovernanceProfile.TryGetRuleSettings(
                    DescriptorValue.Id,
                    out ModelScaleFactorRuleSettings settings))
            {
                return ModelScaleFactorRuleSettings.DefaultExpectedScaleFactor;
            }

            return settings.GetExpectedScaleFactor(context.AssetPath);
        }
    }
}
