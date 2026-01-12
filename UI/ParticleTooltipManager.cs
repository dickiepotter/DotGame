using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DotGame.Models;
using DotGame.Utilities;

namespace DotGame.UI;

/// <summary>
/// Manages display and formatting of particle information tooltips.
/// Extracts tooltip logic from MainWindow to improve maintainability.
/// </summary>
public class ParticleTooltipManager
{
    private readonly Border _tooltipBorder;
    private readonly TextBlock _tooltipTextBlock;

    public ParticleTooltipManager(Border tooltipBorder, TextBlock tooltipTextBlock)
    {
        _tooltipBorder = tooltipBorder;
        _tooltipTextBlock = tooltipTextBlock;
    }

    /// <summary>
    /// Shows a tooltip with particle details at the specified position.
    /// </summary>
    /// <param name="particle">The particle to display information for.</param>
    /// <param name="position">Position on screen where tooltip should appear.</param>
    /// <param name="canvasBounds">Bounds of the canvas to keep tooltip within view.</param>
    public void Show(Particle particle, Point position, Rect canvasBounds)
    {
        // Format particle details
        _tooltipTextBlock.Text = FormatParticleDetails(particle);

        // Position tooltip near cursor with offset
        const double offsetX = RenderingConstants.TOOLTIP_OFFSET_X;
        const double offsetY = RenderingConstants.TOOLTIP_OFFSET_Y;
        double left = position.X + offsetX;
        double top = position.Y + offsetY;

        // Keep tooltip within canvas bounds
        if (left + _tooltipBorder.ActualWidth > canvasBounds.Width)
            left = position.X - _tooltipBorder.ActualWidth - offsetX;

        if (top + _tooltipBorder.ActualHeight > canvasBounds.Height)
            top = position.Y - _tooltipBorder.ActualHeight - offsetY;

        _tooltipBorder.Margin = new Thickness(left, top, 0, 0);
        _tooltipBorder.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public void Hide()
    {
        _tooltipBorder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Formats detailed information about a particle for display.
    /// </summary>
    private string FormatParticleDetails(Particle particle)
    {
        var details = $"╔══ Particle #{particle.Id} ══╗\n";
        details += $"Position: ({particle.Position.X:F1}, {particle.Position.Y:F1})\n";
        details += $"Velocity: ({particle.Velocity.X:F1}, {particle.Velocity.Y:F1})\n";
        details += $"Speed: {particle.Velocity.Length():F1}\n";
        details += $"Mass: {particle.Mass:F2}\n";
        details += $"Radius: {particle.Radius:F1}\n";

        if (particle.HasAbilities && particle.Abilities != null)
        {
            details += FormatAbilityDetails(particle);
        }

        details += "╚" + new string('═', 20) + "╝";

        return details;
    }

    /// <summary>
    /// Formats ability-specific information for a particle.
    /// </summary>
    private string FormatAbilityDetails(Particle particle)
    {
        var abilities = particle.Abilities;
        var details = $"\n╠══ Abilities ══╣\n";
        details += $"Type: {abilities.Type}\n";
        details += $"State: {abilities.CurrentState}\n";
        details += $"Energy: {abilities.Energy:F1}/{abilities.MaxEnergy:F1} ({particle.EnergyPercentage:P0})\n";
        details += $"Generation: {abilities.Generation}\n";
        details += $"Vision Range: {abilities.VisionRange:F1}\n";

        // List available abilities
        var abilityList = GetAbilityList(abilities);
        if (abilityList.Count > 0)
        {
            details += $"Has: {string.Join(", ", abilityList)}\n";
        }

        // Show active special states
        details += FormatActiveStates(abilities);

        return details;
    }

    /// <summary>
    /// Gets a list of ability names that the particle has.
    /// </summary>
    private List<string> GetAbilityList(ParticleAbilities abilities)
    {
        var abilityList = new List<string>();

        if (abilities.HasAbility(AbilitySet.Eating)) abilityList.Add("Eating");
        if (abilities.HasAbility(AbilitySet.Splitting)) abilityList.Add("Splitting");
        if (abilities.HasAbility(AbilitySet.Reproduction)) abilityList.Add("Reproduction");
        if (abilities.HasAbility(AbilitySet.Phasing)) abilityList.Add("Phasing");
        if (abilities.HasAbility(AbilitySet.SpeedBurst)) abilityList.Add("SpeedBurst");
        if (abilities.HasAbility(AbilitySet.Chase)) abilityList.Add("Chase");
        if (abilities.HasAbility(AbilitySet.Flee)) abilityList.Add("Flee");

        return abilityList;
    }

    /// <summary>
    /// Formats information about currently active temporary states.
    /// </summary>
    private string FormatActiveStates(ParticleAbilities abilities)
    {
        var states = "";

        if (abilities.IsPhasing)
            states += $"PHASING: {abilities.PhasingTimeRemaining:F1}s\n";

        if (abilities.IsSpeedBoosted)
            states += $"SPEED BOOST: {abilities.SpeedBoostTimeRemaining:F1}s\n";

        if (abilities.IsCamouflaged)
            states += $"CAMOUFLAGED: {abilities.CamouflageTimeRemaining:F1}s\n";

        return states;
    }
}
