using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HyperEditX.Lander {
  public static class DoLander {
    private const string OldFilename = "landcoords.txt";
    private const string FilenameNoExt = "landcoords";
    private const string RecentEntryName = "Most Recent";

    public static bool IsLanding() {
      if ( FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null ) {
        return false;
      }

      return FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>() != null;
    }

    public static void ToggleLanding(double latitude, double longitude, double altitude, CelestialBody body,
        bool setRotation, Action<double, double, double, CelestialBody> onManualEdit) {
      if ( FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || body == null ) {
        return;
      }

      Utils.Log("ToggleLanding");
      Utils.Log("-------------");

      var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
      if ( lander == null ) {
        DoLander.AddLastCoords(latitude, longitude, altitude, body);
        lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();

        if ( latitude == 0.0f ) {
          latitude = 0.001;
        }

        if ( longitude == 0.0f ) {
          longitude = 0.001;
        }

        lander.Latitude = latitude;
        lander.Longitude = longitude;

        lander.InterimAltitude = body.Radius + body.atmosphereDepth + 10000d; //Altitude threshold

        lander.Altitude = altitude;
        lander.SetRotation = setRotation;
        lander.Body = body;
        lander.OnManualEdit = onManualEdit;

        Utils.Log("Latitude : " + latitude.ToString());
        Utils.Log("Longitude: " + longitude.ToString());
        Utils.Log("Altitude : " + altitude.ToString());
        Utils.Log("Body     : " + body.ToString());
        Utils.Log("B-Radius : " + body.Radius.ToString());
        //Utils.Log("B-Depth  : " + body.atmosphereDepth.ToString());
        Utils.Log("NEW:");
        Utils.Log("lander = " + lander.ToString());
        Utils.Log("interimAltitude = " + lander.InterimAltitude);
        Utils.Log("-----------------------------");

      } else {
        //lander != null
        Utils.Log("Unity destroy lander");
        UnityEngine.Object.Destroy(lander);
      }
    }

    public static void LandHere(Action<double, double, double, CelestialBody> onManualEdit) {
      if ( FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null ) {
        return;
      }

      var vessel = FlightGlobals.ActiveVessel;
      var lander = vessel.GetComponent<LanderAttachment>();
      if ( lander == null ) {
        Utils.Log("LandHere");
        Utils.Log("-----------------------------");

        Utils.Log("Vessel Latitude : " + vessel.latitude.ToString());
        Utils.Log("Vessel Longitude: " + vessel.longitude.ToString());
        Utils.Log("Vessel Altitude : " + vessel.altitude.ToString());

        lander = vessel.gameObject.AddComponent<LanderAttachment>();
        lander.Latitude = vessel.latitude;
        lander.Longitude = vessel.longitude;
        lander.SetRotation = false;
        lander.Body = vessel.mainBody;
        lander.OnManualEdit = onManualEdit;
        lander.AlreadyTeleported = false;
        lander.SetAltitudeToCurrent();

        Utils.Log("UPDATE: lander:");
        Utils.Log("lander = " + lander);
        Utils.Log("-----------------------------");

      }
    }

    private static IEnumerable<LandingCoordinates> DefaultSavedCoords {
      get {
        var kerbin = Planetarium.fetch?.Home;
        var minmus = FlightGlobals.fetch?.bodies?.FirstOrDefault(b => b.bodyName == "Minmus");
        if ( kerbin == null ) {
          return new List<LandingCoordinates>();
        }
        var list = new List<LandingCoordinates>
                {
                    new LandingCoordinates("KSC Launch Pad", -0.0972, 285.4423, 20, kerbin),
                    new LandingCoordinates("KSC Runway", -0.0486, 285.2823, 20, kerbin),
                    new LandingCoordinates("KSC Beach - Wet", -0.04862627, 285.666, 20, kerbin),
                    new LandingCoordinates("Airstrip Island Runway", -1.518, 288.1, 35, kerbin),
                    new LandingCoordinates("Airstrip Island Beach - Wet", -1.518, 287.9503, 20, kerbin)
                };
        if ( minmus != null ) {
          list.Add(new LandingCoordinates("Minmus Flats", 0.562859, 175.968846, 20, minmus));
        }
        return list;
      }
    }

    private static List<LandingCoordinates> SavedCoords {
      get {
        var path = ConfigHelper.GetPath(FilenameNoExt + ".cfg");
        var oldPath = ConfigHelper.GetPath(OldFilename);
        IEnumerable<LandingCoordinates> query;
        if ( System.IO.File.Exists(path) ) {
          query = ConfigNode.Load(path).nodes.OfType<ConfigNode>().Select(c => new LandingCoordinates(c));
        } else if ( System.IO.File.Exists(oldPath) ) {
          query =
              System.IO.File.ReadAllLines(oldPath)
                  .Select(x => new LandingCoordinates(x))
                  .Where(l => string.IsNullOrEmpty(l.Name) == false);
        } else {
          query = new LandingCoordinates[0];
        }
        query = query.Union(DefaultSavedCoords);
        return query.ToList();
      }
      set {
        var cfg = new ConfigNode(FilenameNoExt);
        foreach ( var coord in value ) {
          cfg.AddNode(coord.ToConfigNode());
        }
        cfg.Save();
      }
    }

    public static void AddLastCoords(double latitude, double longitude, double altitude, CelestialBody body) {
      if ( body == null ) {
        return;
      }

      AddSavedCoords(RecentEntryName, latitude, longitude, altitude, body);
    }

    public static void AddSavedCoords(double latitude, double longitude, double altitude, CelestialBody body) {
      if ( body == null ) {
        return;
      }

      UiHelper.WindowHelper.Prompt("Save as...", s => AddSavedCoords(s, latitude, longitude, altitude, body));
    }

    private static void AddSavedCoords(string name, double latitude, double longitude, double altitude, CelestialBody body) {
      var saved = SavedCoords;
      saved.RemoveAll(match => match.Name == name);
      saved.Add(new LandingCoordinates(name, latitude, longitude, altitude, body));
      SavedCoords = saved;
    }

    public static void LoadLast(Action<double, double, double, CelestialBody> onLoad) {
      var lastC = SavedCoords.Find(c => c.Name == RecentEntryName);
      //double-check coords are correct (so that we don't load invalid data!)
      onLoad(Utils.DegreeFix(lastC.Lat, -180), lastC.Lon, lastC.Alt, lastC.Body);
    }

    public static void Load(Action<double, double, double, CelestialBody> onLoad) {
      UiHelper.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c => onLoad(c.Lat, c.Lon, c.Alt, c.Body));
    }

    public static void Delete() {
      var coords = SavedCoords;
      UiHelper.WindowHelper.Selector("Delete...", coords, c => c.Name, toDelete => {
        coords.Remove(toDelete);
        SavedCoords = coords;
      });
    }

    public static void SetToCurrent(Action<double, double, double, CelestialBody> onLoad) {
      if ( FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null ) {
        return;
      }

      //FlightGlobals.ActiveVessel.altitude is incorrect.
      var Body = FlightGlobals.ActiveVessel.mainBody;
      var Latitude = Utils.DegreeFix(FlightGlobals.ActiveVessel.latitude, -180);
      var Longitude = FlightGlobals.ActiveVessel.longitude;
      var alt = FlightGlobals.ActiveVessel.radarAltitude;

      onLoad(Latitude, Longitude, alt, Body);
    }

    public static IEnumerable<Vessel> LandedVessels() {
      return FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.Where(v => v.Landed);
    }

    public static void SetToLanded(Action<double, double, double, CelestialBody> onLoad, Vessel landingBeside) {
      if ( landingBeside == null ) {
        return;
      }

      //doing this here for brevity and correct altitude display.
      var Body = landingBeside.mainBody;
      var Latitude = landingBeside.latitude;
      var Longitude = landingBeside.longitude;
      var alt = landingBeside.radarAltitude;

      onLoad(Latitude, Longitude, alt, Body);
    }

    private struct LandingCoordinates : IEquatable<LandingCoordinates> {
      public string Name {
        get;
      }
      public double Lat {
        get;
      }
      public double Lon {
        get;
      }
      public double Alt {
        get;
      }
      public CelestialBody Body {
        get;
      }

      public LandingCoordinates(string name, double lat, double lon, double alt, CelestialBody body)
          : this() {
        Name = name;
        Lat = lat;
        Lon = lon;
        Alt = alt;
        Body = body;
      }

      public LandingCoordinates(string value)
          : this() {
        var split = value.Split(',');
        if ( split.Length < 3 ) {
          Name = null;
          Lat = 0;
          Lon = 0;
          Alt = 20;
          Body = null;
          return;
        }
        double dlat, dlon, dalt;
        if ( double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon) && double.TryParse(split[2], out dalt) ) {
          Name = split[0];
          Lat = dlat;
          Lon = dlon;
          Alt = dalt;
          CelestialBody body;
          if ( split.Length >= 4 && Utils.CbTryParse(split[3], out body) ) {
            Body = body;
          } else {
            Body = Planetarium.fetch.Home;
          }
        } else {
          Name = null;
          Lat = 0;
          Lon = 0;
          Alt = 20;
          Body = null;
        }
      }

      public LandingCoordinates(ConfigNode node) {
        CelestialBody body = null;
        node.TryGetValue("body", ref body, Utils.CbTryParse);
        Body = body;
        var temp = 0.0;
        var tempAlt = 20.0;
        node.TryGetValue("lat", ref temp, double.TryParse);
        Lat = temp;
        node.TryGetValue("lon", ref temp, double.TryParse);
        Lon = temp;
        node.TryGetValue("alt", ref tempAlt, double.TryParse);
        Alt = tempAlt;
        string name = null;
        node.TryGetValue("name", ref name, null);
        Name = name;
      }

      public override int GetHashCode() {
        return Name.GetHashCode();
      }

      public override bool Equals(object obj) {
        return obj is LandingCoordinates && Equals((LandingCoordinates) obj);
      }

      public bool Equals(LandingCoordinates other) {
        return Name.Equals(other.Name);
      }

      public override string ToString() {
        return Name + "," + Lat + "," + Lon + "," + Alt + "," + Body.CbToString();
      }

      public ConfigNode ToConfigNode() {
        var node = new ConfigNode("coordinate");
        node.AddValue("name", Name);
        node.AddValue("body", Body.CbToString());
        node.AddValue("lat", Lat);
        node.AddValue("lon", Lon);
        node.AddValue("alt", Alt);

        return node;
      }
    }
  }

  public class LanderAttachment : MonoBehaviour {
    public bool AlreadyTeleported {
      get; set;
    }
    public Action<double, double, double, CelestialBody> OnManualEdit {
      get; set;
    }
    public CelestialBody Body {
      get; set;
    }
    public double Latitude {
      get; set;
    }
    public double Longitude {
      get; set;
    }
    public double Altitude {
      get; set;
    }
    public bool SetRotation {
      get; set;
    }
    public double InterimAltitude {
      get; set;
    }

    private readonly object _accelLogObject = new object();
    private bool teleportedToLandingAlt = false;
    private double lastUpdate = 0;
    //private double altAGL = 0; // Need to work out these in relation
    //private double altASL = 0; // to land or sea.

    /// <summary>
    /// Sets the vessel altitude to the current calculation.
    /// </summary>
    public void SetAltitudeToCurrent() {
      var pqs = Body.pqsController;
      if ( pqs == null ) {
        Destroy(this);
        return;
      }
      var alt = pqs.GetSurfaceHeight(
          QuaternionD.AngleAxis(Longitude, Vector3d.down) *
          QuaternionD.AngleAxis(Latitude, Vector3d.forward) * Vector3d.right) -
                pqs.radius;
      Utils.Log("SetAltitudeToCurrent:: alt (pqs.GetSurfaceHeight) = " + alt);

      alt = Math.Max(alt, 0); // Underwater!
      /*
       * I'm not sure whether this is correct to zero the altitude as there are times on certain bodies
       * where the altitude of the surface is below sea level...wish I could remember where it was that
       * I found this.
       * 
       * Also HyperEdit used to allow you to land underwater for things like submarines!
       */

      Altitude = GetComponent<Vessel>().altitude - alt;

      Utils.Log("SetAltitudeToCurrent::");
      Utils.Log(" alt = Math.Max(alt, 0) := " + alt);
      Utils.Log(" <Vessel>.altitude      := " + Altitude);

    }

    /// <summary>
    /// Called every frame.
    /// Used for regular updates like:
    ///   Receiving input, simple timers, moving non-physics objects.
    /// Update interval times vary.
    /// </summary>
    public void Update() {
      double distance = 20;

      var vessel = FlightGlobals.ActiveVessel;

      //Testing whether to kill TimeWarp
      if ( TimeWarp.CurrentRateIndex != 0 ) {
        TimeWarp.SetRate(0, true);
        Utils.Log("Update: Kill TimeWarp");
      }

      // 0.2 meters per frame
      //var degrees = 0.02 / Body.Radius * (180 / Math.PI);

      //Utils.Log("degrees = " + degrees);

      var changed = false;
      if ( GameSettings.TRANSLATE_UP.GetKey() ) {
        Utils.Log("UP (North)");
        Utils.Log("Lat Before: " + Latitude);
        Latitude = Utils.DestinationLatitude(Latitude, Longitude, 0, distance, Body.Radius);
        //Latitude -= degrees;
        Utils.Log("Lat After: " + Latitude);
        changed = true;
      }
      if ( GameSettings.TRANSLATE_DOWN.GetKey() ) {
        Utils.Log("DOWN (South)");
        Utils.Log("Lat Before: " + Latitude);
        Latitude = Utils.DestinationLatitude(Latitude, Longitude, 180, distance, Body.Radius);
        //Latitude += degrees;
        Utils.Log("Lat After: " + Latitude);
        changed = true;
      }
      if ( GameSettings.TRANSLATE_LEFT.GetKey() ) {
        Utils.Log("LEFT (West)");
        Utils.Log("Lon Before: " + Longitude);
        Longitude = Utils.DestinationLongitude(Latitude, Longitude, 270, distance, Body.Radius);
        //Longitude -= degrees / Math.Cos(Latitude * (Math.PI / 180));
        Utils.Log("Lon After: " + Longitude);
        changed = true;
      }
      if ( GameSettings.TRANSLATE_RIGHT.GetKey() ) {
        Utils.Log("RIGHT (East)");
        Utils.Log("Lon Before: " + Longitude);
        Longitude = Utils.DestinationLongitude(Latitude, Longitude, 90, distance, Body.Radius);
        //Longitude += degrees / Math.Cos(Latitude * (Math.PI / 180));
        Utils.Log("Lon After: " + Longitude);
        changed = true;
      }

      if ( Latitude == 0 ) {
        Latitude = 0.0001;
      }
      if ( Longitude == 0 ) {
        Longitude = 0.0001;
      }
      if ( changed ) {
        AlreadyTeleported = false;
        teleportedToLandingAlt = false;
        OnManualEdit(Latitude, Longitude, Altitude, Body);

        Utils.Log("Altitude = " + Altitude);
        Utils.Log("alt      = " + vessel.altitude);
      }
    }

    /// <summary>
    /// Called every physics step.
    /// Fixed Update intervals are consistent.
    /// Used for regular updates like:
    ///    Adjusting physics (Rigidbody) objects.
    /// 
    /// </summary>
    public void FixedUpdate() {
      var vessel = GetComponent<Vessel>();

      if ( vessel != FlightGlobals.ActiveVessel ) {
        Destroy(this);
        return;
      }

      if ( TimeWarp.CurrentRateIndex != 0 ) {
        TimeWarp.SetRate(0, true);
        Utils.Log("Kill time warp for safety reasons!");
      }

      if ( AlreadyTeleported ) {

        if ( vessel.LandedOrSplashed ) {
          Destroy(this);
        } else {
          var accel = (vessel.srf_velocity + vessel.upAxis) * -0.5;
          vessel.ChangeWorldVelocity(accel);

        }
      } else {
        //NOT AlreadyTeleported
        //Still calculating
        var pqs = Body.pqsController;
        if ( pqs == null ) {
          // The sun has no terrain.  Everthing else has a PQScontroller.
          Destroy(this);
          return;
        }

        var alt = pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)) - Body.Radius;
        var tmpAlt = Body.TerrainAltitude(Latitude, Longitude);

        double landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;

        double finalAltitude = 0.0; //trying to isolate this for debugging!

        var checkAlt = FlightGlobals.ActiveVessel.altitude;
        var checkPQSAlt = FlightGlobals.ActiveVessel.pqsAltitude;
        double terrainAlt = GetTerrainAltitude();

        //double vesselHeight = Bounds[vessel.id]

        Utils.ALog("-------------------");
        Utils.ALog("m1. Body.Radius  = ", Body.Radius);
        Utils.ALog("m2. PQS SurfaceHeight = ", pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)));
        Utils.ALog("alt ( m2 - m1 ) = ", alt);
        Utils.ALog("Body.TerrainAltitude = ", tmpAlt);
        Utils.ALog("checkAlt    = ", checkAlt);
        Utils.ALog("checkPQSAlt = ", checkPQSAlt);
        Utils.ALog("landheight  = ", landHeight);
        Utils.ALog("terrainAlt  = ", terrainAlt);
        Utils.ALog("-------------------");
        Utils.ALog("Latitude: ", Latitude, "Longitude: ", Longitude);
        Utils.ALog("-------------------");

        alt = Math.Max(alt, 0d); // Make sure we're not underwater!

        // HoldVesselUnpack is in display frames, not physics frames

        Vector3d teleportPosition;

        if ( !teleportedToLandingAlt ) {
          Utils.ALog("teleportedToLandingAlt == false");
          Utils.ALog("interimAltitude: ", InterimAltitude);
          Utils.ALog("Altitude: ", Altitude);

          if ( InterimAltitude > Altitude ) {

            if ( Planetarium.GetUniversalTime() - lastUpdate >= 0.5 ) {
              InterimAltitude = InterimAltitude / 10;
              terrainAlt = GetTerrainAltitude();

              if ( InterimAltitude < terrainAlt ) {
                InterimAltitude = terrainAlt + Altitude;
              }

              //InterimAltitude = terrainAlt + Altitude;

              teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, InterimAltitude) - Body.position;

              Utils.ALog("1. teleportPosition = ", teleportPosition);
              Utils.ALog("1. interimAltitude: ", InterimAltitude);

              if ( lastUpdate != 0 ) {
                InterimAltitude = Altitude;
              }
              lastUpdate = Planetarium.GetUniversalTime();

            } else {
              Utils.Log("teleportPositionAltitude (no time change):");

              teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + InterimAltitude) - Body.position;

              Utils.ALog("2. teleportPosition = ", teleportPosition);
              Utils.ALog("2. alt: ", alt);
              Utils.ALog("2. interimAltitude: ", InterimAltitude);
            }
          } else {
            //InterimAltitude <= Altitude
            Utils.Log("3. teleportedToLandingAlt sets to true");

            landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
            terrainAlt = GetTerrainAltitude();

            //trying to find the correct altitude here.

            if ( checkPQSAlt > terrainAlt ) {
              alt = checkPQSAlt;
            } else {
              alt = terrainAlt;
            }

            if ( alt == 0.0 ) {
              //now what?
            }

            /*
             * landHeight factors into the final altitude somehow. Possibly.
             */

            teleportedToLandingAlt = true;
            //finalAltitude = alt + Altitude;
            if ( alt < 0 ) {
              finalAltitude = Altitude;
            } else if ( alt > 0 ) {

              finalAltitude = alt + Altitude;
            } else {
              finalAltitude = alt + Altitude;
            }

            teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, finalAltitude) - Body.position;

            Utils.ALog("3. teleportPosition = ", teleportPosition);
            Utils.ALog("3. alt = ", alt, "Altitude = ", Altitude, "InterimAltitude = ", InterimAltitude);
            Utils.ALog("3. TerrainAlt = ", terrainAlt, "landHeight = ", landHeight);
          }
        } else {
          /*
           * With the current way of calculating, it seems like this part of the conditional
           * never gets called. (Well not so far in my (@fronbow) testing.
           */

          Utils.Log("teleportedToLandingAlt == true");

          landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
          terrainAlt = GetTerrainAltitude();

          Utils.ALog("4. finalAltitude = ", finalAltitude);
          /*
           * Depending on finalAltitude, we might not need to calculate it again here.
           */

          //finalAltitude = alt + Altitude;
          if ( alt < 0 ) {
            finalAltitude = Altitude;
          } else if ( alt > 0 ) {
            finalAltitude = alt + Altitude;
          } else {
            finalAltitude = alt + Altitude;
          }

          //teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, finalAltitude);
          teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, finalAltitude) - Body.position;

          Utils.ALog("4. teleportPosition = ", teleportPosition);
          Utils.ALog("4. alt = ", alt, "Altitude = ", Altitude, "InterimAltitude = ", InterimAltitude);
          Utils.ALog("4. TerrainAlt = ", terrainAlt, "landHeight = ", landHeight);
          Utils.ALog("4. finalAltitude = ", finalAltitude);
        }

        var teleportVelocity = Vector3d.Cross(Body.angularVelocity, teleportPosition);

        // convert from world space to orbit space

        teleportPosition = teleportPosition.xzy;
        teleportVelocity = teleportVelocity.xzy;

        Utils.ALog("0. teleportPosition(xzy): ", teleportPosition);
        Utils.ALog("0. teleportVelocity(xzy): ", teleportVelocity);
        Utils.ALog("0. Body                 : ", Body);

        // counter for the momentary fall when on rails (about one second)
        teleportVelocity += teleportPosition.normalized * (Body.gravParameter / teleportPosition.sqrMagnitude);

        Quaternion rotation;

        vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false); //disable SAS as it causes unknown results!

        if ( SetRotation ) {
          // Need to check vessel and find up for the root command pod
          var up = vessel.upAxis;
          var vType = vessel.vesselType.ToString();

          var from = Vector3d.up; //Sensible default for all vessels

          if ( vessel.displaylandedAt == "Runway" ||
            (vessel.vesselType.ToString() == "Plane" || vessel.vesselType.ToString() == "Rover" || vessel.vesselType.ToString() == "Base") ) {
            from = vessel.vesselTransform.up;
          }

          var to = teleportPosition.xzy.normalized;
          rotation = Quaternion.FromToRotation(from, to);
        } else {
          var oldUp = vessel.orbit.pos.xzy.normalized;
          var newUp = teleportPosition.xzy.normalized;
          rotation = Quaternion.FromToRotation(oldUp, newUp) * vessel.vesselTransform.rotation;
        }

        //var orbit = vessel.orbitDriver.orbit.Clone();
        var orbit = Orbits.OrbitEditor.Clone(vessel.orbit);


        orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, Body, Planetarium.GetUniversalTime());

        Orbits.OrbitEditor.SetOrbit(vessel, orbit);
        //vessel.SetOrbit(orbit);
        vessel.SetRotation(rotation);

        vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true); //enable SAS for stability

        if ( teleportedToLandingAlt ) {
          AlreadyTeleported = true;
          Utils.Log(" :FINISHED TELEPORTING:");
        }
      }
    }

    /// <summary>
    ///  Returns the ground's altitude above sea level at this geo position.
    /// </summary>
    /// <returns></returns>
    /// <remarks>Borrowed this from the kOS mod with slight modification</remarks>
    /// <see cref="https://github.com/KSP-KOS/KOS/blob/develop/src/kOS/Suffixed/GeoCoordinates.cs"/>
    public Double GetTerrainAltitude() {
      double alt = 0.0;
      PQS bodyPQS = Body.pqsController;
      if ( bodyPQS != null ) // The sun has no terrain.  Everything else has a PQScontroller.
      {
        // The PQS controller gives the theoretical ideal smooth surface curve terrain.
        // The actual ground that exists in-game that you land on, however, is the terrain
        // polygon mesh which is built dynamically from the PQS controller's altitude values,
        // and it only approximates the PQS controller.  The discrepancy between the two
        // can be as high as 20 meters on relatively mild rolling terrain and is probably worse
        // in mountainous terrain with steeper slopes.  It also varies with the user terrain detail
        // graphics setting.

        // Therefore the algorithm here is this:  Get the PQS ideal terrain altitude first.
        // Then try using RayCast to get the actual terrain altitude, which will only work
        // if the LAT/LONG is near the active vessel so the relevant terrain polygons are
        // loaded.  If the RayCast hit works, it overrides the PQS altitude.

        // PQS controller ideal altitude value:
        // -------------------------------------

        // The vector the pqs GetSurfaceHeight method expects is a vector in the following
        // reference frame:
        //     Origin = body center.
        //     X axis = LATLNG(0,0), Y axis = LATLNG(90,0)(north pole), Z axis = LATLNG(0,-90).
        // Using that reference frame, you tell GetSurfaceHeight what the "up" vector is pointing through
        // the spot on the surface you're querying for.
        var bodyUpVector = new Vector3d(1, 0, 0);
        bodyUpVector = QuaternionD.AngleAxis(Latitude, Vector3d.forward/*around Z axis*/) * bodyUpVector;
        bodyUpVector = QuaternionD.AngleAxis(Longitude, Vector3d.down/*around -Y axis*/) * bodyUpVector;

        alt = bodyPQS.GetSurfaceHeight(bodyUpVector) - bodyPQS.radius;

        // Terrain polygon raycasting:
        // ---------------------------
        const double HIGH_AGL = 1000.0;
        const double POINT_AGL = 800.0;
        const int TERRAIN_MASK_BIT = 15;

        // a point hopefully above the terrain:
        Vector3d worldRayCastStart = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + HIGH_AGL);
        // a point a bit below it, to aim down to the terrain:
        Vector3d worldRayCastStop = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + POINT_AGL);
        RaycastHit hit;
        if ( Physics.Raycast(worldRayCastStart, (worldRayCastStop - worldRayCastStart), out hit, float.MaxValue, 1 << TERRAIN_MASK_BIT) ) {
          // Ensure hit is on the topside of planet, near the worldRayCastStart, not on the far side.
          if ( Mathf.Abs(hit.distance) < 3000 ) {
            // Okay a hit was found, use it instead of PQS alt:
            alt = ((alt + HIGH_AGL) - hit.distance);
          }
        }
      }
      return alt;
    }

  }

  public static class Panel {

    private static bool _autoOpenLander;
    private static ConfigNode _hyperEditConfig;

    public static Action Create() {
      var view = View();
      return () => UiHelper.Window.Create("Vessel Tools", true, true, 200, -1, w => view.Draw());
    }

    // Use myTryParse to validate the string, and, if it is 0, to set it to 0.001f
    static bool myTryParse(string str, out double d) {
      double d1;
      bool b = double.TryParse(str, out d1);
      if ( !b ) {
        d = 0.001f;
        return false;
      }
      if ( d1 == 0 )
        d1 = 0.001d;
      d = d1;
      return true;
    }
    /*
    static bool lonTryParse(string str, out double result) {
      result = null;
      double d1;
      //Extensions.DegreeFix(str, 0);
      bool b = double.TryParse(str, out d1);

      if (!b) {
        result = 0.001f;
        return false;
      } else {
        
        d1 = Extensions.DegreeFix(result, 0);

        return true;
      }
      return true;

    }
    */

    static bool latTryParse(string str, out double d) {
      double d1;
      double highLimit = 89.9d;
      double lowLimit = -89.9d;
      bool b = double.TryParse(str, out d1);
      if ( !b ) {
        d = 0.001f;
        return false;
      }
      if ( d1 == 0 ) {
        d = 0.001d;
        return true;
      }
      if ( d1 > highLimit ) {
        d = highLimit;
        return false;
      }
      if ( d1 < lowLimit ) {
        d = lowLimit;
        return false;
      }
      //d = d1;
      d = Utils.DegreeFix(d1, -180); //checking for massive values
      return true;
    }

    static bool altTryParse(string str, out double d) {
      double d1;
      double lowLimit = 0.0d;
      bool b = SiSuffix.TryParse(str, out d1);
      if ( !b ) {
        d = 0.001f;
        return false;
      }
      if ( d1 == 0 ) {
        d = 0.001d;
        return true;
      }
      if ( d1 < lowLimit ) {
        d = lowLimit;
        return false;
      }
      d = d1;
      return true;
    }

    private static void ReloadConfig() {
      var hypereditCfg = ConfigHelper.GetPath("hypereditX.cfg");
      if ( System.IO.File.Exists(hypereditCfg) ) {
        _hyperEditConfig = ConfigNode.Load(hypereditCfg);
        _hyperEditConfig.name = "hypereditX";
      } else {
        _hyperEditConfig = new ConfigNode("hypereditX");
      }

      var autoOpenLanderValue = false;
      _hyperEditConfig.TryGetValue("AutoOpenLander", ref autoOpenLanderValue, bool.TryParse);
      AutoOpenLander = autoOpenLanderValue;
    }

    public static bool AutoOpenLander {
      get {
        return _autoOpenLander;
      }
      set {
        if ( _autoOpenLander == value )
          return;
        _autoOpenLander = value;
        _hyperEditConfig.SetValue("AutoOpenLander", value.ToString(), true);
        _hyperEditConfig.Save();
      }
    }

    public static UiHelper.IView View() {
      // Load Auto Open status.
      ReloadConfig();

      var setAutoOpen = new UiHelper.DynamicToggleView("Auto Open", "Open this view when entering the Flight or Tracking Center scenes.",
          () => AutoOpenLander, () => true, v => AutoOpenLander = v);
      var bodySelector = new UiHelper.ListSelectView<CelestialBody>("Body", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.bodies, null, Utils.CbToString);
      bodySelector.CurrentlySelected = FlightGlobals.fetch == null ? null : FlightGlobals.ActiveVessel == null ? Planetarium.fetch.Home : FlightGlobals.ActiveVessel.mainBody;
      var lat = new UiHelper.TextBoxView<double>("Lat", "Latitude (North/South). Between +90 (North) and -90 (South).", 0.001d, latTryParse);
      var lon = new UiHelper.TextBoxView<double>("Lon", "Longitude (East/West). Converts to less than 360 degrees.", 0.001d, myTryParse);
      var alt = new UiHelper.TextBoxView<double>("Alt", "Altitude (Up/Down). Distance above the surface.", 20, altTryParse);
      var setRot = new UiHelper.ToggleView("Force Rotation",
          "Rotates vessel such that up on the vessel is up when landing. Otherwise, the current orientation is kept relative to the body.",
          true);
      Func<bool> isValid = () => lat.Valid && lon.Valid && alt.Valid;
      Action<double, double, double, CelestialBody> load = (latVal, lonVal, altVal, body) => {
        lat.Object = latVal;
        lon.Object = lonVal;
        alt.Object = altVal;
        bodySelector.CurrentlySelected = body;
      };

      // Load last entered values.
      DoLander.LoadLast(load);

      return new UiHelper.VerticalView(new UiHelper.IView[]
          {
            setAutoOpen,
            bodySelector,
            new UiHelper.ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.mainBody != bodySelector.CurrentlySelected, new UiHelper.LabelView("Landing on a different body is not recommended.", "This may destroy the vessel. Use the Orbit Editor to orbit the body first, then land on it.")),
            lat,
            new UiHelper.ConditionalView(() => !lat.Valid, new UiHelper.LabelView("Latitude must be a number from 0 to (+/-)89.9.", "Values too close to the poles ((+/-)90) can crash KSP, values beyond that are invalid for a latitude.")),
            lon,
            alt,
            new UiHelper.ConditionalView(() => alt.Object < 0, new UiHelper.LabelView("Altitude must be a positive number.", "This may destroy the vessel. Values less than 0 are sub-surface.")),
            setRot,
            new UiHelper.ConditionalView(() => !isValid(), new UiHelper.ButtonView("Cannot Land", "Entered location is invalid. Correct items in red.", null)),
            new UiHelper.ConditionalView(() => !DoLander.IsLanding() && isValid(), new UiHelper.ButtonView("Land", "Teleport to entered location, then slowly lower to surface.", () => DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected, setRot.Value, load))),
            new UiHelper.ConditionalView(() => DoLander.IsLanding(), new UiHelper.ButtonView("Drop (CAUTION!)", "Release vessel to gravity.", () => DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected, setRot.Value, load))),
            new UiHelper.ConditionalView(() => DoLander.IsLanding(), new UiHelper.LabelView("LANDING IN PROGRESS.", "Vessel is being lowered to the surface.")),
            //Launch button here
            new UiHelper.ConditionalView(() => DoLander.IsLanding(), new UiHelper.LabelView(changeHelpString(), "Change location slightly.")),
            new UiHelper.ConditionalView(() => !DoLander.IsLanding(), new UiHelper.ButtonView("Land Here", "Stop at current location, then slowly lower to surface.", () => DoLander.LandHere(load))),
            new UiHelper.ListSelectView<Vessel>("Set to vessel", DoLander.LandedVessels, select => DoLander.SetToLanded(load, select), Extensions.VesselToString),
            new UiHelper.ButtonView("Current", "Set to current location.", () => DoLander.SetToCurrent(load)),
            new UiHelper.ConditionalView(isValid, new UiHelper.ButtonView("Save", "Save the entered location.", () => DoLander.AddSavedCoords(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected))),
            new UiHelper.ButtonView("Load", "Load a saved location.", () => DoLander.Load(load)),
            new UiHelper.ButtonView("Delete", "Delete a saved location.", DoLander.Delete),
          });
    }

    private static string changeHelpString() {
      return
          $"Use {GameSettings.TRANSLATE_UP.primary} (S),{GameSettings.TRANSLATE_DOWN.primary} (N),{GameSettings.TRANSLATE_LEFT.primary} (W),{GameSettings.TRANSLATE_RIGHT.primary} (E) to fine-tune location.";
    }
  }
}
