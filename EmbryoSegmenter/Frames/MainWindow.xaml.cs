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

        private double z_scale;
        private double xy_scale;
        string _current_Seg_File_Path = "";
        Series m_Series;
        int m_selected_slice_index;
        List<Shapes.Point> m_DrawingPoints;
        List<Shapes.Annotation> m_DrawingAnnotations;
        List<Pipeline> m_Pipelines;
        bool initialized = false;
        

        public MainWindow()
        {
            
            InitializeComponent();
            //String test1 = EmbryoSegmenter.Properties.Settings.Default.LogFile;
            logManager = new LoggerManager(Properties.Settings.Default.LogFile.ToString());
            //string test = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
            log = logManager.CreateNewLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString());
             _Populate_cbx_Colors();
            _InitializeNewImage();
            //Values given in µm/pixel
            txt_XY.Text = Properties.Settings.Default.XY_scale.ToString(); xy_scale = Properties.Settings.Default.XY_scale;
            txt_Z.Text = Properties.Settings.Default.Z_scale.ToString(); z_scale = Properties.Settings.Default.Z_scale;
            _RecomputeScale();

            initialized = true;
            _LogStringDebug("MainWindow Initialized");
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
                        foreach (Shapes.Point point in seg.points)
                        {
                            point.Z = rel_z_scale * sliceIndex;
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
            this.Title = "Embryo Segmenter 1.1";
            _LogStringDebug("_InitializeNewImage");
            lbx_Slice.Items.Clear();
            lbx_Segment.Items.Clear();
            //ClearPanel();
            m_Series = new Series();
            m_DrawingPoints = new List<Shapes.Point>();
            m_DrawingAnnotations = new List<Annotation>();
            _current_Seg_File_Path = "";

        }


        private void _ClearPanel()
        {
            _LogStringDebug("_ClearPanel");
            foreach (DrawingVisual v in m_DrawingPoints)
            {
                pnl_Panel.DeleteVisual(v);
            }
            foreach (DrawingVisual v in m_DrawingAnnotations)
            {
                pnl_Annotation.DeleteVisual(v);
            }
            m_DrawingPoints.Clear();
            m_DrawingAnnotations.Clear();
        }

        private void _Populate_lbx_Slice()
        {
            _LogStringDebug("_Populate_lbx_Slice");
            lbx_Slice.Items.Clear();
            Stack currentStack = _Find_selected_stack();
            if (currentStack != null)
            {
            foreach (Slice s in currentStack.slices)
            {
                ListBoxItem lbxItem = new ListBoxItem();
                lbxItem.Content = s.sliceNo;
                lbxItem.Tag = s.sliceNo;
                lbx_Slice.Items.Add(lbxItem);
            }
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
            cbx_Color.Items.Clear();
            ComboBoxItem black = new ComboBoxItem(); black.Tag = Colors.Black; black.Content = "Black"; cbx_Color.Items.Add(black);
            ComboBoxItem blue = new ComboBoxItem(); blue.Tag = Colors.Blue; blue.Content = "Blue"; cbx_Color.Items.Add(blue);
            ComboBoxItem green = new ComboBoxItem(); green.Tag = Colors.Green; green.Content = "Green"; cbx_Color.Items.Add(green);
            ComboBoxItem red = new ComboBoxItem(); red.Tag = Colors.Red; red.Content = "Red"; cbx_Color.Items.Add(red);
            ComboBoxItem yellow = new ComboBoxItem(); yellow.Tag = Colors.Yellow; yellow.Content = "Yellow"; cbx_Color.Items.Add(yellow);
            cbx_Color.SelectedItem = blue;
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

        public void _DrawPoints(Segment seg, Color color)
        {
            if (seg == null)
            {
            _LogStringDebug("_DrawPoints: Segment is null");
                return;
            }
            foreach (Shapes.Point p in seg.points)
            {
                if (!_CheckVisual(p))
                {
                    p.Draw(color);
                    pnl_Panel.AddVisual(p);
                    m_DrawingPoints.Add(p);
                }
            }
        }

        public void _DrawAnnotation(Segment seg_, Color color)
        {
            if (seg_ == null)
            {
                _LogStringDebug("_DrawAnnotation: Segment is null");
                return;
            }
                 if (!_CheckVisual(seg_.annotation))
                {
                   seg_.annotation.Draw(color, seg_.centerX, seg_.centerY);
                   pnl_Annotation.AddVisual(seg_.annotation);
                    m_DrawingAnnotations.Add(seg_.annotation);
                }
        }

        private bool _CheckVisual(DrawingVisual visual)
        {
           // _LogStringDebug("_CheckVisual");
            bool visualDrawn = false;
            foreach (DrawingVisual v in m_DrawingPoints)
            {
                if (v.Equals(visual))
                { visualDrawn = true; }
            }
            if (!visualDrawn)
            {
                foreach (DrawingVisual v in m_DrawingAnnotations)
                {
                    if (v.Equals(visual))
                    { visualDrawn = true; }
                }
            }
            return visualDrawn;
        }

        private System.Drawing.Bitmap _RunPipeline(string fileIn)
        {
            string filename = fileIn;

            Pipeline pl = PipelineManager.InitializePipeline(Pipelines.PipelineType.EMPTY);
            //Pipeline pl = new Pipelines.TestPipeline();
            pl.Filename_set(fileIn);
            pl.Start();
            string filename_test = pl.Filename_get();
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
            string fileNameIn = slice.filepath + "\\" + slice.fileName;
if (!File.Exists(fileNameIn))
            {
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

            pnl_Image.Source = bmp;
        }

        public BitmapSource CreateBitmapSourceFromBitmap(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
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
                    _DrawPoints(currentSegment, (Color)((ComboBoxItem)cbx_Color.SelectedItem).Tag);
                    _DrawAnnotation(currentSegment, (Color)((ComboBoxItem)cbx_Color.SelectedItem).Tag);
                }
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
		fileInfo += separator_;
		// Now add each of the Segment titles
		for(int i = 1; i <= maxID; i++)
		{
			fileInfo += "Segment " + i.ToString() + separator_;
		}
		fileInfo+="\n";
		// Each subsequent row is each of the slices
        if (!((currentStack.slices != null) || (currentStack == null)))
        {
            {
                foreach (Slice slice_ in currentStack.slices)
                {
                    // First column is the name of the slice
                    //Regardless of wether or not it has segments
                    fileInfo += "Slice: " + slice_.sliceNo.ToString();
                    fileInfo += separator_;
                    // Then each of the segments volumes.
                    // We need to make sure we are putting
                    // them in the right columns
                    if (slice_.segments != null)
                    {
                        foreach (Segment seg_ in slice_.segments)
                        {
                            // Get the segment id
                            int currentId = seg_.segmentNo;
                            int column = 1;

                            while (currentId != column)
                            {
                                // put in a blank
                                fileInfo += separator_;
                                column++;
                            }
                            // Now that we've got to the right place
                            // Insert the segments area

                            float segmentVolume = seg_.segmentArea * (float)xy_scale * (float)xy_scale * (float)z_scale;
                            string test = segmentVolume.ToString("00.00");
                            fileInfo += segmentVolume.ToString("00.00");
                        }
                    }
                    // We've finished looping through the segments
                    // add a new line
                    fileInfo += "\n";
                }
            }
        }
		// We've finished looping through the slices
		// Output the file
		StreamWriter sr = new StreamWriter(filePath, false);
             sr.Write(fileInfo);
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
            if (currentStack.filename == null)
            {
                MessageBox.Show("Stack filename does not exist!");
                return;
            }

            string folderPath = Directory.GetParent(currentStack.filename).FullName + "//OBJ//";

            //Create new OBJ-folder or empty the old one - only one folder/project!
            if (!Directory.Exists(folderPath))
            { Directory.CreateDirectory(folderPath); }
            else
            {
                foreach (string f in Directory.GetFiles(folderPath))
                {
                    File.Delete(f);
                }
            }
          
            //m_Stack.stackName = "640"; //Quickfix!!
            List<int> savedSegments = new List<int>();
            foreach (Slice slice in currentStack.slices)
            {
                foreach (Segment seg in slice.segments)
                {
                    if (!_IsInList(seg.segmentNo, savedSegments))
                    {
                        string fileName = folderPath + "//" + seg.segmentNo + ".obj";
                        _Write_obj(fileName, z_scale, xy_scale, seg.segmentNo);
                        savedSegments.Add(seg.segmentNo);
                    }
                    else
                    {

                    }
                }
            }
           
        }



        private void _Write_obj(string filename, double zScale, double xyScale, int segmentNumber)
        {
            double dbl_z_scale = zScale / xyScale;
            double dbl_xy_scale = xyScale;
            int int_z_scale = (int)(Math.Round(dbl_z_scale));
            int int_xy_scale = (int)(Math.Round(dbl_xy_scale));
            StreamWriter writer = new StreamWriter(filename, false);
            Stack currentStack = _Find_selected_stack();
            writer.WriteLine("# Segmentation file: " + currentStack.filename);
            writer.WriteLine("# Segmentation number: " + segmentNumber);
            writer.WriteLine("# Scale: " + dbl_xy_scale);

            writer.WriteLine("");
            int sliceIndex = 0; //Should be able to handle slices not marked in order
            foreach (Slice slice in currentStack.slices)
            {
                foreach (Segment seg in slice.segments)
                {
                    if (seg.segmentNo == segmentNumber)
                    {
                        foreach (Shapes.Point point in seg.points)
                        {
                            //This needs to be rewritten for the general case!!!
                            writer.WriteLine("v " + (int)(Math.Round(point.X)) + " " + (int)(Math.Round(point.Y)) + " " + int_z_scale * sliceIndex);
                        }
                    }
                }
                sliceIndex++;
            }
            writer.Close();
            writer.Dispose();
           
        }

        private void _Save_New_Seg_File()
        {
            _LogStringDebug("_Save_New_Seg_File");
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "seg";
            sfd.Filter = "SEG-files|*.seg";
            sfd.ShowDialog();
            if (!sfd.FileName.Equals(""))
            {
                _current_Seg_File_Path = sfd.FileName;
                _SaveSegFile(_current_Seg_File_Path);
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

        private bool _OpenSegFile()
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
            _current_Seg_File_Path = txt_File.Text;
            _ReadSegFile(_current_Seg_File_Path);
            
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

        private void _SaveSegFile(string seg_FilePath)
        {

            _LogStringDebug("_SaveSegFile");
            //ALM save only one stack for now
            Stack currentStack = _Find_selected_stack();
            currentStack.filename = seg_FilePath;
                FileStream sr = new FileStream(seg_FilePath, FileMode.Create, FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(sr);
                bw.Write(Convert.ToInt32(currentStack.nbrOfSlices));
                foreach (Slice slice_ in currentStack.slices)
                {
                    Slice newSlice = new Slice();
                    int index = currentStack.filename.LastIndexOf("\\");
                    string filePath = currentStack.filename.Substring(0, index + 1);
                    string fullFileName = filePath + slice_.fileName;
                    bw.Write(Convert.ToInt32(fullFileName.Length));
                    char[] c_array = fullFileName.ToCharArray();
                    for (int i = 0; i < fullFileName.Length; i++)
                    {
                        char c = c_array[i];
                        bw.Write(c);
                    }
                    bw.Write(Convert.ToInt32(slice_.sliceNo));
                    int segmentCount = 0;
                    if (slice_.segments != null) {segmentCount = slice_.segments.Count;}
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

         private void _ReadSegFile(string seg_FilePath)
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
                currentStack.stackName = "Stack 1";
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
                        for (int point_ = 1; point_ <= newSegment.nbrOfPoints; point_ ++)
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

        private void mnu_Save_As_Project_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_As_Project_Click");
            _Save_New_Seg_File();
        }

        private void mnu_Save_Project_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Save_Project_Click");
            if (File.Exists(_current_Seg_File_Path))
            {
                _SaveSegFile(_current_Seg_File_Path);
            }
            else
            {
                _Save_New_Seg_File();
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
                _Populate_scl_Time();
                _Populate_lbx_Slice();
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
                    stack.filename = "";
                    stack.segmentCount = 0;
                    stack.stackName = "";
                    stack.slices = new List<Slice>();
                }
                else
                {
                    stack = _Find_stack(stackIndex);
                }

                Slice s = new Slice();
                int index = file.LastIndexOf("\\");
                string fileName = file.Substring(index + 1, file.Length - index - 1);
                s.filepath = txt_Path.Text;
                s.fileName = fileName;
                s.sliceNo = sliceIndex;
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
            scl_Time.Value = 1;
            scl_Time.Maximum = m_Series.nbrOfStacks;
        }

        private void _Initiale_m_series()
        {
            m_Series = new Series();
            m_Series.seriesName = "";
            m_Series.calibration = new Calibration();
            m_Series.nbrOfStacks = 0;
            m_Series.stacks = new List<Stack>();

        }


        private void cbx_Color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _LogStringDebug("cbx_Color_SelectionChanged");
            if (initialized)
            {
                _RedrawSegment();
            }
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
            _ClearPanel();
            if (lbx_Slice.SelectedItem == null)
            {
                return;
            }
            int sliceNo = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
            Slice currentSlice = _Find_slice(sliceNo);
            _Populate_lbx_Segment(sliceNo);
            
           
            _UpdateImage(currentSlice.sliceNo);
        }

        private void mnu_Open_Project_Click(object sender, RoutedEventArgs e)
        {
            _LogStringDebug("mnu_Open_Project_Click");
            _InitializeNewImage();
            _Initiale_m_series();
            if (_OpenSegFile())
            {
                _SetImagePath();
                _Initate_scl_Time();
                _Populate_lbx_Slice();
                _Select_item(1);
               _UpdateImage(1);
            
            }
        }

        private void _Initate_scl_Time()
        {
            
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
         if ((bool)chb_Annotation.IsChecked)
         {
             pnl_Annotation.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Annotation.Visibility = Visibility.Hidden;
         }
         if ((bool)chb_Image.IsChecked)
         {
             pnl_Image.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Image.Visibility = Visibility.Hidden;
         }
         if ((bool)chb_Outline.IsChecked)
         {
             pnl_Panel.Visibility = Visibility.Visible;
         }
         else
         {
             pnl_Panel.Visibility = Visibility.Hidden;
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
         m_selected_slice_index = (int)((ListBoxItem)(lbx_Slice.SelectedItem)).Tag;
         _Populate_lbx_Slice();
         _Select_item(m_selected_slice_index);
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

 private void button1_Click(object sender, RoutedEventArgs e)
 {

     Pipeline pl = PipelineManager.InitializePipeline(Pipelines.PipelineType.EMPTY);
     BitmapImage src = new BitmapImage();

     //BitmapSource bmp = new WriteableBitmap(src);
     src.BeginInit();
     //TempImage.CacheOption = BitmapCacheOption.OnLoad;
     string test = pl.Filename_get().ToString();
     src.UriSource = new Uri(test);
     BitmapSource bmp = new WriteableBitmap(src);
     pnl_Image.Source = src;
 }

 private void mnu_Pipelines_Click(object sender, RoutedEventArgs e)
 {
     PipelineWindow pipeline = new PipelineWindow();
     if ((bool)pipeline.ShowDialog())
     {
         m_Pipelines = pipeline.pipelines;
     }
 }

       
        
         



       
        

        
       
    }
}
