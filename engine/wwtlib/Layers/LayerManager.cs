﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Html;
using System.Xml;
using System.Html.Services;
using System.Html.Media.Graphics;

namespace wwtlib
{
    public class LayerManager
    {
        static int version = 0;

        public static int Version
        {
            get { return LayerManager.version; }
            set { LayerManager.version = value; }
        }

        static FrameWizard _frameWizardDialog = new FrameWizard();
        public static FrameWizard FrameWizardDialog
        {
            get { return _frameWizardDialog; }
        }

        static DataVizWizard _dataVizWizardDialog = new DataVizWizard();
        public static DataVizWizard DataVizWizardDialog
        {
            get { return _dataVizWizardDialog; }
        }

        static ReferenceFrameProps _referenceFramePropsDialog = new ReferenceFrameProps();
        public static ReferenceFrameProps ReferenceFramePropsDialog
        {
            get { return _referenceFramePropsDialog; }
        }

        static GreatCircleDialog _greatCircleDialog = new GreatCircleDialog();
        public static GreatCircleDialog GreatCircleDlg
        {
            get { return _greatCircleDialog; }
        }

        static bool tourLayers = false;

        public static bool TourLayers
        {
            get { return LayerManager.tourLayers; }
            set
            {
                if (LayerManager.tourLayers != value && value == false)
                {
                    ClearLayers();
                    LayerManager.tourLayers = value;
                    LoadTree();
                }
                else if (LayerManager.tourLayers != value && value == true)
                {
                    LayerManager.tourLayers = value;
                    InitLayers();
                }

            }
        }

        public static void LoadTree()
        {
            if (WWTControl.scriptInterface != null)
            {
                WWTControl.scriptInterface.RefreshLayerManagerNow();
            }
        }



        static Dictionary<string, LayerMap> layerMaps = new Dictionary<string, LayerMap>();
        static Dictionary<string, LayerMap> layerMapsTours = new Dictionary<string, LayerMap>();

        public static Dictionary<string, LayerMap> LayerMaps
        {
            get
            {
                if (TourLayers)
                {
                    return LayerManager.layerMapsTours;
                }
                else
                {
                    return LayerManager.layerMaps;
                }
            }
            set
            {
                if (TourLayers)
                {
                    LayerManager.layerMapsTours = value;
                }
                else
                {
                    LayerManager.layerMaps = value;
                }
            }
        }

        private static Dictionary<string, LayerMap> allMaps = new Dictionary<string, LayerMap>();
        private static Dictionary<string, LayerMap> allMapsTours = new Dictionary<string, LayerMap>();

        public static Dictionary<string, LayerMap> AllMaps
        {
            get
            {
                if (TourLayers)
                {
                    return LayerManager.allMapsTours;
                }
                else
                {
                    return LayerManager.allMaps;
                }
            }
            set
            {
                if (TourLayers)
                {
                    LayerManager.allMapsTours = value;
                }
                else
                {
                    LayerManager.allMaps = value;
                }
            }
        }

        static string currentMap = "Earth";

        public static string CurrentMap
        {
            get { return LayerManager.currentMap; }
            set { LayerManager.currentMap = value; }
        }

        private static Dictionary<Guid, Layer> layerList = new Dictionary<Guid, Layer>();
        static Dictionary<Guid, Layer> layerListTours = new Dictionary<Guid, Layer>();

        public static Dictionary<Guid, Layer> LayerList
        {
            get
            {
                if (TourLayers)
                {
                    return LayerManager.layerListTours;
                }
                else
                {
                    return LayerManager.layerList;
                }
            }
            set
            {
                if (TourLayers)
                {
                    LayerManager.layerListTours = value;
                }
                else
                {
                    LayerManager.layerList = value;
                }
            }
        }

        // This function *can* be called multiple times safely, but it only
        // needs to be called once upon app startup. The `InitLayers` function
        // can be called more than once, if/when the `TourLayers` setting is
        // toggled.
        static public void OneTimeInitialization()
        {
            if (webFileMoons == null) {
                GetMoonFile(URLHelpers.singleton.engineAssetUrl("moons.txt"));
            }

            PushPin.TriggerLoadSprite();
        }

        static string moonfile = "";

        static public void InitLayers()
        {
            ClearLayers();

            LayerMap iss = null;
            bool doISS = !TourLayers && !WWTControl.Singleton.FreestandingMode;

            if (doISS)
            {
                iss = new LayerMap("ISS", ReferenceFrames.Custom);
                iss.Frame.Epoch = SpaceTimeController.TwoLineDateToJulian("10184.51609218");
                iss.Frame.SemiMajorAxis = 6728829.41;
                iss.Frame.ReferenceFrameType = ReferenceFrameTypes.Orbital;
                iss.Frame.Inclination = 51.6442;
                iss.Frame.LongitudeOfAscendingNode = 147.0262;
                iss.Frame.Eccentricity = .0009909;
                iss.Frame.MeanAnomolyAtEpoch = 325.5563;
                iss.Frame.MeanDailyMotion = 360 * 15.72172655;
                iss.Frame.ArgumentOfPeriapsis = 286.4623;
                iss.Frame.Scale = 1;
                iss.Frame.SemiMajorAxisUnits = AltUnits.Meters;
                iss.Frame.MeanRadius = 130;
                iss.Frame.Oblateness = 0;
                iss.Frame.ShowOrbitPath = true;

                string[] isstle = new string[0];

                //This is downloaded now on startup
                string url = URLHelpers.singleton.coreDynamicUrl("wwtweb/isstle.aspx");

                WebFile webFile;

                webFile = new WebFile(url);
                //webFile.ResponseType = "text";
                webFile.OnStateChange = delegate
                {
                    if (webFile.State == StateType.Received)
                    {
                        string data = webFile.GetText();
                        isstle = data.Split("\n");
                        if (isstle.Length > 1)
                        {
                            iss.Frame.FromTLE(isstle[0], isstle[1], 398600441800000);
                        }
                    }
                };

                webFile.Send();
                iss.Enabled = true;
            }

            LayerMaps["Sun"] = new LayerMap("Sun", ReferenceFrames.Sun);
            LayerMaps["Sun"].AddChild(new LayerMap("Mercury", ReferenceFrames.Mercury));
            LayerMaps["Sun"].AddChild(new LayerMap("Venus", ReferenceFrames.Venus));
            LayerMaps["Sun"].AddChild(new LayerMap("Earth", ReferenceFrames.Earth));
            LayerMaps["Sun"].ChildMaps["Earth"].AddChild(new LayerMap("Moon", ReferenceFrames.Moon));

            if (doISS)
            {
                LayerMaps["Sun"].ChildMaps["Earth"].AddChild(iss);
            }

            //LayerMaps["Sun"].ChildMaps["Earth"].AddChild(ol);
            //LayerMaps["Sun"].ChildMaps["Earth"].AddChild(l1);
            //LayerMaps["Sun"].ChildMaps["Earth"].AddChild(l2);

            LayerMaps["Sun"].AddChild(new LayerMap("Mars", ReferenceFrames.Mars));
            LayerMaps["Sun"].AddChild(new LayerMap("Jupiter", ReferenceFrames.Jupiter));
            LayerMaps["Sun"].ChildMaps["Jupiter"].AddChild(new LayerMap("Io", ReferenceFrames.Io));
            LayerMaps["Sun"].ChildMaps["Jupiter"].AddChild(new LayerMap("Europa", ReferenceFrames.Europa));
            LayerMaps["Sun"].ChildMaps["Jupiter"].AddChild(new LayerMap("Ganymede", ReferenceFrames.Ganymede));
            LayerMaps["Sun"].ChildMaps["Jupiter"].AddChild(new LayerMap("Callisto", ReferenceFrames.Callisto));
            LayerMaps["Sun"].AddChild(new LayerMap("Saturn", ReferenceFrames.Saturn));
            LayerMaps["Sun"].AddChild(new LayerMap("Uranus", ReferenceFrames.Uranus));
            LayerMaps["Sun"].AddChild(new LayerMap("Neptune", ReferenceFrames.Neptune));
            LayerMaps["Sun"].AddChild(new LayerMap("Pluto", ReferenceFrames.Pluto));

            AddMoons(moonfile);

            LayerMaps["Sky"] = new LayerMap("Sky", ReferenceFrames.Sky);
            LayerMaps["Sun"].Open = true;
            allMaps = new Dictionary<string, LayerMap>();

            AddAllMaps(LayerMaps, null);

            if (doISS)
            {
                AddIss();
            }

            version++;
            LoadTree();
        }

        static void AddIss()
        {
            ISSLayer layer = new ISSLayer();

            layer.Name = Language.GetLocalizedText(1314, "ISS Model  (Toshiyuki Takahei)");
            layer.Enabled = Settings.Active.ShowISSModel;
            LayerList[layer.ID] = layer;
            layer.ReferenceFrame = "ISS";
            AllMaps["ISS"].Layers.Add(layer);
            AllMaps["ISS"].Open = true;
        }

        private static void AddAllMaps(Dictionary<string, LayerMap> maps, String parent)
        {
            foreach (String key in maps.Keys)
            {
                LayerMap map = maps[key];
                map.Frame.Parent = parent;
                AllMaps[map.Name] = map;
                AddAllMaps(map.ChildMaps, map.Name);
            }
        }

        private static void ClearLayers()
        {
            foreach (Guid key in LayerList.Keys)
            {
                Layer layer = LayerList[key];
                layer.CleanUp();
            }

            LayerList.Clear();
            LayerMaps.Clear();
        }


        static WebFile webFileMoons;

        public static void GetMoonFile(string url)
        {
            webFileMoons = new WebFile(url);
            webFileMoons.OnStateChange = MoonFileStateChange;
            webFileMoons.Send();
        }

        public static void MoonFileStateChange()
        {
            if (webFileMoons.State == StateType.Error)
            {
                Script.Literal("alert({0})", webFileMoons.Message);
            }
            else if (webFileMoons.State == StateType.Received)
            {
                moonfile = webFileMoons.GetText();
                InitLayers();
            }

        }

        private static void AddMoons(string file)
        {

            string[] data = file.Split("\r\n");

            bool first = true;
            foreach (string line in data)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                string[] parts = line.Split("\t");
                if (parts.Length > 16)
                {
                    string planet = parts[0];
                    LayerMap frame = new LayerMap(parts[2], ReferenceFrames.Custom);
                    frame.Frame.SystemGenerated = true;
                    frame.Frame.Epoch = double.Parse(parts[1]);
                    frame.Frame.SemiMajorAxis = double.Parse(parts[3]) * 1000;
                    frame.Frame.ReferenceFrameType = ReferenceFrameTypes.Orbital;
                    frame.Frame.Inclination = double.Parse(parts[7]);
                    frame.Frame.LongitudeOfAscendingNode = double.Parse(parts[8]);
                    frame.Frame.Eccentricity = double.Parse(parts[4]);
                    frame.Frame.MeanAnomolyAtEpoch = double.Parse(parts[6]);
                    frame.Frame.MeanDailyMotion = double.Parse(parts[9]);
                    frame.Frame.ArgumentOfPeriapsis = double.Parse(parts[5]);
                    frame.Frame.Scale = 1;
                    frame.Frame.SemiMajorAxisUnits = AltUnits.Meters;
                    frame.Frame.MeanRadius = double.Parse(parts[16]) * 1000;
                    frame.Frame.RotationalPeriod = double.Parse(parts[17]);
                    frame.Frame.ShowAsPoint = false;
                    frame.Frame.ShowOrbitPath = true;
                    frame.Frame.RepresentativeColor = Color.FromArgb(255, 175, 216, 230);
                    frame.Frame.Oblateness = 0;

                    LayerMaps["Sun"].ChildMaps[planet].AddChild(frame);

                }
            }
        }

        public static VoTableLayer AddVoTableLayer(VoTable table, string title)
        {
            return LayerManager.AddVoTableLayerWithPlotType(table, title, PlotTypes.Circle);
        }

        public static VoTableLayer AddVoTableLayerWithPlotType(VoTable table, string title, PlotTypes plotType)
        {
            VoTableLayer layer = VoTableLayer.Create(table, plotType);
            layer.Name = title;
            layer.Astronomical = true;
            layer.ReferenceFrame = "Sky";
            LayerList[layer.ID] = layer;
            AllMaps["Sky"].Layers.Add(layer);
            AllMaps["Sky"].Open = true;
            layer.Enabled = true;
            version++;
            LoadTree();

            return layer;
        }

        public static ImageSetLayer AddImageSetLayer(Imageset imageset, string title)
        {
            ImageSetLayer layer = ImageSetLayer.Create(imageset);
            return AddFitsImageSetLayer(layer, title);
        }

        public static ImageSetLayer AddImageSetLayerCallback(Imageset imageset, string title, ImagesetLoaded callback)
        {
            ImageSetLayer layer = ImageSetLayer.Create(imageset);

            // The tile rendering codepaths require that "Extension" is exactly
            // .fits -- multiple extensions are not currently supported.

            bool isNonHipsTiledFits =
                imageset.Extension == ".fits" &&
                layer.GetFitsImage() == null &&
                imageset.Projection != ProjectionType.Healpix;

            // The goal here is to fire the callback once the initial imageset
            // data have loaded. In particular, for FITS-type imagesets, we
            // inevitably need to download some data in order to figure out
            // parameters like FitsProperties.LowerCut.
            //
            // At the moment, this is only wired up correctly for non-HiPS tiled
            // FITS. In a pretty egregious hack, the OnMainImageLoaded callback
            // below will be fired once the level-0 FITS tile is loaded. We
            // basically needed to add this new callback hook because there
            // wasn't any other way to get something to fire when the level-0
            // tile data actually arrive.
            //
            // HiPS FITS datasets will *eventually* get the right FitsProperties
            // because the fetch of the HipsProperties data sets this up. (This
            // is triggered by the HipsProperties constructor, used in
            // Imageset.GetNewTile.) But the timing of the callback here is
            // uncorrelated with that process. The same is broadly true for
            // untiled FITS. This function should be improved to make sure that
            // the callback argument gets fired at the right time for such
            // datasets.

            if (isNonHipsTiledFits) {
                imageset.FitsProperties.OnMainImageLoaded = delegate (FitsImage image) {
                    image.ApplyDisplaySettings();
                    if (callback != null) {
                        callback(layer);
                    }
                };
            }

            AddFitsImageSetLayer(layer, title);

            // For everything not yet handled, just trigger the callback now, if
            // needed.
            if (callback != null && (!isNonHipsTiledFits || imageset.FitsProperties.MainImageLoadedEventHasFired)) {
                callback(layer);
            }

            return layer;
        }

        // This method is somewhat misnamed - there's nothing FITS-specific about it.
        public static ImageSetLayer AddFitsImageSetLayer(ImageSetLayer layer, string title)
        {
            layer.DoneLoading(null);
            layer.Name = title;
            layer.Astronomical = true;
            layer.ReferenceFrame = "Sky";
            LayerList[layer.ID] = layer;
            AllMaps["Sky"].Layers.Add(layer);
            AllMaps["Sky"].Open = true;
            layer.Enabled = true;
            version++;
            LoadTree();
            return layer;
        }

        public static string GetNextFitsName()
        {
            return getNextName("Fits Image");
        }

        public static string GetNextImageSetName()
        {
            return getNextName("Image Set");
        }

        private static string getNextName(string type){
            int currentNumber = 0;
            foreach (string key in AllMaps.Keys)
            {
                foreach (Layer layer in AllMaps[key].Layers)
                {
                    if (layer.Name.StartsWith(type + " "))
                    {
                        string number = layer.Name.Replace(type + " ", "");
                        try
                        {
                            int num = Int32.Parse(number);
                            if (num > currentNumber)
                            {
                                currentNumber = num;
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }

            return string.Format("{0} {1}", type, currentNumber + 1);
        }

        internal static void CloseAllTourLoadedLayers()
        {
            List<Guid> purgeTargets = new List<Guid>();
            foreach (Guid key in LayerList.Keys)
            {
                Layer layer = LayerList[key];
                if (layer.LoadedFromTour)
                {
                    purgeTargets.Add(layer.ID);
                }
            }

            foreach (Guid guid in purgeTargets)
            {
                DeleteLayerByID(guid, true, false);
            }

            List<string> purgeMapsNames = new List<string>();

            foreach (String key in AllMaps.Keys)
            {
                LayerMap map = AllMaps[key];

                if (map.LoadedFromTour && map.Layers.Count == 0)
                {
                    purgeMapsNames.Add(map.Name);
                }
            }

            foreach (string name in purgeMapsNames)
            {
                PurgeLayerMapDeep(AllMaps[name], true);
            }



            Version++;
            LoadTree();

        }

        public static void PurgeLayerMapDeep(LayerMap target, bool topLevel)
        {

            foreach (Layer layer in target.Layers)
            {
                LayerManager.DeleteLayerByID(layer.ID, false, false);
            }

            target.Layers.Clear();

            foreach (string key in target.ChildMaps.Keys)
            {
                LayerMap map = target.ChildMaps[key];
                PurgeLayerMapDeep(map, false);
            }

            target.ChildMaps.Clear();
            if (topLevel)
            {
                if (!String.IsNullOrEmpty(target.Frame.Parent))
                {
                    if (AllMaps.ContainsKey(target.Frame.Parent))
                    {
                        AllMaps[target.Frame.Parent].ChildMaps.Remove(target.Name);
                    }
                }
                else
                {
                    if (LayerMaps.ContainsKey(target.Name))
                    {
                        LayerMaps.Remove(target.Name);
                    }
                }
            }
            AllMaps.Remove(target.Name);
            version++;
        }


        internal static void CleanAllTourLoadedLayers()
        {
            foreach (Guid key in LayerList.Keys)
            {
                Layer layer = LayerList[key];
                if (layer.LoadedFromTour)
                {
                    //todo We may want to copy layers into a temp directory later, for now we are just leaving the layer data files in the temp tour directory.
                    layer.LoadedFromTour = false;
                }
            }
        }

        // Merged layers from Tour Player Alternate universe into the real layer manager layers list
        public static void MergeToursLayers()
        {

            tourLayers = false;
            bool OverWrite = false;
            bool CollisionChecked = false;

            foreach (String key in allMapsTours.Keys)
            {
                LayerMap map = allMapsTours[key];
                if (!allMaps.ContainsKey(map.Name))
                {
                    LayerMap newMap = new LayerMap(map.Name, ReferenceFrames.Custom);
                    newMap.Frame = map.Frame;
                    newMap.LoadedFromTour = true;
                    LayerManager.AllMaps[newMap.Name] = newMap;
                }
            }
            ConnectAllChildren();
            foreach (Guid key in layerListTours.Keys)
            {
                Layer layer = layerListTours[key];

                if (LayerList.ContainsKey(layer.ID))
                {
                    if (!CollisionChecked)
                    {
                        //todo add UI in the future with possibility of OverWrite = false
                        OverWrite = true;
                        CollisionChecked = true;
                    }

                    if (OverWrite)
                    {
                        LayerManager.DeleteLayerByID(layer.ID, true, false);
                    }
                }

                if (!LayerList.ContainsKey(layer.ID))
                {
                    if (AllMaps.ContainsKey(layer.ReferenceFrame))
                    {
                        LayerList[layer.ID] = layer;

                        AllMaps[layer.ReferenceFrame].Layers.Add(layer);
                    }
                }
                else
                {
                    layer.CleanUp();
                }
            }

            layerListTours.Clear();
            allMapsTours.Clear();
            layerMapsTours.Clear();
            LoadTree();
        }

        public static void ConnectAllChildren()
        {
            foreach (String key in AllMaps.Keys)
            {
                LayerMap map = AllMaps[key];
                if (String.IsNullOrEmpty(map.Frame.Parent) && !LayerMaps.ContainsKey(map.Frame.Name))
                {
                    LayerMaps[map.Name] = map;
                }
                else if (!String.IsNullOrEmpty(map.Frame.Parent) && AllMaps.ContainsKey(map.Frame.Parent))
                {
                    if (!AllMaps[map.Frame.Parent].ChildMaps.ContainsKey(map.Frame.Name))
                    {
                        AllMaps[map.Frame.Parent].ChildMaps[map.Frame.Name] = map;
                        map.Parent = AllMaps[map.Frame.Parent];
                    }
                }
            }
        }

        public static bool DeleteLayerByID(Guid ID, bool removeFromParent, bool updateTree)
        {
            if (LayerList.ContainsKey(ID))
            {
                Layer layer = LayerList[ID];
                layer.CleanUp();
                if (removeFromParent)
                {
                    AllMaps[layer.ReferenceFrame].Layers.Remove(layer);
                }
                LayerList.Remove(ID);
                version++;
                if (updateTree)
                {
                    LoadTree();
                }
                return true;
            }
            else
            {
                return false;
            }

        }

        internal static FrameTarget GetFrameTarget(RenderContext renderContext, string TrackingFrame)
        {

            FrameTarget target = new FrameTarget();

            Vector3d targetPoint = Vector3d.Empty;

            target.Target = Vector3d.Empty;
            target.Matrix = Matrix3d.Identity;

            if (!AllMaps.ContainsKey(TrackingFrame))
            {
                return target;
            }

            List<LayerMap> mapList = new List<LayerMap>();

            LayerMap current = AllMaps[TrackingFrame];

            mapList.Add(current);

            while (current.Frame.Reference == ReferenceFrames.Custom)
            {
                current = current.Parent;
                mapList.Insert(0, current);
            }

            Matrix3d matOld = renderContext.World.Clone();
            Matrix3d matOldNonRotating = renderContext.WorldBaseNonRotating;
            Matrix3d matOldBase = renderContext.WorldBase;
            double oldNominalRadius = renderContext.NominalRadius;

            foreach (LayerMap map in mapList)
            {
                if (map.Frame.Reference != ReferenceFrames.Custom && map.Frame.Reference != ReferenceFrames.Sandbox)
                {

                    Planets.SetupPlanetMatrix(renderContext, (int)Enums.Parse("SolarSystemObjects", map.Frame.Name), Vector3d.Empty, false);
                }
                else
                {
                    map.ComputeFrame(renderContext);
                    if (map.Frame.useRotatingParentFrame())
                    {
                        renderContext.World = Matrix3d.MultiplyMatrix(map.Frame.WorldMatrix, renderContext.World);
                    }
                    else
                    {
                        renderContext.World = Matrix3d.MultiplyMatrix(map.Frame.WorldMatrix, renderContext.WorldBaseNonRotating);

                    }
                    if (map.Frame.ReferenceFrameType == ReferenceFrameTypes.Synodic)
                    {
                        renderContext.WorldBaseNonRotating = renderContext.World.Clone();
                    }

                    renderContext.NominalRadius = map.Frame.MeanRadius;
                }
            }

            targetPoint = renderContext.World.Transform(targetPoint);

            Vector3d lookAt = renderContext.World.Transform(Vector3d.Create(0, 0, 1));

            Vector3d lookUp = Vector3d.SubtractVectors(renderContext.World.Transform(Vector3d.Create(0, 1, 0)), targetPoint);


            lookUp.Normalize();


            target.Matrix = Matrix3d.LookAtLH(new Vector3d(), Vector3d.SubtractVectors(lookAt, targetPoint), lookUp);


            renderContext.NominalRadius = oldNominalRadius;
            renderContext.World = matOld;
            renderContext.WorldBaseNonRotating = matOldNonRotating;
            renderContext.WorldBase = matOldBase;



            target.Target = targetPoint;
            return target;
        }

        internal static void PrepTourLayers()
        {
            if (TourPlayer.Playing)
            {
                TourPlayer player = (TourPlayer)WWTControl.Singleton.uiController;
                if (player != null)
                {
                    TourDocument tour = player.Tour;

                    if (tour.CurrentTourStop != null)
                    {
                        player.UpdateTweenPosition(-1);


                        if (!tour.CurrentTourStop.KeyFramed)
                        {
                            tour.CurrentTourStop.UpdateLayerOpacity();
                            foreach (Guid key in tour.CurrentTourStop.Layers.Keys)
                            {
                                LayerInfo info = tour.CurrentTourStop.Layers[key];

                                if (LayerList.ContainsKey(info.ID))
                                {
                                    LayerList[info.ID].Opacity = info.FrameOpacity;
                                    LayerList[info.ID].SetParams(info.FrameParams);
                                }
                            }
                        }
                    }
                }
            }
        }


        internal static void Draw(RenderContext renderContext, float opacity, bool astronomical, string referenceFrame, bool nested, bool cosmos)
        {


            if (!AllMaps.ContainsKey(referenceFrame))
            {
                return;
            }



            LayerMap thisMap = AllMaps[referenceFrame];

            if (!thisMap.Enabled || (thisMap.ChildMaps.Count == 0 && thisMap.Layers.Count == 0 && !(thisMap.Frame.ShowAsPoint || thisMap.Frame.ShowOrbitPath)))
            {
                return;
            }
            if (TourPlayer.Playing)
            {
                TourPlayer player = (TourPlayer)WWTControl.Singleton.uiController;
                if (player != null)
                {
                    TourDocument tour = player.Tour;
                    if (tour.CurrentTourStop != null)
                    {
                        player.UpdateTweenPosition(-1);
                        tour.CurrentTourStop.UpdateLayerOpacity();

                        foreach (Guid key in tour.CurrentTourStop.Layers.Keys)
                        {
                            LayerInfo info = tour.CurrentTourStop.Layers[key];

                            if (LayerList.ContainsKey(info.ID))
                            {
                                LayerList[info.ID].Opacity = info.FrameOpacity;
                                LayerList[info.ID].SetParams(info.FrameParams);
                            }
                        }
                    }
                }
            }

            Matrix3d matOld = renderContext.World;
            Matrix3d matOldNonRotating = renderContext.WorldBaseNonRotating;
            double oldNominalRadius = renderContext.NominalRadius;
            if (thisMap.Frame.Reference == ReferenceFrames.Custom | thisMap.Frame.Reference == ReferenceFrames.Custom)
            {
                thisMap.ComputeFrame(renderContext);
                if (thisMap.Frame.ReferenceFrameType != ReferenceFrameTypes.Orbital && thisMap.Frame.ReferenceFrameType != ReferenceFrameTypes.Trajectory)
                //if (true)
                {
                    renderContext.World = Matrix3d.MultiplyMatrix(thisMap.Frame.WorldMatrix, renderContext.World);
                }
                else
                {
                    renderContext.World = Matrix3d.MultiplyMatrix(thisMap.Frame.WorldMatrix, renderContext.WorldBaseNonRotating);

                }
                renderContext.NominalRadius = thisMap.Frame.MeanRadius;
            }



            if (thisMap.Frame.ShowAsPoint)
            {

                // todo Draw point planet...
                // Planets.DrawPointPlanet(renderContext.Device, new Vector3d(0, 0, 0), (float).2, thisMap.Frame.RepresentativeColor, true);

            }



            for (int pass = 0; pass < 2; pass++)
            {
                foreach (Layer layer in AllMaps[referenceFrame].Layers)
                {
                    if ((pass == 0 && layer is ImageSetLayer) || (pass == 1 && !(layer is ImageSetLayer)))
                    {
                        bool skipLayer = false;
                        if (pass == 0)
                        {
                            // Skip default image set layer so that it's not drawn twice
                            skipLayer = !astronomical && ((ImageSetLayer)layer).OverrideDefaultLayer;
                        }

                        if (layer.Enabled && !skipLayer) // && astronomical == layer.Astronomical)
                        {
                            double layerStart = SpaceTimeController.UtcToJulian(layer.StartTime);
                            double layerEnd = SpaceTimeController.UtcToJulian(layer.EndTime);
                            double fadeIn = SpaceTimeController.UtcToJulian(layer.StartTime) - ((layer.FadeType == FadeType.FadeIn || layer.FadeType == FadeType.Both) ? (layer.FadeSpan / 864000000) : 0);
                            double fadeOut = SpaceTimeController.UtcToJulian(layer.EndTime) + ((layer.FadeType == FadeType.FadeOut || layer.FadeType == FadeType.Both) ? (layer.FadeSpan / 864000000) : 0);

                            if (SpaceTimeController.JNow > fadeIn && SpaceTimeController.JNow < fadeOut)
                            {
                                float fadeOpacity = 1;
                                if (SpaceTimeController.JNow < layerStart)
                                {
                                    fadeOpacity = (float)((SpaceTimeController.JNow - fadeIn) / (layer.FadeSpan / 864000000));
                                }

                                if (SpaceTimeController.JNow > layerEnd)
                                {
                                    fadeOpacity = (float)((fadeOut - SpaceTimeController.JNow) / (layer.FadeSpan / 864000000));
                                }
                                layer.Astronomical = astronomical;

                                if (layer is SpreadSheetLayer)
                                {
                                    SpreadSheetLayer tsl = layer as SpreadSheetLayer;
                                    tsl.Draw(renderContext, opacity * fadeOpacity, cosmos);
                                }
                                else
                                {
                                    layer.Draw(renderContext, opacity * fadeOpacity, cosmos);
                                }
                            }
                        }
                    }
                }
            }
            if (nested)
            {
                foreach (string key in AllMaps[referenceFrame].ChildMaps.Keys)
                {
                    LayerMap map = AllMaps[referenceFrame].ChildMaps[key];
                    if (!(map is LayerMap))
                    {
                        continue;
                    }
                    if (map.Enabled && map.Frame.ShowOrbitPath && Settings.Active.SolarSystemOrbits && Settings.Active.SolarSystemMinorOrbits)
                    {
                        if (map.Frame.ReferenceFrameType == ReferenceFrameTypes.Orbital)
                        {
                            if (map.Frame.Orbit == null)
                            {
                                map.Frame.Orbit = new Orbit(map.Frame.Elements, 360, map.Frame.RepresentativeColor, 1, (float)map.Parent.Frame.MeanRadius);
                            }
                            Matrix3d matSaved = renderContext.World;
                            renderContext.World = Matrix3d.MultiplyMatrix(thisMap.Frame.WorldMatrix, renderContext.WorldBaseNonRotating);

                            map.Frame.Orbit.Draw3D(renderContext, 1f * .5f, Vector3d.Create(0, 0, 0));
                            renderContext.World = matSaved;
                        }
                        else if (map.Frame.ReferenceFrameType == ReferenceFrameTypes.Trajectory)
                        {
                            //todo add trajectories back
                            //if (map.Frame.trajectoryLines == null)
                            //{
                            //    map.Frame.trajectoryLines = new LineList(renderContext.Device);
                            //    map.Frame.trajectoryLines.ShowFarSide = true;
                            //    map.Frame.trajectoryLines.UseNonRotatingFrame = true;

                            //    int count = map.Frame.Trajectory.Count - 1;
                            //    for (int i = 0; i < count; i++)
                            //    {
                            //        Vector3d pos1 = map.Frame.Trajectory[i].Position;
                            //        Vector3d pos2 = map.Frame.Trajectory[i + 1].Position;
                            //        pos1.Multiply(1 / renderContext.NominalRadius);
                            //        pos2.Multiply(1 / renderContext.NominalRadius);
                            //        map.Frame.trajectoryLines.AddLine(pos1, pos2, map.Frame.RepresentativeColor, new Dates());
                            //    }
                            //}
                            //Matrix3D matSaved = renderContext.World;
                            //renderContext.World = thisMap.Frame.WorldMatrix * renderContext.WorldBaseNonRotating;

                            //map.Frame.trajectoryLines.DrawLines(renderContext, Earth3d.MainWindow.showMinorOrbits.Opacity * .25f);
                            //renderContext.World = matSaved;
                        }
                    }

                    if ((map.Frame.Reference == ReferenceFrames.Custom || map.Frame.Reference == ReferenceFrames.Identity))
                    {
                        Draw(renderContext, opacity, astronomical, map.Name, nested, cosmos);
                    }
                }
            }
            renderContext.NominalRadius = oldNominalRadius;
            renderContext.World = matOld;
            renderContext.WorldBaseNonRotating = matOldNonRotating;
        }

        internal static Dictionary<Guid, LayerInfo> GetVisibleLayerList(Dictionary<Guid, LayerInfo> previous)
        {
            Dictionary<Guid, LayerInfo> list = new Dictionary<Guid, LayerInfo>();

            foreach (Guid key in LayerList.Keys)
            {
                Layer layer = LayerList[key];
                if (layer.Enabled)
                {
                    LayerInfo info = new LayerInfo();
                    info.StartOpacity = info.EndOpacity = layer.Opacity;
                    info.ID = layer.ID;
                    info.StartParams = layer.GetParams();


                    if (previous.ContainsKey(info.ID))
                    {
                        info.EndOpacity = previous[info.ID].EndOpacity;
                        info.EndParams = previous[info.ID].EndParams;
                    }
                    else
                    {
                        info.EndParams = layer.GetParams();
                    }
                    list[layer.ID] = info;
                }
            }
            return list;
        }

        public static void SetVisibleLayerList(Dictionary<Guid, LayerInfo> list)
        {
            foreach (Guid key in LayerList.Keys)
            {
                Layer layer = LayerList[key];
                layer.Enabled = list.ContainsKey(layer.ID);
                try
                {
                    if (layer.Enabled)
                    {
                        layer.Opacity = list[layer.ID].FrameOpacity;
                        layer.SetParams(list[layer.ID].FrameParams);
                    }
                }
                catch
                {
                }
            }
            //SyncLayerState();
        }

        //todo remove the stuff from draw that is redundant once predraw has run
        internal static void PreDraw(RenderContext renderContext, float opacity, bool astronomical, string referenceFrame, bool nested)
        {


            if (!AllMaps.ContainsKey(referenceFrame))
            {
                return;
            }



            LayerMap thisMap = AllMaps[referenceFrame];

            if (thisMap.ChildMaps.Count == 0 && thisMap.Layers.Count == 0)
            {
                return;
            }
            if (TourPlayer.Playing)
            {
                TourPlayer player = (TourPlayer)WWTControl.Singleton.uiController as TourPlayer;
                if (player != null)
                {
                    TourDocument tour = player.Tour;
                    if (tour.CurrentTourStop != null)
                    {
                        player.UpdateTweenPosition(-1);
                        tour.CurrentTourStop.UpdateLayerOpacity();
                        foreach (Guid key in tour.CurrentTourStop.Layers.Keys)
                        {
                            LayerInfo info = tour.CurrentTourStop.Layers[key];
                            if (LayerList.ContainsKey(info.ID))
                            {
                                LayerList[info.ID].Opacity = info.FrameOpacity;
                                LayerList[info.ID].SetParams(info.FrameParams);
                            }
                        }

                    }
                }
            }

            Matrix3d matOld = renderContext.World;
            Matrix3d matOldNonRotating = renderContext.WorldBaseNonRotating;
            double oldNominalRadius = renderContext.NominalRadius;
            if (thisMap.Frame.Reference == ReferenceFrames.Custom)
            {
                thisMap.ComputeFrame(renderContext);
                if (thisMap.Frame.ReferenceFrameType != ReferenceFrameTypes.Orbital)
                //if (true)
                {
                    renderContext.World = Matrix3d.MultiplyMatrix(thisMap.Frame.WorldMatrix, renderContext.World);
                }
                else
                {
                    renderContext.World = Matrix3d.MultiplyMatrix(thisMap.Frame.WorldMatrix, renderContext.WorldBaseNonRotating);

                }
                renderContext.NominalRadius = thisMap.Frame.MeanRadius;
            }



            for (int pass = 0; pass < 2; pass++)
            {
                foreach (Layer layer in AllMaps[referenceFrame].Layers)
                {
                    if ((pass == 0 && layer is ImageSetLayer) || (pass == 1 && !(layer is ImageSetLayer)))
                    {
                        if (layer.Enabled) // && astronomical == layer.Astronomical)
                        {
                            double layerStart = SpaceTimeController.UtcToJulian(layer.StartTime);
                            double layerEnd = SpaceTimeController.UtcToJulian(layer.EndTime);
                            double fadeIn = SpaceTimeController.UtcToJulian(layer.StartTime) - ((layer.FadeType == FadeType.FadeIn || layer.FadeType == FadeType.Both) ? (layer.FadeSpan / 864000000) : 0);
                            double fadeOut = SpaceTimeController.UtcToJulian(layer.EndTime) + ((layer.FadeType == FadeType.FadeOut || layer.FadeType == FadeType.Both) ? (layer.FadeSpan / 864000000) : 0);

                            if (SpaceTimeController.JNow > fadeIn && SpaceTimeController.JNow < fadeOut)
                            {
                                float fadeOpacity = 1;
                                if (SpaceTimeController.JNow < layerStart)
                                {
                                    fadeOpacity = (float)((SpaceTimeController.JNow - fadeIn) / (layer.FadeSpan / 864000000));
                                }

                                if (SpaceTimeController.JNow > layerEnd)
                                {
                                    fadeOpacity = (float)((fadeOut - SpaceTimeController.JNow) / (layer.FadeSpan / 864000000));
                                }
                                if (thisMap.Frame.Reference == ReferenceFrames.Sky)
                                {
                                    layer.Astronomical = true;
                                }
                                layer.PreDraw(renderContext, opacity * fadeOpacity);
                            }
                        }
                    }

                }
            }
            if (nested)
            {
                foreach (string key in AllMaps[referenceFrame].ChildMaps.Keys)
                {
                    LayerMap map = AllMaps[referenceFrame].ChildMaps[key];
                    if ((map.Frame.Reference == ReferenceFrames.Custom || map.Frame.Reference == ReferenceFrames.Identity))
                    {
                        PreDraw(renderContext, opacity, astronomical, map.Name, nested);
                    }
                }
            }
            renderContext.NominalRadius = oldNominalRadius;
            renderContext.World = matOld;
            renderContext.WorldBaseNonRotating = matOldNonRotating;
        }



        public static void Add(Layer layer, bool updateTree)
        {
            if (!LayerList.ContainsKey(layer.ID))
            {
                if (AllMaps.ContainsKey(layer.ReferenceFrame))
                {
                    LayerList[layer.ID] = layer;

                    AllMaps[layer.ReferenceFrame].Layers.Add(layer);
                    version++;
                    if (updateTree)
                    {
                        LoadTree();
                    }
                }
            }
        }

        static ContextMenuStrip contextMenu;
        static object selectedLayer = null;
        static Vector2d lastMenuClick = new Vector2d();

        static public void layerSelectionChanged(object selected)
        {
            selectedLayer = selected;

            if (selectedLayer != null)
            {
                //if (selectedLayer as ITimeSeriesDescription != null)
                //{
                //    timeScrubber.Maximum = 1000;
                //    ITimeSeriesDescription iTimeSeries = layerTree.SelectedNode.Tag as ITimeSeriesDescription;

                //    timeSeries.Checked = iTimeSeries.IsTimeSeries;
                //    if (iTimeSeries.SeriesStartTime.ToString("HH:mm:ss") == "00:00:00")
                //    {
                //        startDate.Text = iTimeSeries.SeriesStartTime.ToString("yyyy/MM/dd");
                //    }
                //    else
                //    {
                //        startDate.Text = iTimeSeries.SeriesStartTime.ToString("yyyy/MM/dd HH:mm:ss");
                //    }

                //    if (iTimeSeries.SeriesEndTime.ToString("HH:mm:ss") == "00:00:00")
                //    {
                //        endDate.Text = iTimeSeries.SeriesEndTime.ToString("yyyy/MM/dd");
                //    }
                //    else
                //    {
                //        endDate.Text = iTimeSeries.SeriesEndTime.ToString("yyyy/MM/dd HH:mm:ss");
                //    }

                //    return;
                //}
                //else

                if (selectedLayer is LayerMap)
                {
                    LayerMap map = selectedLayer as LayerMap;
                    if (map != null)
                    {
                        CurrentMap = map.Name;
                    }
                }
                else
                {
                    ImageSetLayer layer = selectedLayer as ImageSetLayer;
                    if (layer != null && layer.ImageSet.WcsImage is FitsImage)
                    {
                        //WWTControl.scriptInterface.SetTimeSlider("left", "0");
                        //WWTControl.scriptInterface.SetTimeSlider("right", (layer.GetFitsImage().Depth - 1).ToString());
                        //WWTControl.scriptInterface.SetTimeSlider("title", "Velocity");
                        //Histogram.UpdateImage(layer, timeScrubber.Value);
                        //timeSeries.Checked = false;
                        //startDate.Text = "0";
                        //timeScrubber.Maximum = layer.FitsImage.Depth - 1;
                        ////timeScrubber.Value = layer.FitsImage.min layer.FitsImage.lastBitmapZ;
                        //endDate.Text = timeScrubber.Maximum.ToString();
                        return;
                    }
                }
            }

            //timeSeries.Checked = false;

            WWTControl.scriptInterface.SetTimeSlider("left", "");
            WWTControl.scriptInterface.SetTimeSlider("right", "");
            WWTControl.scriptInterface.SetTimeSlider("title", Language.GetLocalizedText(667, "Time Scrubber"));
        }

        //Fits time slider not implemented for webgl engine (only Windows version)
        static public void SetTimeSliderValue(double pos)
        {
            ImageSetLayer layer = selectedLayer as ImageSetLayer;
            if (layer != null && layer.ImageSet.WcsImage is FitsImage)
            {
                //WWTControl.scriptInterface.SetTimeSlider("title", layer.GetFitsImage().GetZDescription());
            }
        }

        static public void showLayerMenu(object selected, int x, int y)
        {
            lastMenuClick = Vector2d.Create(x, y);
            selectedLayer = selected;

            if (selected is LayerMap)
            {
                CurrentMap = ((LayerMap)selected).Name;
            }
            else if (selected is Layer)
            {
                CurrentMap = ((Layer)selected).ReferenceFrame;
            }


            //if (layer is LayerMap)
            //{

            //    contextMenu = new ContextMenuStrip();

            //    ToolStripMenuItem add = ToolStripMenuItem.Create(Language.GetLocalizedText(1291, "Scale/Histogram"));

            //    ToolStripSeparator sep1 = new ToolStripSeparator();

            //    addGirdLayer.Click = addGirdLayer_Click;

            //    contextMenu.Items.Add(scaleMenu);

            //    contextMenu.Show(Vector2d.Create(x, y));
            //}
            //else if (layer is ImageSetLayer)
            //{
            //    contextMenu = new ContextMenuStrip();

            //    ToolStripMenuItem scaleMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(1291, "Scale/Histogram"));

            //    ToolStripSeparator sep1 = new ToolStripSeparator();

            //    scaleMenu.Click = scaleMenu_click;

            //    contextMenu.Items.Add(scaleMenu);

            //    contextMenu.Show(Vector2d.Create(x, y));
            //}

            if (((selected is Layer) && !(selected is SkyOverlays)))
            {
                Layer selectedLayer = (Layer)selected;

                contextMenu = new ContextMenuStrip();
                ToolStripMenuItem renameMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(225, "Rename"));
                ToolStripMenuItem Expand = ToolStripMenuItem.Create(Language.GetLocalizedText(981, "Expand"));
                ToolStripMenuItem Collapse = ToolStripMenuItem.Create(Language.GetLocalizedText(982, "Collapse"));
                ToolStripMenuItem copyMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(428, "Copy"));
                ToolStripMenuItem deleteMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(167, "Delete"));
                ToolStripMenuItem saveMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(960, "Save..."));
                ToolStripMenuItem publishMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(983, "Publish to Community..."));
                ToolStripMenuItem colorMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(458, "Color/Opacity"));
                ToolStripMenuItem opacityMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(305, "Opacity"));

                ToolStripMenuItem propertiesMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(20, "Properties"));
                ToolStripMenuItem scaleMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(1291, "Scale/Histogram"));
                ToolStripMenuItem lifeTimeMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(683, "Lifetime"));
                ToolStripSeparator spacer1 = new ToolStripSeparator();
                ToolStripMenuItem top = ToolStripMenuItem.Create(Language.GetLocalizedText(684, "Move to Top"));
                ToolStripMenuItem up = ToolStripMenuItem.Create(Language.GetLocalizedText(685, "Move Up"));
                ToolStripMenuItem down = ToolStripMenuItem.Create(Language.GetLocalizedText(686, "Move Down"));
                ToolStripMenuItem bottom = ToolStripMenuItem.Create(Language.GetLocalizedText(687, "Move to Bottom"));
                ToolStripMenuItem showViewer = ToolStripMenuItem.Create(Language.GetLocalizedText(957, "VO Table Viewer"));

                ToolStripSeparator spacer2 = new ToolStripSeparator();


                ToolStripMenuItem defaultImageset = ToolStripMenuItem.Create(Language.GetLocalizedText(1294, "Background Image Set"));


                top.Click = top_Click;
                up.Click = up_Click;
                down.Click = down_Click;
                bottom.Click = bottom_Click;
                saveMenu.Click = saveMenu_Click;
                publishMenu.Click = publishMenu_Click;
                Expand.Click = Expand_Click;
                Collapse.Click = Collapse_Click;
                copyMenu.Click = copyMenu_Click;
                colorMenu.Click = colorMenu_Click;
                deleteMenu.Click = deleteMenu_Click;
                renameMenu.Click = renameMenu_Click;
                propertiesMenu.Click = propertiesMenu_Click;
                scaleMenu.Click = scaleMenu_click;


                defaultImageset.Click = defaultImageset_Click;




                opacityMenu.Click = opacityMenu_Click;
                lifeTimeMenu.Click = lifeTimeMenu_Click;
                showViewer.Click = showViewer_Click;
                contextMenu.Items.Add(renameMenu);

                if (!selectedLayer.Opened && selectedLayer.GetPrimaryUI() != null && selectedLayer.GetPrimaryUI().HasTreeViewNodes)
                {
                    contextMenu.Items.Add(Expand);

                }

                if (selectedLayer.Opened)
                {
                    contextMenu.Items.Add(Collapse);
                }


                if (selectedLayer.CanCopyToClipboard())
                {
                    //contextMenu.Items.Add(copyMenu);
                }

                contextMenu.Items.Add(deleteMenu);
                //contextMenu.Items.Add(saveMenu);

                //if (Earth3d.IsLoggedIn)
                //{
                //    contextMenu.Items.Add(publishMenu);
                //}

                contextMenu.Items.Add(spacer2);
                contextMenu.Items.Add(colorMenu);
                //contextMenu.Items.Add(opacityMenu);

                // ToDo Should we have this only show up in layers under Identity Reference Frames?
                //contextMenu.Items.Add(lifeTimeMenu);


                if (selected is ImageSetLayer)
                {
                    contextMenu.Items.Add(defaultImageset);

                    ImageSetLayer isl = selected as ImageSetLayer;
                    defaultImageset.Checked = isl.OverrideDefaultLayer;
                }
                /*selected is Object3dLayer || selected is GroundOverlayLayer || selected is OrbitLayer */
                if (selected is SpreadSheetLayer || selected is GreatCirlceRouteLayer)
                {
                    contextMenu.Items.Add(propertiesMenu);
                }

                if (selected is VoTableLayer)
                {
                    contextMenu.Items.Add(showViewer);
                }

                if (selected is ImageSetLayer)
                {
                    ImageSetLayer isl = selected as ImageSetLayer;
                    // if (isl.FitsImage != null)
                    {
                        contextMenu.Items.Add(scaleMenu);
                    }
                }

                if (AllMaps[selectedLayer.ReferenceFrame].Layers.Count > 1)
                {
                    contextMenu.Items.Add(spacer1);
                    contextMenu.Items.Add(top);
                    contextMenu.Items.Add(up);
                    contextMenu.Items.Add(down);
                    contextMenu.Items.Add(bottom);
                }


                contextMenu.Show(Vector2d.Create(x, y));
            }
            else if (selected is LayerMap)
            {
                LayerMap map = selected as LayerMap;
                bool sandbox = map.Frame.Reference.ToString() == "Sandbox";
                bool Dome = map.Frame.Name == "Dome";
                bool Sky = map.Frame.Name == "Sky";

                if (Dome)
                {
                    return;
                }
                contextMenu = new ContextMenuStrip();
                ToolStripMenuItem trackFrame = ToolStripMenuItem.Create(Language.GetLocalizedText(1298, "Track this frame"));
                ToolStripMenuItem goTo = ToolStripMenuItem.Create(Language.GetLocalizedText(1299, "Fly Here"));
                ToolStripMenuItem showOrbit = ToolStripMenuItem.Create("Show Orbit");
                ToolStripMenuItem newMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(674, "New Reference Frame"));
                ToolStripMenuItem newLayerGroupMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(675, "New Layer Group"));
                ToolStripMenuItem addMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(166, "Add"));
                ToolStripMenuItem newLight = ToolStripMenuItem.Create("Add Light");
                ToolStripMenuItem addFeedMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(956, "Add OData/table feed as Layer"));
                ToolStripMenuItem addWmsLayer = ToolStripMenuItem.Create(Language.GetLocalizedText(987, "New WMS Layer"));
                ToolStripMenuItem addGridLayer = ToolStripMenuItem.Create(Language.GetLocalizedText(1300, "New Lat/Lng Grid"));
                ToolStripMenuItem addGreatCircle = ToolStripMenuItem.Create(Language.GetLocalizedText(988, "New Great Circle"));
                ToolStripMenuItem importTLE = ToolStripMenuItem.Create(Language.GetLocalizedText(989, "Import Orbital Elements"));
                ToolStripMenuItem addMpc = ToolStripMenuItem.Create(Language.GetLocalizedText(1301, "Add Minor Planet"));
                ToolStripMenuItem deleteFrameMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(167, "Delete"));
                ToolStripMenuItem pasteMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(425, "Paste"));
                ToolStripMenuItem addToTimeline = ToolStripMenuItem.Create(Language.GetLocalizedText(1290, "Add to Timeline"));
                ToolStripMenuItem addKeyframe = ToolStripMenuItem.Create(Language.GetLocalizedText(1280, "Add Keyframe"));

                ToolStripMenuItem popertiesMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(20, "Properties"));
                ToolStripMenuItem saveMenu = ToolStripMenuItem.Create(Language.GetLocalizedText(990, "Save Layers"));
                ToolStripMenuItem publishLayers = ToolStripMenuItem.Create(Language.GetLocalizedText(991, "Publish Layers to Community"));
                ToolStripSeparator spacer1 = new ToolStripSeparator();
                ToolStripSeparator spacer0 = new ToolStripSeparator();
                ToolStripSeparator spacer2 = new ToolStripSeparator();
                ToolStripMenuItem asReferenceFrame = ToolStripMenuItem.Create("As Reference Frame");
                ToolStripMenuItem asOrbitalLines = ToolStripMenuItem.Create("As Orbital Line");


                trackFrame.Click = trackFrame_Click;
                goTo.Click = goTo_Click;
                asReferenceFrame.Click = addMpc_Click;
                asOrbitalLines.Click = AsOrbitalLines_Click;
                // Ad Sub Menus
                addMpc.DropDownItems.Add(asReferenceFrame);
                addMpc.DropDownItems.Add(asOrbitalLines);




                addMenu.Click = addMenu_Click;
                // newLight.Click = newLight_Click;

                newLayerGroupMenu.Click = newLayerGroupMenu_Click;
                pasteMenu.Click = pasteLayer_Click;
                newMenu.Click = newMenu_Click;
                deleteFrameMenu.Click = deleteFrameMenu_Click;
                popertiesMenu.Click = FramePropertiesMenu_Click;
                //  addWmsLayer.Click = addWmsLayer_Click;
                // importTLE.Click = importTLE_Click;
                addGreatCircle.Click = addGreatCircle_Click;
                //    saveMenu.Click = SaveLayers_Click;
                //   publishLayers.Click = publishLayers_Click;
                addGridLayer.Click = addGirdLayer_Click;


                ToolStripMenuItem convertToOrbit = ToolStripMenuItem.Create("Extract Orbit Layer");
                //    convertToOrbit.Click = ConvertToOrbit_Click;


                if (map.Frame.Reference != ReferenceFrames.Identity)
                {
                    if (WWTControl.Singleton.SolarSystemMode | WWTControl.Singleton.SandboxMode) //&& Control.ModifierKeys == Keys.Control)
                    {
                        bool spacerNeeded = false;
                        if (map.Frame.Reference != ReferenceFrames.Custom && !WWTControl.Singleton.SandboxMode)
                        {
                            // fly to
                            if (!Sky)
                            {
                                //contextMenu.Items.Add(goTo);
                                //spacerNeeded = true;
                            }

                            try
                            {
                                string name = map.Frame.Reference.ToString();
                                if (name != "Sandbox")
                                {
                                    SolarSystemObjects ssObj = (SolarSystemObjects)Enums.Parse("SolarSystemObjects", name);
                                    int id = (int)ssObj;

                                    int bit = (int)Math.Pow(2, id);

                                    showOrbit.Checked = (Settings.Active.PlanetOrbitsFilter & bit) != 0;
                                    showOrbit.Click = showOrbitPlanet_Click;
                                    showOrbit.Tag = bit.ToString();
                                }
                            }
                            catch
                            {
                            }
                        }
                        else
                        {
                            // track
                            if (!sandbox && !Sky)
                            {
                                contextMenu.Items.Add(trackFrame);
                                spacerNeeded = true;
                            }
                            showOrbit.Checked = map.Frame.ShowOrbitPath;
                            showOrbit.Click = showOrbit_Click;
                        }

                        if (spacerNeeded)
                        {
                            contextMenu.Items.Add(spacer2);
                        }

                        if (!Sky && !sandbox)
                        {
                            contextMenu.Items.Add(showOrbit);

                            contextMenu.Items.Add(spacer0);
                        }

                        if (map.Frame.Reference.ToString() == "Sandbox")
                        {
                            contextMenu.Items.Add(newLight);
                        }
                    }

                    if (!Sky)
                    {
                        contextMenu.Items.Add(newMenu);
                    }
                    //contextMenu.Items.Add(newLayerGroupMenu);

                }

                //contextMenu.Items.Add(addMenu);
                //contextMenu.Items.Add(addFeedMenu);
                if (!Sky)
                {
                    contextMenu.Items.Add(addGreatCircle);
                    contextMenu.Items.Add(addGridLayer);
                }

                if ((map.Frame.Reference != ReferenceFrames.Identity && map.Frame.Name == "Sun") ||
                    (map.Frame.Reference == ReferenceFrames.Identity && map.Parent != null && map.Parent.Frame.Name == "Sun"))
                {
                    contextMenu.Items.Add(addMpc);
                }

                if (map.Frame.Reference == ReferenceFrames.Custom && map.Frame.ReferenceFrameType == ReferenceFrameTypes.Orbital && map.Parent != null && map.Parent.Frame.Name == "Sun")
                {
                    //contextMenu.Items.Add(convertToOrbit);
                }


                if (!Sky)
                {
                    //contextMenu.Items.Add(addWmsLayer);
                }


                contextMenu.Items.Add(pasteMenu);


                if (map.Frame.Reference == ReferenceFrames.Identity)
                {
                    contextMenu.Items.Add(deleteFrameMenu);
                }

                if (map.Frame.Reference == ReferenceFrames.Custom)
                {
                    contextMenu.Items.Add(deleteFrameMenu);

                    contextMenu.Items.Add(popertiesMenu);

                }

                //if (!Sky)
                {
                    contextMenu.Items.Add(spacer1);
                }
                //contextMenu.Items.Add(saveMenu);
                //if (Earth3d.IsLoggedIn)
                //{
                //    contextMenu.Items.Add(publishLayers);
                //}


                contextMenu.Show(Vector2d.Create(x, y));
            }
            //else if (selectedLayer is LayerUITreeNode)
            //{
            //    LayerUITreeNode node = selectedLayer as LayerUITreeNode;
            //    contextMenu = new ContextMenuStrip();

            //    Layer layer = GetParentLayer(layerTree.SelectedNode);

            //    if (layer != null)
            //    {
            //        LayerUI ui = layer.GetPrimaryUI();
            //        List<LayerUIMenuItem> items = ui.GetNodeContextMenu(node);

            //        if (items != null)
            //        {
            //            foreach (LayerUIMenuItem item in items)
            //            {
            //                ToolStripMenuItem menuItem = ToolStripMenuItem.Create(item.Name);
            //                menuItem.Tag = item;
            //                menuItem.Click = menuItem_Click;
            //                contextMenu.Items.Add(menuItem);

            //                if (item.SubMenus != null)
            //                {
            //                    foreach (LayerUIMenuItem subItem in item.SubMenus)
            //                    {
            //                        ToolStripMenuItem subMenuItem = ToolStripMenuItem.Create(subItem.Name);
            //                        subMenuItem.Tag = subItem;
            //                        subMenuItem.Click = menuItem_Click;
            //                        menuItem.DropDownItems.Add(subMenuItem);
            //                    }
            //                }
            //            }
            //            contextMenu.Show(Cursor.Position);
            //        }


            //    }
            //}
        }

        static void publishMenu_Click(object sender, EventArgs e)
        {

            //if (Earth3d.IsLoggedIn)
            //{

            //    Layer target = (Layer)selectedLayer;

            //    string name = target.Name + ".wwtl";
            //    string filename = Path.GetTempFileName();

            //    LayerContainer layers = new LayerContainer();
            //    layers.SoloGuid = target.ID;

            //    layers.SaveToFile(filename);
            //    layers.Dispose();
            //    GC.SuppressFinalize(layers);
            //    EOCalls.InvokePublishFile(filename, name);
            //    File.Delete(filename);

            //    Earth3d.RefreshCommunity();

            //}
        }

        static void addGirdLayer_Click(object sender, EventArgs e)
        {
            GridLayer layer = new GridLayer();

            layer.Enabled = true;
            layer.Name = "Lat-Lng Grid";
            LayerList[layer.ID] = layer;
            layer.ReferenceFrame = currentMap;

            AllMaps[currentMap].Layers.Add(layer);
            AllMaps[currentMap].Open = true;
            version++;
            LoadTree();

        }

        static void trackFrame_Click(object sender, EventArgs e)
        {
            LayerMap target = (LayerMap)selectedLayer;

            WWTControl.Singleton.RenderContext.SolarSystemTrack = SolarSystemObjects.Custom;
            WWTControl.Singleton.RenderContext.TrackingFrame = target.Name;
            WWTControl.Singleton.RenderContext.ViewCamera.Zoom = WWTControl.Singleton.RenderContext.TargetCamera.Zoom = .000000001;


        }

        static void goTo_Click(object sender, EventArgs e)
        {
            //LayerMap target = (LayerMap)selectedLayer;

            //IPlace place = Catalogs.FindCatalogObjectExact(target.Frame.Reference.ToString());
            //if (place != null)
            //{
            //    WWTControl.Singleton.GotoTarget(place, false, false, true);
            //}
        }

        static void saveMenu_Click(object sender, EventArgs e)
        {
            //Layer layer = (Layer)selectedLayer;
            //SaveFileDialog saveDialog = new SaveFileDialog();
            //saveDialog.Filter = Language.GetLocalizedText(993, "WorldWide Telescope Layer File(*.wwtl)") + "|*.wwtl";
            //saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //saveDialog.AddExtension = true;
            //saveDialog.DefaultExt = ".wwtl";
            //saveDialog.FileName = layer.Name + ".wwtl";
            //if (saveDialog.ShowDialog() == DialogResult.OK)
            //{
            //    // Todo add dialog for dynamic content options.
            //    LayerContainer layers = new LayerContainer();
            //    layers.SoloGuid = layer.ID;
            //    layers.SaveToFile(saveDialog.FileName);
            //    layers.Dispose();
            //    GC.SuppressFinalize(layers);
            //}
        }
        static void Expand_Click(object sender, EventArgs e)
        {
            //Layer selectedLayer = (Layer)selectedLayer;
            //selectedLayer.Opened = true;
            //LoadLayerChildren(selectedLayer, layerTree.SelectedNode);
            //layerTree.SelectedNode.Expand();
            //version++;
        }

        static void Collapse_Click(object sender, EventArgs e)
        {
            //   selectedLayer.Opened = false;
            //todo update UIO
        }

        static void copyMenu_Click(object sender, EventArgs e)
        {
            if (selectedLayer != null && selectedLayer is Layer)
            {
                Layer node = (Layer)selectedLayer;
                node.CopyToClipboard();
            }
        }

        static void newLayerGroupMenu_Click(object sender, EventArgs e)
        {
            //bool badName = true;
            //string name = Language.GetLocalizedText(676, "Enter Layer Group Name");
            //while (badName)
            //{
            //    SimpleInput input = new SimpleInput(name, Language.GetLocalizedText(238, "Name"), Language.GetLocalizedText(677, "Layer Group"), 100);
            //    if (input.ShowDialog() == DialogResult.OK)
            //    {
            //        name = input.ResultText;
            //        if (!AllMaps.ContainsKey(name))
            //        {
            //            MakeLayerGroup(name);
            //            version++;
            //            badName = false;
            //            LoadTreeLocal();
            //        }
            //        else
            //        {
            //            UiTools.ShowMessageBox(Language.GetLocalizedText(1374, "Choose a unique name"), Language.GetLocalizedText(676, "Enter Layer Group Name"));
            //        }
            //    }
            //    else
            //    {
            //        badName = false;
            //    }
            //}
        }


        static private void ImportTLEFile(string filename)
        {
            //LayerMap target = (LayerMap)selectedLayer;
            //ImportTLEFile(filename, target);
        }

        static private void MakeLayerGroupNow(string name)
        {
            LayerMap target = (LayerMap)selectedLayer;
            MakeLayerGroup(name, target);
        }

        private static void MakeLayerGroup(string name, LayerMap target)
        {
            ReferenceFrame frame = new ReferenceFrame();
            frame.Name = name;
            frame.Reference = ReferenceFrames.Identity;
            LayerMap newMap = new LayerMap(frame.Name, ReferenceFrames.Identity);
            newMap.Frame = frame;
            newMap.Frame.SystemGenerated = false;
            target.AddChild(newMap);

            newMap.Frame.Parent = target.Name;
            AllMaps[frame.Name] = newMap;
            version++;
        }

        static void lifeTimeMenu_Click(object sender, EventArgs e)
        {
            //if (selectedLayer is Layer)
            //{
            //    LayerLifetimeProperties props = new LayerLifetimeProperties();
            //    props.Target = (Layer)selectedLayer;
            //    if (props.ShowDialog() == DialogResult.OK)
            //    {
            //        // This might be moot
            //        props.Target.CleanUp();
            //    }
            //}

        }

        static void deleteFrameMenu_Click(object sender, EventArgs e)
        {
            //LayerMap target = (LayerMap)selectedLayer;
            //if (UiTools.ShowMessageBox(Language.GetLocalizedText(678, "This will delete this reference frame and all nested reference frames and layers. Do you want to continue?"), Language.GetLocalizedText(680, "Delete Reference Frame"), MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
            //{
            //    PurgeLayerMapDeep(target, true);
            //    version++;
            //    LoadTreeLocal();
            //}


        }

        static void FramePropertiesMenu_Click(object sender, EventArgs e)
        {
            LayerMap target = (LayerMap)selectedLayer;
            LayerManager.ReferenceFramePropsDialog.Show(target.Frame, e);

        }



        static void newMenu_Click(object sender, EventArgs e)
        {
            ReferenceFrame frame = new ReferenceFrame();
            LayerManager.FrameWizardDialog.Show(frame, e);
        }

        public static void referenceFrameWizardFinished(ReferenceFrame frame)
        {
            LayerMap target = (LayerMap)selectedLayer;
            LayerMap newMap = new LayerMap(frame.Name, ReferenceFrames.Custom);
            if (!AllMaps.ContainsKey(frame.Name))
            {
                newMap.Frame = frame;

                target.AddChild(newMap);
                newMap.Frame.Parent = target.Name;
                AllMaps[frame.Name] = newMap;
                version++;
                LoadTree();
            }
        }


        public static bool PasteFromTle(string[] lines, ReferenceFrame frame)
        {

            string line1 = "";
            string line2 = "";
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
                if (lines[i].Length == 69 && ReferenceFrame.IsTLECheckSumGood(lines[i]))
                {
                    if (line1.Length == 0 && lines[i].Substring(0, 1) == "1")
                    {
                        line1 = lines[i];
                    }
                    if (line2.Length == 0 && lines[i].Substring(0, 1) == "2")
                    {
                        line2 = lines[i];
                    }
                }
            }

            if (line1.Length == 69 && line2.Length == 69)
            {
                frame.FromTLE(line1, line2, 398600441800000);
                return true;

            }
            return false;

        }


        static void opacityMenu_Click(object sender, EventArgs e)
        {
            //OpacityPopup popup = new OpacityPopup();
            //popup.Target = (Layer)selectedLayer;
            //popup.Location = Cursor.Position;
            //popup.StartPosition = FormStartPosition.Manual;
            //popup.Show();

        }

        static void defaultImageset_Click(object sender, EventArgs e)
        {
            ImageSetLayer isl = selectedLayer as ImageSetLayer;
            isl.OverrideDefaultLayer = !isl.OverrideDefaultLayer;
        }

        static void propertiesMenu_Click(object sender, EventArgs e)
        {

            if (selectedLayer is SpreadSheetLayer)
            {
                SpreadSheetLayer target = (SpreadSheetLayer)selectedLayer;
                DataVizWizardDialog.Show(target, e);
                //    DataWizard.ShowPropertiesSheet(target);

                //    target.CleanUp();
                //    LoadTree();
            }
            //else if (selectedLayer is SpreadSheetLayer || selectedLayer is Object3dLayer)
            //{
            //    Object3dProperties props = new Object3dProperties();
            //    props.layer = (Object3dLayer)selectedLayer;
            //    //   props.ShowDialog();
            //    props.Owner = Earth3d.MainWindow;
            //    props.Show();
            //}
            //else if (selectedLayer is GroundOverlayLayer)
            //{
            //    GroundOverlayProperties props = new GroundOverlayProperties();
            //    props.Overlay = ((GroundOverlayLayer)selectedLayer).Overlay;
            //    props.OverlayLayer = ((GroundOverlayLayer)selectedLayer);
            //    props.Owner = Earth3d.MainWindow;
            //    props.Show();
            //}
            if (selectedLayer is GreatCirlceRouteLayer)
            {

                GreatCircleDlg.Show((GreatCirlceRouteLayer)selectedLayer, new EventArgs());

            }
        }

        static void renameMenu_Click(object sender, EventArgs e)
        {
            Layer layer = (Layer)selectedLayer;
            SimpleInput input = new SimpleInput(Language.GetLocalizedText(225, "Rename"), Language.GetLocalizedText(228, "New Name"), layer.Name, 32);

            input.Show(lastMenuClick, delegate ()
            {
                if (!string.IsNullOrEmpty(input.Text))
                {
                    layer.Name = input.Text;
                    version++;
                    LoadTree();
                }
            });


        }

        static void colorMenu_Click(object sender, EventArgs e)
        {
            Layer layer = (Layer)selectedLayer;

            ColorPicker picker = new ColorPicker();
            if (layer.Color != null)
            {
                picker.Color = layer.Color;
            }
            picker.CallBack = delegate
            {
                layer.Color = picker.Color;
            };

            picker.Show(e);
        }

        static void addMenu_Click(object sender, EventArgs e)
        {
            //bool overridable = false;
            //if ( selectedLayer is LayerMap)
            //{
            //    LayerMap map = selectedLayer as LayerMap;
            //    if (map.Frame.reference == ReferenceFrames.Custom)
            //    {
            //        overridable = true;
            //    }
            //}
            //Earth3d.LoadLayerFile(overridable);

        }


        static void deleteMenu_Click(object sender, EventArgs e)
        {
            DeleteSelectedLayer();
        }

        static private void DeleteSelectedLayer()
        {
            if (selectedLayer != null && selectedLayer is Layer)
            {
                Layer node = (Layer)selectedLayer;

                LayerList.Remove(node.ID);
                AllMaps[CurrentMap].Layers.Remove(node);
                node.CleanUp();
                node.Version++;
                LoadTree();
                version++;
            }
        }

        static public void scaleMenu_click(object sender, EventArgs e)
        {
            ImageSetLayer isl = selectedLayer as ImageSetLayer;

            if (isl != null)
            {
                Histogram hist = new Histogram();
                hist.image = isl.GetFitsImage();
                hist.layer = isl;
                hist.Show(Vector2d.Create(200, 200));
            }
        }

        static void showViewer_Click(object sender, EventArgs e)
        {
            if (selectedLayer is VoTableLayer)
            {
                VoTableLayer layer = selectedLayer as VoTableLayer;
                WWTControl.scriptInterface.DisplayVoTableLayer(layer);
            }
        }

        static void bottom_Click(object sender, EventArgs e)
        {
            Layer layer = selectedLayer as Layer;
            if (layer != null)
            {
                AllMaps[layer.ReferenceFrame].Layers.Remove(layer);
                AllMaps[layer.ReferenceFrame].Layers.Add(layer);
            }
            version++;
            LoadTree();
        }

        static void down_Click(object sender, EventArgs e)
        {
            Layer layer = selectedLayer as Layer;
            if (layer != null)
            {
                int index = AllMaps[layer.ReferenceFrame].Layers.LastIndexOf(layer);
                if (index < (AllMaps[layer.ReferenceFrame].Layers.Count - 1))
                {
                    AllMaps[layer.ReferenceFrame].Layers.Remove(layer);
                    AllMaps[layer.ReferenceFrame].Layers.Insert(index + 1, layer);
                }
            }
            version++;
            LoadTree();
        }

        static void up_Click(object sender, EventArgs e)
        {
            Layer layer = selectedLayer as Layer;
            if (layer != null)
            {
                int index = AllMaps[layer.ReferenceFrame].Layers.LastIndexOf(layer);
                if (index > 0)
                {
                    AllMaps[layer.ReferenceFrame].Layers.Remove(layer);
                    AllMaps[layer.ReferenceFrame].Layers.Insert(index - 1, layer);
                }
            }
            version++;
            LoadTree();
        }

        static void top_Click(object sender, EventArgs e)
        {
            Layer layer = selectedLayer as Layer;
            if (layer != null)
            {
                AllMaps[layer.ReferenceFrame].Layers.Remove(layer);
                AllMaps[layer.ReferenceFrame].Layers.Insert(0, layer);
            }
            version++;
            LoadTree();
        }


        static void pasteLayer_Click(object sender, EventArgs e)
        {
            //ClipbaordDelegate clip = delegate (string clipText)
            //{
            //    CreateSpreadsheetLayer(CurrentMap, "Clipboard", clipText);
            //};
            DataVizWizardDialog.Show(CurrentMap, e);

            //Navigator.Clipboard.ReadText().Then(clip);


            //IDataObject dataObject = Clipboard.GetDataObject();
            //if (dataObject.GetDataPresent(DataFormats.UnicodeText))
            //{
            //    string[] formats = dataObject.GetFormats();
            //    object data = dataObject.GetData(DataFormats.UnicodeText);
            //    if (data is String)
            //    {
            //        string layerName = "Pasted Layer";

            //        SpreadSheetLayer layer = new SpreadSheetLayer((string)data, true);
            //        layer.Enabled = true;
            //        layer.Name = layerName;

            //        if (DataWizard.ShowWizard(layer) == DialogResult.OK)
            //        {
            //            LayerList.Add(layer.ID, layer);
            //            layer.ReferenceFrame = CurrentMap;
            //            AllMaps[CurrentMap].Layers.Add(layer);
            //            AllMaps[CurrentMap].Open = true;
            //            version++;
            //            LoadTree();

            //        }
            //    }
            //}

        }
        public static SpreadSheetLayer CreateSpreadsheetLayer(string frame, string name, string data)
        {
            SpreadSheetLayer layer = new SpreadSheetLayer();
            layer.LoadFromString(data, false, false, false, true);
            layer.Name = name;
            LayerManager.AddSpreadsheetLayer(layer, frame);
            return layer;
        }

        public static void AddSpreadsheetLayer(SpreadSheetLayer layer, string frame)
        {
            layer.Enabled = true;
            layer.ReferenceFrame = frame;
            Add(layer, true);
        }

        static void showOrbitPlanet_Click(object sender, EventArgs e)
        {
            try
            {
                int bit = int.Parse(((ToolStripMenuItem)sender).Tag.ToString());

                // Flip the state
                if ((Settings.GlobalSettings.PlanetOrbitsFilter & bit) == 0)
                {
                    Settings.GlobalSettings.PlanetOrbitsFilter |= bit;
                }
                else
                {
                    Settings.GlobalSettings.PlanetOrbitsFilter &= ~bit;
                }

            }
            catch
            {
            }
        }

        static void showOrbit_Click(object sender, EventArgs e)
        {
            // Flip the state
            LayerMap map = selectedLayer as LayerMap;

            map.Frame.ShowOrbitPath = !map.Frame.ShowOrbitPath;
        }

        static void addGreatCircle_Click(object sender, EventArgs e)
        {
            AddGreatCircleLayer();
        }


        static void addMpc_Click(object sender, EventArgs e)
        {
            LayerMap target = (LayerMap)selectedLayer;
            SimpleInput input = new SimpleInput(Language.GetLocalizedText(1302, "Minor planet name or designation"), Language.GetLocalizedText(238, "Name"), "", 32);
            bool retry = false;
            do
            {
                if (input.ShowDialog() == DialogResult.OK)
                {
                    if (target.ChildMaps.ContainsKey(input.Text))
                    {
                        retry = true;
                        //UiTools.ShowMessageBox("That Name already exists");
                    }
                    else
                    {
                        try
                        {
                            GetMpc(input.Text, target);
                            retry = false;
                        }
                        catch
                        {
                            retry = true;
                            //  UiTools.ShowMessageBox(Language.GetLocalizedText(1303, "The designation was not found or the MPC service was unavailable"));
                        }
                    }
                }
                else
                {
                    retry = false;
                }
            } while (retry);
            return;
        }

        static private void AsOrbitalLines_Click(object sender, EventArgs e)
        {
            LayerMap target = (LayerMap)selectedLayer;
            SimpleInput input = new SimpleInput(Language.GetLocalizedText(1302, "Minor planet name or designation"), Language.GetLocalizedText(238, "Name"), "", 32);

            input.Show(Cursor.Position, delegate ()
             {
                 if (target.ChildMaps.ContainsKey(input.Text))
                 {

                     //   UiTools.ShowMessageBox("That Name already exists");
                 }
                 else
                 {
                     GetMpcAsTLE(input.Text, target);
                 }
             });
        }

        static void GetMpcAsTLE(string id, LayerMap target)
        {
            WebFile file = new WebFile("https://www.minorplanetcenter.net/db_search/show_object?object_id=" + id);

            file.OnStateChange = delegate ()
                {
                    if (file.State != StateType.Received)
                    {
                        return;
                    }

                    string data = file.GetText();


                    int startform = data.IndexOf("show-orbit-button");

                    int lastForm = data.IndexOf("/form", startform);

                    string formpart = data.Substring(startform, lastForm);

                    string name = id;

                    ReferenceFrame frame = new ReferenceFrame();

                    frame.Oblateness = 0;
                    frame.ShowOrbitPath = true;
                    frame.ShowAsPoint = true;

                    frame.Epoch = SpaceTimeController.UtcToJulian(Date.Parse(GetValueByID(formpart, "epoch").Substring(0, 10)));
                    frame.SemiMajorAxis = double.Parse(GetValueByID(formpart, "a")) * UiTools.KilometersPerAu * 1000;
                    frame.ReferenceFrameType = ReferenceFrameTypes.Orbital;
                    frame.Inclination = double.Parse(GetValueByID(formpart, "incl"));
                    frame.LongitudeOfAscendingNode = double.Parse(GetValueByID(formpart, "node"));
                    frame.Eccentricity = double.Parse(GetValueByID(formpart, "e"));
                    frame.MeanAnomolyAtEpoch = double.Parse(GetValueByID(formpart, "m"));
                    frame.MeanDailyMotion = ELL.MeanMotionFromSemiMajorAxis(double.Parse(GetValueByID(formpart, "a")));
                    frame.ArgumentOfPeriapsis = double.Parse(GetValueByID(formpart, "peri"));
                    frame.Scale = 1;
                    frame.SemiMajorAxisUnits = AltUnits.Meters;
                    frame.MeanRadius = 10;
                    frame.Oblateness = 0;

                    String TLE = name + "\n" + frame.ToTLE();
                    LoadOrbitsFile(id, TLE, target.Name);

                    LoadTree();
                };
            file.Send();

        }

        //string ConvertToTLE(LayerMap map)
        //{

        //    LayerMap target = map.Parent;

        //    ReferenceFrame frame = map.Frame;
        //    string name = frame.Name;

        //    String TLE = name + "\n" + frame.ToTLE();

        //    String filename = Path.GetTempPath() + "\\" + name;

        //    File.WriteAllText(filename, TLE);

        //    LoadOrbitsFile(filename, target.Name);

        //    LoadTree();

        //    return null;
        //}


        static void GetMpc(string id, LayerMap target)
        {

            WebFile file = new WebFile("https://www.minorplanetcenter.net/db_search/show_object?object_id=" + id);

            file.OnStateChange = delegate ()
            {
                string data = file.GetText();


                int startform = data.IndexOf("show-orbit-button");

                int lastForm = data.IndexOf("/form", startform);

                string formpart = data.Substring(startform, lastForm);

                string name = id;

                LayerMap orbit = new LayerMap(name.Trim(), ReferenceFrames.Custom);


                orbit.Frame.Oblateness = 0;
                orbit.Frame.ShowOrbitPath = true;
                orbit.Frame.ShowAsPoint = true;

                orbit.Frame.Epoch = SpaceTimeController.UtcToJulian(Date.Parse(GetValueByID(formpart, "epoch").Substring(0, 10)));
                orbit.Frame.SemiMajorAxis = double.Parse(GetValueByID(formpart, "a")) * UiTools.KilometersPerAu * 1000;
                orbit.Frame.ReferenceFrameType = ReferenceFrameTypes.Orbital;
                orbit.Frame.Inclination = double.Parse(GetValueByID(formpart, "incl"));
                orbit.Frame.LongitudeOfAscendingNode = double.Parse(GetValueByID(formpart, "node"));
                orbit.Frame.Eccentricity = double.Parse(GetValueByID(formpart, "e"));
                orbit.Frame.MeanAnomolyAtEpoch = double.Parse(GetValueByID(formpart, "m"));
                orbit.Frame.MeanDailyMotion = ELL.MeanMotionFromSemiMajorAxis(double.Parse(GetValueByID(formpart, "a")));
                orbit.Frame.ArgumentOfPeriapsis = double.Parse(GetValueByID(formpart, "peri"));
                orbit.Frame.Scale = 1;
                orbit.Frame.SemiMajorAxisUnits = AltUnits.Meters;
                orbit.Frame.MeanRadius = 10;
                orbit.Frame.Oblateness = 0;

                if (!AllMaps[target.Name].ChildMaps.ContainsKey(name.Trim()))
                {
                    AllMaps[target.Name].AddChild(orbit);
                }

                AllMaps[orbit.Name] = orbit;

                orbit.Frame.Parent = target.Name;

                MakeLayerGroup("Minor Planet", orbit);

                LoadTree();
            };

        }

        static string GetValueByID(string data, string id)
        {

            int valStart = data.IndexOf("id=\"" + id + "\"");
            valStart = data.IndexOf("value=", valStart) + 7;
            int valEnd = data.IndexOf("\"", valStart);
            return data.Substr(valStart, valEnd - valStart);
        }

        private static void AddGreatCircleLayer()
        {

            GreatCirlceRouteLayer layer = new GreatCirlceRouteLayer();
            CameraParameters camera = WWTControl.Singleton.RenderContext.ViewCamera;
            layer.LatStart = camera.Lat;
            layer.LatEnd = camera.Lat - 5;
            layer.LngStart = camera.Lng;
            layer.LngEnd = camera.Lng + 5;
            layer.Width = 4;
            layer.Enabled = true;
            layer.Name = Language.GetLocalizedText(1144, "Great Circle Route");
            LayerList[layer.ID] = layer;
            layer.ReferenceFrame = currentMap;
            AllMaps[currentMap].Layers.Add(layer);
            AllMaps[currentMap].Open = true;
            version++;
            LoadTree();

            GreatCircleDlg.Show(layer, new EventArgs());

        }

        internal static Layer LoadOrbitsFile(string name, string data, string currentMap)
        {
            OrbitLayer layer = new OrbitLayer();
            //todo fix this
            layer.LoadString(data);
            layer.Enabled = true;
            layer.Name = name;
            LayerList[layer.ID] = layer;
            layer.ReferenceFrame = currentMap;
            AllMaps[currentMap].Layers.Add(layer);
            AllMaps[currentMap].Open = true;
            version++;
            LoadTree();
            return layer;
        }



    }
    public class LayerMap
    {
        public LayerMap(string name, ReferenceFrames reference)
        {
            Name = name;
            Frame.Reference = reference;
            double radius = 6371000;

            switch (reference)
            {
                case ReferenceFrames.Sky:
                    break;
                case ReferenceFrames.Ecliptic:
                    break;
                case ReferenceFrames.Galactic:
                    break;
                case ReferenceFrames.Sun:
                    radius = 696000000;
                    break;
                case ReferenceFrames.Mercury:
                    radius = 2439700;
                    break;
                case ReferenceFrames.Venus:
                    radius = 6051800;
                    break;
                case ReferenceFrames.Earth:
                    radius = 6371000;
                    break;
                case ReferenceFrames.Mars:
                    radius = 3390000;
                    break;
                case ReferenceFrames.Jupiter:
                    radius = 69911000;
                    break;
                case ReferenceFrames.Saturn:
                    radius = 58232000;
                    break;
                case ReferenceFrames.Uranus:
                    radius = 25362000;
                    break;
                case ReferenceFrames.Neptune:
                    radius = 24622000;
                    break;
                case ReferenceFrames.Pluto:
                    radius = 1161000;
                    break;
                case ReferenceFrames.Moon:
                    radius = 1737100;
                    break;
                case ReferenceFrames.Io:
                    radius = 1821500;
                    break;
                case ReferenceFrames.Europa:
                    radius = 1561000;
                    break;
                case ReferenceFrames.Ganymede:
                    radius = 2631200;
                    break;
                case ReferenceFrames.Callisto:
                    radius = 2410300;
                    break;
                case ReferenceFrames.Custom:
                    break;
                case ReferenceFrames.Identity:
                    break;
                default:
                    break;
            }
            Frame.MeanRadius = radius;

        }
        public Dictionary<string, LayerMap> ChildMaps = new Dictionary<string, LayerMap>();
        public void AddChild(LayerMap child)
        {
            child.Parent = this;
            ChildMaps[child.Name] = child;
        }

        public LayerMap Parent = null;
        public List<Layer> Layers = new List<Layer>();
        public bool Open = false;
        public bool Enabled = true;
        public bool LoadedFromTour = false;
        public string Name
        {
            get { return Frame.Name; }
            set { Frame.Name = value; }
        }


        public ReferenceFrame Frame = new ReferenceFrame();
        public void ComputeFrame(RenderContext renderContext)
        {
            if (Frame.Reference == ReferenceFrames.Custom)
            {
                Frame.ComputeFrame(renderContext);

            }

        }

        public override string ToString()
        {
            return Name;
        }


    }
    //public enum ReferenceFrames { Earth = 0, Helocentric = 1, Equatorial = 2, Ecliptic = 3, Galactic = 4, Moon = 5, Mercury = 6, Venus = 7, Mars = 8, Jupiter = 9, Saturn = 10, Uranus = 11, Neptune = 12, Custom = 13 };


    public enum ReferenceFrames
    {
        Sky = 0,
        Ecliptic = 1,
        Galactic = 2,
        Sun = 3,
        Mercury = 4,
        Venus = 5,
        Earth = 6,
        Mars = 7,
        Jupiter = 8,
        Saturn = 9,
        Uranus = 10,
        Neptune = 11,
        Pluto = 12,
        Moon = 13,
        Io = 14,
        Europa = 15,
        Ganymede = 16,
        Callisto = 17,
        Custom = 18,
        Identity = 19,
        Sandbox = 20
    };

    public class SkyOverlays
    {

    }

    public class GroundOverlayLayer
    {
    }


    public class FrameTarget
    {
        public Vector3d Target;
        public Matrix3d Matrix;
    }
}
