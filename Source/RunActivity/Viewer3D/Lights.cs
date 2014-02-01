﻿// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

// Uncomment either or both of these for debugging information about lights.
//#define DEBUG_LIGHT_STATES
//#define DEBUG_LIGHT_TRANSITIONS
//#define DEBUG_LIGHT_CONE
//#define DEBUG_LIGHT_CONE_FULL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS;
using ORTS.Processes;

namespace ORTS
{
    /// <summary>
    /// A LightState object encapsulates the data for each State in the States subblock.
    /// </summary>
    public class LightState
    {
        public float Duration;
        public uint Color;
        public Vector3 Position;
        public float Radius;
        public Vector3 Azimuth;
        public Vector3 Elevation;
        public bool Transition;
        public float Angle;

        public LightState(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("duration", ()=>{ Duration = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("lightcolour", ()=>{ Color = stf.ReadHexBlock(null); }),
                new STFReader.TokenProcessor("position", ()=>{ Position = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("radius", ()=>{ Radius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("azimuth", ()=>{ Azimuth = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("elevation", ()=>{ Elevation = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("transition", ()=>{ Transition = 1 <= stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
                new STFReader.TokenProcessor("angle", ()=>{ Angle = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
            });
        }

        public LightState(LightState state, bool reverse)
        {
            Duration = state.Duration;
            Color = state.Color;
            Position = state.Position;
            Radius = state.Radius;
            Azimuth = state.Azimuth;
            Elevation = state.Elevation;
            Transition = state.Transition;
            Angle = state.Angle;

            if (reverse)
            {
                Azimuth.X += 180;
                Azimuth.X %= 360;
                Azimuth.Y += 180;
                Azimuth.Y %= 360;
                Azimuth.Z += 180;
                Azimuth.Z %= 360;
                Position.X *= -1;
                Position.Z *= -1;
            }
        }
    }

    #region Light enums
    public enum LightType
    {
        Glow,
        Cone,
    }

    public enum LightHeadlightCondition
    {
        Ignore,
        Off,
        Dim,
        Bright,
        DimBright, // MSTSBin
        OffDim, // MSTSBin
        OffBright, // MSTSBin
        // TODO: DimBright?, // MSTSBin labels this the same as DimBright. Not sure what it means.
    }

    public enum LightUnitCondition
    {
        Ignore,
        Middle,
        First,
        Last,
        LastRev, // MSTSBin
        FirstRev, // MSTSBin
    }

    public enum LightPenaltyCondition
    {
        Ignore,
        No,
        Yes,
    }

    public enum LightControlCondition
    {
        Ignore,
        AI,
        Player,
    }

    public enum LightServiceCondition
    {
        Ignore,
        No,
        Yes,
    }

    public enum LightTimeOfDayCondition
    {
        Ignore,
        Day,
        Night,
    }

    public enum LightWeatherCondition
    {
        Ignore,
        Clear,
        Rain,
        Snow,
    }

    public enum LightCouplingCondition
    {
        Ignore,
        Front,
        Rear,
        Both,
    }
    #endregion

    /// <summary>
    /// The Light class encapsulates the data for each Light object 
    /// in the Lights block of an ENG/WAG file. 
    /// </summary>
    public class Light
    {
        public int Index;
        public LightType Type;
        public LightHeadlightCondition Headlight;
        public LightUnitCondition Unit;
        public LightPenaltyCondition Penalty;
        public LightControlCondition Control;
        public LightServiceCondition Service;
        public LightTimeOfDayCondition TimeOfDay;
        public LightWeatherCondition Weather;
        public LightCouplingCondition Coupling;
        public bool Cycle;
        public float FadeIn;
        public float FadeOut;
        public List<LightState> States = new List<LightState>();

        public Light(int index, STFReader stf)
        {
            Index = index;
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("type", ()=>{ Type = (LightType)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("conditions", ()=>{ stf.MustMatch("("); stf.ParseBlock(new[] {
                    new STFReader.TokenProcessor("headlight", ()=>{ Headlight = (LightHeadlightCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("unit", ()=>{ Unit = (LightUnitCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("penalty", ()=>{ Penalty = (LightPenaltyCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("control", ()=>{ Control = (LightControlCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("service", ()=>{ Service = (LightServiceCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("timeofday", ()=>{ TimeOfDay = (LightTimeOfDayCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("weather", ()=>{ Weather = (LightWeatherCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("coupling", ()=>{ Coupling = (LightCouplingCondition)stf.ReadIntBlock(null); }),
                });}),
                new STFReader.TokenProcessor("cycle", ()=>{ Cycle = 0 != stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("fadein", ()=>{ FadeIn = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("fadeout", ()=>{ FadeOut = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    var count = stf.ReadInt(null);
                    stf.ParseBlock(new[] {
                        new STFReader.TokenProcessor("state", ()=>{
                            if (States.Count >= count)
                                STFException.TraceWarning(stf, "Skipped extra State");
                            else
                                States.Add(new LightState(stf));
                        }),
                    });
                    if (States.Count < count)
                        STFException.TraceWarning(stf, (count - States.Count).ToString() + " missing State(s)");
                }),
            });
        }

        public Light(Light light, bool reverse)
        {
            Index = light.Index;
            Type = light.Type;
            Headlight = light.Headlight;
            Unit = light.Unit;
            Penalty = light.Penalty;
            Control = light.Control;
            Service = light.Service;
            TimeOfDay = light.TimeOfDay;
            Weather = light.Weather;
            Coupling = light.Coupling;
            Cycle = light.Cycle;
            FadeIn = light.FadeIn;
            FadeOut = light.FadeOut;
            foreach (var state in light.States)
                States.Add(new LightState(state, reverse));

            if (reverse)
            {
                if (Unit == LightUnitCondition.First)
                    Unit = LightUnitCondition.FirstRev;
                else if (Unit == LightUnitCondition.Last)
                    Unit = LightUnitCondition.LastRev;
            }
        }
    }

    /// <summary>
    /// A Lights object is created for any engine or wagon having a 
    /// Lights block in its ENG/WAG file. It contains a collection of
    /// Light objects.
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class LightCollection
    {
        public List<Light> Lights = new List<Light>();

        public LightCollection(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ReadInt(null); // count; ignore this because its not always correct
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("light", ()=>{ Lights.Add(new Light(Lights.Count, stf)); }),
            });
            if (Lights.Count == 0)
                throw new InvalidDataException("lights with no lights");

            // MSTSBin created reverse headlight cones automatically, so we shall do so too.
            foreach (var light in Lights.ToArray())
                if (light.Type == LightType.Cone)
                    Lights.Add(new Light(light, true));
        }
    }

    public class LightDrawer
    {
        readonly Viewer3D Viewer;
        readonly TrainCar Car;
        readonly Material LightGlowMaterial;
        readonly Material LightConeMaterial;

        public int TrainHeadlight;
        public bool CarIsReversed;
        public bool CarIsFirst;
        public bool CarIsLast;
        public bool Penalty;
        public bool CarIsPlayer;
        public bool CarInService;
        public bool IsDay;
        public WeatherType Weather;
        public bool CarCoupledFront;
        public bool CarCoupledRear;

        public bool IsLightConeActive { get { return ActiveLightCone != null; } }
        List<LightMesh> LightMeshes = new List<LightMesh>();

        LightConeMesh ActiveLightCone;
        public bool HasLightCone;
        public float LightConeFadeIn;
        public float LightConeFadeOut;
        public Vector3 LightConePosition;
        public Vector3 LightConeDirection;
        public float LightConeDistance;
        public float LightConeMinDotProduct;
        public Vector4 LightConeColor;

        public LightDrawer(Viewer3D viewer, TrainCar car)
        {
            Viewer = viewer;
            Car = car;
            LightGlowMaterial = viewer.MaterialManager.Load("LightGlow");
            LightConeMaterial = viewer.MaterialManager.Load("LightCone");

            UpdateState();
            if (Car.Lights != null)
            {
                foreach (var light in Car.Lights.Lights)
                {
                    switch (light.Type)
                    {
                        case LightType.Glow:
                            LightMeshes.Add(new LightGlowMesh(this, Viewer.RenderProcess, light));
                            break;
                        case LightType.Cone:
                            LightMeshes.Add(new LightConeMesh(this, Viewer.RenderProcess, light));
                            break;
                    }
                }
            }
            HasLightCone = LightMeshes.Any(lm => lm is LightConeMesh);
#if DEBUG_LIGHT_STATES
            Console.WriteLine();
#endif
            UpdateActiveLightCone();
        }

        void UpdateActiveLightCone()
        {
            var newLightCone = (LightConeMesh)LightMeshes.FirstOrDefault(lm => lm is LightConeMesh && lm.Enabled);

            // Fade-in should be NEW headlight.
            if ((ActiveLightCone == null) && (newLightCone != null))
                LightConeFadeIn = newLightCone.Light.FadeIn;
            else
                LightConeFadeIn = 0;

            // Fade-out should be OLD headlight.
            if ((ActiveLightCone != null) && (newLightCone == null))
                LightConeFadeOut = ActiveLightCone.Light.FadeOut;
            else
                LightConeFadeOut = 0;

#if DEBUG_LIGHT_STATES
            if (ActiveLightCone != null)
                Console.WriteLine("Old headlight: index = {0}, fade-in = {1:F1}, fade-out = {2:F1}, position = {3}, angle = {4:F1}, radius = {5:F1}", ActiveLightCone.Light.Index, ActiveLightCone.Light.FadeIn, ActiveLightCone.Light.FadeOut, ActiveLightCone.Light.States[0].Position, ActiveLightCone.Light.States[0].Angle, ActiveLightCone.Light.States[0].Radius);
            else
                Console.WriteLine("Old headlight: <none>");
            if (newLightCone != null)
                Console.WriteLine("New headlight: index = {0}, fade-in = {1:F1}, fade-out = {2:F1}, position = {3}, angle = {4:F1}, radius = {5:F1}", newLightCone.Light.Index, newLightCone.Light.FadeIn, newLightCone.Light.FadeOut, newLightCone.Light.States[0].Position, newLightCone.Light.States[0].Angle, newLightCone.Light.States[0].Radius);
            else
                Console.WriteLine("New headlight: <none>");
            if ((ActiveLightCone != null) || (newLightCone != null))
            {
                Console.WriteLine("Headlight changed from {0} to {1}, fade-in = {2:F1}, fade-out = {3:F1}", ActiveLightCone != null ? ActiveLightCone.Light.Index.ToString() : "<none>", newLightCone != null ? newLightCone.Light.Index.ToString() : "<none>", LightConeFadeIn, LightConeFadeOut);
                Console.WriteLine();
            }
#endif

            ActiveLightCone = newLightCone;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (UpdateState())
            {
                foreach (var lightMesh in LightMeshes)
                    lightMesh.UpdateState(this);
#if DEBUG_LIGHT_STATES
                Console.WriteLine();
#endif
                UpdateActiveLightCone();
            }

            foreach (var lightMesh in LightMeshes)
                lightMesh.PrepareFrame(frame, elapsedTime);

            int dTileX = Car.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Car.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Car.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = 20; // Even more arbitrary.
            float objectViewingDistance = Viewer.Settings.ViewingDistance; // Arbitrary.
            if (Viewer.Camera.CanSee(mstsLocation, objectRadius, objectViewingDistance))
                foreach (var lightMesh in LightMeshes)
                    if (lightMesh.Enabled || lightMesh.FadeOut)
                        if (lightMesh is LightGlowMesh)
                            frame.AddPrimitive(LightGlowMaterial, lightMesh, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);

#if DEBUG_LIGHT_CONE
            foreach (var lightMesh in LightMeshes)
                if (lightMesh.Enabled || lightMesh.FadeOut)
                    if (lightMesh is LightConeMesh)
                            frame.AddPrimitive(LightConeMaterial, lightMesh, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);
#endif

            // Set the active light cone info for the material code.
            if (HasLightCone && ActiveLightCone != null)
            {
                LightConePosition = Vector3.Transform(Vector3.Lerp(ActiveLightCone.Position1, ActiveLightCone.Position2, ActiveLightCone.Fade.Y), xnaDTileTranslation);
                LightConeDirection = Vector3.Transform(Vector3.Lerp(ActiveLightCone.Direction1, ActiveLightCone.Direction2, ActiveLightCone.Fade.Y), Car.WorldPosition.XNAMatrix);
                LightConeDirection -= Car.WorldPosition.XNAMatrix.Translation;
                LightConeDirection.Normalize();
                LightConeDistance = MathHelper.Lerp(ActiveLightCone.Distance1, ActiveLightCone.Distance2, ActiveLightCone.Fade.Y);
                LightConeMinDotProduct = (float)Math.Cos(MathHelper.Lerp(ActiveLightCone.Angle1, ActiveLightCone.Angle2, ActiveLightCone.Fade.Y));
                LightConeColor = Vector4.Lerp(ActiveLightCone.Color1, ActiveLightCone.Color2, ActiveLightCone.Fade.Y);
            }
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            LightGlowMaterial.Mark();
            LightConeMaterial.Mark();
        }

        public static void CalculateLightCone(LightState lightState, out Vector3 position, out Vector3 direction, out float angle, out float radius, out float distance, out Vector4 color)
        {
            position = lightState.Position;
            position.Z *= -1;
            direction = -Vector3.UnitZ;
            direction = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-lightState.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-lightState.Azimuth.Y)));
            angle = MathHelper.ToRadians(lightState.Angle) / 2;
            radius = lightState.Radius / 2;
            distance = (float)(radius / Math.Sin(angle));
            color = new Color() { PackedValue = lightState.Color }.ToVector4();
        }

#if DEBUG_LIGHT_STATES
        public const string MeshStateLabel = "Index       Enabled     Type        Headlight   Unit        Penalty     Control     Service     Time        Weather     Coupling  ";
        public const string MeshStateFormat = "{0,-10  }  {1,-10   }  {2,-10   }  {3,-10   }  {4,-10   }  {5,-10   }  {6,-10   }  {7,-10   }  {8,-10   }  {9,-10   }  {10,-10  }";
#endif

        bool UpdateState()
        {
			Debug.Assert(Viewer.PlayerTrain.LeadLocomotive == Viewer.PlayerLocomotive, "PlayerTrain.LeadLocomotive must be PlayerLocomotive.");
			var locomotive = Car.Train != null && Car.Train.LeadLocomotive != null ? Car.Train.LeadLocomotive : null;
			var mstsLocomotive = locomotive as MSTSLocomotive;

            // Headlight
			var newTrainHeadlight = locomotive != null ? locomotive.Headlight : 2;
            // Unit
			var locomotiveFlipped = locomotive != null && locomotive.Flipped;
			var locomotiveReverseCab = mstsLocomotive != null && mstsLocomotive.UsingRearCab;
            var newCarIsReversed = Car.Flipped ^ locomotiveFlipped ^ locomotiveReverseCab;
			var newCarIsFirst = Car.Train == null || (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train.LastCar : Car.Train.FirstCar) == Car;
			var newCarIsLast = Car.Train == null || (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train.FirstCar : Car.Train.LastCar) == Car;
            // Penalty
			var newPenalty = mstsLocomotive != null && mstsLocomotive.TrainBrakeController.GetIsEmergency();
            // Control
            var newCarIsPlayer = (Car.Train != null && Car.Train == Viewer.PlayerTrain) || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE);
            // Service
            var newCarInService = Car.Train != null;
            // Time of day
            bool newIsDay = false;
            if (Viewer.Settings.UseMSTSEnv == false)
                newIsDay = Viewer.World.Sky.solarDirection.Y > 0;
            else
                newIsDay = Viewer.World.MSTSSky.mstsskysolarDirection.Y > 0;
            // Weather
            var newWeather = Viewer.Simulator.Weather;
            // Coupling
            var newCarCoupledFront = Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.LastCar : Car.Train.FirstCar) != Car);
            var newCarCoupledRear = Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.FirstCar : Car.Train.LastCar) != Car);

            if (
                (TrainHeadlight != newTrainHeadlight) ||
                (CarIsReversed != newCarIsReversed) ||
                (CarIsFirst != newCarIsFirst) ||
                (CarIsLast != newCarIsLast) ||
                (Penalty != newPenalty) ||
                (CarIsPlayer != newCarIsPlayer) ||
                (CarInService != newCarInService) ||
                (IsDay != newIsDay) ||
                (Weather != newWeather) ||
                (CarCoupledFront != newCarCoupledFront) ||
                (CarCoupledRear != newCarCoupledRear))
            {
                TrainHeadlight = newTrainHeadlight;
                CarIsReversed = newCarIsReversed;
                CarIsFirst = newCarIsFirst;
                CarIsLast = newCarIsLast;
                Penalty = newPenalty;
                CarIsPlayer = newCarIsPlayer;
                CarInService = newCarInService;
                IsDay = newIsDay;
                Weather = newWeather;
                CarCoupledFront = newCarCoupledFront;
                CarCoupledRear = newCarCoupledRear;

#if DEBUG_LIGHT_STATES
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("LightDrawer: {0} {1} {2:D}{3}:{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}",
                    Car.Train != null ? Car.Train.FrontTDBTraveller.WorldLocation : Car.WorldPosition.WorldLocation, Car.Train != null ? "train car" : "car", Car.Train != null ? Car.Train.Cars.IndexOf(Car) : 0, Car.Flipped ? " (flipped)" : "",
                    TrainHeadlight == 2 ? " HL=Bright" : TrainHeadlight == 1 ? " HL=Dim" : "",
                    CarIsReversed ? " Reversed" : "",
                    CarIsFirst ? " First" : "",
                    CarIsLast ? " Last" : "",
                    Penalty ? " Penalty" : "",
                    CarIsPlayer ? " Player" : " AI",
                    CarInService ? " Service" : "",
                    IsDay ? "" : " Night",
                    Weather == WeatherType.Snow ? " Snow" : Weather == WeatherType.Rain ? " Rain" : "",
                    CarCoupledFront ? " CoupledFront" : "",
                    CarCoupledRear ? " CoupledRear" : "");
                if (Car.Lights != null)
                {
                    Console.WriteLine();
                    Console.WriteLine(MeshStateLabel);
                    Console.WriteLine(new String('=', MeshStateLabel.Length));
                }
#endif

                return true;
            }
            return false;
        }
    }

    public abstract class LightMesh : RenderPrimitive
    {
        public Light Light;
        public bool Enabled;
        public Vector2 Fade;
        public bool FadeIn;
        public bool FadeOut;
        protected float FadeTime;
        protected int State;
        protected int StateCount;
        protected float StateTime;

        public LightMesh(Light light)
        {
            Light = light;
            StateCount = Light.Cycle ? 2 * Light.States.Count - 2 : Light.States.Count;
            UpdateStates(State, (State + 1) % StateCount);
        }

        protected void SetUpTransitions(Action<int, int, int> transitionHandler)
        {
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
            Console.WriteLine("LightMesh transitions:");
#endif
            if (Light.Cycle)
            {
                for (var i = 0; i < Light.States.Count - 1; i++)
                    transitionHandler(i, i, i + 1);
                for (var i = Light.States.Count - 1; i > 0; i--)
                    transitionHandler(Light.States.Count * 2 - 1 - i, i, i - 1);
            }
            else
            {
                for (var i = 0; i < Light.States.Count; i++)
                    transitionHandler(i, i, (i + 1) % Light.States.Count);
            }
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
#endif
        }

        internal void UpdateState(LightDrawer lightDrawer)
        {
            var oldEnabled = Enabled;
            Enabled = true;
            if (Light.Headlight != LightHeadlightCondition.Ignore)
            {
                if (Light.Headlight == LightHeadlightCondition.Off)
                    Enabled &= lightDrawer.TrainHeadlight == 0;
                else if (Light.Headlight == LightHeadlightCondition.Dim)
                    Enabled &= lightDrawer.TrainHeadlight == 1;
                else if (Light.Headlight == LightHeadlightCondition.Bright)
                    Enabled &= lightDrawer.TrainHeadlight == 2;
                else if (Light.Headlight == LightHeadlightCondition.DimBright)
                    Enabled &= lightDrawer.TrainHeadlight >= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffDim)
                    Enabled &= lightDrawer.TrainHeadlight <= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffBright)
                    Enabled &= lightDrawer.TrainHeadlight != 1;
                else
                    Enabled &= false;
            }
            if (Light.Unit != LightUnitCondition.Ignore)
            {
                if (Light.Unit == LightUnitCondition.Middle)
                    Enabled &= !lightDrawer.CarIsFirst && !lightDrawer.CarIsLast;
                else if (Light.Unit == LightUnitCondition.First)
                    Enabled &= lightDrawer.CarIsFirst && !lightDrawer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.Last)
                    Enabled &= lightDrawer.CarIsLast && !lightDrawer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.LastRev)
                    Enabled &= lightDrawer.CarIsLast && lightDrawer.CarIsReversed;
                else if (Light.Unit == LightUnitCondition.FirstRev)
                    Enabled &= lightDrawer.CarIsFirst && lightDrawer.CarIsReversed;
                else
                    Enabled &= false;
            }
            if (Light.Penalty != LightPenaltyCondition.Ignore)
            {
                if (Light.Penalty == LightPenaltyCondition.No)
                    Enabled &= !lightDrawer.Penalty;
                else if (Light.Penalty == LightPenaltyCondition.Yes)
                    Enabled &= lightDrawer.Penalty;
                else
                    Enabled &= false;
            }
            if (Light.Control != LightControlCondition.Ignore)
            {
                if (Light.Control == LightControlCondition.AI)
                    Enabled &= !lightDrawer.CarIsPlayer;
                else if (Light.Control == LightControlCondition.Player)
                    Enabled &= lightDrawer.CarIsPlayer;
                else
                    Enabled &= false;
            }
            if (Light.Service != LightServiceCondition.Ignore)
            {
                if (Light.Service == LightServiceCondition.No)
                    Enabled &= !lightDrawer.CarInService;
                else if (Light.Service == LightServiceCondition.Yes)
                    Enabled &= lightDrawer.CarInService;
                else
                    Enabled &= false;
            }
            if (Light.TimeOfDay != LightTimeOfDayCondition.Ignore)
            {
                if (Light.TimeOfDay == LightTimeOfDayCondition.Day)
                    Enabled &= lightDrawer.IsDay;
                else if (Light.TimeOfDay == LightTimeOfDayCondition.Night)
                    Enabled &= !lightDrawer.IsDay;
                else
                    Enabled &= false;
            }
            if (Light.Weather != LightWeatherCondition.Ignore)
            {
                if (Light.Weather == LightWeatherCondition.Clear)
                    Enabled &= lightDrawer.Weather == WeatherType.Clear;
                else if (Light.Weather == LightWeatherCondition.Rain)
                    Enabled &= lightDrawer.Weather == WeatherType.Rain;
                else if (Light.Weather == LightWeatherCondition.Snow)
                    Enabled &= lightDrawer.Weather == WeatherType.Snow;
                else
                    Enabled &= false;
            }
            if (Light.Coupling != LightCouplingCondition.Ignore)
            {
                if (Light.Coupling == LightCouplingCondition.Front)
                    Enabled &= lightDrawer.CarCoupledFront && !lightDrawer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Rear)
                    Enabled &= !lightDrawer.CarCoupledFront && lightDrawer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Both)
                    Enabled &= lightDrawer.CarCoupledFront && lightDrawer.CarCoupledRear;
                else
                    Enabled &= false;
            }

            if (oldEnabled != Enabled)
            {
                FadeIn = Enabled;
                FadeOut = !Enabled;
                FadeTime = 0;
            }

#if DEBUG_LIGHT_STATES
            Console.WriteLine(LightDrawer.MeshStateFormat, Light.Index, Enabled, Light.Type, Light.Headlight, Light.Unit, Light.Penalty, Light.Control, Light.Service, Light.TimeOfDay, Light.Weather, Light.Coupling);
#endif
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (StateCount > 1)
            {
                StateTime += elapsedTime.ClockSeconds;
                if (StateTime >= Light.States[State % Light.States.Count].Duration)
                {
                    StateTime -= Light.States[State % Light.States.Count].Duration;
                    State = (State + 1) % StateCount;
                    UpdateStates(State, (State + 1) % StateCount);
                    Fade.Y = 0;
                }
                if (Light.States[State % Light.States.Count].Transition)
                    Fade.Y = StateTime / Light.States[State % Light.States.Count].Duration;
            }
            if (FadeIn)
            {
                FadeTime += elapsedTime.ClockSeconds;
                Fade.X = FadeTime / Light.FadeIn;
                if (Fade.X > 1)
                {
                    FadeIn = false;
                    Fade.X = 1;
                }
            }
            else if (FadeOut)
            {
                FadeTime += elapsedTime.ClockSeconds;
                Fade.X = 1 - FadeTime / Light.FadeIn;
                if (Fade.X < 0)
                {
                    FadeOut = false;
                    Fade.X = 0;
                }
            }
        }

        protected virtual void UpdateStates(int stateIndex1, int stateIndex2)
        {
        }
    }

    public class LightGlowMesh : LightMesh
    {
        static VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        static IndexBuffer IndexBuffer;

        public LightGlowMesh(LightDrawer lightDrawer, RenderProcess renderProcess, Light light)
            : base(light)
        {
            Debug.Assert(light.Type == LightType.Glow, "LightGlowMesh is only for LightType.Glow lights.");

            if (VertexDeclaration == null)
                VertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, LightGlowVertex.VertexElements);
            if (VertexBuffer == null)
            {
                var vertexData = new LightGlowVertex[6 * StateCount];
                SetUpTransitions((state, stateIndex1, stateIndex2) =>
                {
                    var state1 = Light.States[stateIndex1];
                    var state2 = Light.States[stateIndex2];

#if DEBUG_LIGHT_TRANSITIONS
                    Console.WriteLine("    Transition {0} is from state {1} to state {2} over {3:F1}s", state, stateIndex1, stateIndex2, state1.Duration);
#endif

                    // FIXME: Is conversion of "azimuth" to a normal right?

                    var position1 = state1.Position; position1.Z *= -1;
                    var normal1 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state1.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state1.Azimuth.Y)));
                    var color1 = new Color() { PackedValue = state1.Color }.ToVector4();

                    var position2 = state2.Position; position2.Z *= -1;
                    var normal2 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state2.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state2.Azimuth.Y)));
                    var color2 = new Color() { PackedValue = state2.Color }.ToVector4();

                    vertexData[6 * state + 0] = new LightGlowVertex(new Vector2(1, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 1] = new LightGlowVertex(new Vector2(0, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 2] = new LightGlowVertex(new Vector2(1, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 3] = new LightGlowVertex(new Vector2(1, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 4] = new LightGlowVertex(new Vector2(0, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 5] = new LightGlowVertex(new Vector2(0, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                });
                VertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, typeof(LightGlowVertex), vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(vertexData);
            }
            if (IndexBuffer == null)
            {
                var indexData = new short[] {
                    0, 1, 2, 3, 4, 5
                };
                IndexBuffer = new IndexBuffer(renderProcess.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
            }

            UpdateState(lightDrawer);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, LightGlowVertex.SizeInBytes);
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 6 * State, 0, 6, 0, 2);
        }
    }

    struct LightGlowVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector3 NormalO;
        public Vector3 NormalT;
        public Vector4 ColorO;
        public Vector4 ColorT;
        public Vector2 TexCoords;
        public float RadiusO;
        public float RadiusT;

        public LightGlowVertex(Vector2 texCoords, Vector3 position1, Vector3 position2, Vector3 normal1, Vector3 normal2, Vector4 color1, Vector4 color2, float radius1, float radius2)
        {
            PositionO = position1;
            PositionT = position2;
            NormalO = normal1;
            NormalT = normal2;
            ColorO = color1;
            ColorT = color2;
            TexCoords = texCoords;
            RadiusO = radius1;
            RadiusT = radius2;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(0, sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0, sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 1),
            new VertexElement(0, sizeof(float) * (3 + 3), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 1),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 3), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 0),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 3 + 4), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 1),
            new VertexElement(0, sizeof(float) * (3 + 3 + 3 + 3 + 4 + 4), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
        };

        public static int SizeInBytes = sizeof(float) * (3 + 3 + 3 + 3 + 4 + 4 + 4);
    }

    public class LightConeMesh : LightMesh
    {
        const int CircleSegments = 16;

        static VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        static IndexBuffer IndexBuffer;

        public LightConeMesh(LightDrawer lightDrawer, RenderProcess renderProcess, Light light)
            : base(light)
        {
            Debug.Assert(light.Type == LightType.Cone, "LightConeMesh is only for LightType.Cone lights.");

            if (VertexDeclaration == null)
                VertexDeclaration = new VertexDeclaration(renderProcess.GraphicsDevice, LightConeVertex.VertexElements);
            if (VertexBuffer == null)
            {
                var vertexData = new LightConeVertex[(CircleSegments + 2) * StateCount];
                SetUpTransitions((state, stateIndex1, stateIndex2) =>
                {
                    var state1 = Light.States[stateIndex1];
                    var state2 = Light.States[stateIndex2];

#if DEBUG_LIGHT_TRANSITIONS
                    Console.WriteLine("    Transition {0} is from state {1} to state {2} over {3:F1}s", state, stateIndex1, stateIndex2, state1.Duration);
#endif

                    Vector3 position1, position2, direction1, direction2;
                    float angle1, angle2, radius1, radius2, distance1, distance2;
                    Vector4 color1, color2;
                    LightDrawer.CalculateLightCone(state1, out position1, out direction1, out angle1, out radius1, out distance1, out color1);
                    LightDrawer.CalculateLightCone(state2, out position2, out direction2, out angle2, out radius2, out distance2, out color2);
                    var direction1Right = Vector3.Cross(direction1, Vector3.UnitY);
                    var direction1Up = Vector3.Cross(direction1Right, direction1);
                    var direction2Right = Vector3.Cross(direction2, Vector3.UnitY);
                    var direction2Up = Vector3.Cross(direction2Right, direction2);

                    for (var i = 0; i < CircleSegments; i++)
                    {
                        var a1 = MathHelper.TwoPi * i / CircleSegments;
                        var a2 = MathHelper.TwoPi * (i + 1) / CircleSegments;
                        var v1 = position1 + direction1 * distance1 + direction1Right * (float)(radius1 * Math.Cos(a1)) + direction1Up * (float)(radius1 * Math.Sin(a1));
                        var v2 = position2 + direction2 * distance2 + direction2Right * (float)(radius2 * Math.Cos(a2)) + direction2Up * (float)(radius2 * Math.Sin(a2));
                        vertexData[(CircleSegments + 2) * state + i] = new LightConeVertex(v1, v2, color1, color2);
                    }
                    vertexData[(CircleSegments + 2) * state + CircleSegments + 0] = new LightConeVertex(position1, position2, color1, color2);
                    vertexData[(CircleSegments + 2) * state + CircleSegments + 1] = new LightConeVertex(new Vector3(position1.X, position1.Y, position1.Z - distance1), new Vector3(position2.X, position2.Y, position2.Z - distance2), color1, color2);
                });
                VertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, typeof(LightConeVertex), vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(vertexData);
            }
            if (IndexBuffer == null)
            {
                var indexData = new short[6 * CircleSegments];
                for (var i = 0; i < CircleSegments; i++)
                {
                    var i2 = (i + 1) % CircleSegments;
                    indexData[6 * i + 0] = (short)(CircleSegments + 0);
                    indexData[6 * i + 1] = (short)i2;
                    indexData[6 * i + 2] = (short)i;
                    indexData[6 * i + 3] = (short)i;
                    indexData[6 * i + 4] = (short)i2;
                    indexData[6 * i + 5] = (short)(CircleSegments + 1);
                }
                IndexBuffer = new IndexBuffer(renderProcess.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
            }

            UpdateState(lightDrawer);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, LightConeVertex.SizeInBytes);
            graphicsDevice.Indices = IndexBuffer;

            var rs = graphicsDevice.RenderState;
#if DEBUG_LIGHT_CONE_FULL
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.One;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, (CircleSegments + 2) * State, 0, CircleSegments + 2, 0, 2 * CircleSegments);
#else
            rs.CullMode = CullMode.CullClockwiseFace;
            rs.StencilFunction = CompareFunction.Always;
            rs.StencilPass = StencilOperation.Increment;
            rs.DepthBufferFunction = CompareFunction.Greater;
            rs.DestinationBlend = Blend.One;
            rs.SourceBlend = Blend.Zero;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, (CircleSegments + 2) * State, 0, CircleSegments + 2, 0, 2 * CircleSegments);

            rs.CullMode = CullMode.CullCounterClockwiseFace;
            rs.StencilFunction = CompareFunction.Less;
            rs.StencilPass = StencilOperation.Zero;
            rs.DepthBufferFunction = CompareFunction.LessEqual;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.One;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, (CircleSegments + 2) * State, 0, CircleSegments + 2, 0, 2 * CircleSegments);
#endif
        }

        public Vector3 Position1, Position2, Direction1, Direction2;
        public float Angle1, Angle2, Radius1, Radius2, Distance1, Distance2;
        public Vector4 Color1, Color2;

        protected override void UpdateStates(int stateIndex1, int stateIndex2)
        {
            var state1 = Light.States[stateIndex1];
            var state2 = Light.States[stateIndex2];

            LightDrawer.CalculateLightCone(state1, out Position1, out Direction1, out Angle1, out Radius1, out Distance1, out Color1);
            LightDrawer.CalculateLightCone(state2, out Position2, out Direction2, out Angle2, out Radius2, out Distance2, out Color2);
        }
    }

    struct LightConeVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector4 ColorO;
        public Vector4 ColorT;

        public LightConeVertex(Vector3 position1, Vector3 position2, Vector4 color1, Vector4 color2)
        {
            PositionO = position1;
            PositionT = position2;
            ColorO = color1;
            ColorT = color2;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(0, sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
            new VertexElement(0, sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 1),
            new VertexElement(0, sizeof(float) * (3 + 3), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 0),
            new VertexElement(0, sizeof(float) * (3 + 3 + 4), VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Color, 1),
        };

        public static int SizeInBytes = sizeof(float) * (3 + 3 + 4 + 4);
    }
}
