using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEditX.Orbits {
  public static class OrbitEditor {

    public static IEnumerable<OrbitDriver> OrderedOrbits() {
      var query = (IEnumerable<OrbitDriver>)
          (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.orbitDriver == null
              ? new OrbitDriver[0]
              : new[] { FlightGlobals.ActiveVessel.orbitDriver });
      if (FlightGlobals.fetch != null) {
        query = query
            .Concat(FlightGlobals.Vessels.Select(v => v.orbitDriver))
            .Concat(FlightGlobals.Bodies.Select(v => v.orbitDriver));
      }
      query = query.Where(o => o != null).Distinct();
      return query;
    }

    public static void Simple(OrbitDriver currentlyEditing, double altitude, CelestialBody body) {
      SetOrbit(currentlyEditing, CreateOrbit(0, 0, altitude + body.Radius, 0, 0, 0, 0, body));
    }

    public static void GetSimple(OrbitDriver currentlyEditing, out double altitude, out CelestialBody body) {
      const int min = 1000;
      const int defaultAlt = 100000;

      body = currentlyEditing.orbit.referenceBody;
      altitude = currentlyEditing.orbit.semiMajorAxis - body.Radius;
      if (altitude > min) {
        return;
      }

      /*
      if (currentlyEditing.vessel.Landed) {
        altitude = currentlyEditing.vessel.radarAltitude;
        return;
      } else {
        altitude = currentlyEditing.orbit.ApA;
      }
      */

      altitude = currentlyEditing.orbit.ApA;

      if (altitude > min) {
        return;
      }

      altitude = defaultAlt;
    }

    public static void Complex(OrbitDriver currentlyEditing, double inclination, double eccentricity,
            double semiMajorAxis, double longitudeAscendingNode, double argumentOfPeriapsis,
            double meanAnomalyAtEpoch, double epoch, CelestialBody body) {
      SetOrbit(currentlyEditing, CreateOrbit(inclination, eccentricity, semiMajorAxis,
          longitudeAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, body));
    }

    public static void GetComplex(OrbitDriver currentlyEditing, out double inclination, out double eccentricity,
        out double semiMajorAxis, out double longitudeAscendingNode, out double argumentOfPeriapsis,
        out double meanAnomalyAtEpoch, out double epoch, out CelestialBody body) {
      inclination = currentlyEditing.orbit.inclination;
      eccentricity = currentlyEditing.orbit.eccentricity;
      semiMajorAxis = currentlyEditing.orbit.semiMajorAxis;
      longitudeAscendingNode = currentlyEditing.orbit.LAN;
      argumentOfPeriapsis = currentlyEditing.orbit.argumentOfPeriapsis;
      meanAnomalyAtEpoch = currentlyEditing.orbit.meanAnomalyAtEpoch;
      epoch = currentlyEditing.orbit.epoch;
      body = currentlyEditing.orbit.referenceBody;
    }

    public static void Graphical(OrbitDriver currentlyEditing, double inclination, double eccentricity,
        double periapsis, double longitudeAscendingNode, double argumentOfPeriapsis,
        double meanAnomaly, double epoch) {
      var body = currentlyEditing.orbit.referenceBody;
      var soi = body.Soi();
      var ratio = soi / (body.Radius + body.atmosphereDepth + 1000);
      periapsis = Math.Pow(ratio, periapsis) / ratio;
      periapsis *= soi;

      eccentricity *= Math.PI / 2 - 0.001;

      eccentricity = Math.Tan(eccentricity);
      var semimajor = periapsis / (1 - eccentricity);

      if (semimajor < 0) {
        meanAnomaly -= 0.5;
        meanAnomaly *= eccentricity * 4; // 4 is arbitrary constant
      }

      inclination *= 360;
      longitudeAscendingNode *= 360;
      argumentOfPeriapsis *= 360;
      meanAnomaly *= 2 * Math.PI;

      SetOrbit(currentlyEditing, CreateOrbit(inclination, eccentricity, semimajor, longitudeAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch, body));
    }

    public static void GetGraphical(OrbitDriver currentlyEditing, out double inclination, out double eccentricity,
        out double periapsis, out double longitudeAscendingNode, out double argumentOfPeriapsis,
        out double meanAnomaly, out double epoch) {
      inclination = currentlyEditing.orbit.inclination / 360;
      inclination = inclination.Mod(1);
      longitudeAscendingNode = currentlyEditing.orbit.LAN / 360;
      longitudeAscendingNode = longitudeAscendingNode.Mod(1);
      argumentOfPeriapsis = currentlyEditing.orbit.argumentOfPeriapsis / 360;
      argumentOfPeriapsis = argumentOfPeriapsis.Mod(1);
      var eTemp = Math.Atan(currentlyEditing.orbit.eccentricity);
      eccentricity = eTemp / (Math.PI / 2 - 0.001);
      var soi = currentlyEditing.orbit.referenceBody.Soi();
      var ratio = soi / (currentlyEditing.orbit.referenceBody.Radius + currentlyEditing.orbit.referenceBody.atmosphereDepth + 1000);
      var semimajor = currentlyEditing.orbit.semiMajorAxis * (1 - currentlyEditing.orbit.eccentricity);
      semimajor /= soi;
      semimajor *= ratio;
      semimajor = Math.Log(semimajor, ratio);
      periapsis = semimajor;
      meanAnomaly = currentlyEditing.orbit.meanAnomalyAtEpoch;
      meanAnomaly /= (2 * Math.PI);
      if (currentlyEditing.orbit.semiMajorAxis < 0) {
        meanAnomaly /= currentlyEditing.orbit.eccentricity * 4;
        meanAnomaly += 0.5;
      }
      epoch = currentlyEditing.orbit.epoch;
    }

    public enum VelocityChangeDirection {
      Prograde,
      Normal,
      Radial,
      North,
      East,
      Up
    }

    public static VelocityChangeDirection[] AllVelocityChanges = Enum.GetValues(typeof(VelocityChangeDirection)).Cast<VelocityChangeDirection>().ToArray();

    public static void Velocity(OrbitDriver currentlyEditing, VelocityChangeDirection direction, double speed) {
      Vector3d velocity;
      switch (direction) {
        case VelocityChangeDirection.Prograde:
          velocity = currentlyEditing.orbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).normalized * speed;
          break;
        case VelocityChangeDirection.Normal:
          velocity = currentlyEditing.orbit.GetOrbitNormal().normalized * speed;
          break;
        case VelocityChangeDirection.Radial:
          velocity = Vector3d.Cross(currentlyEditing.orbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()), currentlyEditing.orbit.GetOrbitNormal()).normalized * speed;
          break;
        case VelocityChangeDirection.North:
          var upn = currentlyEditing.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
          velocity = Vector3d.Cross(Vector3d.Cross(upn, new Vector3d(0, 0, 1)), upn) * speed;
          break;
        case VelocityChangeDirection.East:
          var upe = currentlyEditing.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
          velocity = Vector3d.Cross(new Vector3d(0, 0, 1), upe) * speed;
          break;
        case VelocityChangeDirection.Up:
          velocity = currentlyEditing.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized * speed;
          break;
        default:
          throw new Exception("Unknown VelChangeDir");
      }
      var tempOrbit = currentlyEditing.orbit.Clone();
      tempOrbit.UpdateFromStateVectors(currentlyEditing.orbit.pos, currentlyEditing.orbit.vel + velocity, currentlyEditing.orbit.referenceBody, Planetarium.GetUniversalTime());
      SetOrbit(currentlyEditing, tempOrbit);
    }

    public static void GetVelocity(OrbitDriver currentlyEditing, out VelocityChangeDirection direction, out double speed) {
      direction = VelocityChangeDirection.Prograde;
      speed = 0;
    }

    public static void Rendezvous(OrbitDriver currentlyEditing, double leadTime, Vessel target) {
      SetOrbit(currentlyEditing, CreateOrbit(
          target.orbit.inclination,
          target.orbit.eccentricity,
          target.orbit.semiMajorAxis,
          target.orbit.LAN,
          target.orbit.argumentOfPeriapsis,
          target.orbit.meanAnomalyAtEpoch,
          target.orbit.epoch - leadTime,
          target.orbit.referenceBody));
    }

    private static void SetOrbit(OrbitDriver currentlyEditing, Orbit orbit) {
      currentlyEditing.DynamicSetOrbit(orbit);
    }

    private static Orbit CreateOrbit(double inc, double e, double sma, double lan, double w, double mEp, double epoch, CelestialBody body) {
      if (inc == 0) {
        inc = 0.0001d;
      }
      if (double.IsNaN(inc)) {
        inc = 0.0001d;
      }
      if (double.IsNaN(e)) {
        e = 0;
      }
      if (double.IsNaN(sma)) {
        sma = body.Radius + body.atmosphereDepth + 10000;
      }
      if (double.IsNaN(lan)) {
        lan = 0.0001d;
      }
      if (lan == 0) {
        lan = 0.0001d;
      }
      if (double.IsNaN(w)) {
        w = 0;
      }
      if (double.IsNaN(mEp)) {
        mEp = 0;
      }
      if (double.IsNaN(epoch)) {
        mEp = Planetarium.GetUniversalTime();
      }

      if (Math.Sign(e - 1) == Math.Sign(sma)) {
        sma = -sma;
      }

      if (Math.Sign(sma) >= 0) {
        while (mEp < 0)
          mEp += Math.PI * 2;
        while (mEp > Math.PI * 2)
          mEp -= Math.PI * 2;
      }

      // "inc" is probably inclination
      // "e" is probably eccentricity
      // "sma" is probably semi-major axis
      // "lan" is probably longitude of the ascending node
      // "w" is probably the argument of periapsis (omega)
      // mEp is probably a mean anomaly at some time, like epoch
      // t is probably current time

      return new Orbit(inc, e, sma, lan, w, mEp, epoch, body);
    }

    public static void DynamicSetOrbit(this OrbitDriver orbit, Orbit newOrbit) {
      var vessel = orbit.vessel;
      var body = orbit.celestialBody;
      if (vessel != null) {
        vessel.SetOrbit(newOrbit);
      } else if (body != null) {
        body.SetOrbit(newOrbit);
      } else {
        HardsetOrbit(orbit, newOrbit);
      }
    }

    public static void SetOrbit(this Vessel vessel, Orbit newOrbit) {
      var destinationMagnitude = newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude;
      if (destinationMagnitude > newOrbit.referenceBody.sphereOfInfluence) {
        UiHelper.WindowHelper.Error("Destination position was above the sphere of influence");
        return;
      }
      if (destinationMagnitude < newOrbit.referenceBody.Radius) {
        UiHelper.WindowHelper.Error("Destination position was below the surface");
        return;
      }

      vessel.PrepVesselTeleport();

      try {
        OrbitPhysicsManager.HoldVesselUnpack(60);
      }
      catch (NullReferenceException) {
        Utils.Log("OrbitPhysicsManager.HoldVesselUnpack threw NullReferenceException");
      }

      var allVessels = FlightGlobals.fetch?.vessels ?? (IEnumerable<Vessel>)new[] { vessel };
      foreach (var v in allVessels) {
        v.GoOnRails();
      }

      var oldBody = vessel.orbitDriver.orbit.referenceBody;

      HardsetOrbit(vessel.orbitDriver, newOrbit);

      vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
      vessel.orbitDriver.vel = vessel.orbit.vel;

      var newBody = vessel.orbitDriver.orbit.referenceBody;
      if (newBody != oldBody) {
        var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
        GameEvents.onVesselSOIChanged.Fire(evnt);
      }

      //vessel.situation = ORBITING
      if ( vessel.situation == Vessel.Situations.ORBITING ) {

      } else {
        vessel.situation = Vessel.Situations.ORBITING;
      }
      
    }

    public static void SetOrbit(this CelestialBody body, Orbit newOrbit) {
      var oldBody = body.referenceBody;
      HardsetOrbit(body.orbitDriver, newOrbit);
      if (oldBody != newOrbit.referenceBody) {
        oldBody.orbitingBodies.Remove(body);
        newOrbit.referenceBody.orbitingBodies.Add(body);
      }
      body.RealCbUpdate();
    }

    private static readonly object HardsetOrbitLogObject = new object();

    private static void HardsetOrbit(OrbitDriver orbitDriver, Orbit newOrbit) {
      var orbit = orbitDriver.orbit;
      orbit.inclination = newOrbit.inclination;
      orbit.eccentricity = newOrbit.eccentricity;
      orbit.semiMajorAxis = newOrbit.semiMajorAxis;
      orbit.LAN = newOrbit.LAN;
      orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
      orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
      orbit.epoch = newOrbit.epoch;
      orbit.referenceBody = newOrbit.referenceBody;
      orbit.Init();
      orbit.UpdateFromUT(Planetarium.GetUniversalTime());
      if (orbit.referenceBody != newOrbit.referenceBody) {
        orbitDriver.OnReferenceBodyChange?.Invoke(newOrbit.referenceBody);
      }
      RateLimitedLogger.Log(HardsetOrbitLogObject,
          $"Orbit \"{orbitDriver.OrbitDriverToString()}\" changed to: inc={orbit.inclination} ecc={orbit.eccentricity} sma={orbit.semiMajorAxis} lan={orbit.LAN} argpe={orbit.argumentOfPeriapsis} mep={orbit.meanAnomalyAtEpoch} epoch={orbit.epoch} refbody={orbit.referenceBody.CbToString()}");
    }

    public static Orbit Clone(this Orbit o) {
      return new Orbit(o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN,
          o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, o.referenceBody);
    }
  }

  public static class Panel {
    public static Action Create() {
      var view = View();
      return () => UiHelper.Window.Create("Orbit Editor", true, true, 300, -1, w => view.Draw());
    }

    // Also known as "closure hell"
    public static UiHelper.IView View() {
      UiHelper.ListSelectView<OrbitDriver> currentlyEditing = null;
      Action<OrbitDriver> onCurrentlyEditingChange = null;

      var setToCurrentOrbit = new UiHelper.ButtonView("Set to current orbit", "Sets all the fields of the editor to reflect the orbit of the currently selected vessel",
          () => onCurrentlyEditingChange(currentlyEditing.CurrentlySelected));

      var referenceSelector = new UiHelper.ListSelectView<CelestialBody>("Reference body", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.bodies, null, Utils.CbToString);

      #region Simple
      var simpleAltitude = new UiHelper.TextBoxView<double>("Altitude", "Altitude of circular orbit", 110000, SiSuffix.TryParse);
      var simpleApply = new UiHelper.ConditionalView(() => simpleAltitude.Valid && referenceSelector.CurrentlySelected != null,
                            new UiHelper.ButtonView("Apply", "Sets the orbit", () => {
                              OrbitEditor.Simple(currentlyEditing.CurrentlySelected, simpleAltitude.Object, referenceSelector.CurrentlySelected);

                              currentlyEditing.ReInvokeOnSelect();
                            }));
      var simple = new UiHelper.VerticalView(new UiHelper.IView[]
          {
                    simpleAltitude,
                    referenceSelector,
                    simpleApply,
                    setToCurrentOrbit
          });
      #endregion

      #region Complex
      var complexInclination = new UiHelper.TextBoxView<double>("Inclination", "How close to the equator the orbit plane is", 0, double.TryParse);
      var complexEccentricity = new UiHelper.TextBoxView<double>("Eccentricity", "How circular the orbit is (0=circular, 0.5=elliptical, 1=parabolic)", 0, double.TryParse);
      var complexSemiMajorAxis = new UiHelper.TextBoxView<double>("Semi-major axis", "Mean radius of the orbit (ish)", 10000000, SiSuffix.TryParse);
      var complexLongitudeAscendingNode = new UiHelper.TextBoxView<double>("Lon. of asc. node", "Longitude of the place where you cross the equator northwards", 0, double.TryParse);
      var complexArgumentOfPeriapsis = new UiHelper.TextBoxView<double>("Argument of periapsis", "Rotation of the orbit around the normal", 0, double.TryParse);
      var complexMeanAnomalyAtEpoch = new UiHelper.TextBoxView<double>("Mean anomaly at epoch", "Position along the orbit at the epoch", 0, double.TryParse);
      var complexEpoch = new UiHelper.TextBoxView<double>("Epoch", "Epoch at which mEp is measured", 0, SiSuffix.TryParse);
      var complexEpochNow = new UiHelper.ButtonView("Set epoch to now", "Sets the Epoch field to the current time", () => complexEpoch.Object = Planetarium.GetUniversalTime());
      var complexApply = new UiHelper.ConditionalView(() => complexInclination.Valid &&
                             complexEccentricity.Valid &&
                             complexSemiMajorAxis.Valid &&
                             complexLongitudeAscendingNode.Valid &&
                             complexArgumentOfPeriapsis.Valid &&
                             complexMeanAnomalyAtEpoch.Valid &&
                             complexEpoch.Valid &&
                             referenceSelector.CurrentlySelected != null,
                             new UiHelper.ButtonView("Apply", "Sets the orbit", () => {
                               OrbitEditor.Complex(currentlyEditing.CurrentlySelected,
                                         complexInclination.Object,
                                         complexEccentricity.Object,
                                         complexSemiMajorAxis.Object,
                                         complexLongitudeAscendingNode.Object,
                                         complexArgumentOfPeriapsis.Object,
                                         complexMeanAnomalyAtEpoch.Object,
                                         complexEpoch.Object,
                                         referenceSelector.CurrentlySelected);

                               currentlyEditing.ReInvokeOnSelect();
                             }));
      var complex = new UiHelper.VerticalView(new UiHelper.IView[]
          {
                    complexInclination,
                    complexEccentricity,
                    complexSemiMajorAxis,
                    complexLongitudeAscendingNode,
                    complexArgumentOfPeriapsis,
                    complexMeanAnomalyAtEpoch,
                    complexEpoch,
                    complexEpochNow,
                    referenceSelector,
                    complexApply,
                    setToCurrentOrbit
          });
      #endregion

      #region Graphical
      UiHelper.SliderView graphicalInclination = null;
      UiHelper.SliderView graphicalEccentricity = null;
      UiHelper.SliderView graphicalPeriapsis = null;
      UiHelper.SliderView graphicalLongitudeAscendingNode = null;
      UiHelper.SliderView graphicalArgumentOfPeriapsis = null;
      UiHelper.SliderView graphicalMeanAnomaly = null;
      double graphicalEpoch = 0;

      Action<double> graphicalOnChange = ignored => {
        OrbitEditor.Graphical(currentlyEditing.CurrentlySelected,
            graphicalInclination.Value,
            graphicalEccentricity.Value,
            graphicalPeriapsis.Value,
            graphicalLongitudeAscendingNode.Value,
            graphicalArgumentOfPeriapsis.Value,
            graphicalMeanAnomaly.Value,
            graphicalEpoch);

        currentlyEditing.ReInvokeOnSelect();
      };

      graphicalInclination = new UiHelper.SliderView("Inclination", "How close to the equator the orbit plane is", graphicalOnChange);
      graphicalEccentricity = new UiHelper.SliderView("Eccentricity", "How circular the orbit is", graphicalOnChange);
      graphicalPeriapsis = new UiHelper.SliderView("Periapsis", "Lowest point in the orbit", graphicalOnChange);
      graphicalLongitudeAscendingNode = new UiHelper.SliderView("Lon. of asc. node", "Longitude of the place where you cross the equator northwards", graphicalOnChange);
      graphicalArgumentOfPeriapsis = new UiHelper.SliderView("Argument of periapsis", "Rotation of the orbit around the normal", graphicalOnChange);
      graphicalMeanAnomaly = new UiHelper.SliderView("Mean anomaly", "Position along the orbit", graphicalOnChange);

      var graphical = new UiHelper.VerticalView(new UiHelper.IView[]
          {
                    graphicalInclination,
                    graphicalEccentricity,
                    graphicalPeriapsis,
                    graphicalLongitudeAscendingNode,
                    graphicalArgumentOfPeriapsis,
                    graphicalMeanAnomaly,
                    setToCurrentOrbit
          });
      #endregion

      #region Velocity
      var velocitySpeed = new UiHelper.TextBoxView<double>("Speed", "dV to apply", 0, SiSuffix.TryParse);
      var velocityDirection = new UiHelper.ListSelectView<OrbitEditor.VelocityChangeDirection>("Direction", () => OrbitEditor.AllVelocityChanges);
      var velocityApply = new UiHelper.ConditionalView(() => velocitySpeed.Valid,
                              new UiHelper.ButtonView("Apply", "Adds the selected velocity to the orbit", () => {
                                OrbitEditor.Velocity(currentlyEditing.CurrentlySelected, velocityDirection.CurrentlySelected, velocitySpeed.Object);
                              }));
      var velocity = new UiHelper.VerticalView(new UiHelper.IView[]
          {
                    velocitySpeed,
                    velocityDirection,
                    velocityApply
          });
      #endregion

      #region Rendezvous
      var rendezvousLeadTime = new UiHelper.TextBoxView<double>("Lead time", "How many seconds off to rendezvous at (zero = on top of each other, bad)", 1, SiSuffix.TryParse);
      var rendezvousVessel = new UiHelper.ListSelectView<Vessel>("Target vessel", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.vessels, null, Extensions.VesselToString);
      var rendezvousApply = new UiHelper.ConditionalView(() => rendezvousLeadTime.Valid && rendezvousVessel.CurrentlySelected != null,
                                new UiHelper.ButtonView("Apply", "Rendezvous", () => {
                                  OrbitEditor.Rendezvous(currentlyEditing.CurrentlySelected, rendezvousLeadTime.Object, rendezvousVessel.CurrentlySelected);
                                }));
      // rendezvous gets special ConditionalView to force only editing of planets
      var rendezvous = new UiHelper.ConditionalView(() => currentlyEditing.CurrentlySelected != null && currentlyEditing.CurrentlySelected.vessel != null,
                           new UiHelper.VerticalView(new UiHelper.IView[]
              {
                        rendezvousLeadTime,
                        rendezvousVessel,
                        rendezvousApply
              }));
      #endregion

      #region CurrentlyEditing
      onCurrentlyEditingChange = newEditing => {
        if (newEditing == null) {
          return;
        }
        {
          double altitude;
          CelestialBody body;
          OrbitEditor.GetSimple(newEditing, out altitude, out body);
          simpleAltitude.Object = altitude;
          referenceSelector.CurrentlySelected = body;
        }
        {
          double inclination;
          double eccentricity;
          double semiMajorAxis;
          double longitudeAscendingNode;
          double argumentOfPeriapsis;
          double meanAnomalyAtEpoch;
          double epoch;
          CelestialBody body;
          OrbitEditor.GetComplex(newEditing,
              out inclination,
              out eccentricity,
              out semiMajorAxis,
              out longitudeAscendingNode,
              out argumentOfPeriapsis,
              out meanAnomalyAtEpoch,
              out epoch,
              out body);
          complexInclination.Object = inclination;
          complexEccentricity.Object = eccentricity;
          complexSemiMajorAxis.Object = semiMajorAxis;
          complexLongitudeAscendingNode.Object = longitudeAscendingNode;
          complexArgumentOfPeriapsis.Object = argumentOfPeriapsis;
          complexMeanAnomalyAtEpoch.Object = meanAnomalyAtEpoch;
          complexEpoch.Object = epoch;
          referenceSelector.CurrentlySelected = body;
        }
        {
          double inclination;
          double eccentricity;
          double periapsis;
          double longitudeAscendingNode;
          double argumentOfPeriapsis;
          double meanAnomaly;
          OrbitEditor.GetGraphical(newEditing,
              out inclination,
              out eccentricity,
              out periapsis,
              out longitudeAscendingNode,
              out argumentOfPeriapsis,
              out meanAnomaly,
              out graphicalEpoch);
          graphicalInclination.Value = inclination;
          graphicalEccentricity.Value = eccentricity;
          graphicalPeriapsis.Value = periapsis;
          graphicalLongitudeAscendingNode.Value = longitudeAscendingNode;
          graphicalArgumentOfPeriapsis.Value = argumentOfPeriapsis;
          graphicalMeanAnomaly.Value = meanAnomaly;
        }
        {
          OrbitEditor.VelocityChangeDirection direction;
          double speed;
          OrbitEditor.GetVelocity(newEditing, out direction, out speed);
          velocityDirection.CurrentlySelected = direction;
          velocitySpeed.Object = speed;
        }
      };

      currentlyEditing = new UiHelper.ListSelectView<OrbitDriver>("Currently editing", OrbitEditor.OrderedOrbits, onCurrentlyEditingChange, Extensions.OrbitDriverToString);

      if (FlightGlobals.fetch != null && FlightGlobals.fetch.activeVessel != null && FlightGlobals.fetch.activeVessel.orbitDriver != null) {
        currentlyEditing.CurrentlySelected = FlightGlobals.fetch.activeVessel.orbitDriver;
      }
      #endregion

      /*
      var savePlanet = new UiHelper.ButtonView("Save planet", "Saves the current orbit of the planet to a file, so it stays edited even after a restart. Delete the file named the planet's name in " + ConfigHelper.GetPath(null) + " to undo.",
                           () => Planet.PlanetEditor.SavePlanet(currentlyEditing.CurrentlySelected.celestialBody));
      var resetPlanet = new UiHelper.ButtonView("Reset to defaults", "Reset the selected planet to defaults",
                            () => Planet.PlanetEditor.ResetToDefault(currentlyEditing.CurrentlySelected.celestialBody));

      var planetButtons = new UiHelper.ConditionalView(() => currentlyEditing.CurrentlySelected?.celestialBody != null,
                              new UiHelper.VerticalView(new UiHelper.IView[]
              {
                        savePlanet,
                        resetPlanet
              }));
      */

      var tabs = new UiHelper.TabView(new List<KeyValuePair<string, UiHelper.IView>>()
          {
                    new KeyValuePair<string, UiHelper.IView>("Simple", simple),
                    new KeyValuePair<string, UiHelper.IView>("Complex", complex),
                    new KeyValuePair<string, UiHelper.IView>("Graphical", graphical),
                    new KeyValuePair<string, UiHelper.IView>("Velocity", velocity),
                    new KeyValuePair<string, UiHelper.IView>("Rendezvous", rendezvous),
                });

      return new UiHelper.VerticalView(new UiHelper.IView[]
          {
                    currentlyEditing,
                    //planetButtons,
                    new UiHelper.ConditionalView(() => currentlyEditing.CurrentlySelected != null, tabs)
          });
    }
  }
}
