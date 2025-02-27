using System;
using System.Collections.Generic;
using System.Drawing;

namespace GalaxyUI.UIElement
{
    /// <summary>
    /// Base class for all UI elements in GalaxyUI
    /// </summary>
    public abstract class UIElement
    {
        private Point _location;
        private Size _size;
        private Padding _margin;
        private Padding _padding;
        private string _name;
        private UIElement _parent;
        private List<UIElement> _children;
        private bool _isVisible = true;
        private bool _isEnabled = true;

        public UIElement()
        {
            _children = new List<UIElement>();
            _margin = new Padding(0);
            _padding = new Padding(5);
            _size = new Size(100, 100);
            _location = new Point(0, 0);
        }

        #region Properties

        public Point Location
        {
            get => _location;
            set
            {
                if (_location != value)
                {
                    _location = value;
                    OnLocationChanged();
                }
            }
        }

        public int X
        {
            get => _location.X;
            set
            {
                if (_location.X != value)
                {
                    _location = new Point(value, _location.Y);
                    OnLocationChanged();
                }
            }
        }

        public int Y
        {
            get => _location.Y;
            set
            {
                if (_location.Y != value)
                {
                    _location = new Point(_location.X, value);
                    OnLocationChanged();
                }
            }
        }

        public Size Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnSizeChanged();
                }
            }
        }

        public int Width
        {
            get => _size.Width;
            set
            {
                if (_size.Width != value)
                {
                    _size = new Size(value, _size.Height);
                    OnSizeChanged();
                }
            }
        }

        public int Height
        {
            get => _size.Height;
            set
            {
                if (_size.Height != value)
                {
                    _size = new Size(_size.Width, value);
                    OnSizeChanged();
                }
            }
        }

        public Padding Margin
        {
            get => _margin;
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    OnLayoutChanged();
                }
            }
        }

        public Padding Padding
        {
            get => _padding;
            set
            {
                if (_padding != value)
                {
                    _padding = value;
                    OnLayoutChanged();
                }
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public UIElement Parent
        {
            get => _parent;
            internal set => _parent = value;
        }

        public IReadOnlyList<UIElement> Children => _children.AsReadOnly();

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnVisibilityChanged();
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnEnabledChanged();
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler LocationChanged;
        public event EventHandler SizeChanged;
        public event EventHandler LayoutChanged;
        public event EventHandler VisibilityChanged;
        public event EventHandler EnabledChanged;

        protected virtual void OnLocationChanged()
        {
            LocationChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        protected virtual void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        protected virtual void OnLayoutChanged()
        {
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        protected virtual void OnVisibilityChanged()
        {
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        protected virtual void OnEnabledChanged()
        {
            EnabledChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        #endregion

        #region Child Management

        public void Add(UIElement child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (child.Parent != null)
                child.Parent.Remove(child);

            _children.Add(child);
            child.Parent = this;
            InvalidateVisual();
        }

        public void Remove(UIElement child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (_children.Remove(child))
            {
                child.Parent = null;
                InvalidateVisual();
            }
        }

        public void Clear()
        {
            foreach (var child in _children.ToArray())
            {
                Remove(child);
            }
        }

        #endregion

        #region Rendering

        protected virtual void OnRender(RenderContext context)
        {
            if (!_isVisible)
                return;

            // Render children
            foreach (var child in _children)
            {
                child.OnRender(context);
            }
        }

        protected void InvalidateVisual()
        {
            // Request a redraw from the parent window
            Window window = FindParentWindow();
            window?.Invalidate();
        }

        private Window FindParentWindow()
        {
            UIElement current = this;
            while (current != null)
            {
                if (current is Window window)
                    return window;

                current = current.Parent;
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Represents padding or margin values
    /// </summary>
    public struct Padding
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }

        public Padding(int all)
        {
            Left = Top = Right = Bottom = all;
        }

        public Padding(int horizontal, int vertical)
        {
            Left = Right = horizontal;
            Top = Bottom = vertical;
        }

        public Padding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static bool operator ==(Padding a, Padding b)
        {
            return a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;
        }

        public static bool operator !=(Padding a, Padding b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Padding))
                return false;

            Padding other = (Padding)obj;
            return this == other;
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() ^ Top.GetHashCode() ^ Right.GetHashCode() ^ Bottom.GetHashCode();
        }
    }

    /// <summary>
    /// A container that arranges its children in a grid
    /// </summary>
    public class Panel : UIElement
    {
        private Color _backgroundColor = Color.Transparent;
        private CornerRadius _cornerRadius = new CornerRadius(0);
        private bool _hasDropShadow = false;

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public CornerRadius CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        public bool HasDropShadow
        {
            get => _hasDropShadow;
            set
            {
                if (_hasDropShadow != value)
                {
                    _hasDropShadow = value;
                    InvalidateVisual();
                }
            }
        }

        protected override void OnRender(RenderContext context)
        {
            if (!IsVisible)
                return;

            // Render panel background
            if (_backgroundColor != Color.Transparent)
            {
                // Draw background with rounded corners if specified
                // In a real implementation, this would use actual drawing calls
            }

            if (_hasDropShadow)
            {
                // Draw drop shadow
                // In a real implementation, this would use actual shadow rendering
            }

            // Render children
            base.OnRender(context);
        }
    }

    /// <summary>
    /// Represents a button control
    /// </summary>
    public class Button : UIElement
    {
        private string _text;
        private Color _foregroundColor = Color.Black;
        private Color _backgroundColor = Color.LightGray;
        private Color _hoverColor = Color.Gray;
        private Color _pressedColor = Color.DarkGray;
        private CornerRadius _cornerRadius = new CornerRadius(4);
        private bool _isHovered = false;
        private bool _isPressed = false;

        public Button(string text = "Button")
        {
            _text = text;
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    InvalidateVisual();
                }
            }
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color HoverColor
        {
            get => _hoverColor;
            set
            {
                if (_hoverColor != value)
                {
                    _hoverColor = value;
                    if (_isHovered)
                        InvalidateVisual();
                }
            }
        }

        public Color PressedColor
        {
            get => _pressedColor;
            set
            {
                if (_pressedColor != value)
                {
                    _pressedColor = value;
                    if (_isPressed)
                        InvalidateVisual();
                }
            }
        }

        public CornerRadius CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        public event EventHandler Click;

        protected override void OnRender(RenderContext context)
        {
            if (!IsVisible)
                return;

            // Determine background color based on button state
            Color bgColor = _backgroundColor;
            if (!IsEnabled)
            {
                bgColor = Color.FromArgb(bgColor.A / 2, bgColor.R, bgColor.G, bgColor.B);
            }
            else if (_isPressed)
            {
                bgColor = _pressedColor;
            }
            else if (_isHovered)
            {
                bgColor = _hoverColor;
            }

            // In a real implementation, draw the button background, border, and text

            base.OnRender(context);
        }

        internal void OnMouseEnter()
        {
            if (IsEnabled && !_isHovered)
            {
                _isHovered = true;
                InvalidateVisual();
            }
        }

        internal void OnMouseLeave()
        {
            if (_isHovered)
            {
                _isHovered = false;
                InvalidateVisual();
            }
        }

        internal void OnMouseDown()
        {
            if (IsEnabled && !_isPressed)
            {
                _isPressed = true;
                InvalidateVisual();
            }
        }

        internal void OnMouseUp()
        {
            if (_isPressed)
            {
                _isPressed = false;
                InvalidateVisual();

                if (IsEnabled)
                {
                    OnClick();
                }
            }
        }

        protected virtual void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Represents a label for displaying text
    /// </summary>
    public class Label : UIElement
    {
        private string _text;
        private Color _foregroundColor = Color.Black;
        private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
        private VerticalAlignment _verticalAlignment = VerticalAlignment.Center;

        public Label(string text = "")
        {
            _text = text;
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    InvalidateVisual();
                }
            }
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                if (_horizontalAlignment != value)
                {
                    _horizontalAlignment = value;
                    InvalidateVisual();
                }
            }
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                if (_verticalAlignment != value)
                {
                    _verticalAlignment = value;
                    InvalidateVisual();
                }
            }
        }

        protected override void OnRender(RenderContext context)
        {
            if (!IsVisible)
                return;

            // In a real implementation, this would draw text with proper alignment

            base.OnRender(context);
        }
    }

    /// <summary>
    /// A text input control
    /// </summary>
    public class TextBox : UIElement
    {
        private string _text = "";
        private string _placeholder = "";
        private Color _foregroundColor = Color.Black;
        private Color _backgroundColor = Color.White;
        private Color _borderColor = Color.Gray;
        private Color _placeholderColor = Color.LightGray;
        private CornerRadius _cornerRadius = new CornerRadius(4);
        private bool _isFocused = false;
        private int _caretPosition = 0;
        private int _selectionStart = -1;
        private int _selectionLength = 0;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    string oldValue = _text;
                    _text = value ?? "";
                    OnTextChanged(oldValue, _text);
                    InvalidateVisual();
                }
            }
        }

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                if (_placeholder != value)
                {
                    _placeholder = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color PlaceholderColor
        {
            get => _placeholderColor;
            set
            {
                if (_placeholderColor != value)
                {
                    _placeholderColor = value;
                    InvalidateVisual();
                }
            }
        }

        public CornerRadius CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        public bool IsFocused
        {
            get => _isFocused;
            internal set
            {
                if (_isFocused != value)
                {
                    _isFocused = value;
                    OnFocusChanged();
                    InvalidateVisual();
                }
            }
        }

        public int CaretPosition
        {
            get => _caretPosition;
            set
            {
                int newPosition = Math.Max(0, Math.Min(_text.Length, value));
                if (_caretPosition != newPosition)
                {
                    _caretPosition = newPosition;
                    InvalidateVisual();
                }
            }
        }

        public event EventHandler<TextChangedEventArgs> TextChanged;
        public event EventHandler FocusChanged;

        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
            TextChanged?.Invoke(this, new TextChangedEventArgs(oldValue, newValue));
        }

        protected virtual void OnFocusChanged()
        {
            FocusChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void OnKeyPress(char key)
        {
            if (!IsEnabled || !_isFocused)
                return;

            if (_selectionStart >= 0)
            {
                // Replace selection with the typed character
                ReplaceSelection(key.ToString());
            }
            else
            {
                // Insert character at caret position
                _text = _text.Insert(_caretPosition, key.ToString());
                _caretPosition++;
                OnTextChanged(_text, _text);
                InvalidateVisual();
            }
        }

        internal void OnKeyDown(Key key)
        {
            if (!IsEnabled || !_isFocused)
                return;

            switch (key)
            {
                case Key.Left:
                    if (_caretPosition > 0)
                    {
                        _caretPosition--;
                        ClearSelection();
                        InvalidateVisual();
                    }
                    break;

                case Key.Right:
                    if (_caretPosition < _text.Length)
                    {
                        _caretPosition++;
                        ClearSelection();
                        InvalidateVisual();
                    }
                    break;

                case Key.Home:
                    _caretPosition = 0;
                    ClearSelection();
                    InvalidateVisual();
                    break;

                case Key.End:
                    _caretPosition = _text.Length;
                    ClearSelection();
                    InvalidateVisual();
                    break;

                case Key.Delete:
                    if (_selectionStart >= 0)
                    {
                        ReplaceSelection("");
                    }
                    else if (_caretPosition < _text.Length)
                    {
                        _text = _text.Remove(_caretPosition, 1);
                        OnTextChanged(_text, _text);
                        InvalidateVisual();
                    }
                    break;

                case Key.Back:
                    if (_selectionStart >= 0)
                    {
                        ReplaceSelection("");
                    }
                    else if (_caretPosition > 0)
                    {
                        _caretPosition--;
                        _text = _text.Remove(_caretPosition, 1);
                        OnTextChanged(_text, _text);
                        InvalidateVisual();
                    }
                    break;
            }
        }

        private void ReplaceSelection(string replacement)
        {
            if (_selectionStart < 0)
                return;

            int start = Math.Min(_selectionStart, _selectionStart + _selectionLength);
            int length = Math.Abs(_selectionLength);

            string oldText = _text;
            _text = _text.Substring(0, start) + replacement + _text.Substring(start + length);

            _caretPosition = start + replacement.Length;
            ClearSelection();

            OnTextChanged(oldText, _text);
            InvalidateVisual();
        }

        private void ClearSelection()
        {
            _selectionStart = -1;
            _selectionLength = 0;
        }

        protected override void OnRender(RenderContext context)
        {
            if (!IsVisible)
                return;

            // In a real implementation, this would:
            // 1. Draw the text box background and border
            // 2. Draw the text or placeholder
            // 3. Draw the caret if focused
            // 4. Draw the selection if any

            base.OnRender(context);
        }
    }

    /// <summary>
    /// A control for selecting an option from a list
    /// </summary>
    public class ComboBox : UIElement
    {
        private List<string> _items = new List<string>();
        private int _selectedIndex = -1;
        private bool _isDropDownOpen = false;
        private Color _foregroundColor = Color.Black;
        private Color _backgroundColor = Color.White;
        private Color _borderColor = Color.Gray;
        private CornerRadius _cornerRadius = new CornerRadius(4);

        public IReadOnlyList<string> Items => _items.AsReadOnly();

        public void AddItem(string item)
        {
            if (item != null)
            {
                _items.Add(item);
                InvalidateVisual();
            }
        }

        public void RemoveItem(string item)
        {
            if (_items.Remove(item))
            {
                if (_selectedIndex >= _items.Count)
                {
                    SelectedIndex = _items.Count - 1;
                }
                InvalidateVisual();
            }
        }

        public void ClearItems()
        {
            _items.Clear();
            SelectedIndex = -1;
            InvalidateVisual();
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int newIndex = (value >= -1 && value < _items.Count) ? value : -1;
                if (_selectedIndex != newIndex)
                {
                    _selectedIndex = newIndex;
                    OnSelectionChanged();
                    InvalidateVisual();
                }
            }
        }

        public string SelectedItem
        {
            get => (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;
            set
            {
                int index = _items.IndexOf(value);
                SelectedIndex = index;
            }
        }

        public bool IsDropDownOpen
        {
            get => _isDropDownOpen;
            set
            {
                if (_isDropDownOpen != value)
                {
                    _isDropDownOpen = value;
                    InvalidateVisual();
                }
            }
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    InvalidateVisual();
                }
            }
        }

        public CornerRadius CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    InvalidateVisual();
                }
            }
        }

        public event EventHandler SelectionChanged;

        protected virtual void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnRender(RenderContext context)
        {
            if (!IsVisible)
                return;

            // In a real implementation, this would:
            // 1. Draw the combo box main area with the selected item
            // 2. Draw a dropdown button/arrow
            // 3. If dropdown is open, draw the list of items

            base.OnRender(context);
        }
    }

    /// <summary>
    /// A control that allows selection between on and off states
    /// </summary>
    public class CheckBox : UIElement
    {
        private bool _isChecked = false;
        private string _text = "";
        private Color _foregroundColor = Color.Black;
        private Color _checkColor = Color.Blue;
        private Color _boxColor = Color.White;
        private Color _borderColor = Color.Gray;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged();
                    InvalidateVisual();
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    InvalidateVisual();
                }
            }
        }

        public Color ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color CheckColor
        {
            get => _checkColor;
            set
            {
                if (_checkColor != value)
                {
                    _checkColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BoxColor
        {
            get => _boxColor;
            set
            {
                if (_boxColor != value)
                {
                    _boxColor = value;
                    InvalidateVisual();
                }
            }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    InvalidateVisual();
                }
            }
        }

        public event EventHandler CheckedChanged;

        protected virtual void OnCheckedChanged()
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void Toggle()
        {
            if (IsEnabled)
            {
                IsChecked = !IsChecked;
            }
        }

        protected override void OnRender(RenderContext context)
        {
            if (!IsVisible)
                return;

            // In a real implementation, this would:
            // 1. Draw the checkbox box
            // 2. Draw the check mark if checked
            // 3. Draw the text label

            base.OnRender(context);
        }
    }

    /// <summary>
    /// Defines horizontal alignment options
    /// </summary>
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right,
        Stretch
    }

    /// <summary>
    /// Defines vertical alignment options
    /// </summary>
    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom,
        Stretch
    }

    /// <summary>
    /// Represents rounded corners for UI elements
    /// </summary>
    public struct CornerRadius
    {
        public int TopLeft { get; set; }
        public int TopRight { get; set; }
        public int BottomRight { get; set; }
        public int BottomLeft { get; set; }

        public CornerRadius(int all)
        {
            TopLeft = TopRight = BottomRight = BottomLeft = all;
        }

        public CornerRadius(int topLeft, int topRight, int bottomRight, int bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        public static bool operator ==(CornerRadius a, CornerRadius b)
        {
            return a.TopLeft == b.TopLeft && a.TopRight == b.TopRight &&
                   a.BottomRight == b.BottomRight && a.BottomLeft == b.BottomLeft;
        }

        public static bool operator !=(CornerRadius a, CornerRadius b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CornerRadius))
                return false;

            CornerRadius other = (CornerRadius)obj;
            return this == other;
        }

        public override int GetHashCode()
        {
            return TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^
                   BottomRight.GetHashCode() ^ BottomLeft.GetHashCode();
        }
    }

    /// <summary>
    /// Defines keyboard keys for input handling
    /// </summary>
    public enum Key
    {
        None,
        Back,
        Tab,
        Enter,
        Shift,
        Control,
        Alt,
        CapsLock,
        Escape,
        Space,
        PageUp,
        PageDown,
        End,
        Home,
        Left,
        Up,
        Right,
        Down,
        Insert,
        Delete,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9
    }

    /// <summary>
    /// Event arguments for text changed events
    /// </summary>
    public class TextChangedEventArgs : EventArgs
    {
        public string OldText { get; }
        public string NewText { get; }

        public TextChangedEventArgs(string oldText, string newText)
        {
            OldText = oldText;
            NewText = newText;
        }
    }
}