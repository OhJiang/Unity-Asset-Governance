using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityAssetGovernance
{
    /// <summary>
    /// Immutable metadata that identifies and describes an asset rule.
    /// </summary>
    public sealed class RuleDescriptor
    {
        private readonly ReadOnlyCollection<Type> _applicableAssetTypes;

        public RuleDescriptor(
            string id,
            string displayName,
            string description,
            RuleSeverity defaultSeverity,
            IEnumerable<Type> applicableAssetTypes = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A rule ID is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A rule display name is required.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(RuleSeverity), defaultSeverity))
            {
                throw new ArgumentOutOfRangeException(nameof(defaultSeverity));
            }

            Id = id;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            DefaultSeverity = defaultSeverity;
            _applicableAssetTypes = CreateAssetTypeSnapshot(applicableAssetTypes);
        }

        /// <summary>
        /// Gets the stable, globally unique identifier used by configuration and reports.
        /// </summary>
        public string Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public RuleSeverity DefaultSeverity { get; }

        /// <summary>
        /// Gets asset types advertised by this rule for discovery and UI purposes.
        /// An empty collection means the rule does not declare a type restriction here.
        /// <see cref="IAssetRule.CanEvaluate"/> remains the authoritative runtime check.
        /// </summary>
        public IReadOnlyList<Type> ApplicableAssetTypes => _applicableAssetTypes;

        private static ReadOnlyCollection<Type> CreateAssetTypeSnapshot(
            IEnumerable<Type> applicableAssetTypes)
        {
            var types = new List<Type>();

            if (applicableAssetTypes != null)
            {
                foreach (var assetType in applicableAssetTypes)
                {
                    if (assetType == null)
                    {
                        throw new ArgumentException(
                            "Applicable asset types cannot contain null.",
                            nameof(applicableAssetTypes));
                    }

                    types.Add(assetType);
                }
            }

            return new ReadOnlyCollection<Type>(types);
        }
    }
}
