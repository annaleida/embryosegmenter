﻿#pragma checksum "..\..\..\..\Frames\PipelineWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "BFF3BF32DC2746DD84E3E2415D476288"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1022
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using EmbryoSegmenter.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace EmbryoSegmenter.Frames {
    
    
    /// <summary>
    /// PipelineWindow
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
    public partial class PipelineWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 7 "..\..\..\..\Frames\PipelineWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbx_Pipelines;
        
        #line default
        #line hidden
        
        
        #line 8 "..\..\..\..\Frames\PipelineWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListBox lbx_Filters;
        
        #line default
        #line hidden
        
        
        #line 9 "..\..\..\..\Frames\PipelineWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.GroupBox gbx_Parameters;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\..\Frames\PipelineWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal EmbryoSegmenter.Controls.par__GradientMagnitudeFilter GradientMagnitudeFilter;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/EmbryoSegmenter;component/frames/pipelinewindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Frames\PipelineWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.cbx_Pipelines = ((System.Windows.Controls.ComboBox)(target));
            
            #line 7 "..\..\..\..\Frames\PipelineWindow.xaml"
            this.cbx_Pipelines.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.cbx_Pipelines_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.lbx_Filters = ((System.Windows.Controls.ListBox)(target));
            
            #line 8 "..\..\..\..\Frames\PipelineWindow.xaml"
            this.lbx_Filters.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.lbx_Filters_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 3:
            this.gbx_Parameters = ((System.Windows.Controls.GroupBox)(target));
            return;
            case 4:
            this.GradientMagnitudeFilter = ((EmbryoSegmenter.Controls.par__GradientMagnitudeFilter)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

