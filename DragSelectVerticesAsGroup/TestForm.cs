using System;
using System.IO;
using System.Windows.Forms;
using ThinkGeo.MapSuite.Core;


namespace  DragSelectVerticesAsGroup
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void TestForm_Load(object sender, EventArgs e)
        {
            label1.Text = "Select vertices by clicking on them and then drag them as a group.";

            winformsMap1.MapUnit = GeographyUnit.DecimalDegree;
            winformsMap1.CurrentExtent = new RectangleShape(-97.755, 30.319, -97.7266, 30.3018);
            winformsMap1.BackgroundOverlay.BackgroundBrush = new GeoSolidBrush(GeoColor.FromArgb(255, 198, 255, 255));

            //Displays the World Map Kit as a background.
            ThinkGeo.MapSuite.DesktopEdition.WorldMapKitWmsDesktopOverlay worldMapKitDesktopOverlay = new ThinkGeo.MapSuite.DesktopEdition.WorldMapKitWmsDesktopOverlay();
            winformsMap1.Overlays.Add(worldMapKitDesktopOverlay);

            string fileName1 = @"..\..\data\polygon.txt";
            StreamReader sr1 = new StreamReader(fileName1);

            //DragtInteractiveOverlay for setting the PointStyles of the control points and dragged points.
            SelectVerticesEditInteractiveOverlay selectVerticesEditInteractiveOverlay = new SelectVerticesEditInteractiveOverlay();
            selectVerticesEditInteractiveOverlay.EditShapesLayer.InternalFeatures.Add("Polygon", new Feature(BaseShape.CreateShapeFromWellKnownData(sr1.ReadLine())));

            //Sets the PointStyle for the non dragged control points.
            selectVerticesEditInteractiveOverlay.ControlPointStyle = new PointStyle(PointSymbolType.Circle, new GeoSolidBrush(GeoColor.StandardColors.PaleGoldenrod), new GeoPen(GeoColor.StandardColors.Black), 8);
            //Sets the PointStyle for the dragged control points.
            selectVerticesEditInteractiveOverlay.SelectedControlPointStyle = new PointStyle(PointSymbolType.Circle, new GeoSolidBrush(GeoColor.StandardColors.Green), new GeoPen(GeoColor.StandardColors.DarkGreen, 2), 10);
           
            selectVerticesEditInteractiveOverlay.CanAddVertex = false;
            selectVerticesEditInteractiveOverlay.CanDrag = false;
            selectVerticesEditInteractiveOverlay.CanRemoveVertex = false;
            selectVerticesEditInteractiveOverlay.CanResize = false;
            selectVerticesEditInteractiveOverlay.CanRotate = false;
            selectVerticesEditInteractiveOverlay.CalculateAllControlPoints();

            winformsMap1.EditOverlay = selectVerticesEditInteractiveOverlay;

            winformsMap1.Refresh();
        }

      
        private void winformsMap1_MouseMove(object sender, MouseEventArgs e)
        {
            //Displays the X and Y in screen coordinates.
            statusStrip1.Items["toolStripStatusLabelScreen"].Text = "X:" + e.X + " Y:" + e.Y;

            //Gets the PointShape in world coordinates from screen coordinates.
            PointShape pointShape = ExtentHelper.ToWorldCoordinate(winformsMap1.CurrentExtent, new ScreenPointF(e.X, e.Y), winformsMap1.Width, winformsMap1.Height);

            //Displays world coordinates.
            statusStrip1.Items["toolStripStatusLabelWorld"].Text = "(world) X:" + Math.Round(pointShape.X, 4) + " Y:" + Math.Round(pointShape.Y, 4);
        }
        
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
