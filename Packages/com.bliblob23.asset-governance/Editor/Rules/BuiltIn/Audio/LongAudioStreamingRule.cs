using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 检查项目配置所识别的长音频和 BGM 是否使用 Streaming。
    /// </summary>
    public sealed class LongAudioStreamingRule : IFixableAssetRule
    {
        private static readonly RuleDescriptor DescriptorValue = new RuleDescriptor(
            "UAG-AUDIO-002",
            "Long Audio Must Use Streaming",
            "Configured long audio clips must use the Streaming Load Type.",
            RuleSeverity.Warning,
            new[] { typeof(AudioClip) });

        public RuleDescriptor Descriptor => DescriptorValue;

        public bool CanEvaluate(AssetContext context)
        {
            return context.Importer is AudioImporter &&
                   TryGetSettings(context, out var settings) &&
                   settings.MatchesLongAudioPath(context.AssetPath);
        }

        public IEnumerable<ValidationIssue> Evaluate(AssetContext context)
        {
            var audioImporter = (AudioImporter)context.Importer;
            if (audioImporter.defaultSampleSettings.loadType == AudioClipLoadType.Streaming)
            {
                return Array.Empty<ValidationIssue>();
            }

            return new[]
            {
                new ValidationIssue(
                    DescriptorValue.Id,
                    DescriptorValue.DefaultSeverity,
                    context.AssetPath,
                    "Long audio clips must use the Streaming Load Type.")
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
                   context.Importer is AudioImporter audioImporter &&
                   audioImporter.defaultSampleSettings.loadType != AudioClipLoadType.Streaming &&
                   TryGetSettings(context, out var settings) &&
                   settings.MatchesLongAudioPath(context.AssetPath);
        }

        public void Fix(AssetContext context, ValidationIssue issue)
        {
            if (!CanFix(context, issue))
            {
                throw new InvalidOperationException(
                    "The long audio Streaming issue cannot be fixed in its current state.");
            }

            var audioImporter = (AudioImporter)context.Importer;
            var sampleSettings = audioImporter.defaultSampleSettings;
            sampleSettings.loadType = AudioClipLoadType.Streaming;
            audioImporter.defaultSampleSettings = sampleSettings;
            audioImporter.SaveAndReimport();
        }

        private static bool TryGetSettings(
            AssetContext context,
            out LongAudioStreamingRuleSettings settings)
        {
            settings = null;
            return context.GovernanceProfile != null &&
                   context.GovernanceProfile.TryGetRuleSettings(
                       DescriptorValue.Id,
                       out settings);
        }
    }
}
