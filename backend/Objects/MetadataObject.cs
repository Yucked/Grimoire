namespace Grimoire.Objects;

/// <summary>
/// 
/// </summary>
/// <param name="UserDefinedTags"></param>
/// <param name="IsFavourite"></param>
public record MetadataObject(
    IList<string> UserDefinedTags,
    bool IsFavourite
);