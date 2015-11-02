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
using EmbryoSegmenter.Logging;
using EmbryoSegmenter.Pipelines;
using EmbryoSegmenter.Filters;

namespace EmbryoSegmenter.Frames
{
    /// <summary>
    /// Interaction logic for PipelineWindow.xaml
    /// </summary>
    public partial class PipelineWindow : Window
    {
        //Manager for all loggers in project
        private static LoggerManager logManager;
        //Logger for this window
        private static Logger log;
        public List<Pipeline> pipelines;
        

        public PipelineWindow()
        {
            InitializeComponent();
            logManager = new LoggerManager(Properties.Settings.Default.LogFile.ToString());
            //string test = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
            log = logManager.CreateNewLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString());
            _InitializePipelines();
        }

        private void _InitializePipelines()
        {
            pipelines = new List<Pipeline>();
            CreateFakePipeline();
            _Populate_cbx_Pipelines();
            //Replace this with file reading
        }
        
        private void _Populate_cbx_Pipelines()
        {
            _LogStringDebug("_Populate_cbx_Pipelines");
            cbx_Pipelines.Items.Clear();
            foreach (Pipelines.Pipeline pipe in pipelines)
            {
                ComboBoxItem cmb = new ComboBoxItem();
                cmb.Content = pipe.GetPipelineDescription();

                cmb.Tag = pipe.GetType().Name;
                cbx_Pipelines.Items.Add(cmb);
            }
            if (cbx_Pipelines.Items.Count > 0)
            {
                cbx_Pipelines.SelectedItem = cbx_Pipelines.Items[0];
                cbx_Pipelines.Focus();
            }
        }

        private void CreateFakePipeline()
        {
            TestPipeline tpl = new TestPipeline();
            tpl._InitializeFilterList("GradientMagnitureFilter,TestFilter");
            pipelines.Add(tpl);
        }

        private void _Populate_lbx_Filters(Pipeline pipeline)
        {
            _LogStringDebug("_Populate_lbx_Segment");
            lbx_Filters.Items.Clear();
            foreach ( Filter fil in pipeline.GetFilterList())
            {
                ListBoxItem lbxItem = new ListBoxItem();
                lbxItem.Content = fil.GetFilterDescription();
                lbxItem.Tag = fil.GetType().Name;
                lbx_Filters.Items.Add(lbxItem);
            }

            if (lbx_Filters.Items.Count > 0)
            {
                lbx_Filters.SelectedItem = lbx_Filters.Items[0];
                lbx_Filters.Focus();
            }
        }
        #region Logging
        void _LogStringDebug(string logString)
        {    
                log.Debug(logString);
        }
        #endregion Logging

        private Pipeline _FindPipeline(string name)
        {
            foreach (Pipeline p in pipelines)
            {
                if (p.GetType().Name == name)
                { return p; }
            }
            return null;
        }

        private void cbx_Pipelines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string pipelineName = ((ComboBoxItem)(cbx_Pipelines.SelectedItem)).Tag.ToString();
            Pipeline pipe = _FindPipeline(pipelineName);
            _Populate_lbx_Filters(pipe);
        }

        private void lbx_Filters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string filterName = ((ListBoxItem)(lbx_Filters.SelectedItem)).Tag.ToString();
            if (filterName == "GradientMagnitudeFilter")
            {
                GradientMagnitudeFilter.Visibility = Visibility.Visible;
            }
            else
            {
                GradientMagnitudeFilter.Visibility = Visibility.Hidden;
            }
        }

        
    }
}
