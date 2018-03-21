using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Separate the resources into their own panel.

namespace HyperEditX.Resources {

  public static class ResourcePanel {
    public static void RefillVesselResources() {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
        return;
      RefillVesselResources(FlightGlobals.ActiveVessel);
    }

    public static IEnumerable<KeyValuePair<string, double>> GetResources() {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
        return new KeyValuePair<string, double>[0];
      return GetResources(FlightGlobals.ActiveVessel);
    }

    public static IEnumerable<KeyValuePair<string, double>> GetResources(Vessel vessel) {
      if (vessel.parts == null)
        return new KeyValuePair<string, double>[0];
      return vessel.parts
        .SelectMany(part => part.Resources.Cast<PartResource>())
        .GroupBy(p => p.resourceName)
        .Select(g => new KeyValuePair<string, double>(g.Key, g.Sum(x => x.amount) / g.Sum(x => x.maxAmount)));
    }

    public static void SetResource(string key, double value) {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
        return;
      SetResource(FlightGlobals.ActiveVessel, key, value);
    }

    private static readonly object SetResourceLogObject = new object();

    private static void SetResource(Vessel vessel, string key, double value) {
      if (vessel.parts == null)
        return;
      foreach (var part in vessel.parts) {
        //foreach(PartResource resource in part.Resources)
        int resourceCount = part.Resources.Count;
        for (int i = 0; i < resourceCount; ++i) {
          PartResource resource = part.Resources[i];
          if (resource.resourceName == key) {
            part.TransferResource(resource.info.id, resource.maxAmount * value - resource.amount);
            RateLimitedLogger.Log(SetResourceLogObject,
              $"Set part \"{part.partName}\"'s resource \"{resource.resourceName}\" to {value * 100}% by requesting {resource.maxAmount * value - resource.amount} from it");
          }
        }
      }
    }

    public static void RefillVesselResources(Vessel vessel) {
      if (vessel.parts == null)
        return;
      foreach (var part in vessel.parts) {
        //foreach(PartResource resource in part.Resources)
        int resourceCount = part.Resources.Count;
        for (int i = 0; i < resourceCount; ++i) {
          PartResource resource = part.Resources[i];

          part.TransferResource(resource.info.id, resource.maxAmount - resource.amount);
          Utils.Log(
            $"Refilled part \"{part.partName}\"'s resource \"{resource.resourceName}\" by requesting {resource.maxAmount - resource.amount} from it");
        }
      }
    }

    public static KeyCode[] BoostButtonKey {
      get {
        return BoostListener.Fetch.Keys;
      }
      set {
        BoostListener.Fetch.Keys = value;
        //Save value to config file
      }
    }

    public static double BoostButtonSpeed {
      get { return BoostListener.Fetch.Speed; }
      set { BoostListener.Fetch.Speed = value; }
    }
  }

  public class BoostListener : MonoBehaviour {
    private static BoostListener _fetch;

    public static BoostListener Fetch {
      get {
        if (_fetch == null) {
          var go = new GameObject("HyperEditXBoostListener");
          DontDestroyOnLoad(go);
          _fetch = go.AddComponent<BoostListener>();
        }
        return _fetch;
      }
    }

    private bool _doBoost;
    private readonly object _boostLogObject = new object();

    public KeyCode[] Keys { get; set; } = { KeyCode.RightControl, KeyCode.B };

    public double Speed { get; set; }

    public void Update() {
      _doBoost = Keys.Length > 0 && Keys.All(Input.GetKey);
    }

    public void FixedUpdate() {
      if (_doBoost == false)
        return;
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null) {
        _doBoost = false;
        return;
      }
      var vessel = FlightGlobals.ActiveVessel;
      var toAdd = vessel.transform.up;
      toAdd *= (float) Speed;
      vessel.ChangeWorldVelocity(toAdd);
      RateLimitedLogger.Log(_boostLogObject,
        $"Booster changed vessel's velocity by {toAdd.x},{toAdd.y},{toAdd.z} (mag {toAdd.magnitude})");
    }
  }

  public class Panel {
    private static ConfigNode _toggleRes;
    private static int vwidth = 300; //View width (needed for scrollviews)
    private static int vheight = -1; //View height
    private static Vector2 scrollPosition;
    //private static Array toggles[];

    public static Action Create() {
      var view = View();
      
      return () => UiHelper.Window.Create("Resources", true, true, vwidth, vheight, w => view.Draw());
    }

    public static UiHelper.IView View() {
      ReloadConfig();

      Action resources = () => {
        //Using the Vertical to set the box height.
        GUILayout.BeginVertical(GUILayout.Height(100));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(140));

        foreach (var resource in ResourcePanel.GetResources()) {
          GUILayout.BeginHorizontal();
          GUILayout.Label(resource.Key);
          var newval = (double)GUILayout.HorizontalSlider((float)resource.Value, 0, 1);
          if (Math.Abs(newval - resource.Value) > 0.001) {
            ResourcePanel.SetResource(resource.Key, newval);
          }
          //toggles[resource.Key] = GUILayout.Toggle(toggles[resource.Key], "");

          //Just trying an idea
          //toggleRes = GUILayout.Toggle(toggleRes[resource.Key], "lock");
          //toggleRes = GUILayout.Toggle(toggleRes, "lock");
          /*
           * It'd be nice to lock inf resources for specific vessels, or maybe just any vessel?
           */

          //GUILayout.FlexibleSpace();
          GUILayout.Space(5);
          GUILayout.EndHorizontal();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

      };
      return new UiHelper.VerticalView(new UiHelper.IView[]
      {
                new UiHelper.LabelView("Resources", "Set amounts of various resources contained on the active vessel"),
                new UiHelper.CustomView(resources),
                new UiHelper.TextBoxView<KeyCode[]>("Boost button key", "Sets the keybinding used for the boost button",
                    ResourcePanel.BoostButtonKey, Extensions.KeyCodeTryParse, Extensions.KeyCodeToString,
                    v => ResourcePanel.BoostButtonKey = v),
                new UiHelper.TextBoxView<double>("Boost button speed",
                    "Sets the dV applied per frame when the boost button is held down",
                    ResourcePanel.BoostButtonSpeed, SiSuffix.TryParse, null,
                    v => ResourcePanel.BoostButtonSpeed = v)
      });
    }

    private static void ReloadConfig() {
      var hypereditCfg = ConfigHelper.GetPath("miscoptions.cfg");
      if (System.IO.File.Exists(hypereditCfg)) {
        _toggleRes = ConfigNode.Load(hypereditCfg);
        _toggleRes.name = "miscoptions";
      } else {
        _toggleRes = new ConfigNode("miscoptions");
      }

      //var autoOpenLanderValue = true;
      //_toggleRes.TryGetValue("AutoOpenLander", ref autoOpenLanderValue, bool.TryParse);
      //AutoOpenLander = autoOpenLanderValue;


    }
  }

}