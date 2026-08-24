namespace IDFCR.Abstractions.Metadata;

/// <summary>
/// Represents an entity that has a modified timestamp for auditing purposes. This interface defines a property for storing the UTC timestamp of when the entity was last modified, allowing for consistent tracking of modification times across different implementations. The ModifiedTimestampUtc property can be used to record the exact time an entity was last modified, which is essential for auditing, logging, and historical data analysis in various applications and systems. It is important to note that the ModifiedTimestampUtc property should be updated whenever the entity undergoes changes to ensure accurate tracking of modifications over time.
/// </summary>
public interface IAuditModifiedTimestamp
{
    /// <summary>
    /// Gets or sets the UTC timestamp of when the entity was last modified. This property is used for auditing purposes to track the modification time of the entity. It should be updated to the current UTC time whenever the entity undergoes changes to ensure accurate historical records. The ModifiedTimestampUtc property provides a standardized way to capture and store modification timestamps across different implementations, facilitating consistent auditing and logging practices in applications and systems that implement this interface.
    /// </summary>
    [IgnoreApply]
    DateTimeOffset? ModifiedTimestampUtc { get; set; }
}
