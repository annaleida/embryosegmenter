/////////////////////////////////////////////////////////////////////////////////////////////////
//
//	EnterStringWindow.xaml.cs
//
// Copyright (C) 2009 Phase Holographic Imaging AB. All Rights Reserved.
//
// The source code contained or described herein and all documents
// related to the source code are owned by Phase Holographic Imaging AB. 
// The source code is protected by worldwide copyright and trade secret laws and
// treaty provisions. No part of the source code may be used, copied,
// reproduced, modified, published, uploaded, posted, transmitted,
// distributed, or disclosed in any way.
//
/////////////////////////////////////////////////////////////////////////////////////////////////

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

namespace EmbryoSegmenter.Frames
{
    /// <summary>
    /// Interaction logic for AddNew.xaml hufytdf
    /// </summary>
	public partial class EnterNumberWindow : Window
    {
#if ENTERSTRINGWINDOWDEBUGPRINT
		private void DebugPrint(string text)
		{
			Console.WriteLine("EnterStringWindow:"+text);
		}
#else
		private void DebugPrint(string text)
		{
		}
#endif
        int newNumber;

	    public EnterNumberWindow()
        {
            InitializeComponent();
				}
			#region publicFunctions

			public int GetNumber()
			{
				return newNumber;
			}
			#endregion

			# region privateFunctions
			private void Window_Loaded(object sender, RoutedEventArgs e)
				{
					/*btnOk.IsEnabled = false;

					System.Drawing.Point position = System.Windows.Forms.Control.MousePosition;

					if (position.X >= System.Windows.Forms.Control.MousePosition.X - this.Width)
					{
						position.X = System.Windows.Forms.Control.MousePosition.X - (int)this.Width;
					}
					if (position.Y >= System.Windows.Forms.Control.MousePosition.Y - this.Height)
					{
						position.Y = System.Windows.Forms.Control.MousePosition.Y - (int)this.Height;
					}
                */
                    int top = System.Windows.Forms.Screen.AllScreens.ElementAt(0).Bounds.Top;
                    int left = System.Windows.Forms.Screen.AllScreens.ElementAt(0).Bounds.Left;
                    int width = System.Windows.Forms.Screen.AllScreens.ElementAt(0).Bounds.Width;
                    int height = System.Windows.Forms.Screen.AllScreens.ElementAt(0).Bounds.Height;
                    this.Left = left + width * 0.5 - this.Width * 0.5;
                    this.Top = top + height * 0.5 - this.Height * 0.5;

			        txtNew.Focus();
				}
			#endregion

			#region privateEvents

			private void btnOk_Click(object sender, RoutedEventArgs e)
        {
                int tempInt;
            if ((!txtNew.Text.Equals("")) && (int.TryParse(txtNew.Text, out tempInt)))
            {
                newNumber = tempInt;
                DialogResult = true;
                Close();
            }
            else
            {
                			MessageBox.Show("You must enter a number!");
                txtNew.Text = "";
            }
				}

			private void txtNew_TextChanged(object sender, TextChangedEventArgs e)
			{
				if (!txtNew.Text.Equals(""))
				{
					btnOk.IsEnabled = true;
				}
				else
				{
					btnOk.IsEnabled = false;
				}
			}

			private void btnCancel_Click(object sender, RoutedEventArgs e)
			{
				newNumber = -1;
				DialogResult = false;
				Close();
			}

			private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
			{
			}

			#endregion

			
				
    }
}
