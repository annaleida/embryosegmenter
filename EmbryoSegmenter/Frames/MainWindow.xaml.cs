using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EmbryoSegmenter.Logging;
using EmbryoSegmenter.Shapes;
using EmbryoSegmenter.Pipelines;
using System.IO;
using Microsoft.Win32;
using itk;
using Kitware.VTK;
using SharpGL;

namespace EmbryoSegmenter.Frames
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Manager for all loggers in project
        private static LoggerManager logManager;
        //Logger for this window
        private static Logger log;
        private int mode; //1 for ES 1.x, 2 for ES2.x
        private double z_scale;
        private double xy_scale;
        string _current_Seg_File_Path = "";
        Series m_Series;
        int m_selected_slice_index;
        List<Shapes.Point> m_DrawingSegmentationPoints;
        List<Shapes.Annotation> m_DrawingSegmentationAnnotations;
        List<Shapes.Annotation> m_DrawingNucleiAnnotations;
        List<Shapes.Circle> m_DrawingNuclei;
        List<Pipeline> m_Pipelines;
        bool initialized = false;
        Circle temp_dragDisplay_Nuc;
        Segment temp_dragDisplay_Seg;
        bool killFocus = true; //deactivate focus when key pressed
        Color previusColor; //Remember to undo selection changed in crop box
        

        public MainWindow()
        {
            
            InitializeComponent();
            //String test1 = EmbryoSegmenter.Properties.Settings.Default.LogFile;
            logManager = new LoggerManager(Properties.Settings.Default.LogFile.ToString());
            //string test = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
            log = logManager.CreateNewLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString());
             _Populate_cbx_Colors();
             _Populate_cbx_Ann_Stage();
             _Populate_cbx_Ann_Fragmentation();
             _Populate_cbx_Quality();
            _InitializeNewImage();
            //Values given in µm/pixel
            txt_XY.Text = Properties.Settings.Default.XY_scale.ToString(); xy_scale = Properties.Settings.Default.XY_scale;
            txt_Z.Text = Properties.Settings.Default.Z_scale.ToString(); z_scale = Properties.Settings.Default.Z_scale;
            _RecomputeScale();
            initialized = true;
            _LogStringDebug("MainWindow Initialized");
        }

        private int IncreasePictureIndexByNumber(int startIndex, int number)
        {
            int endIndex = startIndex + number;
        if (endIndex < 0) {endIndex = 0;}
        if (endIndex > 500) { endIndex = 500; }
        return endIndex;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            killFocus = true;
            bool redraw = false;
            if (chb_Ann_NewNuclei.IsChecked == true && temp_dragDisplay_Nuc!= null)
            { //We are drawing new nuclei

                int centerX;
                int centerY;
                double radius;
                int.TryParse(txt_Ann_NewNuclei_CenterX.Text, out centerX);
                int.TryParse(txt_Ann_NewNuclei_CenterY.Text, out centerY);
                double.TryParse(txt_Ann_NewNuclei_Radius.Text, out radius);

                switch (e.Key)
                {
                    case Key.Left:
                         centerX = IncreasePictureIndexByNumber(centerX, -1);
                         txt_Ann_NewNuclei_CenterX.Text = centerX.ToString();
                         redraw = true;
                        break;

                    case Key.Right:
                        centerX = IncreasePictureIndexByNumber(centerX, 1);
                        txt_Ann_NewNuclei_CenterX.Text = centerX.ToString();
                        redraw = true;
                        break;

                    case Key.Up:
                        centerY = IncreasePictureIndexByNumber(centerY, -1);
                        txt_Ann_NewNuclei_CenterY.Text = centerY.ToString();
                        redraw = true;
                        break;

                    case Key.Down:
                        centerY = IncreasePictureIndexByNumber(centerY, 1);
                        txt_Ann_NewNuclei_CenterY.Text = centerY.ToString();
                        redraw = true;
                        break;
                }
                if (redraw)
                {
                    pnl_Panel_Nuc.DeleteVisual(temp_dragDisplay_Nuc);
                    temp_dragDisplay_Nuc.centre = new System.Windows.Point((double)centerX, (double)centerY);
                    temp_dragDisplay_Nuc.radius = radius;
                    temp_dragDisplay_Nuc.DrawCircleOutline(GetSelectedColorNuc());
                    pnl_Panel_Nuc.AddVisual(temp_dragDisplay_Nuc);
                }
            }          
        }
    
        
         
       

        #region Window actions

        private void _RenameSlice(int oldSliceNo, int newSliceNo)
        {
            lbx_Slice.Items.Clear();
            Slice slice_ = _Find_slice(oldSliceNo);
            slice_.sliceNo = newSliceNo;
            _Populate_lbx_Slice();
            _Select_item(1);
 }

        //This functionn has not been updated for a while
        private void _AddSegments(int sliceNo, List<int> segmentNos)
        {
            Slice slice_ = _Find_slice(sliceNo);
            Segment newSegment = new Segment();
            bool first = true;
            List<Segment> seg_to_remove = new List<Segment>();
            //Remove all segments
            //Add to new segment at the same time
            foreach (int segNo in segmentNos)
            {
                foreach (Segment seg_ in slice_.segments)
                {
                    if (seg_.segmentNo == segNo)
                    {
                        if (first) //Add to new segment int the meantime
                        {
                            newSegment.inEnclosed = false;
                            newSegment.segmentNo = segNo;
                            newSegment.segmentArea = seg_.segmentArea;
                            newSegment.centerX = seg_.centerX; //XXX Fix this later
                            newSegment.centerY = seg_.centerY; //XXX Fix this later
                            newSegment.points = seg_.points;
                            newSegment.nbrOfPoints = seg_.nbrOfPoints;
                            newSegment.annotation = seg_.annotation;
                            first = false;
                        }
                        else
                        {
                            newSegment.segmentArea += seg_.segmentArea;
                            foreach (Shapes.Point p in seg_.points)
                            {
                                newSegment.points.Add(p);
                            }
                            newSegment.nbrOfPoints += seg_.nbrOfPoints;
                        }

                        seg_to_remove.Add(seg_); //Add to list for later removal
                    }
                }

            }
            foreach (Segment seg_rem in seg_to_remove)  //Remova all old
            {
                slice_.segments.Remove(seg_rem);
            }

            slice_.segments.Add(newSegment); //Add new joint segments

        }

        private void _ComputeZscale()
        {
            if ((m_Series == null) || (m_Series.stacks == null) || (m_Series.stacks.Count == 0) || 
                (_Find_selected_stack().slices == null) || (_Find_selected_stack().slices.Count == 0) || 
                (_Find_selected_stack() == null))
            {
                return;
            }
            double rel_z_scale = z_scale / xy_scale;
            //double dbl_xy_scale = xyScale;
            //int int_z_scale = (int)(Math.Round(dbl_z_scale));
            //int int_xy_scale = (int)(Math.Round(dbl_xy_scale));
            int sliceIndex = 0; //Should be able to handle slices not marked in order
            Stack currentStack = _Find_selected_stack();
            foreach (Slice slice in currentStack.slices)
            {
                foreach (Segment seg in slice.segments)
                {
                    if (seg.points != null)
                    {
                        foreach (Shapes.Point point in seg.points)
                        {
                            point.Z = rel_z_scale * sliceIndex;
                        }
                    }
                }
                sliceIndex++;
            }
        }

        private void _Display3D(Rendering.DisplayMode mode)
        {
            _ComputeZscale();
            if ((_Find_selected_stack().slices == null) || (_Find_selected_stack().slices.Count == 0))
            { return; }
            Rendering.RenderingWindow render = new Rendering.RenderingWindow();
            List<Blob> blobs = new List<Blob>();
            Stack currentStack = _Find_selected_stack();
            int highestId = currentStack.GetHighestSegmentIDFromAllSlices();
            for (int blobId = 0; blobId < highestId; blobId++)
            {
                Blob blob = new Blob(blobId);
                foreach (Slice _slice in currentStack.slices)
                {
                    foreach (Segment _seg in _slice.segments)
                    {
                        if (_seg.segmentNo == blobId)
                        {
                            blob.segments.Add(_seg);
                        }
                    }
                }
                blobs.Add(blob);
            }
            render.SetShapes(blobs);
            render.Show(mode);

        }

        private void _RecomputeScale()
        {
            int pixelArea = 0;
            
           if (lbx_Segment.SelectedItem != null)
            {
                foreach (ListBoxItem item in lbx_Segment.SelectedItems)
                {
                    int segmentNo = (int)((ListBoxItem)item).Tag;
                    int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
                    Segment currentSegment = _Find_segment(sliceNo, segmentNo);
                    pixelArea += currentSegment.segmentArea;
                }
            }

            if ((double.TryParse(txt_XY.Text, out xy_scale)) && (double.TryParse(txt_Z.Text, out z_scale)))
            {
                txt_Area.Text = (pixelArea * xy_scale * xy_scale).ToString("0.00");
                txt_Volume.Text = (pixelArea * xy_scale * xy_scale * z_scale).ToString("0.00");
            }
        }

        private void _InitializeNewImage()
        {
            if (initialized)
            {
                if (m_DrawingSegmentationPoints == null)
                {
                    m_DrawingSegmentationPoints = new List<Shapes.Point>();
                }
                if (m_DrawingSegmentationAnnotations == null)
                {
                    m_DrawingSegmentationAnnotations = new List<Annotation>();
                }
                if (m_DrawingNuclei == null)
                {
                    m_DrawingNuclei = new List<Circle>();
                }
                if (m_DrawingNucleiAnnotations == null)
                {
                    m_DrawingNucleiAnnotations = new List<Annotation>();
                }
                _LogStringDebug("_InitializeNewImage");
                lbx_Slice.Items.Clear();
                lbx_Segment.Items.Clear();
                lbx_Ann_Stage.Items.Clear();
                lbx_Ann_Fragmentation.Items.Clear();
                _ClearControls();
                _ClearPanel();
                m_Series = new Series();
                 _current_Seg_File_Path = "";
            }
            else
            {
                this.Title = "Embryo Segmenter 2.1";
           
            }
        }

        private void _ClearControls()
        {
            chb_Ann_NewNuclei.IsChecked = false;
            chb_Ann_NewSegment.IsChecked = false;
            txt_Ann_Fragmentation_Begin.Text = "";
            txt_Ann_Fragmentation_End.Text = "";
            txt_Ann_NewNuclei_CenterX.Text = "";
            txt_Ann_NewNuclei_CenterY.Text = "";
            txt_Ann_NewNuclei_Radius.Text = "";
            txt_Ann_NewSegment_StartX.Text = "";
            txt_Ann_NewSegment_StartY.Text = "";
            txt_Ann_Stage_Begin.Text = "";
            txt_Ann_Stage_End.Text = "";
        }


        private void _ClearPanel()
        {
            _LogStringDebug("_ClearPanel");
            foreach (DrawingVisual v in m_DrawingSegmentationPoints)
            {
                pnl_Panel_Seg.DeleteVisual(v);
            }
            foreach (DrawingVisual v in m_DrawingSegmentationAnnotations)
            {
                pnl_Annotation_Seg.DeleteVisual(v);
            }
            foreach (DrawingVisual v in m_DrawingNuclei)
            {
                pnl_Panel_Nuc.DeleteVisual(v);
            }
            foreach (DrawingVisual v in m_DrawingNucleiAnnotations)
            {
                pnl_Annotation_Nuc.DeleteVisual(v);
            }
            if (temp_dragDisplay_Seg != null)
            {
                foreach (DrawingVisual v in temp_dragDisplay_Seg.points)
                {
                    pnl_Panel_Seg.DeleteVisual(v);
                }
            }
           
            m_DrawingSegmentationPoints.Clear();
            m_DrawingSegmentationAnnotations.Clear();
            m_DrawingNuclei.Clear();
            m_DrawingNucleiAnnotations.Clear();
        }

        private void _Populate_lbx_Slice()
        {
            _LogStringDebug("_Populate_lbx_Slice");
            lbx_Slice.Items.Clear();
            Stack currentStack = _Find_stack((int)scl_Time.Value);
            foreach (Slice slice_ in currentStack.slices)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = slice_.sliceNo;
                item.Tag = slice_.sliceNo;
                lbx_Slice.Items.Add(item);
            }
        }

        private void _Select_item(int nbr)
        {
            if (lbx_Slice.Items.Count > 0)
            {
                ListBoxItem selectedItem = lbx_Slice.Items.Cast<ListBoxItem>().First(c => (int)c.Tag == nbr);
                if (selectedItem != null)
                {
                    lbx_Slice.SelectedItem = selectedItem;
                }
            }
        }

        private void _Populate_cbx_Colors()
        {
            _LogStringDebug("_Populate_cbx_Colors");
            cbx_Color_Nuc.Items.Clear();
            ComboBoxItem black_Nuc = new ComboBoxItem(); black_Nuc.Tag = Colors.Black; black_Nuc.Content = "Black"; cbx_Color_Nuc.Items.Add(black_Nuc);
            ComboBoxItem blue_Nuc = new ComboBoxItem(); blue_Nuc.Tag = Colors.Blue; blue_Nuc.Content = "Blue"; cbx_Color_Nuc.Items.Add(blue_Nuc);
            ComboBoxItem green_Nuc = new ComboBoxItem(); green_Nuc.Tag = Colors.Green; green_Nuc.Content = "Green"; cbx_Color_Nuc.Items.Add(green_Nuc);
            ComboBoxItem red_Nuc = new ComboBoxItem(); red_Nuc.Tag = Colors.Red; red_Nuc.Content = "Red"; cbx_Color_Nuc.Items.Add(red_Nuc);
            ComboBoxItem yellow_Nuc = new ComboBoxItem(); yellow_Nuc.Tag = Colors.Yellow; yellow_Nuc.Content = "Yellow"; cbx_Color_Nuc.Items.Add(yellow_Nuc);
            cbx_Color_Nuc.SelectedItem = blue_Nuc;

            cbx_Color_Seg.Items.Clear();
            ComboBoxItem black_Seg = new ComboBoxItem(); black_Seg.Tag = Colors.Black; black_Seg.Content = "Black"; cbx_Color_Seg.Items.Add(black_Seg);
            ComboBoxItem blue_Seg = new ComboBoxItem(); blue_Seg.Tag = Colors.Blue; blue_Seg.Content = "Blue"; cbx_Color_Seg.Items.Add(blue_Seg);
            ComboBoxItem green_Seg = new ComboBoxItem(); green_Seg.Tag = Colors.Green; green_Seg.Content = "Green"; cbx_Color_Seg.Items.Add(green_Seg);
            ComboBoxItem red_Seg = new ComboBoxItem(); red_Seg.Tag = Colors.Red; red_Seg.Content = "Red"; cbx_Color_Seg.Items.Add(red_Seg);
            ComboBoxItem yellow_Seg = new ComboBoxItem(); yellow_Seg.Tag = Colors.Yellow; yellow_Seg.Content = "Yellow"; cbx_Color_Seg.Items.Add(yellow_Seg);
            cbx_Color_Seg.SelectedItem = blue_Seg;
        }

        private void _Populate_cbx_Ann_Stage()
        {
            _LogStringDebug("_Populate_cbx_Ann_Stage");
            cbx_Ann_Stage.Items.Clear();
            ComboBoxItem unknown = new ComboBoxItem(); unknown.Tag = (int)STACK_STAGE.UNKNOWN; unknown.Content = "Unknown"; cbx_Ann_Stage.Items.Add(unknown);
            ComboBoxItem onecell = new ComboBoxItem(); onecell.Tag = (int)STACK_STAGE.ONE_CELL; onecell.Content = "1 cell"; cbx_Ann_Stage.Items.Add(onecell);
            ComboBoxItem twocell = new ComboBoxItem(); twocell.Tag = (int)STACK_STAGE.TWO_CELL; twocell.Content = "2 cells"; cbx_Ann_Stage.Items.Add(twocell);
            ComboBoxItem threecell = new ComboBoxItem(); threecell.Tag = (int)STACK_STAGE.THREE_CELL; threecell.Content = "3 cells"; cbx_Ann_Stage.Items.Add(threecell);
            ComboBoxItem fourcell = new ComboBoxItem(); fourcell.Tag = (int)STACK_STAGE.FOUR_CELL; fourcell.Content = "4 cells"; cbx_Ann_Stage.Items.Add(fourcell);
            ComboBoxItem fivecell = new ComboBoxItem(); fivecell.Tag = (int)STACK_STAGE.FIVE_CELL; fivecell.Content = "5 cells"; cbx_Ann_Stage.Items.Add(fivecell);
            ComboBoxItem sixcell = new ComboBoxItem(); sixcell.Tag = (int)STACK_STAGE.SIX_CELL; sixcell.Content = "6 cells"; cbx_Ann_Stage.Items.Add(sixcell);
            ComboBoxItem sevencell = new ComboBoxItem(); sevencell.Tag = (int)STACK_STAGE.SEVEN_CELL; sevencell.Content = "7 cells"; cbx_Ann_Stage.Items.Add(sevencell);
            ComboBoxItem eightcell = new ComboBoxItem(); eightcell.Tag = (int)STACK_STAGE.EIGHT_CELL; eightcell.Content = "8 cells"; cbx_Ann_Stage.Items.Add(eightcell);
            ComboBoxItem ninecell = new ComboBoxItem(); ninecell.Tag = (int)STACK_STAGE.NINE_CELL; ninecell.Content = "9 cells"; cbx_Ann_Stage.Items.Add(ninecell);
            ComboBoxItem tencell = new ComboBoxItem(); tencell.Tag = (int)STACK_STAGE.TEN_CELL; tencell.Content = "10 cells"; cbx_Ann_Stage.Items.Add(tencell);
            ComboBoxItem division = new ComboBoxItem(); division.Tag = (int)STACK_STAGE.DIVISION; division.Content = "Division"; cbx_Ann_Stage.Items.Add(division);
            ComboBoxItem formcompact = new ComboBoxItem(); formcompact.Tag = (int)STACK_STAGE.FORMING_COMPACTATION; formcompact.Content = "Compaction"; cbx_Ann_Stage.Items.Add(formcompact);
            ComboBoxItem compactation = new ComboBoxItem(); compactation.Tag = (int)STACK_STAGE.COMPACTATION; compactation.Content = "Morula"; cbx_Ann_Stage.Items.Add(compactation);
            ComboBoxItem formblast = new ComboBoxItem(); formblast.Tag = (int)STACK_STAGE.FORMING_BLASTOCOEL; formblast.Content = "Cavitation"; cbx_Ann_Stage.Items.Add(formblast);
            ComboBoxItem blastocoel = new ComboBoxItem(); blastocoel.Tag = (int)STACK_STAGE.BLASTOCOEL; blastocoel.Content = "Blastocyst"; cbx_Ann_Stage.Items.Add(blastocoel); 
            cbx_Ann_Stage.SelectedIndex = -1;
        }

        private void _Populate_cbx_Ann_Fragmentation()
        {
            _LogStringDebug("_Populate_cbx_Ann_Fragmentation");
            cbx_Ann_Fragmentation.Items.Clear();
            ComboBoxItem unknown = new ComboBoxItem(); unknown.Tag = (int)STACK_FRAGMENTATION.UNKNOWN; unknown.Content = "Unknown"; cbx_Ann_Fragmentation.Items.Add(unknown);
            ComboBoxItem group1 = new ComboBoxItem(); group1.Tag = (int)STACK_FRAGMENTATION.GROUP1; group1.Content = "Group 1: 0%"; cbx_Ann_Fragmentation.Items.Add(group1);
            ComboBoxItem group2 = new ComboBoxItem(); group2.Tag = (int)STACK_FRAGMENTATION.GROUP2; group2.Content = "Group 2: 1+/-10%"; cbx_Ann_Fragmentation.Items.Add(group2);
            ComboBoxItem group3 = new ComboBoxItem(); group3.Tag = (int)STACK_FRAGMENTATION.GROUP3; group3.Content = "Group 3: 11+/-20%"; cbx_Ann_Fragmentation.Items.Add(group3);
            ComboBoxItem group4 = new ComboBoxItem(); group4.Tag = (int)STACK_FRAGMENTATION.GROUP4; group4.Content = "Group 4: 21+/-50%"; cbx_Ann_Fragmentation.Items.Add(group4);
            ComboBoxItem group5 = new ComboBoxItem(); group5.Tag = (int)STACK_FRAGMENTATION.GROUP5; group5.Content = "Group 5: >50%"; cbx_Ann_Fragmentation.Items.Add(group5);
            cbx_Ann_Stage.SelectedIndex = -1;
        }


        private void _Populate_cbx_Quality()
        {
            _LogStringDebug("_Populate_cbx_Ann_Stage");
            cbx_Quality.Items.Clear();
            ComboBoxItem unknown = new ComboBoxItem(); unknown.Tag = (int)SERIES_QUALITY.UNKNOWN; unknown.Content = "Unknown"; cbx_Quality.Items.Add(unknown);
            ComboBoxItem low = new ComboBoxItem(); low.Tag = (int)SERIES_QUALITY.LOW; low.Content = "Low"; cbx_Quality.Items.Add(low);
            ComboBoxItem medlow = new ComboBoxItem(); medlow.Tag = (int)SERIES_QUALITY.MEDIUM_LOW; medlow.Content = "Medium-Low"; cbx_Quality.Items.Add(medlow);
            ComboBoxItem med = new ComboBoxItem(); med.Tag = (int)SERIES_QUALITY.MEDIUM; med.Content = "Medium"; cbx_Quality.Items.Add(med);
            ComboBoxItem medhi = new ComboBoxItem(); medhi.Tag = (int)SERIES_QUALITY.MEDIUM_HIGH; medhi.Content = "Medium-High"; cbx_Quality.Items.Add(medhi);
            ComboBoxItem high = new ComboBoxItem(); high.Tag = (int)SERIES_QUALITY.HIGH; high.Content = "High"; cbx_Quality.Items.Add(high);
            cbx_Quality.SelectedIndex = 0;
        }

        private void _Populate_lbx_Ann_Segment(int sliceNo)
        {
            _LogStringDebug("_Populate_lbx_Ann_Segment");
            lbx_Ann_Segment.Items.Clear();
            Stack currentStack = _Find_selected_stack();
            foreach (Slice s in currentStack.slices)
            {
                if (s.sliceNo == sliceNo)
                {
                    if (s.segments == null)
                    {
                        _LogStringDebug("_Populate_lbx_Ann_Segment: No segment");
                        return;
                    }
                    foreach (Segment seg in s.segments)
                    {
                        if (seg.shape == SEGMENT_SHAPE.POINTS)
                        {
                            //Populate listbox for displaying
                            ListBoxItem lbxItem = new ListBoxItem();
                            //Lbx text
                            lbxItem.Content = Construct_listbox_text(seg);
                            lbxItem.Tag = seg.segmentNo;
                            lbx_Ann_Segment.Items.Add(lbxItem);
                        }
                    }
                    break;
                }
            }
        }

        private void _Populate_lbx_Ann_Nuclei(int sliceNo)
        {
            _LogStringDebug("_Populate_lbx_Ann_Nuclei");
            lbx_Ann_Nuclei.Items.Clear();
            Stack currentStack = _Find_selected_stack();
            foreach (Slice s in currentStack.slices)
            {
                if (s.sliceNo == sliceNo)
                {
                    if (s.segments == null)
                    {
                        _LogStringDebug("_Populate_lbx_Ann_Nuclei: No nuclei");
                        return;
                    }
                    foreach (Segment nuc in s.segments)
                    {
                        if (nuc.shape == SEGMENT_SHAPE.CIRCLE)
                        {
                            //Populate listbox for displaying
                            ListBoxItem lbxItem = new ListBoxItem();
                            lbxItem.Content = Construct_listbox_text(nuc);
                            lbxItem.Tag = nuc.segmentNo;
                            lbx_Ann_Nuclei.Items.Add(lbxItem);
                        }
                    }
                    break;
                }
            }
        }

        private void _Populate_lbx_Segment(int sliceNo)
        {
            _LogStringDebug("_Populate_lbx_Segment");
            lbx_Segment.Items.Clear();
            Stack currentStack = _Find_selected_stack();
            foreach (Slice s in currentStack.slices)
            {
                if (s.sliceNo == sliceNo)
                {
                    if (s.segments == null)
                    {
                        _LogStringDebug("_Populate_lbx_Segment: No segments");
                        return;
                    }
                    foreach (Segment ss in s.segments)
                    {
                        //Populate listbox for displaying
                        ListBoxItem lbxItem = new ListBoxItem();
                        lbxItem.Content = ss.segmentNo;
                        lbxItem.Tag = ss.segmentNo;
                        lbx_Segment.Items.Add(lbxItem);
                    }
                    break;
                }
            }
            if (lbx_Segment.Items.Count > 0)
            {
                lbx_Segment.SelectAll();
                lbx_Segment.Focus();
            }
        }

        public void _DrawSegment(Segment seg)
        {
            if (seg == null)
            {
            _LogStringDebug("_DrawSegment: Segment is null");
                return;
            }
            switch (seg.shape)
            {
                case SEGMENT_SHAPE.NONE:
                    foreach (Shapes.Point p in seg.points)
                    {
                        if (!_CheckVisual(p))
                        {
                            p.Draw((Color)((ComboBoxItem)cbx_Color_Seg.SelectedItem).Tag);
                            pnl_Panel_Seg.AddVisual(p);
                            m_DrawingSegmentationPoints.Add(p);
                        }
                    }
                    break;

                case SEGMENT_SHAPE.POINTS:
                    foreach (Shapes.Point p in seg.points)
                    {
                        if (!_CheckVisual(p))
                        {
                            p.Draw((Color)((ComboBoxItem)cbx_Color_Seg.SelectedItem).Tag);
                            pnl_Panel_Seg.AddVisual(p);
                            m_DrawingSegmentationPoints.Add(p);
                        }
                    }
                    break;

                case SEGMENT_SHAPE.CIRCLE:
                    if (!_CheckVisual(seg.circle))
                    {
                        seg.circle.DrawCircleOutline((Color)((ComboBoxItem)cbx_Color_Nuc.SelectedItem).Tag);
                        pnl_Panel_Nuc.AddVisual(seg.circle);
                        m_DrawingNuclei.Add(seg.circle);
                    }
                    break;
            }
        }

        public void _DrawAnnotation(Segment seg_)
        {
            if (seg_ == null)
            {
                _LogStringDebug("_DrawSegmentationAnnotation: Segment is null");
                return;
            }
                 if (!_CheckVisual(seg_.annotation))
                {
                    switch (seg_.shape)
                    {
                        case SEGMENT_SHAPE.NONE:
                            seg_.annotation.DrawTextAtPoint((Color)((ComboBoxItem)cbx_Color_Seg.SelectedItem).Tag, seg_.centerX, seg_.centerY);
                            pnl_Annotation_Seg.AddVisual(seg_.annotation);
                            m_DrawingSegmentationAnnotations.Add(seg_.annotation);
                            break;

                        case SEGMENT_SHAPE.POINTS:
                            seg_.annotation.DrawTextAtPoint((Color)((ComboBoxItem)cbx_Color_Seg.SelectedItem).Tag, seg_.centerX, seg_.centerY);
                            pnl_Annotation_Seg.AddVisual(seg_.annotation);
                            m_DrawingSegmentationAnnotations.Add(seg_.annotation);
                            break;

                        case SEGMENT_SHAPE.CIRCLE:
                            seg_.annotation.DrawTextAtPoint((Color)((ComboBoxItem)cbx_Color_Nuc.SelectedItem).Tag, (int)seg_.circle.centre.X, (int)seg_.circle.centre.Y);
                            pnl_Annotation_Nuc.AddVisual(seg_.annotation);
                            m_DrawingNucleiAnnotations.Add(seg_.annotation);
                            break;
                    }
                }
        }

        private bool _CheckVisual(DrawingVisual visual)
        {
           // _LogStringDebug("_CheckVisual");
            bool visualDrawn = false;
            foreach (DrawingVisual v in m_DrawingSegmentationPoints)
            {
                if (v.Equals(visual))
                { visualDrawn = true; }
            }
            if (!visualDrawn)
            {
                foreach (DrawingVisual v in m_DrawingSegmentationAnnotations)
                {
                    if (v.Equals(visual))
                    { visualDrawn = true; }
                }
            }
            foreach (DrawingVisual v in m_DrawingNuclei)
            {
                if (v.Equals(visual))
                { visualDrawn = true; }
            }
            if (!visualDrawn)
            {
                foreach (DrawingVisual v in m_DrawingNucleiAnnotations)
                {
                    if (v.Equals(visual))
                    { visualDrawn = true; }
                }
            }
            /*if (!visualDrawn)
            {
                if (temp_dragDisplay_Nuc.Equals(visual))
                { visualDrawn = true; }
            }
            if (!visualDrawn)
            {
                foreach (DrawingVisual v in temp_dragDisplay_Seg.points)
                {
                    if (v.Equals(visual))
                    { visualDrawn = true; }
                }
            }*/
            return visualDrawn;
        }

        private System.Drawing.Bitmap _RunPipeline(string fileIn)
        {
            string filename = fileIn;

            Pipeline pl = PipelineManager.InitializePipeline(Pipelines.PipelineType.EMPTY);
            //Pipeline pl = new Pipelines.TestPipeline();
            pl.Filename_set(fileIn);
            pl.Start();
            string filename_test = pl.Filename_get().ToString();
            if (File.Exists(filename_test))
            { filename = filename_test; }
            System.Drawing.Bitmap bm = pl.GetImageOut();
           
            return bm;
        }

        private void _UpdateImage(int sliceNo)
        {
            //_LogStringDebug("_UpdateImage");
            Slice slice = _Find_slice(sliceNo);
            if (slice == null)
            { _LogStringDebug("_UpdateImage: No slices"); 
                return; }
            string fileNameIn;
            if (slice.filepath.Contains("/"))
            {
                fileNameIn = slice.filepath + "/" + slice.fileName;
            }
            else
            {
                fileNameIn = slice.filepath + "\\" + slice.fileName;
            }
if (!File.Exists(fileNameIn))
            {
    //Try picking up ES1.x
                _LogStringDebug("_UpdateImage: No file");
                return;
            }
            System.Drawing.Bitmap newImage = _RunPipeline(fileNameIn);
            BitmapSource bmp;
            if (newImage == null)
            {
                BitmapImage src = new BitmapImage();

                src.BeginInit();
                //TempImage.CacheOption = BitmapCacheOption.OnLoad;
                src.UriSource = new Uri(fileNameIn);
                src.EndInit();

                bmp = new WriteableBitmap(src);
            }
            else
            {
                bmp = CreateBitmapSourceFromBitmap(newImage);
            }

            if (mode == 2)
            {
                if (bmp != null)
                {
                    m_Series.imageWidth = (int)bmp.Width;
                    m_Series.imageHeight = (int)bmp.Height;
                }
                else
                {
                    m_Series.imageHeight = 500;
                    m_Series.imageWidth = 500;
                    bmp = new WriteableBitmap(m_Series.imageWidth, m_Series.imageHeight, 96, 96, new PixelFormat(), BitmapPalettes.Gray256);
                }
            }
            pnl_Image.Source = bmp;
        }

        public BitmapSource CreateBitmapSourceFromBitmap(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");
            BitmapSource bmpSrc;
            try
            {
                bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
                return bmpSrc;
            }
            catch
            {
                return null;
            }

            
        }

        private void _RedrawSegment()
        {
            //_LogStringDebug("_RedrawSegment");
            _ClearPanel();
            if (lbx_Segment.SelectedItem != null)
            {
                foreach (ListBoxItem item in lbx_Segment.SelectedItems)
                {
                    int segmentNo = (int)((ListBoxItem)item).Tag;
                    int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
                    Segment currentSegment = _Find_segment(sliceNo, segmentNo);
                    Slice currentSlice = _Find_slice(sliceNo);
                    _DrawSegment(currentSegment);
                    _DrawAnnotation(currentSegment);
                }
            }
            if (temp_dragDisplay_Seg != null)
            {
                foreach (Shapes.Point p in temp_dragDisplay_Seg.points)
                {
                    if (!_CheckVisual(p))
                    {
                        p.Draw((Color)((ComboBoxItem)cbx_Color_Seg.SelectedItem).Tag);
                        pnl_Panel_Seg.AddVisual(p);
                        m_DrawingSegmentationPoints.Add(p);
                    }
                }
            }
            if (!_CheckVisual(temp_dragDisplay_Nuc) && temp_dragDisplay_Nuc != null)
            {
                temp_dragDisplay_Nuc.DrawCircleOutline((Color)((ComboBoxItem)cbx_Color_Nuc.SelectedItem).Tag);
                pnl_Panel_Nuc.AddVisual(temp_dragDisplay_Nuc);
                m_DrawingNuclei.Add(temp_dragDisplay_Nuc);
            }
        }

        #endregion Window acions 

       
        #region Slice handling

        private Slice _Find_slice(int sliceNumber)
        {
            //_LogStringDebug("_Find_slice");
            Stack currentStack = _Find_selected_stack();
            foreach (Slice s in currentStack.slices)
            {
                if (s.sliceNo == sliceNumber)
                {
                    return s;
                }
            }
            return null;
        }

        private Stack _Find_selected_stack()
        {
            //_LogStringDebug("_Find_stack");
            if ((m_Series == null) || (m_Series.stacks == null) || (m_Series.stacks.Count == 0))
            { return null; }
            int stackNumber = (int)(scl_Time.Value);
            foreach (Stack s in m_Series.stacks)
            {
                if (s.stackNo == stackNumber)
                {
                    return s;
                }
            }
            return null;
        }

        private Stack _Find_stack(int stackNumber)
        {
            //_LogStringDebug("_Find_stack");
            foreach (Stack s in m_Series.stacks)
            {
                if (s.stackNo == stackNumber)
                {
                    return s;
                }
            }
            return null;
        }

        private Segment _Find_segment(int sliceNumber, int segmentNumber)
        {
           // _LogStringDebug("_Find_segment");
            Stack currentStack = _Find_selected_stack();

            foreach (Slice s in currentStack.slices)
            {
                if (s.sliceNo == sliceNumber)
                {
                    foreach (Segment ss in s.segments)
                    {
                        if (ss.segmentNo == segmentNumber)
                        {
                            return ss;
                        }
                    }
                }
            }
            return null;
        }

        private Segment _Find_nuclei(int nucleiNumber)
        {
            // _LogStringDebug("_Find_segment");
            Slice currentSlice = _Find_slice((int)((ListBoxItem)lbx_Slice.SelectedItem).Tag);
            if (currentSlice == null)
            { return null; }

                    foreach (Segment seg in currentSlice.segments)
                    {
                        if (seg.segmentNo == nucleiNumber && seg.shape == SEGMENT_SHAPE.CIRCLE)
                        {
                            return seg;
                        }
            }
            return null;
        }

        private int[] _FillSegments(int[] matrix, int width)
        {
            _LogStringDebug("_FillSegments");
            bool foundEdge = false;
            for (int i = 0; i < matrix.Length; i++)
            {
                int y = i / width;
                if (y == 0) { foundEdge = false; }

                int x = i - y * width;
                if (matrix[i] > 0)
                {
                    if (foundEdge)
                    {
                        foundEdge = false;
                    }
                    else
                    {
                        foundEdge = true;
                    }
                }
                else
                {
                    if (foundEdge)
                    {
                        matrix[i] = 255;
                    }
                }
            }
            return matrix;
        }

        private bool _IsInList(int number, List<int> list)
        {
            _LogStringDebug("_IsInList");
            bool returnValue = false;

            foreach (int i in list)
            {
                if (i == number)
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }

        private bool _IsInPointList(double X, double Y, List<Shapes.Point> pointList)
        {
            _LogStringDebug("_IsInPointList");
            bool returnValue = false;

            foreach (Shapes.Point p in pointList)
            {
                if (p.X == X && p.Y == Y)
                {
                    returnValue = true;
                }
            }
            return returnValue;
        }

        #endregion Slice handling

        #region FileIO

         private bool _Export_To_Excel(string filePath)
        {
             //Save separator in config file
            string separator_ = Properties.Settings.Default.Separator;

             //Make sure we have proper scaling before computing
          
            if (!(double.TryParse(txt_XY.Text, out xy_scale)))
            {
                MessageBox.Show("No xy scale filled in!");
                return false;
            }
            if (!(double.TryParse(txt_Z.Text, out z_scale)))
            {
                MessageBox.Show("No z scale filled in!");
                return false;
            }
            

            		// We wish to export our project to a
		// csv file. We will prep a string with 
		// the required information and then
		// output that single element in one go
		string fileInfo = "";
		// Loop through all the slices and look for the
		// Highest segment ID. This will denote
		int maxID = 0;
        Stack currentStack = _Find_selected_stack();
        StreamWriter sr = new StreamWriter(filePath, false);
       
        foreach (Slice slice_ in currentStack.slices)
		{
			int currentSliceMaxID = slice_.GetHighestSegmentID();
			if( currentSliceMaxID > maxID)
			{
				maxID = currentSliceMaxID;
			}
		}
		// By the end of the loop we will have the
		// number of columns
		// First column will be slice numbers
		// So first field in the first row is blank
		fileInfo = separator_;
        sr.Write(fileInfo);
        // Now add each of the Segment titles
		for(int i = 1; i <= maxID; i++)
		{
			fileInfo = "Segment " + i.ToString() + separator_;
            sr.Write(fileInfo);
        }
		fileInfo="\n";
        sr.Write(fileInfo);
        // Each subsequent row is each of the slices
        
        if (currentStack.slices != null && currentStack != null)
        {
            {
                foreach (Slice slice_ in currentStack.slices)
                {
                    // First column is the name of the slice
                    //Regardless of wether or not it has segments
                    fileInfo = "Slice: " + slice_.sliceNo.ToString();
                    sr.Write(fileInfo);
                    fileInfo = separator_;
                    sr.Write(fileInfo);
                    // Then each of the segments volumes.
                    // We need to make sure we are putting
                    // them in the right columns
                    int column = 1;
                    if (slice_.segments != null)
                    {
                        foreach (Segment seg_ in slice_.segments)
                        {
                            // Get the segment id
                            int currentId = seg_.segmentNo;
                            
                            while (currentId != column)
                            {
                                // put in a blank
                                fileInfo = separator_;
                                sr.Write(fileInfo);
                                column++;
                            }
                            // Now that we've got to the right place
                            // Insert the segments area

                            float segmentVolume = seg_.segmentArea * (float)xy_scale * (float)xy_scale * (float)z_scale;
                            //string test = segmentVolume.ToString("00.00");
                            fileInfo = segmentVolume.ToString("00.000");
                            sr.Write(fileInfo);
                        }
                    }
                    // We've finished looping through the segments
                    // add a new line
                    fileInfo = "\n";
                    sr.Write(fileInfo);
                }
            }
        }
		// We've finished looping through the slices
		// Output the file
		     
             sr.Dispose();
		return true;
        }


        private void _WriteAllToOBJ()
        {
            double z_scale;
            double xy_scale;
            string path = "";
            int segmentNo;
            if (!(double.TryParse(txt_XY.Text, out xy_scale)))
            {
                MessageBox.Show("No xy scale filled in!");
                return;
            }
            if (!(double.TryParse(txt_Z.Text, out z_scale)))
            {
                MessageBox.Show("No z scale filled in!");
                return;
            }
            Stack currentStack = _Find_selected_stack();
            if (m_Series.seg_fileName == null)
            {
                MessageBox.Show("Stack filename does not exist!");
                return;
            }

            string folderPath = Directory.GetParent(_current_Seg_File_Path).FullName + "//OBJ";

            

            List<int> savedSegments = new List<int>();
            foreach (Stack stack in m_Series.stacks)
            {
                string stackFolderPath = folderPath + stack.stackNo + "//";
                //Create new OBJ-folder or empty the old one - only one folder/project!
                if (Directory.Exists(stackFolderPath))
                {
                    foreach (string f in Directory.GetFiles(stackFolderPath))
                    {
                        File.Delete(f);
                    }
                }
                savedSegments.Clear();
                foreach (Slice slice in stack.slices)
                {
                    foreach (Segment seg in slice.segments)
                    {
                        if (!_IsInList(seg.segmentNo, savedSegments) && (seg.shape == SEGMENT_SHAPE.POINTS))
                        {
                            //Create new OBJ-folder or empty the old one - only one folder/project!
                            if (!Directory.Exists(stackFolderPath))
                            { Directory.CreateDirectory(stackFolderPath); }

                            string fileName = stackFolderPath + "//" + seg.segmentNo + ".obj";
                            _Write_obj(stack, fileName, z_scale, xy_scale, seg.segmentNo);
                            savedSegments.Add(seg.segmentNo);
                        }
                        else
                        {

                        }
                    }
                }
            }

            
           
        }



        private void _Write_obj(Stack stack, string filename, double zScale, double xyScale, int segmentNumber)
        {
            double dbl_z_scale = zScale / xyScale;
            double dbl_xy_scale = xyScale;
            int int_z_scale = (int)(Math.Round(dbl_z_scale));
            int int_xy_scale = (int)(Math.Round(dbl_xy_scale));
            StreamWriter writer = new StreamWriter(filename, false);
            writer.WriteLine("# Segmentation file: " + m_Series.seg_fileName);
            writer.WriteLine("# Segmentation number: " + segmentNumber);
            writer.WriteLine("# Scale: " + dbl_xy_scale);
            writer.WriteLine("");
            int sliceIndex = 0; //Should be able to handle slices not marked in order
            foreach (Slice slice in stack.slices)
            {
                foreach (Segment seg in slice.segments)
                {
                    if ((seg.segmentNo == segmentNumber) )
                    {
                        if (seg.points != null)
                        {
                            foreach (Shapes.Point point in seg.points)
                            {
                                //This needs to be rewritten for the general case!!!
                                writer.WriteLine("v " + (int)(Math.Round(point.X)) + " " + (int)(Math.Round(point.Y)) + " " + int_z_scale * sliceIndex);
                            }
                        }
                    }
                }
                sliceIndex++;
            }
            writer.Close();
            writer.Dispose();
           
        }

        private void _Save_New_Seg_File_2()
        {
            _LogStringDebug("_Save_New_Seg_File");
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "seg";
            sfd.Filter = "SEG-files|*.seg";
            sfd.ShowDialog();
            if (!sfd.FileName.Equals(""))
            {
                _current_Seg_File_Path = sfd.FileName;
                _SaveSegFile_2(_current_Seg_File_Path);
            }
        }

        private void _Save_New_Seg_File_1()
        {
            _LogStringDebug("_Save_New_Seg_File");
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "seg";
            sfd.Filter = "SEG-files|*.seg";
            sfd.ShowDialog();
            if (!sfd.FileName.Equals(""))
            {
                _current_Seg_File_Path = sfd.FileName;
                _SaveSegFile_1(_current_Seg_File_Path);
            }
        }


        private void _SaveBitmap(BitmapSource bitmap, string ImagePath)
        {

            _LogStringDebug("_SaveBitmap");
            BmpBitmapEncoder BMPEnc = new BmpBitmapEncoder();
            BMPEnc.Frames.Add(BitmapFrame.Create(bitmap));
            using (Stream OutStream = File.OpenWrite(ImagePath))
            {
                BMPEnc.Save(OutStream);
                OutStream.Close();
            }
        }

        //Saves all in listbox selected segments to a single bitmap as outline (fill = false) or segments.
        private void _CreateBitmap(bool fill)
        {

            _LogStringDebug("_CreateBitmap");
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "bmp";
            sfd.Filter = "BMP-files|*.bmp";
            sfd.ShowDialog();
            if (!sfd.FileName.Equals(""))
            {
                List<Segment> selectedSegments = new List<Segment>();
                foreach (ListBoxItem item in lbx_Segment.SelectedItems)
                {
                    int segmentNo = (int)((ListBoxItem)item).Tag;
                    int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
                    Segment currentSegment = _Find_segment(sliceNo, segmentNo);
                    selectedSegments.Add(currentSegment);
                }
                int width = 512;
                int height = 512;
                WriteableBitmap FrameImageSource = new WriteableBitmap(width, height, 96.0, 96.0, System.Windows.Media.PixelFormats.Indexed8, BitmapPalettes.Gray256);
                FrameImageSource.Lock();
                int[] segmentArray = new int[width * height];
                foreach (Segment segment in selectedSegments)
                {
                    foreach (Shapes.Point point in segment.points)
                    {
                        segmentArray[(int)(point.Y * height + point.X)] = 255;
                    }
                }

                if (fill) { segmentArray = _FillSegments(segmentArray, width); }
                unsafe
                {
                    byte* p = (byte*)FrameImageSource.BackBuffer.ToPointer();

                    for (int i = 0; i < width * height; i++)
                    {
                        *p = (byte)segmentArray[i];
                        p++;
                    }
                    FrameImageSource.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    FrameImageSource.Unlock();
                }
                _SaveBitmap(FrameImageSource, sfd.FileName);
            }

        }

        private bool _OpenSegFile(int mode)
        {
            _LogStringDebug("_OpenSegFile");
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckPathExists = true;
            ofd.Filter += "SEG files (*.seg)|*.seg";
            ofd.FilterIndex = 1;
            ofd.RestoreDirectory = true;
            ofd.Title = "Segmentation file";
            ofd.ShowDialog();
            txt_File.Text = ofd.FileName;

            if (txt_File.Text.Equals(""))
            {
                return false;
            }
            //_InitializeNewImage();
            //_Initiale_m_series();
            _current_Seg_File_Path = txt_File.Text;

            switch (mode)
            {
                case 1: //Open ES1.x
                    mode = 1;
                    _ReadSegFile1(_current_Seg_File_Path);
                    break;

                case 2: //Open ES2.x
                    mode = 2;
                    _ReadSegFile2(_current_Seg_File_Path);
                     break;
        }
            return true;
        }

        private bool _SetImagePath()
        {
            _LogStringDebug("_SetImagePath");
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckPathExists = true;
            ofd.Filter += "Image files (*.tif, *bmp, *.jpg, *png)|*.tif;*.bmp;*.jpg;*.png";
            ofd.FilterIndex = 1;
            ofd.RestoreDirectory = true;
            ofd.Title = "Select one image file";
            ofd.ShowDialog();
            if (File.Exists(ofd.FileName))
            {
            txt_Path.Text = Directory.GetParent(ofd.FileName).FullName;
            this.Title = "Embryo Segmenter 2.1: " + txt_Path.Text;
            }
            return true;
}
        //ES1.x
        private void _SaveSegFile_1(string seg_FilePath)
        {

            _LogStringDebug("_SaveSegFile");
            //ALM save only one stack for now
            Stack currentStack = _Find_selected_stack();
            //currentStack.filename = seg_FilePath;
            FileStream sr = new FileStream(seg_FilePath, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(sr);
            bw.Write(Convert.ToInt32(currentStack.nbrOfSlices));
            foreach (Slice slice_ in currentStack.slices)
            {
                Slice newSlice = new Slice();
                //int index = currentStack.filename.LastIndexOf("\\");
                //string filePath = currentStack.filename.Substring(0, index + 1);
                string filePath = txt_Path.Text; 
                 string fullFileName;
                if (filePath.Contains("/"))
                {
                    fullFileName = filePath + "/" + slice_.fileName;
                }
                else
                {
                    fullFileName = filePath + "\\" + slice_.fileName;
                }
                
                bw.Write(Convert.ToInt32(fullFileName.Length));
                char[] c_array = fullFileName.ToCharArray();
                for (int i = 0; i < fullFileName.Length; i++)
                {
                    char c = c_array[i];
                    bw.Write(c);
                }
                bw.Write(Convert.ToInt32(slice_.sliceNo));
                int segmentCount = 0;
                if (slice_.segments != null) { segmentCount = slice_.segments.Count; }
                bw.Write(Convert.ToInt32(segmentCount));

                if (slice_.segments != null)
                {
                    foreach (Segment segment_ in slice_.segments)
                    {
                        bw.Write(Convert.ToInt32(segment_.segmentNo));
                        int pointCount = 0;
                        if (segment_.points != null) { pointCount = segment_.points.Count; }
                        bw.Write(Convert.ToInt32(pointCount));

                        foreach (Shapes.Point point_ in segment_.points)
                        {
                            bw.Write(Convert.ToDouble(point_.X));
                            bw.Write(Convert.ToDouble(point_.Y));
                            bw.Write(Convert.ToDouble(point_.Z));
                        }
                        bw.Write(Convert.ToInt32(segment_.centerX));
                        bw.Write(Convert.ToInt32(segment_.centerY));
                        bw.Write(Convert.ToInt32(segment_.segmentArea));
                        bw.Write(Convert.ToBoolean(segment_.inEnclosed));
                    }
                }
            }
            bw.Dispose();
            sr.Dispose();
        }

        //For ES2.x
        private void _SaveSegFile_2(string seg_FilePath)
        {

            _LogStringDebug("_SaveSegFile");
            
                 FileStream sr = new FileStream(seg_FilePath, FileMode.Create, FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(sr);

                bw.Write(Convert.ToInt32(m_Series.seriesName.Length));
                char[] c_array_name = m_Series.seriesName.ToCharArray();
                for (int i = 0; i < m_Series.seriesName.Length; i++)
                {
                    char c_name = c_array_name[i];
                    bw.Write(c_name);
                }

                bw.Write(Convert.ToInt32(seg_FilePath.Length));
                char[] c_array_path = seg_FilePath.ToCharArray();
                for (int i = 0; i < seg_FilePath.Length; i++)
                {
                    char c_path = c_array_path[i];
                    bw.Write(c_path);
                }

                bw.Write(Convert.ToInt32(m_Series.imageWidth));
                bw.Write(Convert.ToInt32(m_Series.imageHeight));
                bw.Write(Convert.ToInt32(m_Series.quality));
            bw.Write(Convert.ToInt32(m_Series.nbrOfStacks));
            foreach (Stack currentStack in m_Series.stacks)
            {
                bw.Write(Convert.ToInt32(currentStack.stackNo));
                 bw.Write(Convert.ToInt32(currentStack.stage));
                 bw.Write(Convert.ToInt32(currentStack.fragmentation));
                bw.Write(Convert.ToInt32(currentStack.nbrOfSlices));
                foreach (Slice slice_ in currentStack.slices)
                {
                    Slice newSlice = new Slice();
                    string fullFileName = slice_.filepath + "\\" + slice_.fileName;
                    bw.Write(Convert.ToInt32(fullFileName.Length));
                    char[] c_array = fullFileName.ToCharArray();
                    for (int i = 0; i < fullFileName.Length; i++)
                    {
                        char c = c_array[i];
                        bw.Write(c);
                    }
                    bw.Write(Convert.ToInt32(slice_.sliceNo));
                    int segmentCount = 0;
                    if (slice_.segments != null) { segmentCount = slice_.segments.Count; }
                    bw.Write(Convert.ToInt32(segmentCount));

                    if (slice_.segments != null)
                    {
                        foreach (Segment segment_ in slice_.segments)
                        {
                            bw.Write(Convert.ToInt32(segment_.segmentNo));
                            bw.Write(Convert.ToInt32(segment_.shape));

                            switch (segment_.shape)
                            {
                                case SEGMENT_SHAPE.POINTS:
                                    int pointCount = 0;
                                    if (segment_.points != null) { pointCount = segment_.points.Count; }
                                    bw.Write(Convert.ToInt32(pointCount));
                                    foreach (Shapes.Point point_ in segment_.points)
                                    {
                                        bw.Write(Convert.ToDouble(point_.X));
                                        bw.Write(Convert.ToDouble(point_.Y));
                                        bw.Write(Convert.ToDouble(point_.Z));
                                    }
                                    bw.Write(Convert.ToInt32(segment_.centerX));
                                    bw.Write(Convert.ToInt32(segment_.centerY));
                                    bw.Write(Convert.ToInt32(segment_.segmentArea));
                                    bw.Write(Convert.ToBoolean(segment_.inEnclosed));
                                    break;

                                case SEGMENT_SHAPE.CIRCLE:
                                    bw.Write(Convert.ToInt32(segment_.circle.centre.X));
                                    bw.Write(Convert.ToInt32(segment_.circle.centre.Y));
                                    bw.Write(Convert.ToDouble(segment_.circle.radius));
                                    break;
                            }
                        }
                    }
                }
            }
                bw.Dispose();
                sr.Dispose();
        }

        private bool _FindSegmentCenterAndArea(ref Segment _seg)
        {
            //XXX check area calculation
            if (_seg == null || _seg.points == null || _seg.points.Count == 0)
            {
                return false;
            }
            double[] allX = new double[_seg.points.Count];
            double[] allY = new double[_seg.points.Count];
            int n = 0;
            foreach (Shapes.Point p in _seg.points)
            {
                allX[n] = p.X;
                allY[n] = p.Y;
                n++;
            }
            double maxX = allX.Max(); double minX = allX.Min();
            double maxY = allY.Max(); double minY = allY.Min();
            _seg.centerX = (int)((maxX - minX) / 2 + minX);
            _seg.centerY = (int)((maxY-minY)/2 + minY);

            bool inside = false;
            bool hitEdge = false;
            int segmentArea = 0;
            for (int y = 0; y <= m_Series.imageHeight; y++)
            {
                for (int x = 0; x <= m_Series.imageWidth; x++)
                {
                    if (_IsInPointList(x, y, _seg.points))
                    //We are on segment outline
                    {
                        hitEdge = true;
                        segmentArea++;
                    }
                    else
                        //Outside or inside cell
                    {
                        if (inside) //Inside cell
                        {
                            segmentArea++;
                            if (hitEdge == true)
                            {
                                inside = false; //We have moved outside
                                hitEdge = false;
                            }
                        }
                        else //Outside cell
                        {
                            if (hitEdge == true) //We have moved inside
                            {
                                inside = true;
                                hitEdge = false;
                            }
                        }
                    }
                }
            }
            _seg.segmentArea = segmentArea;
            return true;
        }

        private void _ReadSegFile1(string seg_FilePath)
        {
            _LogStringDebug("_ReadSegFile");
            if (!File.Exists(seg_FilePath))
            {
                MessageBox.Show("File does not exist!");
                return;
            }

            if (File.Exists(seg_FilePath))
            {
                FileStream sr = new FileStream(seg_FilePath, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(sr);
                int nbrOfSlices = br.ReadInt32();
                Stack currentStack = new Stack();
                currentStack.slices = new List<Slice>();
                currentStack.nbrOfSlices = nbrOfSlices;
                currentStack.stackNo = 1;
                //currentStack.stackName = "Stack 1";
                //m_Stack.stackName = lblStackName.Content.ToString();
                for (int slice_ = 1; slice_ <= nbrOfSlices; slice_++)
                {
                    Slice newSlice = new Slice();
                    int fileNameLength = br.ReadInt32();
                    char[] chars = br.ReadChars(fileNameLength);

                    string fullFileName = new String(chars);
                    int index = fullFileName.LastIndexOf("/");
                    if (index > 0) // Old type image with "/"
                    {
                        newSlice.fileName = fullFileName.Substring(index + 1, fullFileName.Length - index - 1);
                        newSlice.filepath = fullFileName.Substring(0, index);
                    }
                    else // Try new type image with "\"
                    {
                        index = fullFileName.LastIndexOf("\\");
                        if (index > 0)
                        {
                            newSlice.fileName = fullFileName.Substring(index + 1, fullFileName.Length - index - 1);
                            newSlice.filepath = fullFileName.Substring(0, index);
                        }
                    }

                    newSlice.sliceNo = br.ReadInt32();
                    int nbrOfSegments = br.ReadInt32();
                    newSlice.segments = new List<Segment>();

                    for (int segment_ = 1; segment_ <= nbrOfSegments; segment_++)
                    {
                        Segment newSegment = new Segment();
                        newSegment.segmentNo = br.ReadInt32();
                        newSegment.nbrOfPoints = br.ReadInt32();
                        newSegment.points = new List<Shapes.Point>();
                        newSegment.annotation = new Annotation(newSegment.segmentNo.ToString());
                        for (int point_ = 1; point_ <= newSegment.nbrOfPoints; point_++)
                        {
                            Shapes.Point newPoint = new Shapes.Point();
                            newPoint.X = br.ReadDouble();
                            newPoint.Y = br.ReadDouble();
                            newPoint.Z = br.ReadDouble();
                            newSegment.points.Add(newPoint);
                        }
                        newSegment.centerX = br.ReadInt32();
                        newSegment.centerY = br.ReadInt32();
                        newSegment.segmentArea = br.ReadInt32();
                        newSegment.inEnclosed = br.ReadBoolean();
                        newSlice.segments.Add(newSegment);
                    }
                    currentStack.slices.Add(newSlice);
                    //stack.segmentCount = br.ReadInt32();
                }
                br.Dispose();
                sr.Dispose();
                m_Series.stacks.Add(currentStack);
            }
        }

         private void _ReadSegFile2(string seg_FilePath)
        {

            _LogStringDebug("_ReadSegFile");
            if (!File.Exists(seg_FilePath))
            {
                MessageBox.Show("File does not exist!");
                return;
            }
           
            if (File.Exists(seg_FilePath))
            {
                FileStream sr = new FileStream(seg_FilePath, FileMode.Open, FileAccess.Read);
                BinaryReader br = new BinaryReader(sr);
                int seriesNameLenght = br.ReadInt32();
                char[] char_name = br.ReadChars(seriesNameLenght);
                m_Series.seriesName = new String(char_name);
                int segPathLength = br.ReadInt32();
                char[] char_path = br.ReadChars(segPathLength);
                m_Series.seg_fileName = new String(char_path);
                m_Series.imageWidth = br.ReadInt32();
                m_Series.imageHeight = br.ReadInt32();
                m_Series.quality = (SERIES_QUALITY)br.ReadInt32();
                m_Series.nbrOfStacks = br.ReadInt32();
                for (int stack_ = 1; stack_ <= m_Series.nbrOfStacks; stack_++)
                {
                Stack currentStack = new Stack();
                int stackIndex = br.ReadInt32();
                int stackStage = br.ReadInt32();
                    int stackFragmentation = br.ReadInt32();
                int nbrOfSlices = br.ReadInt32();
                int sliceIndex = _Find_max_slice_index() + 1;
                
                m_Series.seg_fileName = seg_FilePath;
                
                    currentStack.segmentCount = 0;
                    currentStack.stage = (STACK_STAGE)stackStage;
                    currentStack.fragmentation = (STACK_FRAGMENTATION)stackFragmentation;
                    currentStack.nbrOfSlices = nbrOfSlices;
                    currentStack.slices = new List<Slice>();
               
                for (int slice_ = 1; slice_ <= nbrOfSlices; slice_++)
                {
                    Slice newSlice = new Slice();
                    int fileNameLength = br.ReadInt32();
                    char[] chars = br.ReadChars(fileNameLength);
                    string fullFileName = new String(chars);
                    int index = fullFileName.LastIndexOf("/");

                    if (index > 0) // Old type image with "/"
                    {
                        newSlice.fileName = fullFileName.Substring(index + 1, fullFileName.Length - index - 1);
                        newSlice.filepath = fullFileName.Substring(0, index);
                    }
                    else // Try new type image with "\"
                    {
                        index = fullFileName.LastIndexOf("\\");
                        if (index > 0)
                        {
                            newSlice.fileName = fullFileName.Substring(index + 1, fullFileName.Length - index - 1);
                            newSlice.filepath = fullFileName.Substring(0, index);
                        }
                    }
                    newSlice.sliceNo = br.ReadInt32();
                    int nbrOfSegments = br.ReadInt32();
                    newSlice.segments = new List<Segment>();
                    for (int segment_ = 1; segment_ <= nbrOfSegments; segment_++)
                    {
                        Segment newSegment = new Segment();
                        newSegment.segmentNo = br.ReadInt32();
                        newSegment.shape = (SEGMENT_SHAPE)br.ReadInt32();
                        newSegment.annotation = new Annotation(newSegment.segmentNo.ToString());
                        switch (newSegment.shape)
                        {
                            case SEGMENT_SHAPE.POINTS:
                                newSegment.nbrOfPoints = br.ReadInt32();
                                newSegment.points = new List<Shapes.Point>();
                                for (int point_ = 1; point_ <= newSegment.nbrOfPoints; point_++)
                                {
                                    Shapes.Point newPoint = new Shapes.Point();
                                    newPoint.X = br.ReadDouble();
                                    newPoint.Y = br.ReadDouble();
                                    newPoint.Z = br.ReadDouble();
                                    newSegment.points.Add(newPoint);
                                }
                                newSegment.centerX = br.ReadInt32();
                                newSegment.centerY = br.ReadInt32();
                                newSegment.segmentArea = br.ReadInt32();
                                newSegment.inEnclosed = br.ReadBoolean();
                                break;

                            case SEGMENT_SHAPE.CIRCLE:
                                newSegment.circle = new Circle();
                                newSegment.circle.centre.X = br.ReadInt32();
                                newSegment.circle.centre.Y = br.ReadInt32();
                                newSegment.circle.radius = br.ReadDouble();
                                break;
                        }

                        newSlice.segments.Add(newSegment);
                    }
                    currentStack.slices.Add(newSlice);
                    currentStack.nbrOfSlices = currentStack.slices.Count;
                    currentStack.stackNo = stackIndex;
                    //stack.segmentCount = br.ReadInt32();
                } 
                    m_Series.stacks.Add(currentStack);  
            }
                br.Dispose();
                sr.Dispose();
                m_Series.nbrOfStacks = m_Series.stacks.Count;
            }            
        }


        #endregion FileIO
        #region Logging
        void _LogStringDebug(string logString)
        {
            if ((bool)chb_Logging.IsChecked)
            {
                _txt_Log.Text = _txt_Log.Text + logString + Environment.NewLine;
                log.Debug(logString);
            }
        }
        #endregion Logging

        #region Testing

        private void _Test()
        {
            Rendering.Sphere sphere = new Rendering.Sphere();
            sphere.Create();
        }

       
#endregion Testing

        #region Window events

        private void mnu_Save_OBJ_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_OBJ_Click");
            _WriteAllToOBJ();
        }

        private void mnu_Export_Excel_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Export_Excel_Click");
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "csv";
            sfd.Filter = "CSV-files|*.csv";
            sfd.ShowDialog();
            if (!sfd.FileName.Equals(""))
            {
                _Export_To_Excel(sfd.FileName);
            }

           
        }

        private void mnu_Save_As_Series_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_As_Project_Click");
            _Save_New_Seg_File_2();
        }

        private void mnu_Save_As_Stack_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_As_Project_Click");
            _Save_New_Seg_File_1();
        }

        private void mnu_Save_Stack_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_Project_Click");
            if (File.Exists(_current_Seg_File_Path))
            {
                _SaveSegFile_1(_current_Seg_File_Path);
            }
            else
            {
                _Save_New_Seg_File_1();
            }
        }

        private void mnu_Save_Series_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_Project_Click");
            if (File.Exists(_current_Seg_File_Path))
            {
                _SaveSegFile_2(_current_Seg_File_Path);
            }
            else
            {
                _Save_New_Seg_File_2();
            }
        }

        private void chb_Logging_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chb_Logging.IsChecked)
            {
                chb_Logging.Content = "Logging: ON";
            }
            else
            {
                chb_Logging.Content = "Logging: OFF";
            }
        }


        private void mnu_Clear_Log_Click(object sender, RoutedEventArgs e)
        {
            _txt_Log.Text = "";
        }


        private void mnu_New_Project_Click(object sender, RoutedEventArgs e)
        {
             _LogStringDebug("mnu_New_Project_Click");

             _InitializeNewImage();
             _Initiale_m_series();

            if (_Read_new_stack())
            {
                initialized = false;
                _Populate_scl_Time();
                _Populate_lbx_Slice();
                initialized = true;
                _Select_item(1);
               // _UpdateImage(1);
            }
        }

        private int _Find_max_slice_index()
        {
            if (m_Series.stacks == null || m_Series.stacks.Count == 0)
            {
                return 0;
            }
            else
            {
                Stack stack = m_Series.stacks.First();
                return stack.nbrOfSlices;
            }
        }

        private bool _Read_new_stack()
        {
            MessageBoxResult res = MessageBoxResult.OK; //Bypass this message
            //MessageBoxResult res = MessageBox.Show("Select a folder containing images.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
            if (res != MessageBoxResult.OK)
            { return false; }

            _SetImagePath();

            int stackIndex = 1;
            int sliceIndex = _Find_max_slice_index() + 1;
            if (!Directory.Exists(txt_Path.Text)) { return false; }
            foreach (string file in Directory.GetFiles(txt_Path.Text))
            {
                Stack stack;
                if (m_Series.nbrOfStacks == 0)
                {
                    stack = new Stack();
                    stack.segmentCount = 0;
                    stack.slices = new List<Slice>();
                }
                else
                {
                    stack = _Find_stack(stackIndex);
                    if (stack == null)
                    {
                        continue;
                    }

                }

                Slice s = new Slice();
                int index = file.LastIndexOf("\\");
                string fileName = file.Substring(index + 1, file.Length - index - 1);
                s.filepath = txt_Path.Text;
                s.fileName = fileName;
                s.sliceNo = sliceIndex;
                s.segments = new List<Segment>();
                stack.slices.Add(s);
                stack.nbrOfSlices = stack.slices.Count;
                stack.stackNo = stackIndex;
                if (m_Series.nbrOfStacks == 0)
                {
                    m_Series.stacks.Add(stack);
                }
                stackIndex++;
            }
            m_Series.nbrOfStacks = m_Series.stacks.Count;
            return true;
        }

        private void _Populate_scl_Time()
        {
            scl_Time.Minimum = 1;
            scl_Time.Value = scl_Time.Minimum;
            scl_Time.Maximum = m_Series.nbrOfStacks;
        }

        private void _Initiale_m_series()
        {
            m_Series = new Series();
            m_Series.seriesName = "";
            m_Series.calibration = new Calibration();
            m_Series.nbrOfStacks = 0;
            m_Series.stacks = new List<Stack>();
            m_Series.quality = SERIES_QUALITY.UNKNOWN;
            m_Series.imageHeight = 0;
            m_Series.imageWidth = 0;
            m_Series.seg_fileName = "";
        }


        private void cbx_Color_Seg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("cbx_Color_Seg_SelectionChanged");
            if (initialized)
            {
                if (!killFocus)
                {
                    _RedrawSegment();
                    previusColor = (Color)(((ComboBoxItem)cbx_Color_Nuc.SelectedItem).Tag);
                }
                else
                {
                    //Undo selection changed
                    cbx_Color_Nuc.SelectedItem = _FindColor(cbx_Color_Nuc, previusColor);
                }
            }
        }
        
        private void cbx_Color_Nuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("cbx_Color_Nuc_SelectionChanged");
            if (initialized)  
            {
                if (!killFocus)
                {
                    _RedrawSegment();
                    previusColor = (Color)(((ComboBoxItem)cbx_Color_Nuc.SelectedItem).Tag);
                }
                else
                {
                    //Undo selection changed
                    cbx_Color_Nuc.SelectedItem = _FindColor(cbx_Color_Nuc, previusColor);
                }
                }
        }

        private ComboBoxItem _FindColor(ComboBox comboBox, Color color)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if ((Color)(item.Tag) == color)
                {
                    return item;
                }
            }
            return null;
        }

        private void mnu_Bmp_Save_Segments_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Bmp_Save_Segments_Click");
            _CreateBitmap(true);
        }

        private void mnu_Bmp_Save_Outlines_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Bmp_Save_Outlines_Click");
            _CreateBitmap(false);
        }


        private void lbx_Segment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("lbx_Segment_SelectionChanged");
            _RecomputeScale();
            _RedrawSegment();
        }
        private void lbx_Slice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("lbx_Slice_SelectionChanged");
            lbx_Segment.Items.Clear();
            lbx_Ann_Segment.Items.Clear();
            lbx_Ann_Nuclei.Items.Clear();
            _ClearPanel();
            temp_dragDisplay_Nuc = null;
            temp_dragDisplay_Seg = null;
            if (lbx_Slice.SelectedItem == null)
            {
                return;
            }
            int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
            Slice currentSlice = _Find_slice(sliceNo);
            _Populate_lbx_Segment(sliceNo);
            _Populate_lbx_Ann_Nuclei(sliceNo);
            _Populate_lbx_Ann_Segment(sliceNo);
            _RedrawSegment();
            _UpdateImage(currentSlice.sliceNo);
        }

        private void SetImageSize(int mode)
        {
            switch (mode)
            {
                case 1:
                    pnl_Image.Width = 512; pnl_Image.Height = 512;
                    pnl_User.Width = 512; pnl_User.Height = 512;
                    pnl_Annotation_Nuc.Width = 512; pnl_Annotation_Nuc.Height = 512;
                    pnl_Annotation_Seg.Width = 512; pnl_Annotation_Seg.Height = 512;
                    pnl_Panel_Seg.Width = 512; pnl_Panel_Seg.Height = 512;
                    pnl_Panel_Nuc.Width = 512; pnl_Panel_Nuc.Height = 512;
                    grd_SharpGLControl.Width = 512; grd_SharpGLControl.Height = 512;
                    break;

                case 2:
                    pnl_Image.Width = 500; pnl_Image.Height = 500;
                    pnl_User.Width = 500; pnl_User.Height = 500;
                    pnl_Annotation_Nuc.Width = 500; pnl_Annotation_Nuc.Height = 500;
                    pnl_Annotation_Seg.Width = 500; pnl_Annotation_Seg.Height = 500;
                    pnl_Panel_Seg.Width = 500; pnl_Panel_Seg.Height = 500;
                    pnl_Panel_Nuc.Width = 500; pnl_Panel_Nuc.Height = 500;
                    grd_SharpGLControl.Width = 500; grd_SharpGLControl.Height = 500;
                    break;
            }
        }

        //This open is for single stack images, compatible with EmbryoSegmenter 1.x
        private void mnu_Open_Stack_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Open_Stack_Click");
            _InitializeNewImage();
            _Initiale_m_series();
            SetImageSize(1);
            if (_OpenSegFile(1))
            {
                _SetImagePath();
                //Reset filepath for old files
                foreach (Slice slice in m_Series.stacks[0].slices)
            {
                slice.filepath = txt_Path.Text;
            }_Populate_lbx_Slice();
                _Select_item(1);
                _UpdateImage(1);

            }
        }

        
        //This open is for time series
        private void mnu_Open_Series_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Open_Series_Click");

            _InitializeNewImage();
            _Initiale_m_series();
            SetImageSize(2);
            if (_OpenSegFile(2))
            {
               // _SetImagePath();
                txt_Path.Text = m_Series.seg_fileName;
                this.Title = "Embryo Segmenter 2.1: " + txt_Path.Text;

                _Populate_scl_Time();
                _Populate_lbx_Ann_Stage();
                _Populate_lbx_Ann_Fragmentation();
                _Populate_lbx_Slice();
                _Select_item(1);
                _Select_Quality(m_Series.quality);
               _UpdateImage(1);
            }
        }

        private void _Select_Quality(SERIES_QUALITY q)
        {
            foreach (ComboBoxItem item in cbx_Quality.Items)
            {
                if ((int)item.Tag == (int)q)
                {
                    cbx_Quality.SelectedItem = item;
                }
            }
        }

        private void _Populate_lbx_Ann_Stage()
        {
            STACK_STAGE previousStage = STACK_STAGE.UNKNOWN;
            STACK_STAGE stage;
            int indexFrom = 0;
            int indexTo = 0;
            lbx_Ann_Stage.Items.Clear();
            foreach (Stack stack in m_Series.stacks)
            {
                if (stack.stackNo == 1)
                {
                    indexFrom = stack.stackNo;
                    previousStage = stack.stage;
                }
                stage = stack.stage;
                if ((previousStage != stage) || (stack.stackNo == m_Series.stacks.Count))
                {
                        indexTo = stack.stackNo - 1;
                                            if (stack.stackNo == m_Series.stacks.Count)
                        {
                            indexTo = stack.stackNo;
                        }
                   if  (indexFrom != 0 && indexTo != 0)
                    {
                        if (previousStage != STACK_STAGE.UNKNOWN)
                        {
                            ListBoxItem item = new ListBoxItem();
                            item.Tag = (int)previousStage;
                            item.Content = Construct_listbox_text(previousStage, indexFrom, indexTo);
                            lbx_Ann_Stage.Items.Add(item);
                        }
                    }
                    //Setup for next round
                    previousStage = stack.stage;
                    indexFrom = stack.stackNo;
                }
            }
        }

        private void _Populate_lbx_Ann_Fragmentation()
        {
            STACK_FRAGMENTATION previousFrag = STACK_FRAGMENTATION.UNKNOWN;
            STACK_FRAGMENTATION frag;
            int indexFrom = 0;
            int indexTo = 0;
            lbx_Ann_Fragmentation.Items.Clear();
            foreach (Stack stack in m_Series.stacks)
            {
                if (stack.stackNo == 1)
                {
                    indexFrom = stack.stackNo;
                    previousFrag = stack.fragmentation;
                }
                frag = stack.fragmentation;
               
                if ((previousFrag != frag) || (stack.stackNo == m_Series.stacks.Count))
                    {
                        indexTo = stack.stackNo - 1;
                        if (stack.stackNo == m_Series.stacks.Count)
                        {
                            indexTo = stack.stackNo;
                        }
                        if (indexFrom != 0 && indexTo != 0)
                        {
                            if (previousFrag != STACK_FRAGMENTATION.UNKNOWN)
                            {
                                ListBoxItem item = new ListBoxItem();
                                item.Tag = (int)previousFrag;
                                item.Content = Construct_listbox_text(previousFrag, indexFrom, indexTo);
                                lbx_Ann_Fragmentation.Items.Add(item);
                            }
                        }
                        //Setup for next round
                        previousFrag = stack.fragmentation;
                        indexFrom = stack.stackNo;
                    }
                
            }
        }


        private void txt_scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            _RecomputeScale();
        }

 private void mnu_Quit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

 private void mnu_Sort_Slice_Click(object sender, RoutedEventArgs e)
 {
     int sliceNo = 1;
     if (lbx_Slice.SelectedItem != null)
     {
         sliceNo = (int)((ListBoxItem)lbx_Slice.SelectedItem).Tag;
     }
     _ClearPanel();
     Stack currentStack = _Find_selected_stack();
     SortWindow sorting = new SortWindow(currentStack, txt_Path.Text, sliceNo);
     if ((bool)sorting.ShowDialog())
     {
         _LogStringDebug("Sorting ok");

         Stack changedStack = sorting.m_Stack;
         m_Series.stacks.Remove(currentStack);
         m_Series.stacks.Add(changedStack);
     }
     sorting._ClearPanel1(); sorting._ClearPanel2();
     sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
     Slice currentSlice = _Find_slice(sliceNo);
    
     _UpdateImage(currentSlice.sliceNo);
     lbx_Segment.Items.Clear();
     _Populate_lbx_Segment(sliceNo);
 }

 //This function has not been updated for a while
 private void mnu_Add_Segment_Click(object sender, RoutedEventArgs e)
 {
     MessageBoxResult res = MessageBox.Show("Do you wish to add all selected segments on this slice?", "Add segments", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
     if (res == MessageBoxResult.No)
     {
         return;
     }

     _LogStringDebug("mnu_Add_Segment_Click");
      if (lbx_Segment.SelectedItems.Count == 0)
     {
         return;
     }
     if (lbx_Slice.SelectedItem == null)
     {
         return;
     }
     _ClearPanel();
    
     int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
     List<int> segments = new List<int>();
     foreach (ListBoxItem lbx_seg_ in lbx_Segment.SelectedItems)
     {
         segments.Add((int)lbx_seg_.Tag);
     }
     _AddSegments(sliceNo, segments);
     lbx_Segment.Items.Clear();
     _Populate_lbx_Segment(sliceNo);
     _UpdateImage(sliceNo);
 }

 private void mnu_Remove_Segment_Click(object sender, RoutedEventArgs e)
 {
     //Remove segments

     MessageBoxResult res = MessageBox.Show("Do you wish to remove all selected segments from this slice?", "Remove segments", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
     if (res == MessageBoxResult.No)
     {
         return;
     }

     _LogStringDebug("mnu_Remove_Segment_Click");
    if (lbx_Segment.SelectedItem == null)
     {
         return;
     }
     if (lbx_Slice.SelectedItem == null)
     {
         return;
     }
     _ClearPanel();
     int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
     Slice slice_ = _Find_slice(sliceNo);
     foreach (ListBoxItem lbx_seg_ in lbx_Segment.SelectedItems)
     {
         int segNo = (int)lbx_seg_.Tag;
         List<Segment> seg_to_remove = new List<Segment>();
         foreach (Segment seg_ in slice_.segments)
         {
             if (seg_.segmentNo == segNo)
             {
                 seg_to_remove.Add(seg_);
             }
         }
         foreach (Segment seg_rem in seg_to_remove)
         {
             slice_.segments.Remove(seg_rem);
         }
     }
     lbx_Segment.Items.Clear();
     _Populate_lbx_Segment(sliceNo);
    
     _UpdateImage(slice_.sliceNo);
 }

 private void mnu_Divide_Segment_Click(object sender, RoutedEventArgs e)
 {
     if (lbx_Segment.SelectedItems.Count > 1)
     {
         MessageBox.Show("To divide a segment you can only select one segment!");
         return;
     }

     //XXX Divide segment
 }

 private void btn_Capture_Segment_Click(object sender, RoutedEventArgs e)
 {

 }

 private void chb_Display_Checked(object sender, RoutedEventArgs e)
 {
     //Alter displaymode of image
     if (initialized)
     {
         if ((bool)chb_Annotation_Seg.IsChecked)
         {
             pnl_Annotation_Seg.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Annotation_Seg.Visibility = Visibility.Hidden;
         }
         if ((bool)chb_Annotation_Nuc.IsChecked)
         {
             pnl_Annotation_Nuc.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Annotation_Nuc.Visibility = Visibility.Hidden;
         }
         if ((bool)chb_Outline_Seg.IsChecked)
         {
             pnl_Panel_Seg.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Panel_Seg.Visibility = Visibility.Hidden;
         }
         if ((bool)chb_Outline_Nuc.IsChecked)
         {
             pnl_Panel_Nuc.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Panel_Nuc.Visibility = Visibility.Hidden;
         }
     }
 }

private void mnu_Rename_Slice_Click(object sender, RoutedEventArgs e)
        {

            int newNumber = -1;
            EnterNumberWindow esw = new EnterNumberWindow();
            
            if ((bool)esw.ShowDialog())
            {
                if (lbx_Slice.SelectedItem != null)
                {
                    int sliceNo = (int)((ListBoxItem)lbx_Slice.SelectedItem).Tag;
                    newNumber = esw.GetNumber();
                    if (newNumber >= 0)
                    {
                        _RenameSlice(sliceNo, newNumber);
                    }
                }
           }
        }

 private void mnu_3D_view_Click(object sender, RoutedEventArgs e)
 {
     MenuItem item = (MenuItem)sender;
     Rendering.DisplayMode mode = Rendering.DisplayMode.POINT; //Default
     switch (item.Name)
     {
         case "mnu_3D_point":
             mode = Rendering.DisplayMode.POINT; break;
         case "mnu_3D_body":
             mode = Rendering.DisplayMode.BODY; break;
     }
     _Display3D(mode);
 }


        #endregion Window events

 private void mnu_Rename_Segment_Click(object sender, RoutedEventArgs e)
 {
     if (lbx_Segment.SelectedItems.Count > 1)
     {
         MessageBoxResult res = MessageBox.Show("More than one segment has been selected. Are you sure you want to rename all?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
         if (res == MessageBoxResult.No)
         { return; }
     }
     int newNumber = -1;

     EnterNumberWindow esw = new EnterNumberWindow();
     if ((bool)esw.ShowDialog())
     {
         newNumber = esw.GetNumber();
         if (lbx_Segment.SelectedItems.Count > 0 && newNumber != -1)
         {
             int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
                 foreach (ListBoxItem item in lbx_Segment.SelectedItems)
             {
                 int segmentNo = (int)((ListBoxItem)item).Tag;
                 Stack currentStack = _Find_selected_stack();

                 foreach (Slice s in currentStack.slices)
                 {
                     if (s.sliceNo == sliceNo)
                     {
                         foreach (Segment ss in s.segments)
                         {
                             if (ss.segmentNo == segmentNo)
                             {
                                 ss.segmentNo = newNumber;
                             }
                         }
                     }
                 }
             }
             lbx_Segment.Items.Clear();
             _Populate_lbx_Segment(sliceNo);
         }
         _RedrawSegment();
     }

 }

 private void scl_Time_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
 {
     int stackIndex = (int)scl_Time.Value;
    if (initialized)
     {
         if (lbx_Slice.SelectedItem != null)
         {
             m_selected_slice_index = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
             _Populate_lbx_Slice();
             _Select_item(m_selected_slice_index);
         }
     }
 }

 private void mnu_Add_Stack_Click(object sender, RoutedEventArgs e)
 {
     _Read_new_stack();
     _Populate_scl_Time();
     _Select_item(1);
     _Populate_lbx_Slice();
     _UpdateImage(1);
 }

 private void mnu_Remove_Stack_Click(object sender, RoutedEventArgs e)
 {
     //ALM fix
 }

 private void txt_Series_index_TextChanged(object sender, TextChangedEventArgs e)
 {
     if (initialized)
     {
         int i_text = (int)scl_Time.Value;
         txt_Series_index.Text = i_text.ToString();
     }
 }

 static vtkSphereSource sphere;
 static vtkConeSource cone;
 static vtkGlyph3D glyph;
 static vtkAppendPolyData apd;
 static vtkPolyDataMapper maceMapper;
 static vtkLODActor maceActor;
 static vtkPlanes planes;
 static vtkClipPolyData clipper;
 static vtkPolyDataMapper selectMapper;
 static vtkLODActor selectActor;
 static vtkRenderer ren1;
 static vtkRenderWindow renWin;
 static vtkRenderWindowInteractor iren;
 static vtkBoxWidget boxWidget;


 private void button1_Click(object sender, RoutedEventArgs e)
 {

     _Recompute_areas();
 }


 private void _Recompute_areas()
 {
     foreach (Stack stack in m_Series.stacks)
     {
         foreach (Slice slice in stack.slices)
         {
             if (slice.sliceNo == 10) //For test
             {
                 
             }
             foreach (Segment segment in slice.segments)
             {
                 /*if (segment.segmentNo == 1)
                 {
                     Segment segtest = new Segment();
                     segtest.nbrOfPoints = 4;
                     segtest.points = new List<Shapes.Point>();
                     for (int j = 0; j < 4; j++)
                     {
                         segtest.points.Add(new Shapes.Point());
                     }
                     int i = 0;
                     foreach (EmbryoSegmenter.Shapes.Point point in segtest.points)
                     {
                         if (i == 0)
                         { point.X = 11; point.Y = 2; }
                         else if (i == 1)
                         { point.X = 2; point.Y = 2; }
                         else if (i == 2)
                         { point.X = 4; point.Y = 10; }
                         else if (i == 3)
                         { point.X = 9; point.Y = 7; }
                         i++;
                     }
                     int area = _Calculate_area(segtest);
                     segtest.segmentArea = area;
                 }*/
                 if (((segment.shape == Shapes.SEGMENT_SHAPE.NONE) || (segment.shape == Shapes.SEGMENT_SHAPE.POINTS))
                     && (segment.points != null))
                 {
                     int area2 = _Calculate_area(segment);
                     segment.segmentArea = area2;
                 }
             }
         }
     }
 }

 private int _Calculate_area(Segment segment)
 {
     //Calculate area
      // http://www.mathopenref.com/coordpolygonarea.html
		double xTimesYTotal = 0;
		double yTimesXTotal = 0;
     double[] tempPointA = new double[3]; 
         double[] tempPointB = new double[3];
         double[] tempPointFirst = new double[3];
     double area = 0;
     int i = 0;
     foreach (EmbryoSegmenter.Shapes.Point point in segment.points)
     {
         if (i == 0) //first round
         {
         tempPointA[0] = point.X;tempPointA[1] = point.Y;tempPointA[2] = point.Z;
         tempPointFirst[0] = point.X; tempPointFirst[1] = point.Y; tempPointFirst[2] = point.Z;
         }
        
         else
         {
        tempPointB[0] = point.X;tempPointB[1] = point.Y;tempPointB[2] = point.Z;
        //tempPointA = from previous round
        // Multiply the x co-ordinate of each vertex by the
			// y co-ordinate of the next vertex
             xTimesYTotal = tempPointA[0] * tempPointB[1];
             yTimesXTotal = tempPointA[1] * tempPointB[0];
             area += xTimesYTotal - yTimesXTotal;
             tempPointA[0] = tempPointB[0]; tempPointA[1] = tempPointB[1]; tempPointA[2] = tempPointB[2];
        
	    }
         i++;
         if (i == segment.nbrOfPoints) //last round
         {
             //Removing this is wrong, but Kierans EmbryoSegmentwer calculates this way
             tempPointB[0] = point.X; tempPointB[1] = point.Y; tempPointB[2] = point.Z;
             xTimesYTotal = tempPointA[0] * tempPointFirst[1];
             yTimesXTotal = tempPointA[1] * tempPointFirst[0];
             area += xTimesYTotal - yTimesXTotal;
            
         }
     }
		area = Math.Abs(area/2);
		return (int)area;
 }
 /// <summary>
 /// Callback function for boxWidget.EndInteractionEvt
 /// </summary>
 public static void SelectPolygons(vtkObject sender, vtkObjectEventArgs e)
 {
     boxWidget.GetPlanes(planes);
     selectActor.VisibilityOn();
 }

 ///<summary>
 ///Deletes all static objects created
 ///</summary>
 public static void deleteAllVTKObjects()
 {
     //clean up vtk objects
     if (sphere != null) { sphere.Dispose(); }
     if (cone != null) { cone.Dispose(); }
     if (glyph != null) { glyph.Dispose(); }
     if (apd != null) { apd.Dispose(); }
     if (maceMapper != null) { maceMapper.Dispose(); }
     if (maceActor != null) { maceActor.Dispose(); }
     if (planes != null) { planes.Dispose(); }
     if (clipper != null) { clipper.Dispose(); }
     if (selectMapper != null) { selectMapper.Dispose(); }
     if (selectActor != null) { selectActor.Dispose(); }
     if (ren1 != null) { ren1.Dispose(); }
     if (renWin != null) { renWin.Dispose(); }
     if (iren != null) { iren.Dispose(); }
     if (boxWidget != null) { boxWidget.Dispose(); }
 }

 private void mnu_Pipelines_Click(object sender, RoutedEventArgs e)
 {
     PipelineWindow pipeline = new PipelineWindow();
     if ((bool)pipeline.ShowDialog())
     {
         m_Pipelines = pipeline.pipelines;
     }
 }

 private void mnu_View_Switch_Unchecked(object sender, RoutedEventArgs e)
 {
     MenuItem _sender = (MenuItem)sender;
     if (initialized)
     {
         switch (_sender.Name.ToString())
         {
             case "mnu_View_Mini":
                 grd_Display_Mini.Visibility = Visibility.Collapsed;
                 break;

             case "mnu_View_Annotation":
                 grd_Annotation.Visibility = Visibility.Collapsed;
                 grd_Mouse_Position.Visibility = Visibility.Collapsed;
                 break;

             case "mnu_View_Segmentation":
                 grd_Segmentation.Visibility = Visibility.Collapsed;
                 break;

             case "mnu_View_3D":
                grd_3Dtools.Visibility = Visibility.Collapsed;
                break;
         }
     }
 }

 private void mnu_View_Switch_Checked(object sender, RoutedEventArgs e)
 {
     MenuItem _sender = (MenuItem)sender;
     if (initialized)
     {
         //Collapse all
         /*grd_Annotation.Visibility = Visibility.Collapsed;
         grd_Segmentation.Visibility = Visibility.Collapsed;
         grd_Display_Mini.Visibility = Visibility.Collapsed;
         grd_Mouse_Position.Visibility = Visibility.Collapsed;*/
         chb_Ann_NewNuclei.IsChecked = false;
         chb_Ann_NewSegment.IsChecked = false;

         switch (_sender.Name.ToString())
         {
             case "mnu_View_Mini":
                 grd_Display_Mini.Visibility = Visibility.Visible;
                 mnu_View_Annotation.IsChecked = false;
                 mnu_View_Segmentation.IsChecked = false;
                mnu_View_3D.IsChecked = false;
                break;

             case "mnu_View_Annotation":
                 grd_Annotation.Visibility = Visibility.Visible;
                 grd_Mouse_Position.Visibility = Visibility.Visible;
                 chb_Annotation_Seg.IsChecked = true;
                 chb_Annotation_Nuc.IsChecked = true;
                 mnu_View_Mini.IsChecked = false;
                 mnu_View_Segmentation.IsChecked = false;
                mnu_View_3D.IsChecked = false;
                break;

             case "mnu_View_Segmentation":
                 grd_Segmentation.Visibility = Visibility.Visible;
                 mnu_View_Annotation.IsChecked = false;
                 mnu_View_Mini.IsChecked = false;
                 mnu_View_3D.IsChecked = false;
                break;

             case "mnu_View_3D":
                mnu_View_3D.IsChecked = true;
                grd_3Dtools.Visibility = Visibility.Visible;
                mnu_View_Annotation.IsChecked = false;
                 mnu_View_Mini.IsChecked = false;
                 mnu_View_Segmentation.IsChecked = false;
                break;
         }
     }

 }


 private void pnl_MouseMove(object sender, MouseEventArgs e)
 {
     lbl_Mouse_X.Content = Mouse.GetPosition(pnl_Image).X.ToString();
     lbl_Mouse_Y.Content = Mouse.GetPosition(pnl_Image).Y.ToString();

     if ((Mouse.LeftButton == MouseButtonState.Pressed) && lbx_Slice.SelectedItem !=null)
     {
         if ((bool)chb_Ann_NewNuclei.IsChecked)
         {
             //Find coordinates
             int centerX;
             int centerY;
             int.TryParse(txt_Ann_NewNuclei_CenterX.Text, out centerX);
             int.TryParse(txt_Ann_NewNuclei_CenterY.Text, out centerY);
             double diffX = Math.Abs(((double)centerX - Mouse.GetPosition(pnl_Image).X));
             double diffY = Math.Abs(((double)centerY - Mouse.GetPosition(pnl_Image).Y));
             double radius = Math.Sqrt((diffX * diffX) + (diffY * diffY));
             txt_Ann_NewNuclei_Radius.Text = radius.ToString("###.#");

             //Draw temporary circle after deleting old
             if (_CheckVisual(temp_dragDisplay_Nuc))
             {
                 pnl_Panel_Nuc.DeleteVisual(temp_dragDisplay_Nuc);
                 m_DrawingNuclei.Remove(temp_dragDisplay_Nuc);
                 temp_dragDisplay_Nuc = null; 
             }
             if (temp_dragDisplay_Nuc == null)
             {
                 temp_dragDisplay_Nuc = new Circle(); temp_dragDisplay_Nuc.lineThickness = 3;
             }
             if (!_CheckVisual(temp_dragDisplay_Nuc))
             {
                 temp_dragDisplay_Nuc.centre = new System.Windows.Point((double)centerX, (double)(centerY));
                 temp_dragDisplay_Nuc.radius = radius;
                 temp_dragDisplay_Nuc.DrawCircleOutline(GetSelectedColorNuc());
                 pnl_Panel_Nuc.AddVisual(temp_dragDisplay_Nuc);
                 m_DrawingNuclei.Add(temp_dragDisplay_Nuc);
             }
         }
         else if ((bool)chb_Ann_NewSegment.IsChecked)
         {
             //Find coordinates
             int startX;
             int startY;
             int.TryParse(txt_Ann_NewSegment_StartX.Text, out startX);
             int.TryParse(txt_Ann_NewSegment_StartY.Text, out startY);
            
             if (temp_dragDisplay_Seg == null)
             {
                 temp_dragDisplay_Seg = new Segment();
                 Shapes.Point startingPoint = new Shapes.Point();
                 startingPoint.X = (double)startX;
                 startingPoint.Y = (double)startY;
                 startingPoint.Z = (double)0; //XXX value here
                 temp_dragDisplay_Seg.points = new List<Shapes.Point>();
                 temp_dragDisplay_Seg.points.Add(startingPoint);
                 if (!(_CheckVisual(startingPoint)))
                 {
                 startingPoint.Draw(GetSelectedColorSeg());
                 pnl_Panel_Seg.AddVisual(startingPoint);
                 m_DrawingSegmentationPoints.Add(startingPoint);
                 }
             }
             else
             {
                 Shapes.Point newPoint = new Shapes.Point();
                 newPoint.X = Mouse.GetPosition(pnl_Image).X;
                 newPoint.Y = Mouse.GetPosition(pnl_Image).Y;
                 _AddPointsBetween(temp_dragDisplay_Seg, newPoint);

                
             }
         }
     }
 }

 private void _AddPointsBetween(Segment seg, Shapes.Point newPoint)
 {
     Shapes.Point lastPoint = seg.points.Last();
     bool onNewPoint = false;
     int nbrIterations = 0;
     while (!onNewPoint)
     {
         nbrIterations++;
         int disX = (int)(newPoint.X - lastPoint.X);
         int disY = (int)(newPoint.Y - lastPoint.Y);
         Shapes.Point tmpPoint = new Shapes.Point();
         if (Math.Abs(disX) > Math.Sqrt(2) * Math.Abs(disY))     
         {
             tmpPoint.X = lastPoint.X + Math.Sign(disX);
             tmpPoint.Y = lastPoint.Y;
         }
         else if (Math.Abs(disY) > Math.Sqrt(2) * Math.Abs(disX))
         {
             tmpPoint.Y = lastPoint.Y + Math.Sign(disY);
             tmpPoint.X = lastPoint.X;
         }
         else
         {
             tmpPoint.X = lastPoint.X + Math.Sign(disX);
             tmpPoint.Y = lastPoint.Y + Math.Sign(disY);
         }
         seg.points.Add(tmpPoint);
         if (!(_CheckVisual(tmpPoint)))
         {
             tmpPoint.Draw(GetSelectedColorSeg());
             pnl_Panel_Seg.AddVisual(tmpPoint);
             m_DrawingSegmentationPoints.Add(tmpPoint);
         }
         lastPoint = tmpPoint;
         if (lastPoint.X == newPoint.X && lastPoint.Y == newPoint.Y || nbrIterations >20)
         {
             onNewPoint = true;
         }
     }
     
 }

 private void pnl_MouseDown(object sender, MouseButtonEventArgs e)
 {
     if (lbx_Slice.SelectedItem != null)
     {
         if ((bool)chb_Ann_NewNuclei.IsChecked)
         {
             txt_Ann_NewNuclei_CenterX.Text = Mouse.GetPosition(pnl_Image).X.ToString();
             txt_Ann_NewNuclei_CenterY.Text = Mouse.GetPosition(pnl_Image).Y.ToString();
         }
         if ((bool)chb_Ann_NewSegment.IsChecked)
         {
             txt_Ann_NewSegment_StartX.Text = Mouse.GetPosition(pnl_Image).X.ToString();
             txt_Ann_NewSegment_StartY.Text = Mouse.GetPosition(pnl_Image).Y.ToString();
             temp_dragDisplay_Seg = null;
             _RedrawSegment();
         }
     }
 }

 private void chb_Ann_NewNuclei_Checked(object sender, RoutedEventArgs e)
 {
     if ((bool)chb_Ann_NewNuclei.IsChecked)
     {
         grd_Ann_Nuclei.IsEnabled = true;
         chb_Ann_NewSegment.IsChecked = false;
     }
     else
     {
         grd_Ann_Nuclei.IsEnabled = false;
     }

 }

 private void chb_Ann_NewSegment_Checked(object sender, RoutedEventArgs e)
 {
     if ((bool)chb_Ann_NewSegment.IsChecked)
     {
         grd_Ann_Segment.IsEnabled = true;
         chb_Ann_NewNuclei.IsChecked = false;
     }
     else
     {
         grd_Ann_Segment.IsEnabled = false;
     }

 }

 private void txt_Validate_NewNuclei(object sender, TextChangedEventArgs e)
 {
     int centerX;
     int centerY;
     double radius;
     int.TryParse(txt_Ann_NewNuclei_CenterX.Text, out centerX);
     int.TryParse(txt_Ann_NewNuclei_CenterY.Text, out centerY);
     double.TryParse(txt_Ann_NewNuclei_Radius.Text, out radius);
     if (centerX >= 0 && centerX <= 500 && centerY >= 0 && centerY <= 500 && radius > 0 && lbx_Slice.SelectedItem != null)
     {
         btn_Add_Nuclei.IsEnabled = true;
     }
     else
     {
         btn_Add_Nuclei.IsEnabled = false;
     }
 }

 private void txt_Validate_NewSegment(object sender, TextChangedEventArgs e)
 {
     int startX;
     int startY;
     int.TryParse(txt_Ann_NewSegment_StartX.Text, out startX);
     int.TryParse(txt_Ann_NewSegment_StartY.Text, out startY);
     if (startX >= 0 && startX <= 500 && startY >= 0 && startY <= 500 && lbx_Slice.SelectedItem != null)
     {
         btn_Add_Segment.IsEnabled = true;
     }
     else
     {
         btn_Add_Segment.IsEnabled = false;
     }
 }

 private void txt_Validate_Stage(object sender, TextChangedEventArgs e)
 {
     int begin;
     int end;
     int.TryParse(txt_Ann_Stage_Begin.Text, out begin);
     int.TryParse(txt_Ann_Stage_End.Text, out end);
     if (begin >= scl_Time.Minimum && begin <= scl_Time.Maximum && end >= scl_Time.Minimum && end <= scl_Time.Maximum && begin <= end && m_Series.stacks != null)
     {
         btn_Add_Stage.IsEnabled = true;
     }
     else
     {
         btn_Add_Stage.IsEnabled = false;
     }
 }

 private void txt_Validate_Fragmentation(object sender, TextChangedEventArgs e)
 {
     int begin;
     int end;
     int.TryParse(txt_Ann_Fragmentation_Begin.Text, out begin);
     int.TryParse(txt_Ann_Fragmentation_End.Text, out end);
     if (begin >= scl_Time.Minimum && begin <= scl_Time.Maximum && end >= scl_Time.Minimum && end <= scl_Time.Maximum && begin <= end && m_Series.stacks != null)
     {
         btn_Add_Fragmentation.IsEnabled = true;
     }
     else
     {
         btn_Add_Fragmentation.IsEnabled = false;
     }
 }


 private void lbx_Ann_Nuclei_SelectionChanged(object sender, SelectionChangedEventArgs e)
 {
     if (lbx_Ann_Nuclei.SelectedItem != null)
     {
         btn_Ann_Remove_Nuclei.IsEnabled = true;
     }
     else
     {
         btn_Ann_Remove_Nuclei.IsEnabled = false;
     }
 }

 private void lbx_Ann_Segment_SelectionChanged(object sender, SelectionChangedEventArgs e)
 {
     if (lbx_Ann_Segment.SelectedItem != null)
     {
         btn_Ann_Remove_Segment.IsEnabled = true;
     }
     else
     {
         btn_Ann_Remove_Segment.IsEnabled = false;
     }
 }

 private void lbx_Ann_Stage_SelectionChanged(object sender, SelectionChangedEventArgs e)
 {
     if (lbx_Ann_Stage.SelectedItem != null)
     {
         btn_Ann_Remove_Stage.IsEnabled = true;
     }
     else
     {
         btn_Ann_Remove_Stage.IsEnabled = false;
     }
 }

 private void lbx_Ann_Fragmentation_SelectionChanged(object sender, SelectionChangedEventArgs e)
 {
     if (lbx_Ann_Fragmentation.SelectedItem != null)
     {
         btn_Ann_Remove_Fragmentation.IsEnabled = true;
     }
     else
     {
         btn_Ann_Remove_Fragmentation.IsEnabled = false;
     }
 }

 private void btn_Add_Nuclei_Click(object sender, RoutedEventArgs e)
 {
     int centerX;
     int centerY;
     double radius;
     int.TryParse(txt_Ann_NewNuclei_CenterX.Text, out centerX);
     int.TryParse(txt_Ann_NewNuclei_CenterY.Text, out centerY);
     double.TryParse(txt_Ann_NewNuclei_Radius.Text, out radius);
     Slice currentSlice = _Find_slice((int)((ListBoxItem)lbx_Slice.SelectedItem).Tag);
     Segment newNuclei = new Segment(); newNuclei.shape = SEGMENT_SHAPE.CIRCLE; newNuclei.circle = new Circle();
     newNuclei.circle.centre.X = (double)centerX; newNuclei.circle.centre.Y = (double)centerY;
     newNuclei.centerX = centerX; newNuclei.centerY = centerY;
     newNuclei.circle.radius = radius; newNuclei.segmentNo = currentSlice.GetHighestSegmentID() + 1;
     newNuclei.annotation = new Annotation(newNuclei.segmentNo.ToString());
     string nucleitext = Construct_listbox_text(newNuclei);
     ListBoxItem newItem = new ListBoxItem();
     newItem.Content = nucleitext; newItem.Tag = newNuclei.segmentNo;
     lbx_Ann_Nuclei.Items.Add(newItem);
     //Remove temporary drawing
     pnl_Panel_Nuc.DeleteVisual(temp_dragDisplay_Nuc);
     temp_dragDisplay_Nuc = null;
     currentSlice.segments.Add(newNuclei);
     _Populate_lbx_Segment(currentSlice.sliceNo);
     _Recompute_areas();
     _RecomputeScale();
     _RedrawSegment();
 }

 private string Construct_listbox_text(Segment seg)
 {
     string text = "";
     switch (seg.shape)
     {
         case SEGMENT_SHAPE.POINTS:
             text = seg.segmentNo + ":   X: " + seg.centerX + "    Y: " + seg.centerY;
             break;
         case SEGMENT_SHAPE.CIRCLE:
             text = seg.segmentNo + ":   X: " + seg.circle.centre.X + "    Y: " + seg.circle.centre.Y + "    R: " + seg.circle.radius;
             break;
     }
     return text;
 }


 private string Construct_listbox_text(STACK_STAGE stage, int from, int to)
 {
     string text = stage + ": " + from + " - " + to;
     return text;
 }

 private bool Deconstruct_listbox_text(string text, ref int from, ref int to)
 {
     int separatorIndex = text.LastIndexOf(":");
     string restString = text.Substring(separatorIndex+2);
     int secondSeparatorIndex = restString.LastIndexOf("-");
     string fromstring = text.Substring(separatorIndex +2, secondSeparatorIndex -1);
     string tostring = restString.Substring(secondSeparatorIndex +2, restString.Length - secondSeparatorIndex - 2);
     if (int.TryParse(fromstring, out from) && int.TryParse(tostring, out to))
     {
         return true;
     }
     return false;
 }

 private string Construct_listbox_text(STACK_FRAGMENTATION frag, int from, int to)
 {
     string fragmentationGroup = "Unknown";

     switch (frag)
     {
         case STACK_FRAGMENTATION.UNKNOWN:
             fragmentationGroup = "Unknown";
             break;

         case STACK_FRAGMENTATION.GROUP1:
             fragmentationGroup = "Group I: 0%";
             break;

         case STACK_FRAGMENTATION.GROUP2:
             fragmentationGroup = "Group II: 1+/-10%";
             break;

         case STACK_FRAGMENTATION.GROUP3:
             fragmentationGroup = "Group III: 11+/-20%";
             break;

         case STACK_FRAGMENTATION.GROUP4:
             fragmentationGroup = "Group IV: 21+/-50%";
             break;

         case STACK_FRAGMENTATION.GROUP5:
             fragmentationGroup = "Group V: >50%";
             break;
     }
     string text = fragmentationGroup + ": " + from + " - " + to;
     return text;
 }

 private void btn_Add_Segment_Click(object sender, RoutedEventArgs e)
 {
     int startX;
     int startY;
     int.TryParse(txt_Ann_NewSegment_StartX.Text, out startX);
     int.TryParse(txt_Ann_NewSegment_StartY.Text, out startY);
     Slice currentSlice = _Find_slice((int)((ListBoxItem)lbx_Slice.SelectedItem).Tag);
     Segment newSegment = new Segment(); newSegment.shape = SEGMENT_SHAPE.POINTS;
     newSegment.segmentNo = currentSlice.GetHighestSegmentID() +1;
     newSegment.points = temp_dragDisplay_Seg.points; newSegment.nbrOfPoints = newSegment.points.Count;
     if (_FindSegmentCenterAndArea(ref newSegment))
     {
         newSegment.annotation = new Annotation(newSegment.segmentNo.ToString());
         //string celltext = Construct_listbox_text(newSegment);
     //ListBoxItem newItem = new ListBoxItem();
     //newItem.Content = celltext; newItem.Tag = newSegment.segmentNo;
     //lbx_Ann_Segment.Items.Add(newItem);
     //Remove temporary drawing
     foreach (Shapes.Point point in temp_dragDisplay_Seg.points)
     {
         pnl_Panel_Seg.DeleteVisual(point);
     }
         currentSlice.segments.Add(newSegment);
         _Populate_lbx_Segment(currentSlice.sliceNo);
         _Populate_lbx_Ann_Segment(currentSlice.sliceNo);
     }
     _Recompute_areas();
     _RecomputeScale();
     _RedrawSegment();
 }

 private void btn_Add_Stage_Click(object sender, RoutedEventArgs e)
 {
     if (cbx_Ann_Stage.SelectedItem == null)
     {return;}
     int begin;
     int end;
     int.TryParse(txt_Ann_Stage_Begin.Text, out begin);
     int.TryParse(txt_Ann_Stage_End.Text, out end);
     ComboBoxItem cbx_stage_item = (ComboBoxItem)cbx_Ann_Stage.SelectedItem;
     /*string stage = cbx_stage_item.Content.ToString();
     string stagetext = Construct_listbox_text((STACK_STAGE)cbx_stage_item.Tag, begin, end);
     ListBoxItem newItem = new ListBoxItem();
     newItem.Content = stagetext;
     newItem.Tag = cbx_stage_item.Tag;
     lbx_Ann_Stage.Items.Add(newItem);*/
     for (int i = begin; i <= end; i++)
     {
         Stack currentStack = _Find_stack(i);
         currentStack.stage = (STACK_STAGE)cbx_stage_item.Tag;
     }
     _Populate_lbx_Ann_Stage();
 }

 private void btn_Add_Fragmentation_Click(object sender, RoutedEventArgs e)
 {
     if (cbx_Ann_Fragmentation.SelectedItem == null)
     { return; }
     int begin;
     int end;
     int.TryParse(txt_Ann_Fragmentation_Begin.Text, out begin);
     int.TryParse(txt_Ann_Fragmentation_End.Text, out end);
     ComboBoxItem cbx_frag_item = (ComboBoxItem)cbx_Ann_Fragmentation.SelectedItem;
     for (int i = begin; i <= end; i++)
     {
         Stack currentStack = _Find_stack(i);
         currentStack.fragmentation = (STACK_FRAGMENTATION)cbx_frag_item.Tag;
     }
     _Populate_lbx_Ann_Fragmentation();
 }


 private void btn_Ann_Remove_Nuclei_Click(object sender, RoutedEventArgs e)
 {
     int nucleiNo = (int)((ListBoxItem)lbx_Ann_Nuclei.SelectedItem).Tag;
     Segment currentNuclei = _Find_nuclei(nucleiNo);
     Slice currentSlice = _Find_slice((int)((ListBoxItem)lbx_Slice.SelectedItem).Tag);
     currentSlice.segments.Remove(currentNuclei);
     lbx_Ann_Nuclei.Items.Remove(lbx_Ann_Nuclei.SelectedItem);
     _RedrawSegment();
 }

 private void btn_Ann_Remove_Segment_Click(object sender, RoutedEventArgs e)
 {
     int segmentNo = (int)((ListBoxItem)lbx_Ann_Segment.SelectedItem).Tag;
     Slice currentSlice = _Find_slice((int)((ListBoxItem)lbx_Segment.SelectedItem).Tag);
     Segment currentSegment = _Find_segment(currentSlice.sliceNo, segmentNo);
     currentSlice.segments.Remove(currentSegment);
     lbx_Ann_Segment.Items.Remove(lbx_Ann_Segment.SelectedItem);
     _RedrawSegment();
 }

 private void btn_Ann_Remove_Stage_Click(object sender, RoutedEventArgs e)
 {
     int begin = -1;
     int end = -1;
     ListBoxItem cbx_stage_item = (ListBoxItem)lbx_Ann_Stage.SelectedItem;
     string stageText = cbx_stage_item.Content.ToString();
     if (Deconstruct_listbox_text(stageText, ref begin, ref end))
     {
         for (int i = begin; i <= end; i++)
         {
             Stack currentStack = _Find_stack(i);
             currentStack.stage = (STACK_STAGE.UNKNOWN);
         }
     }
     _Populate_lbx_Ann_Stage();
 }

 private void btn_Ann_Remove_Fragmentation_Click(object sender, RoutedEventArgs e)
 {
     int begin = -1;
     int end = -1;
     ListBoxItem cbx_stage_item = (ListBoxItem)lbx_Ann_Fragmentation.SelectedItem;
     string fragText = cbx_stage_item.Content.ToString();
     if (Deconstruct_listbox_text(fragText, ref begin, ref end))
     {
         for (int i = begin; i <= end; i++)
         {
             Stack currentStack = _Find_stack(i);
             currentStack.fragmentation = (STACK_FRAGMENTATION.UNKNOWN);
         }
     }
     _Populate_lbx_Ann_Fragmentation();
 } 

private Color GetSelectedColorSeg()
        {
            return (Color)((ComboBoxItem)cbx_Color_Seg.SelectedItem).Tag;
        }
private Color GetSelectedColorNuc()
{
    return (Color)((ComboBoxItem)cbx_Color_Nuc.SelectedItem).Tag;
}

private void mnu_View_Annotation_Unchecked(object sender, RoutedEventArgs e)
{

}

private void Window_KeyDown(object sender, KeyEventArgs e)
{

}

private void cbx_Color_Nuc_DropDownOpened(object sender, EventArgs e)
{
    killFocus = false;
}

private void cbx_Color_Seg_DropDownOpened(object sender, EventArgs e)
{
    killFocus = false;
}

private void tcl_Annotation_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (((TabItem)tcl_Annotation.SelectedItem) == tab_Ann_Stage)
    {
        chb_Ann_NewNuclei.IsChecked = false;
        chb_Ann_NewSegment.IsChecked = false;
    }
}

private void cbx_Quality_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (initialized)
    {
        m_Series.quality = (SERIES_QUALITY)((ComboBoxItem)(cbx_Quality.SelectedItem)).Tag;
    }
}

private void _Update_3D_Rendering(DisplayMode mode)
{
    _ComputeZscale();
    if ((_Find_selected_stack().slices == null) || (_Find_selected_stack().slices.Count == 0))
    { return; }
    
    List<vtkActor> _all_actors = new List<vtkActor>();
    List<vtkPolyData> _all_polydata = new List<vtkPolyData>();
    List<vtkCellArray> _all_vertices = new List<vtkCellArray>();
    List<vtkPoints> _all_points = new List<vtkPoints>();
    List<vtkDataSetMapper> _all_mappers = new List<vtkDataSetMapper>();
   
    List<Blob> blobs = new List<Blob>();
    Stack currentStack = _Find_selected_stack();
    int highestId = currentStack.GetHighestSegmentIDFromAllSlices();
    for (int blobId = 0; blobId < highestId; blobId++)
    {
        Blob blob = new Blob(blobId);
        foreach (Slice _slice in currentStack.slices)
        {
            foreach (Segment _seg in _slice.segments)
            {
                if (_seg.segmentNo == blobId)
                {
                    blob.segments.Add(_seg);
                }
            }
        }
        blobs.Add(blob);
    }

    foreach (Blob blob in blobs)
    {
        vtkCellArray _vertices = vtkCellArray.New();
        vtkPoints _points = vtkPoints.New();
        int index = 1;
        foreach (Segment _seg in blob.segments)
        {
            foreach (EmbryoSegmenter.Shapes.Point _p in _seg.points)
            {
                _points.InsertNextPoint(_p.X, _p.Y, _p.Z);
                vtkVertex _vertex = vtkVertex.New();
                _vertex.GetPointIds().SetId(0, index);
                _vertices.InsertNextCell(_vertex);
                index++;
            }
        }

        vtkPolyData polydata = vtkPolyData.New();
        polydata.SetPoints(_points);
        polydata.SetVerts(_vertices);
        _all_vertices.Add(_vertices);
        _all_points.Add(_points);
        _all_polydata.Add(polydata);
    }
    vtkRenderer _renderer = _renderControl.RenderWindow.GetRenderers().GetFirstRenderer();
       
    foreach (vtkPolyData poly in _all_polydata)
    {
        vtkActor actor = vtkActor.New();
        vtkDataSetMapper mapper = vtkDataSetMapper.New();
        switch (mode)
        {
            case DisplayMode.POINT:            
    mapper.SetInputConnection(poly.GetProducerPort());
    actor.SetMapper(mapper);
    _all_mappers.Add(mapper);
                break;

            case DisplayMode.BODY:
    vtkDelaunay3D delny = vtkDelaunay3D.New();
    delny.SetInput(poly);
    mapper.SetInputConnection(delny.GetOutputPort());
    actor.SetMapper(mapper);
    _all_mappers.Add(mapper);
    delny.Dispose();break;
        }

        
        if (actor != null)
        {
            _all_actors.Add(actor);
        }
    }


    if ((_all_actors == null) || (_all_actors.Count == 0))
    {
        return;
    }
    if (_renderer == null)
    {
        return;
    }
    int colorChoice = 0;
    foreach (vtkActor actor in _all_actors)
    {
        if (1 == 1)
        {
            colorChoice++;
            if (colorChoice == 6)
            {
                actor.GetProperty().SetColor(1, 0, 1);
                colorChoice = 0;
            }
            else if (colorChoice == 1)
            {
                actor.GetProperty().SetColor(0, 1, 0);
            }
            else if (colorChoice == 2)
            {
                actor.GetProperty().SetColor(0, 0.3, 0);
            }
            else if (colorChoice == 3)
            {
                actor.GetProperty().SetColor(0, 0, 1);
            }
            else if (colorChoice == 4)
            {
                actor.GetProperty().SetColor(0, 0.3, 0);
            }
            else if (colorChoice == 5)
            {
                actor.GetProperty().SetColor(0, 0, 1);
            }
        }
        //actor.GetProperty().SetOpacity(0.6);
        _renderer.AddActor(actor);

    }
    _renderer.SetBackground(0, 0, 0);
   
    //_renderControl.RenderWindow.GetInteractor().Initialize();
    _renderControl.RenderWindow.GetInteractor().Render();
    //_renderControl.RenderWindow.GetInteractor().Start();
if (_all_polydata != null)
    {
        foreach (vtkPolyData polydata in _all_polydata)
        {
            //polydata.Dispose();
        }
    }
    if (_all_actors != null)
    {
        foreach (vtkActor actor in _all_actors)
        {
            //actor.Dispose();
        }
    }
    if (_all_vertices != null)
    {
        foreach (vtkCellArray vertices in _all_vertices)
        {
            //vertices.Dispose();
        }
    }
    if (_all_points != null)
    {
        foreach (vtkPoints point in _all_points)
        {
            //point.Dispose();
        }
    }
    //if (_render_window != null) { _render_window.Dispose(); }
    //if (_render_window_interactor != null) { _render_window_interactor.Dispose(); }
    //if (_renderer != null) { _renderer.Dispose(); }
}

private void _Update_3D_Rendering3(DisplayMode Dmode)
{
}
private void _Update_3D_Rendering2(DisplayMode Dmode)
{
    _DoTestRendering();
   /* _ComputeZscale();
    if ((_Find_selected_stack().slices == null) || (_Find_selected_stack().slices.Count == 0))
    { return; }
    Rendering.RenderingWindow render = new Rendering.RenderingWindow();
    List<Blob> blobs = new List<Blob>();
    Stack currentStack = _Find_selected_stack();
    int highestId = currentStack.GetHighestSegmentIDFromAllSlices();
    for (int blobId = 0; blobId < highestId; blobId++)
    {
        Blob blob = new Blob(blobId);
        foreach (Slice _slice in currentStack.slices)
        {
            foreach (Segment _seg in _slice.segments)
            {
                if (_seg.segmentNo == blobId)
                {
                    blob.segments.Add(_seg);
                }
            }
        }
        blobs.Add(blob);
    }
    render.SetShapes(blobs);
    render.Show((EmbryoSegmenter.Rendering.DisplayMode)(Dmode));*/

}


private void _DoTestRendering()
{

    /*
    List<vtkActor> _all_actors = new List<vtkActor>();
    List<vtkPolyData> _all_polydata = new List<vtkPolyData>();
    List<vtkCellArray> _all_vertices = new List<vtkCellArray>();
    List<vtkPoints> _all_points = new List<vtkPoints>();
    List<vtkDataSetMapper> _all_mappers = new List<vtkDataSetMapper>();
    vtkPolyData polydata = vtkPolyData.New();

    int index = 0;
    vtkCellArray _vertices = vtkCellArray.New();
    vtkPoints _points = vtkPoints.New();*/
    vtkRenderer _renderer = _renderControl.RenderWindow.GetRenderers().GetFirstRenderer();
    int index = 1;
    foreach (Slice slice in m_Series.stacks[0].slices)
    {
        if (slice.sliceNo < 10)
        {
        foreach (Segment _seg in slice.segments)
        {
            //if (_seg.segmentNo == 1)
            {
                foreach (EmbryoSegmenter.Shapes.Point _p in _seg.points)
                {
                     vtkSphereSource sphere0 = vtkSphereSource.New();
        //double test = sphere.GetRadius();
                     double scl = 0.001;
                     sphere0.SetRadius(scl*0.1);
        sphere0.SetCenter(scl * _p.X, scl * _p.Y, scl * _p.Z);
        maceMapper = vtkPolyDataMapper.New();
        maceMapper.SetInputConnection(sphere0.GetOutputPort());
        maceActor = vtkLODActor.New();
        maceActor.SetMapper(maceMapper);
        maceActor.VisibilityOn();
        _renderer.AddActor(maceActor);
                    index++;
                }
            }
        }
        }

       
    }

    /*vtkActor actor = vtkActor.New();
    //vtkDataSetMapper mapper = vtkDataSetMapper.New();
    vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
    vtkRenderer _renderer = vtkRenderer.New();
    vtkRenderWindowInteractor _render_window_interactor = vtkRenderWindowInteractor.New();
    _renderControl.RenderWindow.AddRenderer(_renderer);
    _render_window_interactor.SetRenderWindow(_renderControl.RenderWindow);


    //vtkRenderer _renderer = _renderControl.RenderWindow.GetRenderers().GetFirstRenderer();

            foreach (vtkPolyData poly in _all_polydata)
            {
                
                        mapper.SetInputConnection(poly.GetProducerPort());
                        actor.SetMapper(mapper);
                        actor.VisibilityOn();
                        //_all_mappers.Add(mapper);
                        _all_actors.Add(actor);
                        _renderer.AddActor(actor);
            }

    */

     //vtkRenderer _renderer = vtkRenderer.New();
    //vtkRenderWindowInteractor _render_window_interactor = vtkRenderWindowInteractor.New();
    //_renderControl.RenderWindow.AddRenderer(_renderer);
    //_render_window_interactor.SetRenderWindow(_renderControl.RenderWindow);

    /*vtkRenderer _renderer = _renderControl.RenderWindow.GetRenderers().GetFirstRenderer();
   for (int i = 1; i < 4; i++)
    {
        vtkSphereSource sphere0 = vtkSphereSource.New();
        //double test = sphere.GetRadius();
        sphere0.SetRadius(0.001);
        sphere0.SetCenter(0.05*i, 0.05*i, 0.05*i);
        maceMapper = vtkPolyDataMapper.New();
        maceMapper.SetInputConnection(sphere0.GetOutputPort());
        maceActor = vtkLODActor.New();
        maceActor.SetMapper(maceMapper);
        maceActor.VisibilityOn();
        _renderer.AddActor(maceActor);
    }*/

   /* vtkRenderer _renderer = vtkRenderer.New();
    vtkRenderWindowInteractor _render_window_interactor = vtkRenderWindowInteractor.New();
    _renderControl.RenderWindow.AddRenderer(_renderer);
    _render_window_interactor.SetRenderWindow(_renderControl.RenderWindow);*/
    
    /*vtkPolyData polydata = vtkPolyData.New();
    vtkCellArray _vertices = vtkCellArray.New();
    vtkPoints _points = vtkPoints.New();
    _points.InsertNextPoint(10, 10, 10);
    vtkVertex _vertex1 = vtkVertex.New();
    _vertex1.GetPointIds().SetId(0, 1);
    _vertices.InsertNextCell(_vertex1);
    //polydata.SetPoints(_points);
    //polydata.SetVerts(_vertices);
    vtkLine _line = vtkLine.New();
    _line.GetPointIds().SetId(0, 0);
    _line.GetPointIds().SetId(10, 10);
    vtkCellArray _cellArray = vtkCellArray.New();
    _cellArray.InsertNextCell(_line);
    polydata.SetLines(_cellArray);
    vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
    mapper.SetInputConnection(polydata.GetProducerPort());           
 maceActor = vtkLODActor.New();
 maceActor.SetMapper(mapper);
 maceActor.VisibilityOn();*/
 //_renderer.AddActor(maceActor); 


            //_renderControl.RenderWindow.GetInteractor().Start();
            //_renderer.Render();
            _renderControl.RenderWindow.Render();
            //_renderControl.RenderWindow.GetInteractor().Start();
            //_renderControl.RenderWindow.GetInteractor().Render(); 
           
    /*if (_all_polydata != null)
            {
                foreach (vtkPolyData poly in _all_polydata)
                {
                    poly.Dispose();
                }
            }
            if (_all_actors != null)
            {
                foreach (vtkActor act in _all_actors)
                {
                    act.Dispose();
                }
            }
            if (_all_vertices != null)
            {
                foreach (vtkCellArray vertices in _all_vertices)
                {
                    vertices.Dispose();
                }
            }
            if (_all_points != null)
            {
                foreach (vtkPoints point in _all_points)
                {
                    point.Dispose();
                }
            }

            if (polydata != null) { polydata.Dispose(); }
            if (_renderer != null) { _renderer.Dispose(); }
            if (_render_window_interactor != null) { _render_window_interactor.Dispose(); }
            if (_renderControl.RenderWindow != null) { _renderControl.RenderWindow.Dispose(); }*/
}



private void mnu_Save_Ann_Click(object sender, RoutedEventArgs e)
{

}

private void btn_Update_3D_Rendering_Click(object sender, RoutedEventArgs e)
{
    DisplayMode displayMode;
    if ((bool)chb_3D_Display_mode.IsChecked)
    {
        displayMode = DisplayMode.BODY;
    }
    else
    {
        displayMode = DisplayMode.POINT;
    }
    _DrawPolygonTest2(pnl_SharpOpenGL.OpenGL);
    //_Update_3D_Rendering3(displayMode);
}

private void chb_3D_Display_Checked(object sender, RoutedEventArgs e)
{
    if ((bool)chb_3D_Display.IsChecked)
    {//XXX temporaryily turning this off for test
        //wfh_renderControl.Visibility = Visibility.Visible;
        grd_SharpGLControl.Visibility = Visibility.Visible;
    }
    else
    {
        //wfh_renderControl.Visibility = Visibility.Collapsed;
        grd_SharpGLControl.Visibility = Visibility.Collapsed;
    }
}

private void Window_Loaded(object sender, RoutedEventArgs e)
{
    // "zoom out" to view the objects. This is a bit of a hack to see the objects without having to zoom out manually
    //_renderControl.RenderWindow.GetRenderers().GetFirstRenderer().GetActiveCamera().SetPosition(0, 0, 10);
}
float rotatePyramid = 0;
float rquad = 0;

private void OpenGLControl_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
{

    //_DrawTestOrg(pnl_SharpOpenGL.OpenGL);
    _DrawTriangleTest1(pnl_SharpOpenGL.OpenGL);
}

private void _DrawTestOrg(OpenGL gl)
{
    //  Clear the color and depth buffers.
    gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

    //  Reset the modelview matrix.
    gl.LoadIdentity();

    //  Move the geometry into a fairly central position.
    gl.Translate(-0.5f, 0.0f, -6.0f);

    //  Draw a pyramid. First, rotate the modelview matrix.
    //gl.Rotate(rotatePyramid, 0.0f, 1.0f, 0.0f);

    //  Start drawing triangles.
    _DrawTriangleTest(gl);
    //  Reset the modelview.
    gl.LoadIdentity();

    //  Move into a more central position.
    gl.Translate(1.5f, 0.0f, -7.0f);

    //  Rotate the cube.
    //gl.Rotate(rquad, 1.0f, 1.0f, 1.0f);

    //  Provide the cube colors and geometry.
    gl.Begin(OpenGL.GL_POLYGON);

    gl.Color(0.0f, 1.0f, 0.0f);
    gl.Vertex(1.0f, 1.0f, -1.0f);
    gl.Vertex(-1.0f, 1.0f, -1.0f);
    gl.Vertex(-1.0f, 1.0f, 1.0f);
    gl.Vertex(1.0f, 1.0f, 1.0f);

    gl.Color(1.0f, 0.5f, 0.0f);
    gl.Vertex(1.0f, -1.0f, 1.0f);
    gl.Vertex(-1.0f, -1.0f, 1.0f);
    gl.Vertex(-1.0f, -1.0f, -1.0f);
    gl.Vertex(1.0f, -1.0f, -1.0f);

    gl.Color(1.0f, 0.0f, 0.0f);
    gl.Vertex(1.0f, 1.0f, 1.0f);
    gl.Vertex(-1.0f, 1.0f, 1.0f);
    gl.Vertex(-1.0f, -1.0f, 1.0f);
    gl.Vertex(1.0f, -1.0f, 1.0f);

    gl.Color(1.0f, 1.0f, 0.0f);
    gl.Vertex(1.0f, -1.0f, -1.0f);
    gl.Vertex(-1.0f, -1.0f, -1.0f);
    gl.Vertex(-1.0f, 1.0f, -1.0f);
    gl.Vertex(1.0f, 1.0f, -1.0f);

    gl.Color(0.0f, 0.0f, 1.0f);
    gl.Vertex(-1.0f, 1.0f, 1.0f);
    gl.Vertex(-1.0f, 1.0f, -1.0f);
    gl.Vertex(-1.0f, -1.0f, -1.0f);
    gl.Vertex(-1.0f, -1.0f, 1.0f);

    gl.Color(1.0f, 0.0f, 1.0f);
    gl.Vertex(1.0f, 1.0f, -1.0f);
    gl.Vertex(1.0f, 1.0f, 1.0f);
    gl.Vertex(1.0f, -1.0f, 1.0f);
    gl.Vertex(1.0f, -1.0f, -1.0f);

    gl.End();

    //  Flush OpenGL.
    gl.Flush();

    //  Rotate the geometry a bit.
    rotatePyramid += 3.0f;
    rquad -= 3.0f;

}
private void _DrawPolygonTest2(OpenGL gl)
{

    //  Clear the color and depth buffers.
    gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

    //  Reset the modelview matrix.
    gl.LoadIdentity();

    //  Move the geometry into a fairly central position.
    gl.Translate(-0.5f, 0.0f, -6.0f);

    gl.Perspective(_gl_fovy, 1, 0.1, 100.0);

    //  Draw a pyramid. First, rotate the modelview matrix.
    gl.Rotate(-_gl_rotateY, 0.0f, 1.0f, 0.0f);
    gl.Rotate(-_gl_rotateX, 1.0f, 0.0f, 0.0f);
    gl.Rotate(-_gl_rotateZ, 0.0f, 0.0f, 1.0f);


    //  Start drawing triangles.

    gl.Begin(OpenGL.GL_LINE);

    double[] tempPointA = new double[3];
    double[] tempPointB = new double[3];
    double[] tempPointFirst = new double[3];
    double area = 0;
    int i = 0;
    foreach (Slice slice in m_Series.stacks[0].slices)
    {
        if (slice.sliceNo == 10)
        {
            foreach (Segment segment in slice.segments)
            {
                if (segment.segmentNo == 1)
                {
                    foreach (EmbryoSegmenter.Shapes.Point point in segment.points)
                    {

                        if (i == 0) //first round
                        {
                            tempPointA[0] = point.X; tempPointA[1] = point.Y; tempPointA[2] = point.Z;
                            tempPointFirst[0] = point.X; tempPointFirst[1] = point.Y; tempPointFirst[2] = point.Z;
                        }

                        else
                        {
                            tempPointB[0] = point.X; tempPointB[1] = point.Y; tempPointB[2] = point.Z;
                            //Do the stuff

                            gl.Color(1.0f, 0.0f, 0.0f);
                            gl.Vertex(tempPointB[0], tempPointB[1], tempPointB[2]);
                            gl.Color(0.0f, 0.0f, 1.0f);
                            gl.Vertex(tempPointA[0], tempPointA[1], tempPointA[2]);
                           tempPointA[0] = tempPointB[0]; tempPointA[1] = tempPointB[1]; tempPointA[2] = tempPointB[2];

                        }
                        i++;
                        if (i == segment.nbrOfPoints) //last round
                        {
                            gl.Color(1.0f, 0.0f, 0.0f);
                            gl.Vertex(tempPointFirst[0], tempPointFirst[1], tempPointFirst[2]);
                            gl.Color(0.0f, 0.0f, 1.0f);
                            gl.Vertex(tempPointA[0], tempPointA[1], tempPointA[2]);
                           

                        }
                    }
                }
            }
        }
    }


    gl.End();
    
    
    
    //  Reset the modelview.
    gl.LoadIdentity();



    //  Flush OpenGL.
    gl.Flush();
}

private void _DrawTriangleTest1(OpenGL gl)
{

    //  Clear the color and depth buffers.
    gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

    //  Reset the modelview matrix.
    gl.LoadIdentity();

    //  Move the geometry into a fairly central position.
    gl.Translate(-0.5f, 0.0f, -6.0f);

    gl.Perspective(_gl_fovy, 1, 0.1, 100.0);

    gl.PixelZoom((float)zoomGL, (float)zoomGL);
    //  Draw a pyramid. First, rotate the modelview matrix.
    gl.Rotate(-_gl_rotateY, 0.0f, 1.0f, 0.0f);
    gl.Rotate(-_gl_rotateX, 1.0f, 0.0f, 0.0f);
    gl.Rotate(-_gl_rotateZ, 0.0f, 0.0f, 1.0f);

    
    //  Start drawing triangles.
    _DrawTriangleTest(gl);
    //  Reset the modelview.
    gl.LoadIdentity();

 

    //  Flush OpenGL.
    gl.Flush();
}

private void _DrawLineTest2(OpenGL gl)
{

    //  Clear the color and depth buffers.
    gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

    //  Reset the modelview matrix.
    gl.LoadIdentity();

    //  Move the geometry into a fairly central position.
    gl.Translate(-0.5f, 0.0f, -6.0f);

    gl.Perspective(_gl_fovy, 1, 0.1, 100.0);

    gl.PixelZoom((float)zoomGL, (float)zoomGL);
    //  Draw a pyramid. First, rotate the modelview matrix.
    gl.Rotate(-_gl_rotateY, 0.0f, 1.0f, 0.0f);
    gl.Rotate(-_gl_rotateX, 1.0f, 0.0f, 0.0f);
    gl.Rotate(-_gl_rotateZ, 0.0f, 0.0f, 1.0f);


    //  Start drawing triangles.
    _DrawLineTest(gl);
    //  Reset the modelview.
    gl.LoadIdentity();



    //  Flush OpenGL.
    gl.Flush();
}

        private void _DrawTriangleTest(OpenGL gl)
        {
             gl.Begin(OpenGL.GL_TRIANGLES);

    gl.Color(1.0f, 0.0f, 0.0f);
    gl.Vertex(0.0f, 1.0f, 0.0f);
    gl.Color(0.0f, 1.0f, 0.0f);
    gl.Vertex(-1.0f, -1.0f, 1.0f);
    gl.Color(0.0f, 0.0f, 1.0f);
    gl.Vertex(1.0f, -1.0f, 1.0f);

    gl.Color(1.0f, 0.0f, 0.0f);
    gl.Vertex(0.0f, 1.0f, 0.0f);
    gl.Color(0.0f, 0.0f, 1.0f);
    gl.Vertex(1.0f, -1.0f, 1.0f);
    gl.Color(0.0f, 1.0f, 0.0f);
    gl.Vertex(1.0f, -1.0f, -1.0f);

    gl.Color(1.0f, 0.0f, 0.0f);
    gl.Vertex(0.0f, 1.0f, 0.0f);
    gl.Color(0.0f, 1.0f, 0.0f);
    gl.Vertex(1.0f, -1.0f, -1.0f);
    gl.Color(0.0f, 0.0f, 1.0f);
    gl.Vertex(-1.0f, -1.0f, -1.0f);

    gl.Color(1.0f, 0.0f, 0.0f);
    gl.Vertex(0.0f, 1.0f, 0.0f);
    gl.Color(0.0f, 0.0f, 1.0f);
    gl.Vertex(-1.0f, -1.0f, -1.0f);
    gl.Color(0.0f, 1.0f, 0.0f);
    gl.Vertex(-1.0f, -1.0f, 1.0f);

    gl.End();
        }

        private void _DrawLineTest(OpenGL gl)
        {
            gl.Begin(OpenGL.GL_LINE);

            gl.Color(1.0f, 0.0f, 0.0f);
            gl.Vertex(0.0f, 1.0f, 0.0f);
            gl.Color(0.0f, 1.0f, 0.0f);
            gl.Vertex(-1.0f, -1.0f, 1.0f);
            gl.Color(0.0f, 0.0f, 1.0f);
            gl.Vertex(1.0f, -1.0f, 1.0f);

            gl.Color(1.0f, 0.0f, 0.0f);
            gl.Vertex(0.0f, 1.0f, 0.0f);
            gl.Color(0.0f, 0.0f, 1.0f);
            gl.Vertex(1.0f, -1.0f, 1.0f);
            gl.Color(0.0f, 1.0f, 0.0f);
            gl.Vertex(1.0f, -1.0f, -1.0f);

            gl.Color(1.0f, 0.0f, 0.0f);
            gl.Vertex(0.0f, 1.0f, 0.0f);
            gl.Color(0.0f, 1.0f, 0.0f);
            gl.Vertex(1.0f, -1.0f, -1.0f);
            gl.Color(0.0f, 0.0f, 1.0f);
            gl.Vertex(-1.0f, -1.0f, -1.0f);

            gl.Color(1.0f, 0.0f, 0.0f);
            gl.Vertex(0.0f, 1.0f, 0.0f);
            gl.Color(0.0f, 0.0f, 1.0f);
            gl.Vertex(-1.0f, -1.0f, -1.0f);
            gl.Color(0.0f, 1.0f, 0.0f);
            gl.Vertex(-1.0f, -1.0f, 1.0f);

            gl.End();
        }

private void OpenGLControl_Resized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
{
    // Get the OpenGL instance.
    OpenGL gl = args.OpenGL;

    // Load and clear the projection matrix.
    gl.MatrixMode(OpenGL.GL_PROJECTION);
    gl.LoadIdentity();

    // Perform a perspective transformation
    gl.Perspective(45.0f, (float)gl.RenderContextProvider.Width /
        (float)gl.RenderContextProvider.Height,
        0.1f, 100.0f);

    // Load the modelview.
    gl.MatrixMode(OpenGL.GL_MODELVIEW);
}

private void OpenGLControl_OpenGLInitialized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
{
    //  Enable the OpenGL depth testing functionality.
    args.OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
}



private void OpenGLControl_MouseMove(object sender, MouseEventArgs e)
{
   
}

private void Window_KeyUp(object sender, KeyEventArgs e)
{
    // Get the OpenGL instance.
    OpenGL gl = pnl_SharpOpenGL.OpenGL;
    switch (e.Key)
    {

        case Key.P:
            _gl_fovy += 4;
            _DrawLineTest2(gl);
            break;

        case Key.L:
            _gl_fovy -= 4;
            _DrawLineTest2(gl);
            break;

        case Key.Z:
            zoomGL += 10;
            _DrawPolygonTest2(gl);
            break;

        case Key.E:
            _gl_rotateX += 10f;
            _DrawPolygonTest2(gl);
            break;
        case Key.R:
            _gl_rotateY += 10f;
            _DrawPolygonTest2(gl);
            break;
        case Key.T:
            _gl_rotateZ += 10f;
            _DrawPolygonTest2(gl);
            break;
    }

   
   
}
double _gl_rotateX = 0;
double _gl_rotateY = 0;
double _gl_rotateZ = 0;
double _gl_fovy = 45;
        double zoomGL = 0;
    }
    public enum DisplayMode
    {
        POINT = 0, BODY = 1
    } 
}

