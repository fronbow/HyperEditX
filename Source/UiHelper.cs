using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEditX.UiHelper {
  public interface IView {
    void Draw();
  }

  public class CustomView : IView {
    private readonly Action _draw;

    public CustomView(Action draw) {
      _draw = draw;
    }

    public void Draw() {
      _draw();
    }
  }

  public class ConditionalView : IView {
    private readonly Func<bool> _doDisplay;
    private readonly IView _toDisplay;

    public ConditionalView(Func<bool> doDisplay, IView toDisplay) {
      _doDisplay = doDisplay;
      _toDisplay = toDisplay;
    }

    public void Draw() {
      if (_doDisplay())
        _toDisplay.Draw();
    }
  }

  public class LabelView : IView {
    private readonly GUIContent _label;

    public LabelView(string label, string help) {
      _label = new GUIContent(label, help);
    }

    public void Draw() {
      GUILayout.Label(_label);
    }
  }

  public class VerticalView : IView {
    private readonly ICollection<IView> _views;

    public VerticalView(ICollection<IView> views) {
      _views = views;
    }

    public void Draw() {
      GUILayout.BeginVertical();
      foreach (var view in _views) {
        view.Draw();
      }
      GUILayout.EndVertical();
    }
  }

  public class ButtonView : IView {
    private readonly GUIContent _label;
    private readonly Action _onChange;

    public ButtonView(string label, string help, Action onChange) {
      _label = new GUIContent(label, help);
      _onChange = onChange;
    }

    public void Draw() {
      if (GUILayout.Button(_label)) {
        _onChange();
        Utils.ClearGuiFocus();
      }
    }
  }

  public class ToggleView : IView {
    private readonly GUIContent _label;
    private readonly Action<bool> _onChange;

    public bool Value { get; set; }

    public ToggleView(string label, string help, bool initialValue, Action<bool> onChange = null) {
      _label = new GUIContent(label, help);
      Value = initialValue;
      _onChange = onChange;
    }

    public void Draw() {
      var oldValue = Value;
      Value = GUILayout.Toggle(oldValue, _label);
      if (oldValue != Value && _onChange != null) {
        _onChange(Value);
        Utils.ClearGuiFocus();
      }
    }
  }

  public class DynamicToggleView : IView {
    private readonly GUIContent _label;
    private readonly Func<bool> _getValue;
    private readonly Func<bool> _isValid;
    private readonly Action<bool> _onChange;

    public DynamicToggleView(string label, string help, Func<bool> getValue, Func<bool> isValid,
        Action<bool> onChange) {
      _label = new GUIContent(label, help);
      _getValue = getValue;
      _isValid = isValid;
      _onChange = onChange;
    }

    public void Draw() {
      var oldValue = _getValue();
      var newValue = GUILayout.Toggle(oldValue, _label);
      if (oldValue != newValue && _isValid()) {
        _onChange(newValue);
        Utils.ClearGuiFocus();
      }
    }
  }

  public class DynamicSliderView : IView {
    private readonly Action<double> _onChange;
    private readonly GUIContent _label;
    private readonly Func<double> _get;

    public DynamicSliderView(string label, string help, Func<double> get, Action<double> onChange) {
      _onChange = onChange;
      _label = new GUIContent(label, help);
      _get = get;
    }

    public void Draw() {
      GUILayout.BeginHorizontal();
      GUILayout.Label(_label);
      var oldValue = _get();
      var newValue = (double)GUILayout.HorizontalSlider((float)oldValue, 0, 1);
      if (Math.Abs(newValue - oldValue) > 0.001) {
        _onChange?.Invoke(newValue);
        Utils.ClearGuiFocus();
      }
      GUILayout.EndHorizontal();
    }
  }

  public class SliderView : IView {
    private readonly Action<double> _onChange;
    private readonly GUIContent _label;

    public double Value { get; set; }

    public SliderView(string label, string help, Action<double> onChange = null) {
      _onChange = onChange;
      _label = new GUIContent(label, help);
      Value = 0;
    }

    public void Draw() {
      GUILayout.BeginHorizontal();
      GUILayout.Label(_label);
      var newValue = (double)GUILayout.HorizontalSlider((float)Value, 0, 1);
      if (Math.Abs(newValue - Value) > 0.001) {
        Value = newValue;
        _onChange?.Invoke(Value);
        Utils.ClearGuiFocus();
      }
      GUILayout.EndHorizontal();
    }
  }

  public class ListSelectView<T> : IView {
    private readonly string _prefix;
    private readonly Func<IEnumerable<T>> _list;
    private readonly Func<T, string> _toString;
    private readonly Action<T> _onSelect;
    private T _currentlySelected;

    public T CurrentlySelected {
      get { return _currentlySelected; }
      set {
        _currentlySelected = value;
        _onSelect?.Invoke(value);
      }
    }

    public void ReInvokeOnSelect() {
      _onSelect?.Invoke(_currentlySelected);
    }

    public ListSelectView(string prefix, Func<IEnumerable<T>> list, Action<T> onSelect = null,
        Func<T, string> toString = null) {
      _prefix = prefix + ": ";
      _list = list;
      _toString = toString ?? (x => x.ToString());
      _onSelect = onSelect;
      _currentlySelected = default(T);
    }

    public void Draw() {
      GUILayout.BeginHorizontal();
      GUILayout.Label(_prefix + (_currentlySelected == null ? "<none>" : _toString(_currentlySelected)));
      if (GUILayout.Button("Select")) {
        Utils.ClearGuiFocus();
        var realList = _list();
        if (realList != null)
          WindowHelper.Selector("Select", realList, _toString, t => CurrentlySelected = t);
      }
      GUILayout.EndHorizontal();
    }
  }

  public class TextBoxView<T> : IView {
    private readonly GUIContent _label;
    private readonly TryParse<T> _parser;
    private readonly Func<T, string> _toString;
    private readonly Action<T> _onSet;
    private string _value;
    private T _obj;

    public bool Valid { get; private set; }

    public T Object {
      get { return _obj; }
      set {
        _value = _toString(value);
        _obj = value;
      }
    }

    public TextBoxView(string label, string help, T start, TryParse<T> parser, Func<T, string> toString = null,
        Action<T> onSet = null) {
      _label = label == null ? null : new GUIContent(label, help);
      _toString = toString ?? (x => x.ToString());
      _value = _toString(start);
      _parser = parser;
      _onSet = onSet;
    }

    public void Draw() {
      if (_label != null || _onSet != null) {
        GUILayout.BeginHorizontal();
        if (_label != null)
          GUILayout.Label(_label);
      }

      T tempValue;
      Valid = _parser(_value, out tempValue);

      if (Valid) {
        _value = GUILayout.TextField(_value);
        _obj = tempValue;
      } else {
        var color = GUI.color;
        GUI.color = Color.red;
        _value = GUILayout.TextField(_value);
        GUI.color = color;
      }
      if (_label != null || _onSet != null) {
        if (_onSet != null && Valid && GUILayout.Button("Set")) {
          _onSet(Object);
          Utils.ClearGuiFocus();
        }
        GUILayout.EndHorizontal();
      }
    }
  }

  public class TabView : IView {
    private readonly List<KeyValuePair<string, IView>> _views;
    private KeyValuePair<string, IView> _current;

    public TabView(List<KeyValuePair<string, IView>> views) {
      _views = views;
      _current = views[0];
    }

    public void Draw() {
      GUILayout.BeginHorizontal();
      foreach (var view in _views) {
        if (view.Key == _current.Key) {
          GUILayout.Button(view.Key, Utils.PressedButton);
        } else {
          if (GUILayout.Button(view.Key)) {
            _current = view;
            Utils.ClearGuiFocus();
          }
        }
      }
      GUILayout.EndHorizontal();
      _current.Value.Draw();
    }
  }

  public static class WindowHelper {
    public static void Prompt(string prompt, Action<string> complete) {
      var str = "";
      Window.Create(prompt, false, false, 200, 100, w => {
        str = GUILayout.TextField(str);
        if (GUILayout.Button("OK")) {
          complete(str);
          w.Close();
        }
      });
    }

    public static void Error(string message) {
      Window.Create("Error", false, false, 400, -1, w => {
        GUILayout.Label(message);
        if (GUILayout.Button("OK")) {
          w.Close();
        }
      });
    }

    public static void Selector<T>(string title, IEnumerable<T> elements, Func<T, string> nameSelector,
        Action<T> onSelect) {
      var collection = elements.Select(t => new { value = t, name = nameSelector(t) }).ToList();
      var scrollPos = new Vector2();
      Window.Create(title, false, false, 300, 500, w => {
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        foreach (var item in collection) {
          if (GUILayout.Button(item.name)) {
            onSelect(item.value);
            w.Close();
            return;
          }
        }
        GUILayout.EndScrollView();
      });
    }
  }

  public class Window : MonoBehaviour {
    private static GameObject _gameObject;

    internal static GameObject GameObject {
      get {
        if (_gameObject == null) {
          _gameObject = new GameObject("HyperEditXWindowManager");
          DontDestroyOnLoad(_gameObject);
        }
        return _gameObject;
      }
    }

    private static ConfigNode _windowPos;

    private static ConfigNode WindowPos {
      get {
        if (_windowPos != null)
          return _windowPos;
        var fp = ConfigHelper.GetPath("windowpos.cfg");
        if (System.IO.File.Exists(fp)) {
          _windowPos = ConfigNode.Load(fp);
          _windowPos.name = "windowpos";
        } else
          _windowPos = new ConfigNode("windowpos");
        return _windowPos;
      }
    }

    private static void SaveWindowPos() {
      WindowPos.Save();
    }

    public static event Action<bool> AreWindowsOpenChange;

    private string _tempTooltip;
    private string _oldTooltip;
    internal string Title;
    private bool _shrinkHeight;
    private Rect _windowRect;
    private Action<Window> _drawFunc;
    private bool _isOpen;

    public static void Create(string title, bool savepos, bool ensureUniqueTitle, int width, int height,
        Action<Window> drawFunc) {
      var allOpenWindows = GameObject.GetComponents<Window>();
      if (ensureUniqueTitle && allOpenWindows.Any(w => w.Title == title)) {
        Utils.Log("Not opening window \"" + title + "\", already open");
        return;
      }

      var winx = 100;
      var winy = 100;
      if (savepos) {
        var winposNode = WindowPos.GetNode(title.Replace(' ', '_'));
        if (winposNode != null) {
          winposNode.TryGetValue("x", ref winx, int.TryParse);
          winposNode.TryGetValue("y", ref winy, int.TryParse);
        } else {
          Utils.Log("No winpos found for \"" + title + "\", defaulting to " + winx + "," + winy);
        }
        if (winx >= Screen.width - width)
          winx = Screen.width - width;
        if (height == -1) {
          if (winy >= Screen.height - 100)
            winy = (Screen.height - 100) / 2;
        } else {
          if (winy > Screen.height - height)
            winy = Screen.height - height;
        }
        Utils.Log("Screen.width: " + Screen.width.ToString() + " width: " + width.ToString() + "   winx: " + winx.ToString());
        Utils.Log("Screen.height: " + Screen.height.ToString() + " height: " + height.ToString() + "   winy: " + winy.ToString());
      } else {
        winx = (Screen.width - width) / 2;
        winy = (Screen.height - height) / 2;
      }

      var window = GameObject.AddComponent<Window>();
      window._isOpen = true;
      window._shrinkHeight = height == -1;
      if (window._shrinkHeight)
        height = 5;
      window.Title = title;
      window._windowRect = new Rect(winx, winy, width, height);
      window._drawFunc = drawFunc;
      if (allOpenWindows.Length == 0)
        AreWindowsOpenChange?.Invoke(true);
    }

    private Window() {
      GameEvents.onScreenResolutionModified.Add(OnScreenResolutionModified);
    }

    void OnScreenResolutionModified(int x, int y) {
      if (this._windowRect.y >= Screen.height)
        _windowRect.y = Screen.height - _windowRect.height;
      if (this._windowRect.x >= Screen.width)
        _windowRect.x = Screen.width - _windowRect.width;

    }

    public void Update() {
      if (_shrinkHeight)
        _windowRect.height = 5;
      _oldTooltip = _tempTooltip;
    }

    public void OnGUI() {
      GUI.skin = HighLogic.Skin;
      _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, Title, GUILayout.ExpandHeight(true));

      if (string.IsNullOrEmpty(_oldTooltip))
        return;
      var rect = new Rect(_windowRect.xMin, _windowRect.yMax, _windowRect.width, 50);
      GUI.Label(rect, _oldTooltip);
    }

    private void DrawWindow(int windowId) {
      GUILayout.BeginVertical();
      if (GUI.Button(new Rect(_windowRect.width - 18, 2, 16, 16), "X")) // X button from mechjeb
        Close();
      _drawFunc(this);

      _tempTooltip = GUI.tooltip;

      GUILayout.EndVertical();
      GUI.DragWindow();
    }

    public void Close() {
      var node = new ConfigNode(Title.Replace(' ', '_'));
      node.AddValue("x", (int)_windowRect.x);
      node.AddValue("y", (int)_windowRect.y);
      if (WindowPos.SetNode(node.name, node) == false)
        WindowPos.AddNode(node);
      SaveWindowPos();
      _isOpen = false;
      GameEvents.onScreenResolutionModified.Remove(OnScreenResolutionModified);
      Destroy(this);
      if (GameObject.GetComponents<Window>().Any(w => w._isOpen) == false)
        AreWindowsOpenChange?.Invoke(false);
    }

    internal static void CloseAll() {
      foreach (var window in GameObject.GetComponents<Window>())
        window.Close();
    }
  }
}
