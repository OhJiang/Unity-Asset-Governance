using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEditor;

namespace UnityAssetGovernance
{
    /// <summary>
    /// 负责发现、验证和实例化项目中的资源规则。
    /// </summary>
    public static class RuleRegistry
    {
        /// <summary>
        /// 使用 Unity 类型缓存发现所有有效的资源规则。
        /// </summary>
        public static IReadOnlyList<IAssetRule> DiscoverRules()
        {
            return DiscoverRules(TypeCache.GetTypesDerivedFrom<IAssetRule>());
        }

        internal static IReadOnlyList<IAssetRule> DiscoverRules(IEnumerable<Type> ruleTypes)
        {
            if (ruleTypes == null)
            {
                throw new ArgumentNullException(nameof(ruleTypes));
            }

            var discoveredRules = new List<DiscoveredRule>();
            var ruleTypesById = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (var ruleType in ruleTypes)
            {
                if (ruleType == null)
                {
                    throw new ArgumentException(
                        "Rule type collections cannot contain null.",
                        nameof(ruleTypes));
                }

                if (ruleType.IsInterface || ruleType.IsAbstract || ruleType.ContainsGenericParameters)
                {
                    continue;
                }

                // 规则是面向第三方程序集的公开扩展点。忽略非公开辅助类型，
                // 避免测试夹具或程序集内部实现被 Unity TypeCache 当成可注册规则。
                if (!ruleType.IsPublic && !ruleType.IsNestedPublic)
                {
                    continue;
                }

                if (!typeof(IAssetRule).IsAssignableFrom(ruleType))
                {
                    throw new InvalidOperationException(
                        $"Rule type '{ruleType.FullName}' does not implement {nameof(IAssetRule)}.");
                }

                var constructor = ruleType.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    throw new InvalidOperationException(
                        $"Rule type '{ruleType.FullName}' must declare a public parameterless constructor.");
                }

                var rule = CreateRule(ruleType, constructor);
                var descriptor = GetDescriptor(ruleType, rule);

                if (descriptor == null)
                {
                    throw new InvalidOperationException(
                        $"Rule type '{ruleType.FullName}' returned a null descriptor.");
                }

                if (string.IsNullOrWhiteSpace(descriptor.Id))
                {
                    throw new InvalidOperationException(
                        $"Rule type '{ruleType.FullName}' returned an empty rule ID.");
                }

                if (ruleTypesById.TryGetValue(descriptor.Id, out var existingRuleType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate rule ID '{descriptor.Id}' was declared by " +
                        $"'{existingRuleType.FullName}' and '{ruleType.FullName}'.");
                }

                ruleTypesById.Add(descriptor.Id, ruleType);
                discoveredRules.Add(new DiscoveredRule(rule, descriptor));
            }

            discoveredRules.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.Descriptor.Id, right.Descriptor.Id));

            var rules = new List<IAssetRule>(discoveredRules.Count);
            foreach (var discoveredRule in discoveredRules)
            {
                rules.Add(discoveredRule.Rule);
            }

            return new ReadOnlyCollection<IAssetRule>(rules);
        }

        private static IAssetRule CreateRule(Type ruleType, ConstructorInfo constructor)
        {
            try
            {
                return (IAssetRule)constructor.Invoke(null);
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException(
                    $"Failed to create rule type '{ruleType.FullName}'.",
                    exception.InnerException ?? exception);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Failed to create rule type '{ruleType.FullName}'.",
                    exception);
            }
        }

        private static RuleDescriptor GetDescriptor(Type ruleType, IAssetRule rule)
        {
            try
            {
                return rule.Descriptor;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Failed to read the descriptor from rule type '{ruleType.FullName}'.",
                    exception);
            }
        }

        private sealed class DiscoveredRule
        {
            public DiscoveredRule(IAssetRule rule, RuleDescriptor descriptor)
            {
                Rule = rule;
                Descriptor = descriptor;
            }

            public IAssetRule Rule { get; }

            public RuleDescriptor Descriptor { get; }
        }
    }
}
