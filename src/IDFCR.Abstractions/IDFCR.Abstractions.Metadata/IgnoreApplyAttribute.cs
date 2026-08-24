namespace IDFCR.Abstractions.Metadata;

/// <summary>
/// Defines an attribute to mark properties or fields to be ignored during the Apply operation in ObjectExtensions.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IgnoreApplyAttribute : Attribute
{

}
