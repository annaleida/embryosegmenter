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
using System.Windows.Shapes;
using System.Windows.Shapes;
using EmbryoSegmenter.Logging;
using EmbryoSegmenter.Shapes;
using System.IO;
using Microsoft.Win32;

namespace EmbryoSegmenter.Frames
{
    /// <summary>
    /// Interaction logic for SortWindow.xaml
    /// </summary>
    public partial class SortWindow : Window
    {
        //Manager for all loggers in project
        private static LoggerManager logManager;
        //Logger for this window
        private static Logger log;
        string txt_Path;
        public Stack m_Stack;
        List<Shapes.Point> m_DrawingPoints1;
        List<Shapes.Annotation> m_DrawingAnnotations1;
        List<Shapes.Point> m_DrawingPoints2;
        List<Shapes.Annotation> m_DrawingAnnotations2;
        int current1Slice = 1;
        int current2Slice = 1;
        bool initialized = false;
        
        public SortWindow(Stack stack,string path, int currentSlice)
        {
            InitializeComponent();
            m_Stack = stack;
            txt_Path = path;
            current1Slice = currentSlice;
            current2Slice = current1Slice + 1;
            //String test1 = EmbryoSegmenter.Properties.Settings.Default.LogFile;
            logManager = new LoggerManager(Properties.Settings.Default.LogFile.ToString());
            //string test = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
            log = logManager.CreateNewLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString());
            _Populate_cbx_Colors1(); _Populate_cbx_Colors2();
            _InitializeNewImage();
            _Populate_cbx_Slice1(); _Populate_cbx_Slice2();
            _ChangeToSlice(current1Slice, current2Slice);
            initialized = true;
            _LogStringDebug("MainWindow Initialized");
        }

#region Windows actions general

        private ComboBoxItem _FindCbxSliceItem(int sliceNo, int cbxNo)
        {
            if (cbxNo == 1)
            {
                foreach (ComboBoxItem item in cbx_Slice1.Items)
                {
                    if ((int)item.Tag == sliceNo)
                    {
                        return item;
                    }
                }
            }
            else if (cbxNo == 2)
            {
                foreach (ComboBoxItem item in cbx_Slice2.Items)
                {
                    if ((int)item.Tag == sliceNo)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

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

        private Segment _Find_segment(int sliceNumber, int segmentNumber)
        {
            _LogStringDebug("_Find_segment1");
            foreach (Slice s in m_Stack.slices)
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

        private Slice _Find_slice(int sliceNumber)
        {
            _LogStringDebug("_Find_slice");
            foreach (Slice s in m_Stack.slices)
            {
                if (s.sliceNo == sliceNumber)
                {
                    return s;
                }
            }
            return null;
        }

        private void _ChangeToSlice(int slice1, int slice2)
        {
            current1Slice = slice1;
            current2Slice = slice2;

            if (current1Slice > m_Stack.nbrOfSlices)
            { current1Slice = m_Stack.nbrOfSlices; }
            if (current2Slice > m_Stack.nbrOfSlices)
            { current2Slice = m_Stack.nbrOfSlices; }

            if (current1Slice < 1)
            { current1Slice = 1; }
            if (current2Slice < 1)
            { current2Slice = 1; }

            cbx_Slice1.SelectedItem = _FindCbxSliceItem(current1Slice, 1);
            cbx_Slice2.SelectedItem = _FindCbxSliceItem(current2Slice, 2);
            _UpdateImage1(current1Slice);
            _UpdateImage2(current2Slice);
        }
        private void _InitializeNewImage()
        {
            this.Title = "Embryo Sorter 2.0";
            _LogStringDebug("_InitializeNewImage");
            cbx_Slice1.Items.Clear();
            lbx_Segment1.Items.Clear();
            //ClearPanel();
            m_DrawingPoints1 = new List<Shapes.Point>();
            m_DrawingAnnotations1 = new List<Annotation>();

            cbx_Slice2.Items.Clear();
            lbx_Segment2.Items.Clear();
            //ClearPanel();
            m_DrawingPoints2 = new List<Shapes.Point>();
            m_DrawingAnnotations2 = new List<Annotation>();
        }
#endregion

        #region Window actions 1

        private void _UpdateImage1(int sliceNo)
        {

            _LogStringDebug("_UpdateImage1");
            Slice slice = _Find_slice(sliceNo);
            if (slice == null)
            {
                _LogStringDebug("_UpdateImage: No slices");
                return;
            }
            string fileName = txt_Path + "\\" + slice.fileName;
            if (!File.Exists(fileName))
            {
                _LogStringDebug("_UpdateImage: No file");
                return;
            }
            BitmapImage src = new BitmapImage();

            src.BeginInit();
            //TempImage.CacheOption = BitmapCacheOption.OnLoad;
            src.UriSource = new Uri(fileName);
            src.EndInit();

            BitmapSource bmp = new WriteableBitmap(src);
            pnl_Image1.Source = bmp;
        }

       
       

        public void _ClearPanel1()
        {
            _LogStringDebug("_ClearPanel1");
            foreach (DrawingVisual v in m_DrawingPoints1)
            {
                pnl_Panel1.DeleteVisual(v);
            }
            foreach (DrawingVisual v in m_DrawingAnnotations1)
            {
                pnl_Annotation1.DeleteVisual(v);
            }
            m_DrawingPoints1.Clear();
            m_DrawingAnnotations1.Clear();
        }

        private void _Populate_cbx_Slice1()
        {
            foreach (Slice _slice in m_Stack.slices)
            {
                ComboBoxItem cbxItem = new ComboBoxItem();
                cbxItem.Content = _slice.sliceNo;
                cbxItem.Tag = _slice.sliceNo;
                cbx_Slice1.Items.Add(cbxItem);
            }
        }

        private void _Populate_cbx_Colors1()
        {
            _LogStringDebug("_Populate_cbx_Colors");
            ComboBoxItem black = new ComboBoxItem(); black.Tag = Colors.Black; black.Content = "Black"; cbx_Color1.Items.Add(black);
            ComboBoxItem blue = new ComboBoxItem(); blue.Tag = Colors.Blue; blue.Content = "Blue"; cbx_Color1.Items.Add(blue);
            ComboBoxItem green = new ComboBoxItem(); green.Tag = Colors.Green; green.Content = "Green"; cbx_Color1.Items.Add(green);
            ComboBoxItem red = new ComboBoxItem(); red.Tag = Colors.Red; red.Content = "Red"; cbx_Color1.Items.Add(red);
            ComboBoxItem yellow = new ComboBoxItem(); yellow.Tag = Colors.Yellow; yellow.Content = "Yellow"; cbx_Color1.Items.Add(yellow);
            cbx_Color1.SelectedItem = blue;
        }

        private void _Populate_lbx_Segment1(int sliceNo)
        {
            _LogStringDebug("_Populate_lbx_Segment");
            lbx_Segment1.Items.Clear();
            foreach (Slice s in m_Stack.slices)
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
                        lbx_Segment1.Items.Add(lbxItem);
                    }
                    break;
                }
            }
            if (lbx_Segment1.Items.Count > 0)
            {
                lbx_Segment1.SelectAll();
                lbx_Segment1.Focus();
            }
        }

        private bool _CheckVisual1(DrawingVisual visual)
        {
            _LogStringDebug("_CheckVisual1");
            bool visualDrawn = false;
            foreach (DrawingVisual v in m_DrawingPoints1)
            {
                if (v.Equals(visual))
                { visualDrawn = true; }
            }
            if (!visualDrawn)
            {
                foreach (DrawingVisual v in m_DrawingAnnotations1)
                {
                    if (v.Equals(visual))
                    { visualDrawn = true; }
                }
            }
            return visualDrawn;
        }

        public void _DrawPoints1(Segment seg, Color color)
        {
            _LogStringDebug("_DrawPoints1");
            foreach (Shapes.Point p in seg.points)
            {
                if (!_CheckVisual1(p))
                {
                    if (_CheckVisual2(p)) //This segment exists in the other window
                    {
                        Shapes.Point p2 = new Shapes.Point();
                        p2.X = p.X; p2.Y = p.Y; p2.Z = p.Z;
                        p2.Draw(color);
                        pnl_Panel1.AddVisual(p2);
                        m_DrawingPoints1.Add(p2);
                    }
                    else
                    {
                        p.Draw(color);
                        pnl_Panel1.AddVisual(p);
                        m_DrawingPoints1.Add(p);
                    }
                }
                
            }
        }

        public void _DrawAnnotation1(Segment seg_, Color color)
        {
            _LogStringDebug("_DrawAnnotation1");
            if (!_CheckVisual1(seg_.annotation))
            {
                if (_CheckVisual2(seg_.annotation)) //This exists in the other window - need to make copy
                {
                    Annotation newAnnotation = new Annotation(seg_.annotation.text);
                    newAnnotation.DrawTextAtPoint(color, seg_.centerX, seg_.centerY);
                    pnl_Annotation1.AddVisual(newAnnotation);
                    m_DrawingAnnotations1.Add(newAnnotation);
                }
                else
                {
                    seg_.annotation.DrawTextAtPoint(color, seg_.centerX, seg_.centerY);
                    pnl_Annotation1.AddVisual(seg_.annotation);
                    m_DrawingAnnotations1.Add(seg_.annotation);
                }
            }
        }

        private void _RedrawSegment1()
        {
            _LogStringDebug("_RedrawSegment1");
            _ClearPanel1();
            if (lbx_Segment1.SelectedItem != null)
            {
                foreach (ListBoxItem item in lbx_Segment1.SelectedItems)
                {
                    int segmentNo = (int)((ListBoxItem)item).Tag;
                    int sliceNo = (int)((ListBoxItem)(cbx_Slice1.SelectedItem)).Tag;
                    Segment currentSegment = _Find_segment(sliceNo, segmentNo);
                     _DrawPoints1(currentSegment, (Color)((ComboBoxItem)cbx_Color1.SelectedItem).Tag);
                    _DrawAnnotation1(currentSegment, (Color)((ComboBoxItem)cbx_Color1.SelectedItem).Tag);
                }
            }
        }
        #endregion

        #region Window actions 2

        private void _UpdateImage2(int sliceNo)
        {
            _LogStringDebug("_UpdateImage2");
            Slice slice = _Find_slice(sliceNo);
            if (slice == null)
            {
                _LogStringDebug("_UpdateImage: No slices");
                return;
            }
            string fileName = txt_Path + "\\" + slice.fileName;
            if (!File.Exists(fileName))
            {
                _LogStringDebug("_UpdateImage: No file");
                return;
            }
            BitmapImage src = new BitmapImage();

            src.BeginInit();
            //TempImage.CacheOption = BitmapCacheOption.OnLoad;
            src.UriSource = new Uri(fileName);
            src.EndInit();

            BitmapSource bmp = new WriteableBitmap(src);
            pnl_Image2.Source = bmp;
        }

        public void _ClearPanel2()
        {
            _LogStringDebug("_ClearPanel2");
            foreach (DrawingVisual v in m_DrawingPoints2)
            {
                pnl_Panel2.DeleteVisual(v);
            }
            foreach (DrawingVisual v in m_DrawingAnnotations2)
            {
                pnl_Annotation2.DeleteVisual(v);
            }
            m_DrawingPoints2.Clear();
            m_DrawingAnnotations2.Clear();
        }

       
        private void _Populate_cbx_Colors2()
        {
            _LogStringDebug("_Populate_cbx_Colors");
            ComboBoxItem black = new ComboBoxItem(); black.Tag = Colors.Black; black.Content = "Black"; cbx_Color2.Items.Add(black);
            ComboBoxItem blue = new ComboBoxItem(); blue.Tag = Colors.Blue; blue.Content = "Blue"; cbx_Color2.Items.Add(blue);
            ComboBoxItem green = new ComboBoxItem(); green.Tag = Colors.Green; green.Content = "Green"; cbx_Color2.Items.Add(green);
            ComboBoxItem red = new ComboBoxItem(); red.Tag = Colors.Red; red.Content = "Red"; cbx_Color2.Items.Add(red);
            ComboBoxItem yellow = new ComboBoxItem(); yellow.Tag = Colors.Yellow; yellow.Content = "Yellow"; cbx_Color2.Items.Add(yellow);
            cbx_Color2.SelectedItem = blue;
        }

        private void _Populate_lbx_Segment2(int sliceNo)
        {
            _LogStringDebug("_Populate_lbx_Segment2");
            lbx_Segment2.Items.Clear();
            foreach (Slice s in m_Stack.slices)
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
                        lbx_Segment2.Items.Add(lbxItem);
                    }
                    break;
                }
            }
            if (lbx_Segment2.Items.Count > 0)
            {
                lbx_Segment2.SelectAll();
                lbx_Segment2.Focus();
            }
        }

        private void _Populate_cbx_Slice2()
        {
            foreach (Slice _slice in m_Stack.slices)
            {
                ComboBoxItem cbxItem = new ComboBoxItem();
                cbxItem.Content = _slice.sliceNo;
                cbxItem.Tag = _slice.sliceNo;
                cbx_Slice2.Items.Add(cbxItem);
            }
        }

        private bool _CheckVisual2(DrawingVisual visual)
        {
            _LogStringDebug("_CheckVisual2");
            bool visualDrawn = false;
            foreach (DrawingVisual v in m_DrawingPoints2)
            {
                if (v.Equals(visual))
                { visualDrawn = true; }
            }
            if (!visualDrawn)
            {
                foreach (DrawingVisual v in m_DrawingAnnotations2)
                {
                    if (v.Equals(visual))
                    { visualDrawn = true; }
                }
            }
            return visualDrawn;
        }

        public void _DrawPoints2(Segment seg, Color color)
        {
            _LogStringDebug("_DrawPoints2");
            foreach (Shapes.Point p in seg.points)
            {
                if (!_CheckVisual2(p))
                {
                    if (_CheckVisual1(p)) //This segment exists in the other window
                    {
                        Shapes.Point p2 = new Shapes.Point();
                        p2.X = p.X; p2.Y = p.Y; p2.Z = p.Z;
                        p2.Draw(color);
                        pnl_Panel2.AddVisual(p2);
                        m_DrawingPoints2.Add(p2);
                    }
                    else
                    {
                        p.Draw(color);
                        pnl_Panel2.AddVisual(p);
                        m_DrawingPoints2.Add(p);
                    }
                }
            }
        }

        public void _DrawAnnotation2(Segment seg_, Color color)
        {
            _LogStringDebug("_DrawAnnotation2");
            if (!_CheckVisual2(seg_.annotation))
            {
                if (_CheckVisual1(seg_.annotation)) //This exists in the other window - need to make copy
                {
                    Annotation newAnnotation = new Annotation(seg_.annotation.text);
                    newAnnotation.DrawTextAtPoint(color, seg_.centerX, seg_.centerY);
                    pnl_Annotation2.AddVisual(newAnnotation);
                    m_DrawingAnnotations2.Add(newAnnotation);
                }
                else
                {
                    seg_.annotation.DrawTextAtPoint(color, seg_.centerX, seg_.centerY);
                    pnl_Annotation2.AddVisual(seg_.annotation);
                    m_DrawingAnnotations2.Add(seg_.annotation);
                }
            }
        }

        private void _RedrawSegment2()
        {
            _LogStringDebug("_RedrawSegment2");
            _ClearPanel2();
            if (lbx_Segment2.SelectedItem != null)
            {
                foreach (ListBoxItem item in lbx_Segment2.SelectedItems)
                {
                    int segmentNo = (int)((ListBoxItem)item).Tag;
                    int sliceNo = (int)((ListBoxItem)(cbx_Slice2.SelectedItem)).Tag;
                    Segment currentSegment = _Find_segment(sliceNo, segmentNo);
                     _DrawPoints2(currentSegment, (Color)((ComboBoxItem)cbx_Color2.SelectedItem).Tag);
                    _DrawAnnotation2(currentSegment, (Color)((ComboBoxItem)cbx_Color2.SelectedItem).Tag);
                }
            }
        }
        #endregion
        
        #region Logging
        void _LogStringDebug(string logString)
        {
           
                log.Debug(logString);
           
        }
        #endregion Logging

        #region Window events

        private void cbx_Color1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //XX make one control
            _LogStringDebug("cbx_Color1_SelectionChanged");
            if (initialized)
            {
                _RedrawSegment1();
            }
        }

        private void lbx_Segment1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("lbx_Segment1_SelectionChanged");
            _RedrawSegment1();
        }
        
        private void chb_Display1_Checked(object sender, RoutedEventArgs e)
        {
            //Alter displaymode of image
            if (initialized)
            {
                if ((bool)chb_Annotation1.IsChecked)
                {
                    pnl_Annotation1.Visibility = Visibility.Visible;
                }
                else
                {
                    pnl_Annotation1.Visibility = Visibility.Hidden;
                }
                if ((bool)chb_Image1.IsChecked)
                {
                    pnl_Image1.Visibility = Visibility.Visible;
                }
                else
                {
                    pnl_Image1.Visibility = Visibility.Hidden;
                }
                if ((bool)chb_Outline1.IsChecked)
                {
                    pnl_Panel1.Visibility = Visibility.Visible;
                }
                else
                {
                    pnl_Panel1.Visibility = Visibility.Hidden;
                }
            }
        }

        private void cbx_Color2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("cbx_Color2_SelectionChanged");
            if (initialized)
            {
                _RedrawSegment2();
            }
        }

        private void mnu_Remove_Segment_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("Do you wish to remove all selected segments from both slices?", "Remove segments", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
            if (res == MessageBoxResult.No)
            {
                return;
            }

            _LogStringDebug("mnu_Remove_Segment_Click");
            if ((lbx_Segment1.SelectedItem == null) && (lbx_Segment2.SelectedItem == null))
            {
                return;
            }
            if ((cbx_Slice1.SelectedItem == null) && (cbx_Slice2.SelectedItem == null))
            {
                return;
            }
            _ClearPanel1(); _ClearPanel2();
            int sliceNo1 = (int)((ListBoxItem)(cbx_Slice1.SelectedItem)).Tag;
            Slice slice1_ = _Find_slice(sliceNo1);
            int sliceNo2 = (int)((ListBoxItem)(cbx_Slice2.SelectedItem)).Tag;
            Slice slice2_ = _Find_slice(sliceNo2);
            foreach (ListBoxItem lbx_seg_ in lbx_Segment1.SelectedItems)
            {
                int segNo = (int)lbx_seg_.Tag;
                List<Segment> seg_to_remove = new List<Segment>();
                foreach (Segment seg_ in slice1_.segments)
                {
                    if (seg_.segmentNo == segNo)
                    {
                        seg_to_remove.Add(seg_);
                    }
                }
                foreach (Segment seg_rem in seg_to_remove)
                {
                    slice1_.segments.Remove(seg_rem);
                }
            }
            foreach (ListBoxItem lbx_seg_ in lbx_Segment1.SelectedItems)
            {
                int segNo = (int)lbx_seg_.Tag;
                List<Segment> seg_to_remove = new List<Segment>();
                foreach (Segment seg_ in slice2_.segments)
                {
                    if (seg_.segmentNo == segNo)
                    {
                        seg_to_remove.Add(seg_);
                    }
                }
                foreach (Segment seg_rem in seg_to_remove)
                {
                    slice2_.segments.Remove(seg_rem);
                }
            }
            lbx_Segment1.Items.Clear(); lbx_Segment2.Items.Clear();
            _Populate_lbx_Segment1(sliceNo1); _Populate_lbx_Segment2(sliceNo2);
            if (lbx_Segment1.Items.Count > 0)
            {
                lbx_Segment1.SelectAll();
                lbx_Segment1.Focus();
            }
            _UpdateImage1(slice1_.sliceNo);
            if (lbx_Segment2.Items.Count > 0)
            {
                lbx_Segment2.SelectAll();
                lbx_Segment2.Focus();
            }
            _UpdateImage2(slice2_.sliceNo);
        }


        private void lbx_Segment2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("lbx_Segment2_SelectionChanged");
            _RedrawSegment2();
        }

        private void chb_Display2_Checked(object sender, RoutedEventArgs e)
        {
            //Alter displaymode of image
            if (initialized)
            {
                if ((bool)chb_Annotation2.IsChecked)
                {
                    pnl_Annotation2.Visibility = Visibility.Visible;
                }
                else
                {
                    pnl_Annotation2.Visibility = Visibility.Hidden;
                }
                if ((bool)chb_Image2.IsChecked)
                {
                    pnl_Image2.Visibility = Visibility.Visible;
                }
                else
                {
                    pnl_Image2.Visibility = Visibility.Hidden;
                }
                if ((bool)chb_Outline2.IsChecked)
                {
                    pnl_Panel2.Visibility = Visibility.Visible;
                }
                else
                {
                    pnl_Panel2.Visibility = Visibility.Hidden;
                }
            }
        }

        private void mnu_Rename_Segment_Click(object sender, RoutedEventArgs e)
        {
int nbrOfSelectedSegments = lbx_Segment1.SelectedItems.Count + lbx_Segment2.SelectedItems.Count;
            if (nbrOfSelectedSegments > 1)
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
                if (lbx_Segment1.SelectedItems.Count > 0 && newNumber != -1)
                {
                    int sliceNo = (int)((ListBoxItem)(cbx_Slice1.SelectedItem)).Tag;
                        foreach (ListBoxItem item in lbx_Segment1.SelectedItems)
                    {
                        int segmentNo = (int)((ListBoxItem)item).Tag;
                        foreach (Slice s in m_Stack.slices)
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
                    lbx_Segment1.Items.Clear();
                    _Populate_lbx_Segment1(sliceNo);
                }

                if (lbx_Segment2.SelectedItems.Count > 0 && newNumber != -1)
                {
                    int sliceNo = (int)((ListBoxItem)(cbx_Slice2.SelectedItem)).Tag;
                        foreach (ListBoxItem item in lbx_Segment2.SelectedItems)
                    {
                        int segmentNo = (int)((ListBoxItem)item).Tag;
                        foreach (Slice s in m_Stack.slices)
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
                    lbx_Segment2.Items.Clear();
                    _Populate_lbx_Segment2(sliceNo);
                }
                _RedrawSegment1(); _RedrawSegment2();
            }
        }

        private void cbx_Slice1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbx_Slice1.SelectedItem == null)
            { return; }
            int slice1 = (int)((ComboBoxItem)cbx_Slice1.SelectedItem).Tag;
            current1Slice = slice1;
            Slice slice_ = _Find_slice(slice1);
            _UpdateImage1(slice_.sliceNo);
            _Populate_lbx_Segment1(slice1);
        }

        private void cbx_Slice2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbx_Slice2.SelectedItem == null)
            { return; }
            int slice2 = (int)((ComboBoxItem)cbx_Slice2.SelectedItem).Tag;
            current2Slice = slice2;
            Slice slice_ = _Find_slice(slice2);
            _UpdateImage2(slice_.sliceNo);
            _Populate_lbx_Segment2(slice2);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
           DialogResult = true;
            this.Close();
        }

private void mnu_Add_Segment_Click(object sender, RoutedEventArgs e)
        {
            if (lbx_Segment1.SelectedItems.Count > 1 && lbx_Segment2.SelectedItems.Count > 1)
            {
                MessageBoxResult res = MessageBox.Show("You have selected segments from both slices. Only segments within the same slice will be added. Are you sure you want to continue?", "Add segments", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
                if (res == MessageBoxResult.No)
                { return; }
            }

            _LogStringDebug("mnu_Add_Segment_Click");
            

            if (cbx_Slice1.SelectedItem != null && lbx_Segment1.SelectedItems.Count > 0)
            {
                _ClearPanel1();
                int sliceNo1 = (int)((ListBoxItem)(cbx_Slice1.SelectedItem)).Tag;
                List<int> segments1 = new List<int>();
                foreach (ListBoxItem lbx_seg_ in lbx_Segment1.SelectedItems)
                {
                    segments1.Add((int)lbx_seg_.Tag);
                }
                _AddSegments(sliceNo1, segments1);
                lbx_Segment1.Items.Clear();
                _Populate_lbx_Segment1(sliceNo1);
                if (lbx_Segment1.Items.Count > 0)
                {
                    lbx_Segment1.SelectAll();
                    lbx_Segment1.Focus();
                }
                _UpdateImage1(sliceNo1);
            }

            if (cbx_Slice2.SelectedItem != null && lbx_Segment2.SelectedItems.Count > 0)
            {
                _ClearPanel2();
                 int sliceNo2 = (int)((ListBoxItem)(cbx_Slice2.SelectedItem)).Tag;
                List<int> segments2 = new List<int>();
                foreach (ListBoxItem lbx_seg_ in lbx_Segment2.SelectedItems)
                {
                    segments2.Add((int)lbx_seg_.Tag);
                }
                _AddSegments(sliceNo2, segments2);
                lbx_Segment2.Items.Clear();
                _Populate_lbx_Segment2(sliceNo2);
                if (lbx_Segment2.Items.Count > 0)
                {
                    lbx_Segment2.SelectAll();
                    lbx_Segment2.Focus();
                }
                _UpdateImage2(sliceNo2);
            }
           
        }

        #endregion Window events

private void btn_Previous_Click(object sender, RoutedEventArgs e)
{
    _ChangeToSlice(current1Slice - 1, current2Slice - 1);
}

private void btn_Next_Click(object sender, RoutedEventArgs e)
{
    _ChangeToSlice(current1Slice + 1, current2Slice + 1);
}

        
       
        

    }
}
