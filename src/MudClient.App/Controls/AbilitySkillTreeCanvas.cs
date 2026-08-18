using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using MudClient.App.Models;

namespace MudClient.App.Controls;

/// <summary>
/// Draws the Wędrowiec ability set as a radial "skill constellation": one wedge per branch
/// (spell school, passive skills, active skills, ...), each ability placed on a concentric ring
/// by its level tier and linked back toward the hub — visually a tree, though (per
/// <see cref="AbilitySkillTreeEntry"/>'s own docs) the game gives no real prerequisite data, so
/// the connectors are purely a level-ordered visual grouping, not literal unlock requirements.
/// Hovering a node shows its full "help" text via the native <see cref="ToolTip"/> mechanism —
/// the same approach <c>WorldMapControl</c> uses for its teacher/mob markers.
/// </summary>
public sealed class AbilitySkillTreeCanvas : Control
{
    private const double HitPadding = 5;

    private static readonly IBrush HubFillBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x1C, 0x10));
    private static readonly IBrush HubBorderBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xC1, 0x4A));
    private static readonly IBrush OwnedNodeFillBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xC1, 0x4A));
    private static readonly IBrush OwnedNodeBorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xEE, 0xB0));
    private static readonly IBrush PreviewNodeFillBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x3C, 0x4C));
    private static readonly IBrush PreviewNodeBorderBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0xA8));
    private static readonly IBrush SelectionRingBrush = new SolidColorBrush(Color.FromArgb(200, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush HoverRingBrush = new SolidColorBrush(Color.FromArgb(140, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush EmptyTextBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0xA8));
    private static readonly IBrush BranchLabelBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xC7, 0x9D));
    private static readonly IBrush LabelTextBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xE6, 0xCE));
    private static readonly IBrush LabelSubTextBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xAE, 0x98));
    private static readonly IBrush LabelHaloBrush = new SolidColorBrush(Color.FromArgb(195, 0x10, 0x0D, 0x08));

    private static readonly Pen OwnedConnectorPen =
        new(new SolidColorBrush(Color.FromArgb(150, 0xE6, 0xC1, 0x4A)), 1.7);

    private static readonly Pen PreviewConnectorPen = new(
        new SolidColorBrush(Color.FromArgb(110, 0x8A, 0x93, 0xA8)),
        1.2,
        dashStyle: new DashStyle([3, 3], 0));

    private static readonly System.Collections.Generic.Dictionary<string, Color> BranchColors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Umiejętności bierne"] = Color.FromRgb(0xB0, 0x7A, 0x3E),
            ["Umiejętności aktywne"] = Color.FromRgb(0xC9, 0x5B, 0x3E),
            ["Przemiany"] = Color.FromRgb(0x4E, 0x8E, 0x6B),
            ["Nekromancja"] = Color.FromRgb(0x8E, 0x3E, 0x5A),
            ["Przywołania"] = Color.FromRgb(0x3E, 0x6B, 0x8E),
            ["Odrzucanie"] = Color.FromRgb(0x8E, 0x7A, 0x3E),
            ["Zauroczenie"] = Color.FromRgb(0x8E, 0x3E, 0x8A),
            ["Inwokacje"] = Color.FromRgb(0x3E, 0x5A, 0x8E),
            ["Poznanie"] = Color.FromRgb(0x5A, 0x3E, 0x8E),
            ["Czary"] = Color.FromRgb(0x5B, 0x6B, 0x8E),
        };

    private static readonly Color[] FallbackBranchPalette =
    [
        Color.FromRgb(0x4E, 0x8E, 0x6B), Color.FromRgb(0x8E, 0x3E, 0x5A), Color.FromRgb(0x3E, 0x6B, 0x8E),
        Color.FromRgb(0x8E, 0x7A, 0x3E), Color.FromRgb(0x8E, 0x3E, 0x8A), Color.FromRgb(0x3E, 0x5A, 0x8E),
    ];

    /// <summary>Canonical branch order so wedges stay in the same place across re-renders instead
    /// of reshuffling as the filtered ability set changes.</summary>
    private static readonly string[] BranchOrder =
    [
        "Umiejętności bierne", "Umiejętności aktywne",
        "Przemiany", "Nekromancja", "Przywołania", "Odrzucanie", "Zauroczenie", "Inwokacje", "Poznanie",
        "Czary",
    ];

    private const double MinZoom = 0.5;
    private const double MaxZoom = 3.5;
    private const double ZoomWheelFactor = 1.15;

    private IReadOnlyList<AbilitySkillTreeEntry> _abilities = [];
    private SkillTreeLayout _layout = SkillTreeLayout.Empty;
    private AbilitySkillTreeEntry? _hoveredAbility;
    private AbilitySkillTreeEntry? _selectedAbility;
    private double _zoom = 1.0;
    private Vector _pan;
    private Point? _panDragStart;
    private Vector _panDragStartPan;
    private bool _panDragMoved;

    public AbilitySkillTreeCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    /// <summary>Raised when the user clicks a node — the hosting view mirrors this onto the
    /// Killeropedia view model's <c>SelectedAbility</c>.</summary>
    public event Action<AbilitySkillTreeEntry?>? AbilitySelected;

    public AbilitySkillTreeEntry? SelectedAbility
    {
        get => _selectedAbility;
        set
        {
            if (ReferenceEquals(_selectedAbility, value))
            {
                return;
            }

            _selectedAbility = value;
            InvalidateVisual();
        }
    }

    public void SetAbilities(IReadOnlyList<AbilitySkillTreeEntry> abilities)
    {
        _abilities = abilities;
        RecomputeLayout();
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(
            double.IsInfinity(availableSize.Width) ? 480 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 480 : availableSize.Height);

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RecomputeLayout();
    }

    private void RecomputeLayout()
    {
        _layout = ComputeLayout(_abilities, Bounds.Size);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetCurrentPoint(this).Position;

        if (_panDragStart is { } start)
        {
            var delta = position - start;
            if (!_panDragMoved && (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3))
            {
                _panDragMoved = true;
            }

            if (_panDragMoved)
            {
                _pan = _panDragStartPan + delta;
                InvalidateVisual();
                return;
            }
        }

        UpdateHover(position);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoveredAbility is null)
        {
            return;
        }

        _hoveredAbility = null;
        ToolTip.SetTip(this, null);
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var position = e.GetCurrentPoint(this).Position;
        if (HitTestNode(_layout, ToLayout(position)) is { } node)
        {
            SelectedAbility = node.Ability;
            AbilitySelected?.Invoke(node.Ability);

            // Clicking must show details immediately rather than depending on the native
            // ToolTip's hover-and-hold-still delay, which rarely triggers on a canvas this
            // densely packed with small hit-targets — the pointer is almost always still moving
            // between nodes rather than resting on one long enough to auto-open.
            _hoveredAbility = node.Ability;
            InvalidateVisual();
            ToolTip.SetTip(this, BuildTooltip(node.Ability));
            ToolTip.SetIsOpen(this, true);
            return;
        }

        _panDragStart = position;
        _panDragStartPan = _pan;
        _panDragMoved = false;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _panDragStart = null;
        _panDragMoved = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomAtPointer(e.Delta.Y, e.GetCurrentPoint(this).Position);
        e.Handled = true;
    }

    /// <summary>Resets zoom/pan to the default fitted view — wired to the host view's "Resetuj
    /// widok" button, since the graph has no other way back once panned/zoomed away.</summary>
    public void ResetView()
    {
        _zoom = 1.0;
        _pan = default;
        InvalidateVisual();
    }

    private void ZoomAtPointer(double wheelDelta, Point pointerPosition)
    {
        var layoutBefore = ToLayout(pointerPosition);
        var factor = Math.Pow(ZoomWheelFactor, wheelDelta > 0 ? 1 : -1);
        var newZoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - _zoom) < 0.0001)
        {
            return;
        }

        _zoom = newZoom;
        var center = _layout.Center;
        _pan = (pointerPosition - center) - (layoutBefore - center) * _zoom;
        InvalidateVisual();
    }

    /// <summary>Screen-space point for a layout-space point, applying the current zoom/pan —
    /// scaling is anchored on the hub so zooming never drifts the whole tree off-center.</summary>
    private Point ToScreen(Point layoutPoint)
    {
        var center = _layout.Center;
        return center + (layoutPoint - center) * _zoom + _pan;
    }

    private Point ToLayout(Point screenPoint)
    {
        var center = _layout.Center;
        return center + (screenPoint - _pan - center) / _zoom;
    }

    /// <summary>Native ToolTip hover delay already gives the "hold the cursor a bit" feel, so no
    /// custom timer is needed — mirrors <c>WorldMapControl.UpdateHoverTooltip</c>.</summary>
    private void UpdateHover(Point position)
    {
        var node = HitTestNode(_layout, ToLayout(position));
        var ability = node?.Ability;
        if (ReferenceEquals(ability, _hoveredAbility))
        {
            return;
        }

        _hoveredAbility = ability;
        InvalidateVisual();
        ToolTip.SetTip(this, ability is null ? null : BuildTooltip(ability));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        DrawBranchWedges(context, _layout);
        DrawOrbitRings(context, _layout);
        DrawConnectors(context, _layout);
        DrawNodes(context, _layout);
        DrawHub(context, _layout);
        DrawBranchLabels(context, _layout);

        if (_layout.Nodes.Count == 0 && _abilities.Count > 0)
        {
            DrawCenteredText(context, "Brak pasujących umiejętności.", ToScreen(_layout.Center), EmptyTextBrush, 13);
        }
    }

    private void DrawBranchWedges(DrawingContext context, SkillTreeLayout layout)
    {
        foreach (var wedge in layout.Wedges)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(ToScreen(layout.Center), isFilled: true);
                ctx.LineTo(ToScreen(PointOnCircle(layout.Center, wedge.OuterRadius, wedge.StartAngle)));
                ctx.ArcTo(
                    ToScreen(PointOnCircle(layout.Center, wedge.OuterRadius, wedge.EndAngle)),
                    new Size(wedge.OuterRadius * _zoom, wedge.OuterRadius * _zoom),
                    0,
                    isLargeArc: false,
                    SweepDirection.Clockwise);
                ctx.LineTo(ToScreen(layout.Center));
                ctx.EndFigure(true);
            }

            context.DrawGeometry(new SolidColorBrush(wedge.Color, 0.10), null, geometry);
        }
    }

    private void DrawOrbitRings(DrawingContext context, SkillTreeLayout layout)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(28, 0xD8, 0xC7, 0x9D)), 1);
        var center = ToScreen(layout.Center);
        foreach (var radius in layout.OrbitRadii)
        {
            var scaled = radius * _zoom;
            context.DrawEllipse(null, pen, center, scaled, scaled);
        }
    }

    private void DrawConnectors(DrawingContext context, SkillTreeLayout layout)
    {
        foreach (var connector in layout.Connectors)
        {
            context.DrawLine(
                connector.Dim ? PreviewConnectorPen : OwnedConnectorPen,
                ToScreen(connector.From),
                ToScreen(connector.To));
        }
    }

    private void DrawNodes(DrawingContext context, SkillTreeLayout layout)
    {
        foreach (var node in layout.Nodes)
        {
            var isSpell = node.Ability.Type?.Contains("czar", StringComparison.OrdinalIgnoreCase) == true;
            var fill = node.Ability.IsOwned ? OwnedNodeFillBrush : PreviewNodeFillBrush;
            var borderBrush = node.Ability.IsOwned ? OwnedNodeBorderBrush : PreviewNodeBorderBrush;
            var borderWidth = node.Ability.IsOwned ? 2.0 : 1.3;
            var center = ToScreen(node.Center);
            var radius = node.Radius * _zoom;

            var isHovered = ReferenceEquals(node.Ability, _hoveredAbility);
            var isSelected = ReferenceEquals(node.Ability, _selectedAbility);
            if (isSelected)
            {
                DrawNodeShape(context, isSpell, center, radius + 5, null, new Pen(SelectionRingBrush, 2.2));
            }
            else if (isHovered)
            {
                DrawNodeShape(context, isSpell, center, radius + 4, null, new Pen(HoverRingBrush, 1.8));
            }

            DrawNodeShape(context, isSpell, center, radius, fill, new Pen(borderBrush, borderWidth));
            DrawNodeLabel(context, node.Ability, center, radius);
        }
    }

    private static void DrawNodeShape(
        DrawingContext context, bool isHexagon, Point center, double radius, IBrush? fill, Pen pen)
    {
        if (isHexagon)
        {
            context.DrawGeometry(fill, pen, BuildHexagon(center, radius));
        }
        else
        {
            context.DrawEllipse(fill, pen, center, radius, radius);
        }
    }

    /// <summary>Draws the ability's name and level centered on the node — a small "chip" that may
    /// extend past the node's own tiny hex/circle outline, since fitting a name inside an 8px
    /// marker isn't possible. Font sizes track <see cref="_zoom"/> so crowded areas become legible
    /// simply by scrolling in, without extra UI. Krąg (spell circle) and every other detail live in
    /// the hover tooltip (<see cref="BuildTooltip"/>) instead, since there's no room to make them
    /// readable at node scale.</summary>
    private void DrawNodeLabel(DrawingContext context, AbilitySkillTreeEntry ability, Point center, double radius)
    {
        var nameSize = Math.Clamp(7.5 * _zoom, 6, 12);
        var subSize = Math.Clamp(6.2 * _zoom, 5, 10);
        var maxWidth = Math.Max(48, radius * 6.5);

        var nameTypeface = new Typeface(FontFamily.Default, weight: FontWeight.SemiBold);
        var nameText = TruncateForLabel(ability.Name, nameTypeface, nameSize, maxWidth);
        var nameFormatted = new FormattedText(
            nameText, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            nameTypeface, nameSize, LabelTextBrush);

        var level = ability.BrowsedClassLevel ?? ability.WandererLevel;
        var subText = $"{level} lvl";
        var subTypeface = new Typeface(FontFamily.Default);
        var subFormatted = new FormattedText(
            subText, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            subTypeface, subSize, LabelSubTextBrush);

        var blockWidth = Math.Max(nameFormatted.Width, subFormatted.Width);
        var blockHeight = nameFormatted.Height + subFormatted.Height;
        var top = center.Y - blockHeight / 2;

        context.DrawRectangle(
            LabelHaloBrush, null,
            new Rect(center.X - blockWidth / 2 - 4, top - 2, blockWidth + 8, blockHeight + 4), 3, 3);
        context.DrawText(nameFormatted, new Point(center.X - nameFormatted.Width / 2, top));
        context.DrawText(subFormatted, new Point(center.X - subFormatted.Width / 2, top + nameFormatted.Height));
    }

    private static string TruncateForLabel(string text, Typeface typeface, double fontSize, double maxWidth)
    {
        if (MeasureWidth(text, typeface, fontSize) <= maxWidth)
        {
            return text;
        }

        for (var length = text.Length - 1; length > 1; length--)
        {
            var candidate = text[..length].TrimEnd() + "…";
            if (MeasureWidth(candidate, typeface, fontSize) <= maxWidth)
            {
                return candidate;
            }
        }

        return text.Length > 0 ? text[..1] + "…" : text;
    }

    private static double MeasureWidth(string text, Typeface typeface, double fontSize) =>
        new FormattedText(
            text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, fontSize, Brushes.Transparent).Width;

    private void DrawHub(DrawingContext context, SkillTreeLayout layout)
    {
        var center = ToScreen(layout.Center);
        var radius = layout.HubRadius * _zoom;
        context.DrawEllipse(HubFillBrush, new Pen(HubBorderBrush, 2.4), center, radius, radius);
        context.DrawEllipse(
            null, new Pen(HubBorderBrush, 1, dashStyle: new DashStyle([1, 3], 0)), center,
            Math.Max(0, radius - 6), Math.Max(0, radius - 6));
        DrawCenteredText(context, layout.HubLabel, center, HubBorderBrush, 11.5, bold: true);
    }

    private void DrawBranchLabels(DrawingContext context, SkillTreeLayout layout)
    {
        foreach (var label in layout.BranchLabels)
        {
            var typeface = new Typeface(FontFamily.Default, weight: FontWeight.Bold);
            var text = new FormattedText(
                label.Name.ToUpperInvariant(),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12.5,
                new SolidColorBrush(label.Color, 0.95));
            var position = ToScreen(label.Position);
            var origin = new Point(position.X - text.Width / 2, position.Y - text.Height / 2);
            context.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(150, 0x14, 0x11, 0x0B)), null,
                new Rect(origin.X - 6, origin.Y - 3, text.Width + 12, text.Height + 6), 4, 4);
            context.DrawText(text, origin);
            _ = BranchLabelBrush; // reserved for a future non-colored label style
        }
    }

    private static void DrawCenteredText(
        DrawingContext context, string text, Point center, IBrush brush, double size, bool bold = false)
    {
        var typeface = new Typeface(FontFamily.Default, weight: bold ? FontWeight.Bold : FontWeight.Normal);
        var formatted = new FormattedText(
            text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }

    private static Point PointOnCircle(Point center, double radius, double angleRadians) =>
        new(center.X + radius * Math.Cos(angleRadians), center.Y + radius * Math.Sin(angleRadians));

    private static StreamGeometry BuildHexagon(Point center, double radius)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        for (var i = 0; i < 6; i++)
        {
            var angle = Math.PI / 180 * (60 * i - 90);
            var point = PointOnCircle(center, radius, angle);
            if (i == 0)
            {
                ctx.BeginFigure(point, isFilled: true);
            }
            else
            {
                ctx.LineTo(point);
            }
        }

        ctx.EndFigure(true);
        return geometry;
    }

    internal static SkillTreeNode? HitTestNode(SkillTreeLayout layout, Point position)
    {
        SkillTreeNode? best = null;
        var bestDistance = double.MaxValue;

        foreach (var node in layout.Nodes)
        {
            var dx = position.X - node.Center.X;
            var dy = position.Y - node.Center.Y;
            var distance = dx * dx + dy * dy;
            var threshold = node.Radius + HitPadding;
            if (distance > threshold * threshold)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node;
            }
        }

        return best;
    }

    /// <summary>Builds the hover tooltip content — name, levels (including Krąg, when known),
    /// ownership state and every other detail (syntax, target, school, alignment, teachers,
    /// "zobacz też" and the full description). This is now the sole place ability details are
    /// shown in the Wędrowiec tab, replacing the old always-visible right-side panel, so it
    /// carries everything that panel used to. Kept as an <c>internal static</c> pure function,
    /// like <c>WorldMapControl.FormatTeacherTooltip</c>, so it's unit-testable without needing a
    /// live control.</summary>
    private static readonly IBrush TooltipTextBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xDC, 0xC0));
    private static readonly IBrush TooltipHeadingBrush = new SolidColorBrush(Color.FromRgb(0xC9, 0xA0, 0x54));

    internal static Control BuildTooltip(AbilitySkillTreeEntry ability)
    {
        var root = new StackPanel { Spacing = 6 };
        root.Children.Add(new TextBlock
        {
            Text = ability.Name,
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            LineHeight = 19,
            Foreground = OwnedNodeFillBrush,
        });

        if (!string.IsNullOrWhiteSpace(ability.Type))
        {
            root.Children.Add(new TextBlock
            {
                Text = ability.Type, FontSize = 13, LineHeight = 16, Opacity = 0.75, Foreground = TooltipTextBrush,
            });
        }

        root.Children.Add(new TextBlock
        {
            Text = ability.LevelSummaryText,
            FontSize = 13,
            LineHeight = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = TooltipHeadingBrush,
        });
        root.Children.Add(new TextBlock
        {
            Text = ability.WandererAvailabilityText,
            FontSize = 13,
            LineHeight = 16,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = TooltipTextBrush,
        });

        if (!string.IsNullOrWhiteSpace(ability.AvailableForClassesText))
        {
            AddLabeledLine(root, "Dostępne dla klas", ability.AvailableForClassesText);
        }

        if (ability.HasSyntax)
        {
            AddLabeledLine(root, "Składnia", ability.Syntax!);
        }

        if (ability.HasTarget)
        {
            AddLabeledLine(root, "Cel", ability.Target!);
        }

        if (ability.HasSchool)
        {
            AddLabeledLine(root, "Szkoła", ability.School!);
        }

        if (ability.HasAlignment)
        {
            AddLabeledLine(root, "Alignment", ability.Alignment!);
        }

        if (ability.HasDescription)
        {
            root.Children.Add(new TextBlock
            {
                Text = ability.Description,
                FontSize = 13,
                LineHeight = 17,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = TooltipTextBrush,
                Margin = new Thickness(0, 4, 0, 0),
            });
        }

        if (ability.HasTeachers)
        {
            AddLabeledLine(root, "Nauczyciele", string.Join(", ", ability.Teachers));
        }

        if (ability.HasSeeAlso)
        {
            AddLabeledLine(root, "Zobacz też", ability.SeeAlso!);
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x14, 0x0B)),
            BorderBrush = OwnedNodeFillBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14),
            MaxWidth = 440,
            Child = root,
        };
    }

    private static void AddLabeledLine(StackPanel root, string label, string value)
    {
        var line = new StackPanel { Spacing = 3, Margin = new Thickness(0, 2, 0, 0) };
        line.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(), FontSize = 11, LineHeight = 13, FontWeight = FontWeight.SemiBold,
            Foreground = TooltipHeadingBrush,
        });
        line.Children.Add(new TextBlock
        {
            Text = value, FontSize = 13, LineHeight = 16, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Foreground = TooltipTextBrush,
        });
        root.Children.Add(line);
    }

    /// <summary>
    /// Pure geometry: groups <paramref name="abilities"/> into branches (spell school, passive
    /// skills, active skills, ...), spreads each branch across an angular wedge, and places its
    /// abilities on concentric rings ordered by level tier. <c>internal static</c> and
    /// side-effect-free so the layout math is unit-testable independent of rendering.
    /// </summary>
    internal static SkillTreeLayout ComputeLayout(IReadOnlyList<AbilitySkillTreeEntry> abilities, Size viewport)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return SkillTreeLayout.Empty;
        }

        var center = new Point(viewport.Width / 2, viewport.Height / 2);
        var shortestSide = Math.Min(viewport.Width, viewport.Height);
        var hubRadius = Math.Clamp(shortestSide * 0.045, 20, 34);
        var nodeRadius = Math.Clamp(shortestSide * 0.011, 6, 10);
        var maxRadius = Math.Max(0, shortestSide / 2 - 46);

        var branches = abilities
            .GroupBy(GetBranch, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => BranchOrderIndex(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (branches.Count == 0 || maxRadius <= hubRadius)
        {
            return new SkillTreeLayout
            {
                Center = center,
                HubRadius = hubRadius,
                HubLabel = HubLabel(abilities),
            };
        }

        var maxRingIndex = branches.Max(branch => branch
            .Select(a => a.BrowsedClassLevel ?? 0)
            .Distinct()
            .Count() - 1);
        maxRingIndex = Math.Max(0, maxRingIndex);

        var baseRadius = Math.Min(hubRadius + 40, maxRadius);
        var ringSpacing = maxRingIndex <= 0
            ? Math.Max(0, maxRadius - baseRadius)
            : Math.Clamp((maxRadius - baseRadius) / maxRingIndex, 4, 68);

        var nodes = new List<SkillTreeNode>();
        var connectors = new List<SkillTreeConnector>();
        var wedges = new List<SkillTreeWedge>();
        var branchLabels = new List<SkillTreeBranchLabel>();
        var orbitRadii = new SortedSet<double>();

        var angleStep = 2 * Math.PI / branches.Count;
        var gap = angleStep * 0.12;
        var sliceWidth = angleStep - gap;

        for (var branchIndex = 0; branchIndex < branches.Count; branchIndex++)
        {
            var branch = branches[branchIndex];
            var branchName = branch.Key;
            var branchColor = BranchColors.TryGetValue(branchName, out var known)
                ? known
                : FallbackBranchPalette[branchIndex % FallbackBranchPalette.Length];
            var centerAngle = -Math.PI / 2 + branchIndex * angleStep + angleStep / 2;

            var tiers = branch
                .GroupBy(a => a.BrowsedClassLevel ?? 0)
                .OrderBy(g => g.Key)
                .ToList();

            SkillTreeNode[] previousRingNodes = [];
            var branchMaxRadius = hubRadius;

            for (var ringIndex = 0; ringIndex < tiers.Count; ringIndex++)
            {
                var ringRadius = baseRadius + ringIndex * ringSpacing;
                orbitRadii.Add(ringRadius);

                var ringAbilities = tiers[ringIndex].OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
                var count = ringAbilities.Count;
                var currentRingNodes = new SkillTreeNode[count];

                // Crowded rings (many abilities sharing a level) get more of the wedge's angular
                // room, plus a 3-row radial stagger — otherwise a dozen same-level skills (common
                // for level-1 passives) render as one overlapping blob instead of a readable fan.
                var spreadFraction = count <= 2 ? 0.55 : count <= 5 ? 0.85 : 1.0;
                var spreadStart = centerAngle - sliceWidth * spreadFraction / 2;
                var staggerRows = count switch { <= 3 => 1, <= 9 => 2, _ => 3 };
                var staggerStep = nodeRadius * 3.4;

                for (var i = 0; i < count; i++)
                {
                    var angle = count == 1
                        ? centerAngle
                        : spreadStart + sliceWidth * spreadFraction * (i + 0.5) / count;
                    var effectiveRadius = Math.Min(ringRadius + (i % staggerRows) * staggerStep, maxRadius);
                    branchMaxRadius = Math.Max(branchMaxRadius, effectiveRadius);

                    var nodeCenter = PointOnCircle(center, effectiveRadius, angle);
                    var node = new SkillTreeNode(ringAbilities[i], nodeCenter, nodeRadius, branchName);
                    currentRingNodes[i] = node;
                    nodes.Add(node);

                    var parentCenter = FindNearestParent(previousRingNodes, angle, center) ?? center;
                    connectors.Add(new SkillTreeConnector(parentCenter, nodeCenter, !ringAbilities[i].IsOwned));
                }

                previousRingNodes = currentRingNodes;
            }

            wedges.Add(new SkillTreeWedge(
                centerAngle - sliceWidth / 2, centerAngle + sliceWidth / 2, maxRadius, branchColor));
            branchLabels.Add(new SkillTreeBranchLabel(
                branchName, PointOnCircle(center, Math.Min(branchMaxRadius + 34, maxRadius + 20), centerAngle), branchColor));
        }

        return new SkillTreeLayout
        {
            Center = center,
            HubRadius = hubRadius,
            HubLabel = HubLabel(abilities),
            Nodes = nodes,
            Connectors = connectors,
            Wedges = wedges,
            BranchLabels = branchLabels,
            OrbitRadii = orbitRadii.ToList(),
        };
    }

    private static Point? FindNearestParent(SkillTreeNode[] previousRingNodes, double angle, Point origin)
    {
        if (previousRingNodes.Length == 0)
        {
            return null;
        }

        SkillTreeNode best = previousRingNodes[0];
        var bestDelta = double.MaxValue;
        foreach (var candidate in previousRingNodes)
        {
            var delta = Math.Abs(NormalizeAngle(candidate.AngleFrom(origin) - angle));
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = candidate;
            }
        }

        return best.Center;
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > Math.PI)
        {
            angle -= 2 * Math.PI;
        }

        while (angle < -Math.PI)
        {
            angle += 2 * Math.PI;
        }

        return angle;
    }

    private static int BranchOrderIndex(string branch)
    {
        var index = Array.IndexOf(BranchOrder, branch);
        return index >= 0 ? index : BranchOrder.Length;
    }

    private static string HubLabel(IReadOnlyList<AbilitySkillTreeEntry> abilities) =>
        abilities.Count == 0 ? "WĘDROWIEC" : abilities[0].BrowsedClass.ToUpperInvariant();

    internal static string GetBranch(AbilitySkillTreeEntry ability)
    {
        var type = ability.Type ?? string.Empty;
        if (type.Contains("czar", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(ability.School) ? "Czary" : ability.School!;
        }

        if (type.Contains("aktywny", StringComparison.OrdinalIgnoreCase))
        {
            return "Umiejętności aktywne";
        }

        if (type.Contains("bierny", StringComparison.OrdinalIgnoreCase))
        {
            return "Umiejętności bierne";
        }

        return "Czary";
    }
}

internal readonly record struct SkillTreeNode(AbilitySkillTreeEntry Ability, Point Center, double Radius, string Branch)
{
    public double AngleFrom(Point origin) => Math.Atan2(Center.Y - origin.Y, Center.X - origin.X);
}

internal readonly record struct SkillTreeConnector(Point From, Point To, bool Dim);

internal readonly record struct SkillTreeWedge(double StartAngle, double EndAngle, double OuterRadius, Color Color);

internal readonly record struct SkillTreeBranchLabel(string Name, Point Position, Color Color);

internal sealed class SkillTreeLayout
{
    public static readonly SkillTreeLayout Empty = new() { Center = default, HubRadius = 30, HubLabel = "WĘDROWIEC" };

    public Point Center { get; init; }

    public double HubRadius { get; init; }

    public string HubLabel { get; init; } = "WĘDROWIEC";

    public IReadOnlyList<SkillTreeNode> Nodes { get; init; } = [];

    public IReadOnlyList<SkillTreeConnector> Connectors { get; init; } = [];

    public IReadOnlyList<SkillTreeWedge> Wedges { get; init; } = [];

    public IReadOnlyList<SkillTreeBranchLabel> BranchLabels { get; init; } = [];

    public IReadOnlyList<double> OrbitRadii { get; init; } = [];
}
