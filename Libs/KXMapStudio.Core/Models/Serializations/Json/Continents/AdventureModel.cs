namespace KXMapStudio.Core.Models.Serializations.Json.Continents;

/// <summary>
///     Represents an adventure entry inside a continent floor payload.
/// </summary>
/// <remarks>
///     The GW2 API adventure payload shape varies by endpoint and is not currently used by the application.
///     This type exists to preserve deserialization compatibility.
/// </remarks>
public sealed record AdventureModel;