namespace KXMapStudio.Libs.Converters;

/// <summary>
///     Converts a boolean value to a Visibility value (inverted: true → Collapsed, false → Visible).
/// </summary>
/// <remarks>
///     <para>
///         This converter is useful for hiding UI elements when a condition is true.
///         For example, hiding a button when data is loaded.
///     </para>
///     <para>
///         Supports two-way binding. ConvertBack returns <see langword="true" /> when <see cref="Visibility.Collapsed" />.
///     </para>
/// </remarks>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
	/// <summary>
	///     Converts a boolean to a Visibility (inverted).
	/// </summary>
	/// <param name="value">The boolean value to convert.</param>
	/// <param name="targetType">The target type (ignored).</param>
	/// <param name="parameter">Optional parameter (ignored).</param>
	/// <param name="culture">Culture information (ignored).</param>
	/// <returns>
	///     <see cref="Visibility.Collapsed" /> if <paramref name="value" /> is <see langword="true" />; otherwise
	///     <see cref="Visibility.Visible" />.
	/// </returns>
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is true ? Visibility.Collapsed : Visibility.Visible;
	}

	/// <summary>
	///     Converts a Visibility back to a boolean (inverted).
	/// </summary>
	/// <param name="value">The Visibility value to convert.</param>
	/// <param name="targetType">The target type (ignored).</param>
	/// <param name="parameter">Optional parameter (ignored).</param>
	/// <param name="culture">Culture information (ignored).</param>
	/// <returns>
	///     <see langword="true" /> if <paramref name="value" /> is <see cref="Visibility.Collapsed" />; otherwise
	///     <see langword="false" />.
	/// </returns>
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is Visibility.Collapsed;
	}
}