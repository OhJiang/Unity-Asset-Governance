using System;
using System.Reflection;
using NUnit.Framework;

namespace UnityAssetGovernance.Tests
{
    public sealed class PackageSmokeTests
    {
        [Test]
        public void PackageAssembly_CanBeLoaded()
        {
            Assembly packageAssembly = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "UnityAssetGovernance.Editor")
                {
                    packageAssembly = assembly;
                    break;
                }
            }

            Assert.That(packageAssembly, Is.Not.Null);
            Assert.That(
                packageAssembly.GetType("UnityAssetGovernance.PackageMarker"),
                Is.Not.Null);
        }
    }
}
