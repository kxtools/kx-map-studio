namespace KXMapStudio.Libs.Converters;

/// <summary>
///     Converts a null or empty string to Visibility.
/// </summary>
/// <remarks>
///     <para>
///         Null or empty/whitespace string → <see cref="Visibility.Collapsed" />
///     </para>
///     <para>
///         Non-empty string → <see cref="Visibility.Visible" />
///     </para>
///     <para>
///         This converter is useful for conditionally showing UI elements based on whether text content exists.
///         For example, showing an error message only when the error text is not empty.
///     </para>
///     <para>
///         <strong>Note</strong>: <see cref="ConvertBack" /> is not supported and throws
///         <see cref="NotSupportedException" />.
///     </para>
/// </remarks>
public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
{
	/// <summary>
	///     Converts a string to Visibility based on null/empty check.
	/// </summary>
	/// <param name="value">The string value to convert.</param>
	/// <param name="targetType">The target type (ignored).</param>
	/// <param name="parameter">Optional parameter (ignored).</param>
	/// <param name="culture">Culture information (ignored).</param>
	/// <returns>
	///     <see cref="Visibility.Collapsed" /> if <paramref name="value" /> is null or whitespace;
	///     otherwise <see cref="Visibility.Visible" />.
	/// </returns>
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
	}

	/// <summary>
	///     Not supported. Always throws <see cref="NotSupportedException" />.
	/// </summary>
	/// <param name="value">The value to convert back (unused).</param>
	/// <param name="targetType">The target type (unused).</param>
	/// <param name="parameter">Optional parameter (unused).</param>
	/// <param name="culture">Culture information (unused).</param>
	/// <returns>Never returns.</returns>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("NullOrEmptyToVisibilityConverter does not support ConvertBack.");
	}
}