using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.DesktopEdition;

namespace DragSelectVerticesAsGroup
{
    class SelectVerticesEditInteractiveOverlay : EditInteractiveOverlay
    {
        private PointStyle controlPointStyle;
        private PointStyle selectedControlPointStyle;
        private Collection<Feature> existingFeatures;
        private PointShape startPoint;

        public SelectVerticesEditInteractiveOverlay()
            : base()
        {
            this.CanAddVertex = false;
            this.CanDrag = true;
            this.CanRemoveVertex = false;
            this.CanResize = false;
            this.CanRotate = false;

            ExistingControlPointsLayer.Open();
            ExistingControlPointsLayer.Columns.Add(new FeatureSourceColumn("IsSelected"));
            ExistingControlPointsLayer.Close();

            existingFeatures = new Collection<Feature>();
        }

        public PointStyle ControlPointStyle
        {
            get { return controlPointStyle; }
            set { controlPointStyle = value; }
        }

        public PointStyle SelectedControlPointStyle
        {
            get { return selectedControlPointStyle; }
            set { selectedControlPointStyle = value; }
        }

        protected override InteractiveResult MouseDownCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseDownCore(interactionArguments);
            startPoint = null;
            if (interactionArguments.MouseButton == MapMouseButton.Left)
            {
                if (EditShapesLayer.InternalFeatures.Count > 0)
                {
                    if (SetSelectedControlPoint(new PointShape(interactionArguments.WorldX, interactionArguments.WorldY), interactionArguments.SearchingTolerance))
                    {
                        startPoint = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
                    }
                }
            }
            return result;
        }

        Collection<int> ids = new Collection<int>();
        protected override InteractiveResult MouseMoveCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = new InteractiveResult();
            if (ControlPointType != ControlPointType.None)
            {
                if (startPoint == null)
                {
                    return result;
                }
                else
                {
                    PointShape currentPoint = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
                    double xOffset = currentPoint.X - startPoint.X;
                    double yOffset = currentPoint.Y - startPoint.Y;

                    Collection<Feature> features = new Collection<Feature>();

                    int index = 0;
                    System.Diagnostics.Debug.WriteLine("ids count");
                    foreach (int id in ids)
                    {
                        System.Diagnostics.Debug.WriteLine(id.ToString());
                    }
                    if (ids.Count == 0)
                    {
                        foreach (Feature feature in ExistingControlPointsLayer.InternalFeatures)
                        {
                            if (feature.ColumnValues.ContainsKey("IsSelected") && feature.ColumnValues["IsSelected"] == "true")
                            {
                                features.Add(feature);
                                ids.Add(index);
                            }
                            index++;
                        }
                    }
                    else
                    {
                        foreach (int id in ids)
                        {
                            features.Add(ExistingControlPointsLayer.InternalFeatures[id]);
                        }
                    }

                    foreach (Feature feature in features)
                    {
                        PointShape oldPoint = feature.GetShape() as PointShape;
                        PointShape targetPoint = new PointShape(oldPoint.X + xOffset, oldPoint.Y + yOffset);
                        EditShapesLayer.InternalFeatures[0] = MoveVertex(EditShapesLayer.InternalFeatures[0], oldPoint, targetPoint);
                    }
                    
                    CalculateAllControlPoints();

                    startPoint = currentPoint.CloneDeep() as PointShape;
                    bool existingNode = false;
                    foreach (string key in EditShapesLayer.InternalFeatures.GetKeys())
                    {
                        if (string.Equals(EditShapesLayer.InternalFeatures[key].Id, OriginalEditingFeature.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            Lock.EnterWriteLock();
                            try
                            {
                                EditShapesLayer.InternalFeatures[key] = EditShapesLayer.InternalFeatures[0];
                            }
                            finally
                            {
                                Lock.ExitWriteLock();
                            }
                            foreach (string featureKey in ExistingControlPointsLayer.InternalFeatures.GetKeys())
                            {
                                Feature feature = ExistingControlPointsLayer.InternalFeatures[featureKey];
                                if (feature.ColumnValues != null)
                                {
                                    if (feature.ColumnValues["state"] == "selected")
                                    {
                                        existingNode = true;
                                    }
                                }
                            }
                            ShowAllControlPointLayers(false, existingNode);
                            break;
                        }
                    }

                    result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                    result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                }
            }

            return result;
        }

        private void ShowAllControlPointLayers(bool visiable, bool existingControlPointVisible)
        {
            Lock.EnterWriteLock();
            try
            {
                DragControlPointsLayer.IsVisible = visiable;
                RotateControlPointsLayer.IsVisible = visiable;
                ResizeControlPointsLayer.IsVisible = visiable;
                ExistingControlPointsLayer.IsVisible = existingControlPointVisible;

            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        protected override InteractiveResult MouseClickCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseClickCore(interactionArguments);
            if (result.DrawThisOverlay != InteractiveOverlayDrawType.Draw)
            {
                RectangleShape searchingArea = new RectangleShape(interactionArguments.WorldX - interactionArguments.SearchingTolerance, interactionArguments.WorldY + interactionArguments.SearchingTolerance, interactionArguments.WorldX + interactionArguments.SearchingTolerance, interactionArguments.WorldY - interactionArguments.SearchingTolerance);

                foreach (Feature feature in ExistingControlPointsLayer.InternalFeatures)
                {
                    BaseShape shape = feature.GetShape();
                    if (shape.IsWithin(searchingArea))
                    {
                        if (feature.ColumnValues.ContainsKey("IsSelected") && (feature.ColumnValues["IsSelected"] == "true"))
                        {
                            feature.ColumnValues["IsSelected"] = string.Empty;
                            result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                            result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                            Lock.IsDirty = true;
                            break;
                        }
                        else
                        {
                            feature.ColumnValues["IsSelected"] = "true";

                            result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                            result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                            Lock.IsDirty = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        protected override Feature AddVertexCore(Feature targetFeature, PointShape targetPointShape, double searchingTolerance)
        {
            existingFeatures.Clear();
            foreach (Feature feature in ExistingControlPointsLayer.InternalFeatures)
            {
                if (feature.ColumnValues.ContainsKey("IsSelected") && feature.ColumnValues["IsSelected"] == "true")
                {
                    existingFeatures.Add(feature);
                }
            }

            return base.AddVertexCore(targetFeature, targetPointShape, searchingTolerance);
        }

        protected override IEnumerable<Feature> CalculateVertexControlPointsCore(Feature feature)
        {
            IEnumerable<Feature> features = base.CalculateVertexControlPointsCore(feature);

            foreach (Feature item in features)
            {
                PointShape pointShape = item.GetShape() as PointShape;
                foreach (Feature existingFeature in existingFeatures)
                {
                    if (pointShape.Equal2D(existingFeature))
                    {
                        item.ColumnValues["IsSelected"] = "true";
                    }
                }
            }
            return features;
        }

        protected override void OnControlPointSelected(ControlPointSelectedEditInteractiveOverlayEventArgs e)
        {
            base.OnControlPointSelected(e);

            foreach (Feature feature in ExistingControlPointsLayer.InternalFeatures)
            {
                if (feature.ColumnValues["State"] == "selected")
                {
                    feature.ColumnValues["IsSelected"] = "true";
                }
            }
        }

        protected override InteractiveResult MouseUpCore(InteractionArguments interactionArguments)
        {
            Collection<int> draggedPointsIndex = new Collection<int>();

            this.ExistingControlPointsLayer.Open();
            Collection<Feature> draggedPoints = this.ExistingControlPointsLayer.QueryTools.GetAllFeatures(new string[] { "IsSelected" });
            this.ExistingControlPointsLayer.Close();

            for (int index = 0; index < draggedPoints.Count; index++)
            {
                if (draggedPoints[index].ColumnValues["IsSelected"] == "true")
                {
                    draggedPointsIndex.Add(index);
                }
            }

            InteractiveResult interactiveResult = base.MouseUpCore(interactionArguments);
            foreach (int index in draggedPointsIndex)
            {
                this.ExistingControlPointsLayer.InternalFeatures[index].ColumnValues["IsSelected"] = "true";
            }
            this.ExistingControlPointsLayer.Close();
            if (startPoint != null)
            {
                startPoint = null;
                ControlPointType = ControlPointType.None;
            }

            ids = new Collection<int>();
            return interactiveResult;
        }

        //Overrides the DrawCore function.
        protected override void DrawCore(GeoCanvas canvas)
        {
            if (EditShapesLayer.InternalFeatures.Count > 1)
            {
                throw new NotImplementedException("Only allow one shape to be editing.");
            }

            //Draws the Edit Shapes as default.
            Collection<SimpleCandidate> labelsInAllLayers = new Collection<SimpleCandidate>();
            EditShapesLayer.Open();
            EditShapesLayer.Draw(canvas, labelsInAllLayers);
            canvas.Flush();

            ExistingControlPointsLayer.Open();
            Collection<Feature> ExistingControlPoints = ExistingControlPointsLayer.QueryTools.GetAllFeatures(new string[1] { "IsSelected" });
            ExistingControlPointsLayer.Close();

            //Loops thru the control points.
            for (int i = ExistingControlPoints.Count - 1; i >= 0; i--)
            {
                Feature feature = ExistingControlPoints[i];
                if (feature.ColumnValues.ContainsKey("IsSelected") && feature.ColumnValues["IsSelected"] == "true")
                {
                    Feature[] features = new Feature[1] { feature };
                    selectedControlPointStyle.Draw(features, canvas, labelsInAllLayers, labelsInAllLayers);
                }
                else
                {
                    Feature[] features = new Feature[1] { feature };
                    controlPointStyle.Draw(features, canvas, labelsInAllLayers, labelsInAllLayers);
                }
            }
        }
    }
}
