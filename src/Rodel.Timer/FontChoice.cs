namespace Rodel.Timer;

/// <summary>A selectable UI font: the family name persisted and applied, and the localized name shown in the picker.</summary>
/// <param name="DisplayName">The human-readable family name shown in the settings font picker.</param>
/// <param name="FamilyName">The canonical family name persisted and applied to text.</param>
public readonly record struct FontChoice(string DisplayName, string FamilyName);
