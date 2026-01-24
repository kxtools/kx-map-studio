namespace KXMapStudio.Core.Models.Github;

/// <summary>
///     Represents the subset of a GitHub release payload used by KXMapStudio.
/// </summary>
/// <param name="TagName">The tag name (for example, <c>v1.2.3</c>).</param>
/// <param name="Name">The release display name.</param>
/// <param name="Body">The release notes body.</param>
/// <param name="HtmlUrl">The GitHub HTML URL for the release.</param>
public sealed record GitHubReleaseModel(
	[property: JsonPropertyName("tag_name")]
	string TagName,
	[property: JsonPropertyName("name")] string Name,
	[property: JsonPropertyName("body")] string Body,
	[property: JsonPropertyName("html_url")]
	string HtmlUrl
);