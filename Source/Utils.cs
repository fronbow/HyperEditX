using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HyperEditX {
  public static class Utils {

    const double PI = Math.PI;

    public static Vector3 GetWorldUp(this Vessel vessel) => vessel.upAxis;

    public static Vector3 GetVesselRight(this Vessel vessel) => vessel.transform.right;
    // transform.forward is down, not forward (and transform.up is forward, not up)
    public static Vector3 GetVesselUp(this Vessel vessel) => -vessel.transform.forward;
    // transform.up is forward, not up
    public static Vector3 GetVesselForward(this Vessel vessel) => vessel.transform.up;

    public static Vector3 GetCelestialNorth(this Vessel vessel) => vessel.mainBody.transform.up;

    public static Vector3 GetEast(this Vessel vessel) => Vector3.Cross(vessel.GetWorldUp(), vessel.GetCelestialNorth()).normalized;

    public static Vector3 GetNorth(this Vessel vessel) => Vector3.Cross(vessel.GetEast(), vessel.GetWorldUp());

    ///   Borrowed from https://github.com/KSP-KOS/KOS.
    /// <summary>
    ///   Fix the strange too-large or too-small angle degrees that are sometimes
    ///   returned by KSP, normalizing them into a constrained 360 degree range.
    /// </summary>
    /// <param name="inAngle">input angle in degrees</param>
    /// <param name="rangeStart">
    ///   Bottom of 360 degree range to normalize to.
    ///   ( 0 means the range [0..360]), while -180 means [-180,180] )
    /// </param>
    /// <returns>the same angle, normalized to the range given.</returns>
    public static double DegreeFix(double inAngle, double rangeStart) {
      double rangeEnd = rangeStart + 360.0;
      double outAngle = inAngle;
      while (outAngle > rangeEnd)
        outAngle -= 360.0;
      while (outAngle < rangeStart)
        outAngle += 360.0;
      return outAngle;
    }
    
    /* Convert from degrees */
    
    public static double ConvertDegreeAngleToDouble(double degrees, double minutes, double seconds) {
        var multiplier = (degrees < 0 ? -1 : 1);
        var _deg = (double)Math.Abs(degrees);
        var result = _deg + (minutes / 60) + (seconds / 3600);
        return result * multiplier;
    }
    
    /* Convert from degrees
     * Implies the coord is in the form d:m:s
    */
    
    public static double ConvertDegreesToDecimal(string coordinate) {
      double decimalCoordinate = 0;

      string[] coordinateArray = coordinate.Split(':');
      if (3 == coordinateArray.Length) {
          double degrees = Double.Parse(coordinateArray[0]);
          double minutes = Double.Parse(coordinateArray[1]) / 60;
          double seconds = Double.Parse(coordinateArray[2]) / 3600;

          if (degrees > 0) {
              decimalCoordinate = (degrees + minutes + seconds);
          } else {
              decimalCoordinate = (degrees - minutes - seconds);
          }
      }
      return decimalCoordinate;
    }

    /*
     * Simpler conversions.
     * See http://www.vcskicks.com/csharp_net_angles.php
     * 
     * Also see:
     * https://adamprescott.net/2013/07/17/convert-latitudelongitude-between-decimal-and-degreesminutesseconds-in-c/
     */
     

    public static double DegreesToRadians(double degrees) {
      return degrees * (PI / 180);
    }

    public static double RadiansToDegrees(double radians) {
      return radians * (180 / PI);
    }

    /// <summary>
    /// Get destination latitude point for use with fine-tuning.
    /// <para>See http://www.movable-type.co.uk/scripts/latlong.html</para>
    /// <para>(Destination point given distance and bearing from start point)</para>
    /// </summary>
    /// <param name="latStart">The starting latitude</param>
    /// <param name="lonStart">The starting longitude</param>
    /// <param name="bearing">The direction</param>
    /// <param name="distance">The distance to move in metres.</param>
    /// <param name="radius">The current Body's radius</param>
    /// <returns></returns>
    public static double DestinationLatitude(double latStart, double lonStart, double bearing, double distance, double radius) {

      //distance = distance / 100; //this should equate to metres
      ALog("Bearing::", bearing);

      latStart = PI / 180 * latStart;
      lonStart = PI / 180 * lonStart;
      bearing = PI / 180 * bearing;
      
      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      ALog("latStart", latStart, "latEnd", latEnd, "LonStart", lonStart, "LonEnd", lonEnd, "Bearing", bearing);

      return latEnd;
    }

    /// <summary>
    /// Get destination longitude point for use with fine-tuning.
    /// <para>See http://www.movable-type.co.uk/scripts/latlong.html</para>
    /// <para>(Destination point given distance and bearing from start point)</para>
    /// </summary>
    /// <param name="latStart">The starting latitude</param>
    /// <param name="lonStart">The starting longitude</param>
    /// <param name="bearing">The direction</param>
    /// <param name="distance">The distance to move in metres.</param>
    /// <param name="radius">The current Body's radius</param>
    /// <returns></returns>
    public static double DestinationLongitude(double latStart, double lonStart, double bearing, double distance, double radius) {

      //distance = distance / 100; //this should equate to metres
      ALog("Bearing::", bearing);

      latStart = PI / 180 * latStart;
      lonStart = PI / 180 * lonStart;
      bearing = PI / 180 * bearing;

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      ALog("latStart", latStart, "latEnd", latEnd, "LonStart", lonStart, "LonEnd", lonEnd, "Bearing", bearing);

      return lonEnd;
    }

    public static double DestinationLatitudeRad(double latStart, double lonStart, double bearing, double distance, double radius) {

      distance = distance / 100; //this should equate to metres

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      return latEnd;
    }

    public static double DestinationLongitudeRad(double latStart, double lonStart, double bearing, double distance, double radius) {

      distance = distance / 100; //this should equate to metres

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      return lonEnd;
    }

    /// <summary>
    /// Debug logging. Only compiles in DEBUG builds.
    /// </summary>
    /// <param name="message"></param>
    [ConditionalAttribute("DEBUG")]
    public static void Log(string message) {
      UnityEngine.Debug.Log("HeX: " + message);
    }

    /// <summary>
    /// Comma separated debug logging. Only compiles in DEBUG builds.
    /// </summary>
    /// <param name="message"></param>
    [ConditionalAttribute("DEBUG")]
    public static void ALog(params object[] message) {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < message.Length; i++) {
        sb.Append(message[i].ToString());
        sb.Append("\t");
      }
      String s = sb.ToString().Trim();
      UnityEngine.Debug.Log("HeX: " + s);
    }
    
    public static void ClearGuiFocus() {
      GUIUtility.keyboardControl = 0;
    }
    
    private static GUIStyle _pressedButton;

    public static GUIStyle PressedButton => _pressedButton ?? (_pressedButton = new GUIStyle(HighLogic.Skin.button) {
      normal = HighLogic.Skin.button.active,
      hover = HighLogic.Skin.button.active,
      active = HighLogic.Skin.button.normal
    });

    //from mechjeb via HaystackContinued: figured it'd be better to keep conversion consistent between various plugins
    //Puts numbers into SI format, e.g. 1234 -> "1.234 k", 0.0045678 -> "4.568 m"
    //maxPrecision is the exponent of the smallest place value that will be shown; for example
    //if maxPrecision = -1 and digitsAfterDecimal = 3 then 12.345 will be formatted as "12.3"
    //while 56789 will be formated as "56.789 k"
    public static string ToSI(double d, int maxPrecision = -99, int sigFigs = 4) {
      if (d == 0 || double.IsInfinity(d) || double.IsNaN(d))
        return d.ToString() + " ";

      int exponent = (int)Math.Floor(Math.Log10(Math.Abs(d)));
      //exponent of d if it were expressed in scientific notation

      string[] units = new string[]
      {"y", "z", "a", "f", "p", "n", "μ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y"};
      const int unitIndexOffset = 8; //index of "" in the units array
      int unitIndex = (int)Math.Floor(exponent / 3.0) + unitIndexOffset;
      if (unitIndex < 0)
        unitIndex = 0;
      if (unitIndex >= units.Length)
        unitIndex = units.Length - 1;
      string unit = units[unitIndex];

      int actualExponent = (unitIndex - unitIndexOffset) * 3; //exponent of the unit we will us, e.g. 3 for k.
      d /= Math.Pow(10, actualExponent);

      int digitsAfterDecimal = sigFigs - (int)(Math.Ceiling(Math.Log10(Math.Abs(d))));

      if (digitsAfterDecimal > actualExponent - maxPrecision)
        digitsAfterDecimal = actualExponent - maxPrecision;
      if (digitsAfterDecimal < 0)
        digitsAfterDecimal = 0;

      string ret = d.ToString("F" + digitsAfterDecimal) + " " + unit;

      return ret;
    }

    public static void RealCbUpdate(this CelestialBody body) {
      body.CBUpdate();
      try {
        body.resetTimeWarpLimits();
      }
      catch (NullReferenceException) {
        Utils.Log("resetTimeWarpLimits threw NRE " + (TimeWarp.fetch == null ? "as expected" : "unexpectedly"));
      }

      // CBUpdate doesn't update hillSphere
      // http://en.wikipedia.org/wiki/Hill_sphere
      var orbit = body.orbit;
      var cubedRoot = Math.Pow(body.Mass / orbit.referenceBody.Mass, 1.0 / 3.0);
      body.hillSphere = orbit.semiMajorAxis * (1.0 - orbit.eccentricity) * cubedRoot;

      // Nor sphereOfInfluence
      // http://en.wikipedia.org/wiki/Sphere_of_influence_(astrodynamics)
      body.sphereOfInfluence = orbit.semiMajorAxis * Math.Pow(body.Mass / orbit.referenceBody.Mass, 2.0 / 5.0);
    }

    /// <summary>
    /// Convert Celestial Body to human readable form.
    /// </summary>
    /// <param name="body">Celestial Body</param>
    /// <returns>The name of the Celestial Body.</returns>
    public static string CbToString(this CelestialBody body) {
      return body.bodyName;
    }

    public static bool CbTryParse(string bodyName, out CelestialBody body) {
      body = FlightGlobals.Bodies == null ? null : FlightGlobals.Bodies.FirstOrDefault(cb => cb.name == bodyName);
      return body != null;
    }




  } //End class
}