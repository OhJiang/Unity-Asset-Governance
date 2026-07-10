namespace UnityAssetGovernance
{
    /// <summary>
    /// Describes how strongly a validation issue should affect the project.
    /// The numeric order is intentional so callers can compare severity thresholds.
    /// </summary>
    public enum RuleSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}
