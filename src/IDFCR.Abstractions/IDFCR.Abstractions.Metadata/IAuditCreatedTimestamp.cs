namespace IDFCR.Abstractions.Metadata;

/// <summary>
/// Represents an entity that has a created timestamp for auditing purposes. This interface defines a property for storing the UTC timestamp of when the entity was created, allowing for consistent tracking of creation times across different implementations. The CreatedTimestampUtc property can be used to record the exact time an entity was created, which is essential for auditing, logging, and historical data analysis in various applications and systems.
/// </summary>
public interface IAuditCreatedTimestamp
{
    /// <summary>
    /// Gets or sets the UTC timestamp of when the entity was created. This property is used for auditing purposes to track the creation time of the entity. It should be set to the current UTC time when the entity is created and should not be modified afterward to ensure accurate historical records. The CreatedTimestampUtc property provides a standardized way to capture and store creation timestamps across different implementations, facilitating consistent auditing and logging practices in applications and systems that implement this interface.
    /// </summary>
    [IgnoreApply]
    DateTimeOffset CreatedTimestampUtc { get; set; }
}
