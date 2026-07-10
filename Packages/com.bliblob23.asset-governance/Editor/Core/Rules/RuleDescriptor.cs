using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 用于标识和描述资源规则的不可变元数据。
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
        /// 获取用于配置和报告的稳定且全局唯一的规则标识符。
        /// </summary>
        public string Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public RuleSeverity DefaultSeverity { get; }

        /// <summary>
        /// 获取规则声明支持的资源类型，供规则发现和界面展示使用。
        /// 空集合表示规则未在此声明静态类型限制。
        /// 运行时是否适用仍以 <see cref="IAssetRule.CanEvaluate"/> 的返回结果为准。
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
