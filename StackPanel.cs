using System;
using System.Collections.Generic;
using System.Drawing;
using GalaxyUI;

namespace GalaxyUI.UIElement
{
    /// <summary>
    /// A panel that arranges its children in a stack
    /// </summary>
    public class StackPanel : Panel
    {
        private Orientation _orientation = Orientation.Vertical;
        private int _spacing = 5;

        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    InvalidateLayout();
                }
            }
        }

        public int Spacing
        {
            get => _spacing;
            set
            {
                if (_spacing != value)
                {
                    _spacing = value;
                    InvalidateLayout();
                }
            }
        }

        public StackPanel()
        {
            SizeChanged += (s, e) => InvalidateLayout();
        }

        private void InvalidateLayout()
        {
            ArrangeChildren();
            InvalidateVisual();
        }

        private void ArrangeChildren()
        {
            if (Children.Count == 0)
                return;

            int x = Padding.Left;
            int y = Padding.Top;

            foreach (var child in Children)
            {
                if (!child.IsVisible)
                    continue;

                if (_orientation == Orientation.Horizontal)
                {
                    child.X = x;
                    child.Y = y;
                    x += child.Width + child.Margin.Left + child.Margin.Right + _spacing;
                }
                else // Vertical
                {
                    child.X = x;
                    child.Y = y;
                    y += child.Height + child.Margin.Top + child.Margin.Bottom + _spacing;
                }
            }
        }

        protected override void OnLayoutChanged()
        {
            base.OnLayoutChanged();
            InvalidateLayout();
        }
    }

    /// <summary>
    /// A panel that positions its children at specific positions
    /// </summary>
    public class Canvas : Panel
    {
        public Canvas()
        {
        }

        public static void SetLeft(UIElement element, int value)
        {
            if (element != null)
            {
                element.X = value;
            }
        }

        public static void SetTop(UIElement element, int value)
        {
            if (element != null)
            {
                element.Y = value;
            }
        }

        public static int GetLeft(UIElement element)
        {
            return element?.X ?? 0;
        }

        public static int GetTop(UIElement element)
        {
            return element?.Y ?? 0;
        }
    }

    /// <summary>
    /// A panel that arranges its children in a grid
    /// </summary>
    public class GridPanel : Panel
    {
        private List<ColumnDefinition> _columnDefinitions = new List<ColumnDefinition>();
        private List<RowDefinition> _rowDefinitions = new List<RowDefinition>();
        private Dictionary<UIElement, GridCell> _childrenCells = new Dictionary<UIElement, GridCell>();

        public IReadOnlyList<ColumnDefinition> ColumnDefinitions => _columnDefinitions.AsReadOnly();
        public IReadOnlyList<RowDefinition> RowDefinitions => _rowDefinitions.AsReadOnly();

        public GridPanel()
        {
            SizeChanged += (s, e) => InvalidateLayout();
        }

        public void AddColumn(ColumnDefinition column)
        {
            if (column != null)
            {
                _columnDefinitions.Add(column);
                InvalidateLayout();
            }
        }

        public void AddRow(RowDefinition row)
        {
            if (row != null)
            {
                _rowDefinitions.Add(row);
                InvalidateLayout();
            }
        }

        public void SetCell(UIElement element, int row, int column)
        {
            if (element != null)
            {
                _childrenCells[element] = new GridCell(row, column);
                InvalidateLayout();
            }
        }

        public void SetCell(UIElement element, int row, int column, int rowSpan, int columnSpan)
        {
            if (element != null)
            {
                _childrenCells[element] = new GridCell(row, column, rowSpan, columnSpan);
                InvalidateLayout();
            }
        }

        private void InvalidateLayout()
        {
            ArrangeChildren();
            InvalidateVisual();
        }

        private void ArrangeChildren()
        {
            if (Children.Count == 0 || _rowDefinitions.Count == 0 || _columnDefinitions.Count == 0)
                return;

            // Calculate row heights and column widths
            int[] rowHeights = CalculateRowHeights();
            int[] columnWidths = CalculateColumnWidths();

            // Calculate row and column positions
            int[] rowPositions = new int[_rowDefinitions.Count];
            int[] columnPositions = new int[_columnDefinitions.Count];

            int y = Padding.Top;
            for (int i = 0; i < _rowDefinitions.Count; i++)
            {
                rowPositions[i] = y;
                y += rowHeights[i];
            }

            int x = Padding.Left;
            for (int i = 0; i < _columnDefinitions.Count; i++)
            {
                columnPositions[i] = x;
                x += columnWidths[i];
            }

            // Position children according to their grid cells
            foreach (var child in Children)
            {
                if (!child.IsVisible || !_childrenCells.TryGetValue(child, out GridCell cell))
                    continue;

                if (cell.Row < 0 || cell.Row >= _rowDefinitions.Count ||
                    cell.Column < 0 || cell.Column >= _columnDefinitions.Count)
                    continue;

                int endRow = Math.Min(_rowDefinitions.Count - 1, cell.Row + cell.RowSpan - 1);
                int endColumn = Math.Min(_columnDefinitions.Count - 1, cell.Column + cell.ColumnSpan - 1);

                child.X = columnPositions[cell.Column] + child.Margin.Left;
                child.Y = rowPositions[cell.Row] + child.Margin.Top;

                child.Width = (columnPositions[endColumn] + columnWidths[endColumn]) - columnPositions[cell.Column] - child.Margin.Left - child.Margin.Right;
                child.Height = (rowPositions[endRow] + rowHeights[endRow]) - rowPositions[cell.Row] - child.Margin.Top - child.Margin.Bottom;
            }
        }

        private int[] CalculateRowHeights()
        {
            int[] heights = new int[_rowDefinitions.Count];
            int totalHeight = Height - Padding.Top - Padding.Bottom;
            int totalStars = 0;
            int allocatedHeight = 0;

            // First pass: calculate fixed and auto heights
            for (int i = 0; i < _rowDefinitions.Count; i++)
            {
                var rowDef = _rowDefinitions[i];

                if (rowDef.Height.IsAbsolute)
                {
                    heights[i] = (int)rowDef.Height.Value;  // Cast double to int
                    allocatedHeight += heights[i];
                }
                else if (rowDef.Height.IsAuto)
                {
                    // Find maximum height of auto-sized elements in this row
                    int maxHeight = 0;
                    foreach (var child in Children)
                    {
                        if (_childrenCells.TryGetValue(child, out GridCell cell) && cell.Row == i)
                        {
                            maxHeight = Math.Max(maxHeight, child.Height + child.Margin.Top + child.Margin.Bottom);
                        }
                    }
                    heights[i] = maxHeight;
                    allocatedHeight += heights[i];
                }
                else if (rowDef.Height.IsStar)
                {
                    totalStars += (int)rowDef.Height.Value;  // Cast double to int
                }
            }

            // Second pass: distribute remaining height to star-sized rows
            if (totalStars > 0 && allocatedHeight < totalHeight)
            {
                int remainingHeight = totalHeight - allocatedHeight;
                for (int i = 0; i < _rowDefinitions.Count; i++)
                {
                    var rowDef = _rowDefinitions[i];
                    if (rowDef.Height.IsStar)
                    {
                        // Cast to int and ensure proper division
                        heights[i] = (int)((remainingHeight * rowDef.Height.Value) / totalStars);
                    }
                }
            }

            return heights;
        }

        private int[] CalculateColumnWidths()
        {
            int[] widths = new int[_columnDefinitions.Count];
            int totalWidth = Width - Padding.Left - Padding.Right;
            int totalStars = 0;
            int allocatedWidth = 0;

            // First pass: calculate fixed and auto widths
            for (int i = 0; i < _columnDefinitions.Count; i++)
            {
                var colDef = _columnDefinitions[i];

                if (colDef.Width.IsAbsolute)
                {
                    widths[i] = (int)colDef.Width.Value;  // Cast double to int
                    allocatedWidth += widths[i];
                }
                else if (colDef.Width.IsAuto)
                {
                    // Find maximum width of auto-sized elements in this column
                    int maxWidth = 0;
                    foreach (var child in Children)
                    {
                        if (_childrenCells.TryGetValue(child, out GridCell cell) && cell.Column == i)
                        {
                            maxWidth = Math.Max(maxWidth, child.Width + child.Margin.Left + child.Margin.Right);
                        }
                    }
                    widths[i] = maxWidth;
                    allocatedWidth += widths[i];
                }
                else if (colDef.Width.IsStar)
                {
                    totalStars += (int)colDef.Width.Value;  // Cast double to int
                }
            }

            // Second pass: distribute remaining width to star-sized columns
            if (totalStars > 0 && allocatedWidth < totalWidth)
            {
                int remainingWidth = totalWidth - allocatedWidth;
                for (int i = 0; i < _columnDefinitions.Count; i++)
                {
                    var colDef = _columnDefinitions[i];
                    if (colDef.Width.IsStar)
                    {
                        // Cast to int and ensure proper division
                        widths[i] = (int)((remainingWidth * colDef.Width.Value) / totalStars);
                    }
                }
            }

            return widths;
        }

        // Changed from override to new to fix CS0506
        public new void Add(UIElement child)
        {
            base.Add(child);
            InvalidateLayout();
        }

        // Changed from override to new to fix CS0506
        public new void Remove(UIElement child)
        {
            base.Remove(child);
            if (child != null)
            {
                _childrenCells.Remove(child);  // Fixed missing out parameter
            }
            InvalidateLayout();
        }

        protected override void OnLayoutChanged()
        {
            base.OnLayoutChanged();
            InvalidateLayout();
        }
    }

    /// <summary>
    /// Represents a grid cell position and span
    /// </summary>
    public class GridCell
    {
        public int Row { get; }
        public int Column { get; }
        public int RowSpan { get; }
        public int ColumnSpan { get; }

        public GridCell(int row, int column)
            : this(row, column, 1, 1)
        {
        }

        public GridCell(int row, int column, int rowSpan, int columnSpan)
        {
            Row = Math.Max(0, row);
            Column = Math.Max(0, column);
            RowSpan = Math.Max(1, rowSpan);
            ColumnSpan = Math.Max(1, columnSpan);
        }
    }

    /// <summary>
    /// Defines a column in a GridPanel
    /// </summary>
    public class ColumnDefinition
    {
        public GridLength Width { get; set; }

        public ColumnDefinition()
            : this(GridLength.Auto)
        {
        }

        public ColumnDefinition(GridLength width)
        {
            Width = width;
        }
    }

    /// <summary>
    /// Defines a row in a GridPanel
    /// </summary>
    public class RowDefinition
    {
        public GridLength Height { get; set; }

        public RowDefinition()
            : this(GridLength.Auto)
        {
        }

        public RowDefinition(GridLength height)
        {
            Height = height;
        }
    }

    /// <summary>
    /// Represents a length value for grid rows and columns
    /// </summary>
    public struct GridLength : IEquatable<GridLength>
    {
        private readonly double _value;
        private readonly GridUnitType _unitType;

        // Common predefined values for convenience
        public static readonly GridLength Auto = new GridLength(0, GridUnitType.Auto);
        public static readonly GridLength Star = new GridLength(1, GridUnitType.Star);
        public static readonly GridLength Zero = new GridLength(0);

        public double Value => _value;
        public GridUnitType UnitType => _unitType;

        public bool IsAbsolute => _unitType == GridUnitType.Pixel;
        public bool IsAuto => _unitType == GridUnitType.Auto;
        public bool IsStar => _unitType == GridUnitType.Star;
        public bool IsZero => _unitType == GridUnitType.Pixel && _value == 0;

        /// <summary>
        /// Creates a pixel-based GridLength with the specified value
        /// </summary>
        public GridLength(double value)
            : this(value, GridUnitType.Pixel)
        {
        }

        /// <summary>
        /// Creates a GridLength with the specified value and unit type
        /// </summary>
        public GridLength(double value, GridUnitType unitType)
        {
            if (unitType == GridUnitType.Auto)
            {
                _value = 0; // Auto ignores any specified value
            }
            else
            {
                _value = Math.Max(0, value);
            }
            _unitType = unitType;
        }

        /// <summary>
        /// Creates a pixel-based GridLength
        /// </summary>
        public static GridLength FromPixels(double pixels)
        {
            return new GridLength(pixels, GridUnitType.Pixel);
        }

        /// <summary>
        /// Creates a star-based GridLength for proportional sizing
        /// </summary>
        public static GridLength FromStars(double stars)
        {
            return new GridLength(stars, GridUnitType.Star);
        }

        /// <summary>
        /// Parses a string into a GridLength
        /// </summary>
        /// <param name="value">The string to parse (e.g., "Auto", "1*", "100px", "50")</param>
        /// <returns>A GridLength instance based on the parsed string</returns>
        public static GridLength Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or empty", nameof(value));

            value = value.Trim();

            if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
                return Auto;

            if (value.EndsWith("*"))
            {
                if (value.Length == 1)
                    return Star;

                if (double.TryParse(value.Substring(0, value.Length - 1), out double starValue))
                    return new GridLength(starValue, GridUnitType.Star);
            }
            else if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(value.Substring(0, value.Length - 2), out double pixelValue))
                    return new GridLength(pixelValue);
            }
            else if (double.TryParse(value, out double pixelValue))
            {
                return new GridLength(pixelValue);
            }

            throw new FormatException($"Cannot parse '{value}' as a GridLength");
        }

        /// <summary>
        /// Tries to parse a string into a GridLength
        /// </summary>
        public static bool TryParse(string value, out GridLength result)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch
            {
                result = Zero;
                return false;
            }
        }

        public override string ToString()
        {
            switch (_unitType)
            {
                case GridUnitType.Pixel:
                    return $"{_value}px";
                case GridUnitType.Auto:
                    return "Auto";
                case GridUnitType.Star:
                    return _value == 1 ? "*" : $"{_value}*";
                default:
                    return base.ToString();
            }
        }

        #region Equality Members

        public bool Equals(GridLength other)
        {
            if (_unitType != other._unitType)
                return false;

            // For Auto, actual value doesn't matter
            if (_unitType == GridUnitType.Auto)
                return true;

            return Math.Abs(_value - other._value) < 0.00001;
        }

        public override bool Equals(object obj)
        {
            return obj is GridLength other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, _unitType);
        }

        public static bool operator ==(GridLength left, GridLength right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridLength left, GridLength right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    /// <summary>
    /// Defines the unit type for a GridLength
    /// </summary>
    public enum GridUnitType
    {
        /// <summary>
        /// The value is expressed in pixels
        /// </summary>
        Pixel,

        /// <summary>
        /// The value is determined automatically based on the content
        /// </summary>
        Auto,

        /// <summary>
        /// The value is expressed as a weighted proportion of available space
        /// </summary>
        Star
    }

    /// <summary>
    /// Defines orientation options
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// Horizontal orientation
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical orientation
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Extension methods for easier GridPanel configuration
    /// </summary>
    public static class GridPanelExtensions
    {
        /// <summary>
        /// Sets the grid cell for a UIElement
        /// </summary>
        public static T SetGridCell<T>(this T element, int row, int column) where T : UIElement
        {
            if (element?.Parent is GridPanel panel)
            {
                panel.SetCell(element, row, column);
            }
            return element;
        }

        /// <summary>
        /// Sets the grid cell and span for a UIElement
        /// </summary>
        public static T SetGridCell<T>(this T element, int row, int column, int rowSpan, int columnSpan) where T : UIElement
        {
            if (element?.Parent is GridPanel panel)
            {
                panel.SetCell(element, row, column, rowSpan, columnSpan);
            }
            return element;
        }
    }
}