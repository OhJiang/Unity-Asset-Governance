using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查模型是否关闭了 Read/Write，例外资源由项目级按规则白名单管理。
    /// </summary>
    public sealed class ModelReadWriteDisabledRule : IFixableAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-MODEL-002",
            "Model Read/Write Must Be Disabled",
            "Models must have Read/Write disabled unless explicitly whitelisted.",
            RuleSeverity.Warning,
            new[] { typeof(GameObject) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            return context.Importer is ModelImporter;
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var modelImporter = (ModelImporter)context.Importer;
            if (!modelImporter.isReadable)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "Model Read/Write must be disabled unless the asset is explicitly whitelisted.")
            };
        }

        public bool CanFix(AssetContext context, ValidationIssue issue)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            return string.Equals(issue.RuleId, DescriptorValue.Id, StringComparison.Ordinal) &&
                   string.Equals(issue.AssetPath, context.AssetPath, StringComparison.Ordinal) &&
                   context.Importer is ModelImporter modelImporter &&
                   modelImporter.isReadable;
        }

        public void Fix(AssetContext context, ValidationIssue issue)
        {
            if (!CanFix(context, issue))
            {
                throw new InvalidOperationException(
                    "The model Read/Write issue cannot be fixed in its current state.");
            }

            var modelImporter = (ModelImporter)context.Importer;
            modelImporter.isReadable = false;
            modelImporter.SaveAndReimport();
        }
    }
}
