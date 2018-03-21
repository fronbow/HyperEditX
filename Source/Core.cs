using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using KSP.UI.Screens;
using UnityEngine;

[KSPAddon(KSPAddon.Startup.SpaceCentre, true)] // Determines when plugin starts.
public class HyperEditXModule : MonoBehaviour {
  static List<ApplicationLauncherButton> appListModHidden;

  public void Awake() // Called after scene (designated w/ KSPAddon) loads, but before Start().  Init data here.
  {
    HyperEditX.Immortal.AddImmortal<HyperEditX.HyperEditXBehaviour>();
  }

  private void Start() {
    // following needed to fix a stock bug
    appListModHidden = (List<ApplicationLauncherButton>) typeof(ApplicationLauncher).GetField("appListModHidden", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
    DontDestroyOnLoad(this);
  }

  double lasttimecheck = 0;
  GameScenes lastScene = GameScenes.LOADING;
  double lastTime = 0;

  private void FixedUpdate() {
    if ( HighLogic.LoadedScene != lastScene ) {
      lastScene = HighLogic.LoadedScene;
      lastTime = Time.fixedTime;
    }
    if ( Time.fixedTime - lastTime < 2 ) {
      if ( Time.fixedTime - lasttimecheck > .1 ) {
        lasttimecheck = Time.fixedTime;
        // following fixes a stock bug
        if ( appListModHidden.Contains(HyperEditX.HyperEditXBehaviour.appButton) ) {
          HyperEditX.HyperEditXBehaviour.appButton.gameObject.SetActive(false);
          if ( HyperEditX.HyperEditXBehaviour.appButton.enabled ) {
            HyperEditX.HyperEditXBehaviour.appButton.onDisable();
          }
        }
      }

    }
  }
}

namespace HyperEditX {
  public delegate bool TryParse<T>(string str, out T value);

  public static class Immortal {
    private static GameObject _gameObject;

    public static T AddImmortal<T>() where T : Component {
      if ( _gameObject == null ) {
        _gameObject = new GameObject("HyperEditXImmortal", typeof(T));
        UnityEngine.Object.DontDestroyOnLoad(_gameObject);
      }
      return _gameObject.GetComponent<T>() ?? _gameObject.AddComponent<T>();
    }
  }

  public class HyperEditXBehaviour : MonoBehaviour {
    private ConfigNode _hyperEditXConfig;
    private bool _useAppLauncherButton;
    private static ApplicationLauncherButton _appLauncherButton;
    private Action _createCoreView;
    private Action _createLanderView;
    private bool _autoOpenLanderValue;

    public static ApplicationLauncherButton appButton {
      get {
        return _appLauncherButton;
      }
    }

    /// <summary>
    /// Called after the scene loads but before Start().
    /// <para>Initialise data here.</para>
    /// (Used instead of class constructor)
    /// </summary>
    public void Awake() {
      UiHelper.Window.AreWindowsOpenChange += AreWindowsOpenChange;
      GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncher);
      GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveAppLauncher);
      GameEvents.onLevelWasLoaded.Add(SceneUpdate);
    }

    /// <summary>
    /// Called after Awake().
    /// </summary>
    public void Start() {
      ReloadConfig();
    }

    private void CreateCoreView() {
      ReloadConfig();
      if ( _createCoreView == null ) {
        _createCoreView = CorePanel.Create(this);
      }
      _createCoreView();
      if ( _autoOpenLanderValue == true && !UiHelper.Window.GameObject.GetComponents<UiHelper.Window>().Any(w => w.Title == "Lander") ) {
        CreateLanderView();
      }
    }

    private void CreateLanderView() {
      if ( _createLanderView == null ) {
        _createLanderView = Lander.Panel.Create();
      }
      _createLanderView();
    }

    // SceneUpdate() fires only when the scene changes.
    private void SceneUpdate(GameScenes data) {
      ReloadConfig();
      if ( HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION ) {
        if ( _autoOpenLanderValue == true && !UiHelper.Window.GameObject.GetComponents<UiHelper.Window>().Any(w => w.Title == "Lander") ) {
          CreateLanderView();
        }
      } else {
        UiHelper.Window.CloseAll();
      }
    }

    /// <summary>
    /// Fires every physics step.
    /// </summary>
    //public void FixedUpdate() => Planet.PlanetEditor.TryApplyFileDefaults();

    /// <summary>
    /// Fires every frame.
    /// </summary>
    public void Update() {
      RateLimitedLogger.Update();
      if ( (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.H) ) {
        // Linuxgurugamer added this scene check to keep HyperEdit off in the editors.
        if ( HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION ) {
          if ( UiHelper.Window.GameObject.GetComponents<UiHelper.Window>().Any(w => w.Title == "HyperEditX") ) {
            if ( _appLauncherButton == null ) {
              UiHelper.Window.CloseAll();
            } else {
              _appLauncherButton.SetFalse();
            }
          } else {
            if ( _appLauncherButton == null ) {
              CreateCoreView();
            } else {
              _appLauncherButton.SetTrue();
            }
          }
        }
      }
    }

    private void ReloadConfig() {
      var hypereditCfg = ConfigHelper.GetPath("hypereditX.cfg");
      if ( System.IO.File.Exists(hypereditCfg) ) {
        _hyperEditXConfig = ConfigNode.Load(hypereditCfg);
        _hyperEditXConfig.name = "hypereditX";
      } else {
        _hyperEditXConfig = new ConfigNode("hypereditX");
        _hyperEditXConfig.SetValue("AutoOpenLander", false.ToString(), true);
      }

      var launcherButtonValue = true;
      _hyperEditXConfig.TryGetValue("UseAppLauncherButton", ref launcherButtonValue, bool.TryParse);
      UseAppLauncherButton = launcherButtonValue;

      _hyperEditXConfig.TryGetValue("AutoOpenLander", ref _autoOpenLanderValue, bool.TryParse);
    }

    private void AreWindowsOpenChange(bool isOpen) {
      if ( _appLauncherButton != null ) {
        if ( isOpen ) {
          _appLauncherButton.SetTrue(false);
        } else {
          _appLauncherButton.SetFalse(false);
        }
      }
    }

    public bool UseAppLauncherButton {
      get {
        return _useAppLauncherButton;
      }
      set {
        if ( _useAppLauncherButton == value )
          return;
        _useAppLauncherButton = value;
        if ( value ) {
          AddAppLauncher();
        } else {
          RemoveAppLauncher();
        }
        _hyperEditXConfig.SetValue("UseAppLauncherButton", value.ToString(), true);
        _hyperEditXConfig.Save();
      }
    }

    private void AddAppLauncher() {
      if ( _useAppLauncherButton == false )
        return;
      if ( _appLauncherButton != null ) {
        Utils.Log("Not adding to ApplicationLauncher, button already exists (yet onGUIApplicationLauncherReady was called?)");
        return;
      }
      var applauncher = ApplicationLauncher.Instance;
      if ( applauncher == null ) {
        Utils.Log("Cannot add to ApplicationLauncher, instance was null");
        return;
      }
      const ApplicationLauncher.AppScenes scenes =
        ApplicationLauncher.AppScenes.FLIGHT |
        ApplicationLauncher.AppScenes.MAPVIEW |
        ApplicationLauncher.AppScenes.TRACKSTATION;

      //GetTexture is always relative to GameData, and shouldn't include the file extension
      Texture launchButtonTexture = GameDatabase.Instance.GetTexture("HyperEditX/hex-icon", asNormalMap: false);

      _appLauncherButton = applauncher.AddModApplication(
        CreateCoreView, // onTrue
        UiHelper.Window.CloseAll, // onFalse
        () => {
        }, // onHover
        () => {
        }, // onHoverOut
        () => {
        }, // onEnable
        () => {
        }, // onDisable
        scenes, // visibleInScenes
        launchButtonTexture // texture
      );
    }

    private void RemoveAppLauncher() {
      var applauncher = ApplicationLauncher.Instance;
      if ( applauncher == null ) {
        Utils.Log("Cannot remove from ApplicationLauncher, instance was null");
        return;
      }
      if ( _appLauncherButton == null ) {
        return;
      }
      applauncher.RemoveModApplication(_appLauncherButton);
      _appLauncherButton = null;
    }

    // End of class.
  }

  public static class CorePanel {
    public static Action Create(HyperEditXBehaviour hyperEditX) {
      var view = View(hyperEditX);
      return () => UiHelper.Window.Create("HyperEditX", true, true, 120, -1, w => view.Draw());
    }

    public static UiHelper.IView View(HyperEditXBehaviour hyperEditX) {
      var orbitEditorView = Orbits.Panel.Create();
      var landerView = Lander.Panel.Create();
      var resourceView = Resources.Panel.Create();
      var aboutView = About.Panel.Create();
      var settingsView = Settings.Panel.Create();

      var closeAll = new UiHelper.ButtonView("Close all", "Closes all windows", UiHelper.Window.CloseAll);
      var orbitEditor = new UiHelper.ButtonView("Orbits", "Opens the Orbit Editor window", orbitEditorView);
      var shipLander = new UiHelper.ButtonView("Landing", "Opens the Vessel Tools window", landerView);
      var resources = new UiHelper.ButtonView("Resources", "Opens the Resources window", resourceView);
      //var debugMenu = new ButtonView("KSP Debug Menu", "Opens the KSP Debug Toolbar (also available with Mod+F12)", () => DebugToolbar.toolbarShown = true); // !DebugToolbar.toolbarShown);
      var about = new UiHelper.ButtonView("About", "Opens the About window", aboutView);
      //var appLauncher = new UiHelper.DynamicToggleView("App Button", "Enables or disables the AppLauncher button (top right H button)", () => hyperEditX.UseAppLauncherButton, () => true, v => hyperEditX.UseAppLauncherButton = v);
      var settings = new UiHelper.ButtonView("Settings", "The plugin configuration", settingsView);

      return new UiHelper.VerticalView(new UiHelper.IView[] {
        closeAll,
        orbitEditor,
        shipLander,
        resources,
        settings,
        //debugMenu,
        about,
        //appLauncher
      });
    }
  }



}