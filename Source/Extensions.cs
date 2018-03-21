using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI.Screens;
using UnityEngine;

namespace HyperEditX {
  public static class Extensions {

    public static void TryGetValue<T>(this ConfigNode node, string key, ref T value, TryParse<T> tryParse) {
      var strvalue = node.GetValue(key);
      if (strvalue == null)
        return;
      if (tryParse == null) {
        // `T` better be `string`...
        value = (T)(object) strvalue;
        return;
      }
      T temp;
      if (tryParse(strvalue, out temp) == false) {
        return;
      }
      value = temp;
    }

    

    public static void PrepVesselTeleport(this Vessel vessel) {
      if (vessel.Landed) {
        vessel.Landed = false;
        Utils.Log("Set ActiveVessel.Landed = false");
      }
      if (vessel.Splashed) {
        vessel.Splashed = false;
        Utils.Log("Set ActiveVessel.Splashed = false");
      }
      if (vessel.landedAt != string.Empty) {
        vessel.landedAt = string.Empty;
        Utils.Log("Set ActiveVessel.landedAt = \"\"");
      }
      var parts = vessel.parts;
      if (parts != null) {
        var killcount = 0;
        foreach (var part in parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList()) {
          killcount++;
          part.Die();
        }
        if (killcount != 0) {
          Utils.Log($"Removed {killcount} launch clamps from {vessel.vesselName}");
        }
      }
    }

    /// <summary>
    /// Sphere of Influence.
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    public static double Soi(this CelestialBody body) {
      var radius = body.sphereOfInfluence * 0.95;
      if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0 || radius > 200000000000) {
        radius = 200000000000; // jool apo = 72,212,238,387
      }
      return radius;
    }

    public static double Mod(this double x, double y) {
      var result = x % y;
      if (result < 0) {
        result += y;
      }
      return result;
    }

    public static string VesselToString(this Vessel vessel) {
      if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel == vessel) {
        return "Active vessel";
      }
      return vessel.vesselName;
    }

    public static string OrbitDriverToString(this OrbitDriver driver) {
      if (driver == null) {
        return null;
      }
      if (driver.celestialBody != null) {
        return driver.celestialBody.bodyName;
      }
      if (driver.vessel != null) {
        return driver.vessel.VesselToString();
      }
      if (!string.IsNullOrEmpty(driver.name)) {
        return driver.name;
      }
      return "Unknown";
    }

    private static Dictionary<string, KeyCode> _keyCodeNames;

    public static Dictionary<string, KeyCode> KeyCodeNames {
      get {
        return _keyCodeNames ?? (_keyCodeNames =
          Enum.GetNames(typeof(KeyCode))
          .Distinct()
          .ToDictionary(k => k, k =>(KeyCode) Enum.Parse(typeof(KeyCode), k)));
      }
    }

    public static bool KeyCodeTryParse(string str, out KeyCode[] value) {
      var split = str.Split('-', '+');
      if (split.Length == 0) {
        value = null;
        return false;
      }
      value = new KeyCode[split.Length];
      for (int i = 0; i < split.Length; i++) {
        if (KeyCodeNames.TryGetValue(split[i], out value[i]) == false) {
          return false;
        }
      }
      return true;
    }

    public static string KeyCodeToString(this KeyCode[] values) {
      return string.Join("-", values.Select(v => v.ToString()).ToArray());
    }

    
    private static string TrimUnityColor(string value) {
      value = value.Trim();
      if (value.StartsWith("RGBA", StringComparison.OrdinalIgnoreCase)) {
        value = value.Substring(4).Trim();
      }
      value = value.Trim('(', ')');
      return value;
    }

    public static bool ColorTryParse(string value, out Color color) {
      color = new Color();
      string parseValue = TrimUnityColor(value);
      if (parseValue == null) {
        return false;
      }
      string[] values = parseValue.Split(new [] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (values.Length == 3 || values.Length == 4) {
        if (!float.TryParse(values[0], out color.r) ||
          !float.TryParse(values[1], out color.g) ||
          !float.TryParse(values[2], out color.b)) {
          return false;
        }
        if (values.Length == 3 && !float.TryParse(values[3], out color.a)) {
          return false;
        }
        return true;
      }
      return false;
    }

  }

  public static class RateLimitedLogger {
    private const int MaxFrequency = 100; // measured in number of frames

    private class Countdown {
      public string LastMessage;
      public int FramesLeft;
      public bool NeedsPrint;

      public Countdown(string msg, int frames) {
        LastMessage = msg;
        FramesLeft = frames;
        NeedsPrint = false;
      }
    }

    private static readonly Dictionary<object, Countdown> Messages = new Dictionary<object, Countdown>();

    public static void Update() {
      List<object> toRemove = null;
      foreach (var kvp in Messages) {
        if (kvp.Value.FramesLeft == 0) {
          if (kvp.Value.NeedsPrint) {
            kvp.Value.NeedsPrint = false;
            kvp.Value.FramesLeft = MaxFrequency;

            Utils.Log(kvp.Value.LastMessage);
          } else {
            if (toRemove == null) {
              toRemove = new List<object>();
            }
            toRemove.Add(kvp.Key);
          }
        } else {
          kvp.Value.FramesLeft--;
        }
      }
      if (toRemove != null) {
        foreach (var key in toRemove) {
          Messages.Remove(key);
        }
      }
    }

    public static void Log(object key, string message) {
      Countdown countdown;
      if (Messages.TryGetValue(key, out countdown)) {
        countdown.NeedsPrint = true;
        countdown.LastMessage = message;
      } else {
        Utils.Log(message);
        Messages[key] = new Countdown(message, MaxFrequency);
      }
    }
  }
}