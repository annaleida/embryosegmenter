   M S B A M L   `   `   `  ���� D  @PHICommon, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null8*clr-namespace:PHICommon;assembly=PHICommon	PHICommon   EmbryoSegmenter'   !EmbryoSegmenter.Frames.SortWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     9my*clr-namespace:PHICommon;assembly=PHICommon       � Title$  
SortWindow��$	��750��$
��1200��.���  � RowDefinitions �� $��70 ��� $��* ��� $��70 �.+�� -   $��
grp_Header��$��0��.+�o� -   $��	_mnu_Main��$��Stretch=�$	��Top=�.@�m� $
��File��{�.  m� $��Slice��{�.  m� $��Segment��.@�m� -   $��mnu_Remove_Segment��$��Remove Segments�� � ToolTip$' !Remove segment from current slice��m� -   $��mnu_Divide_Segment��$��Divide Segment��$y�	Collapsed=�m� -   $��mnu_Add_Segment��$��Add Segments��$ Join selected segments��m� -   $��mnu_Rename_Segment��$��Rename Segment��$ Rename selected segments���� -   $��btn_Previous��$��Previous��$��30��$
��Left=�$��
480,34,0,0q�$	��Top=�$	��104���� -   $��
cbx_Slice1��$��23��$
��Left=�$��6,41,0,0q�$	��Top=�$	��313���� -	   $��
cbx_Slice2��$��23��$
��Left=�$��
859,41,0,0q�$	��Top=�$	��313���� -
   $��btn_Next��$
��Next��$��30��$
��Left=�$��
596,34,0,0q�$	��Top=�$	��104��� -   $��grp_Main��$��1�� � ColumnDefinitions �� $	��590 ��� $	��590 �.+�� -   $��grd_Display1��$��0��.+��� -   $��
pnl_Image1��$
��Fill=�$��
5,10,28,60q�$
��Left=�$	��Top=�$	��512��$	��512��   PHICommon.DrawingCanvas  -   $��
pnl_Panel1��
������ $��
5,10,28,60q�$
��Left=�$	��Top=�$	��512��$	��512��  -   $��pnl_Annotation1��
������ $��
5,10,28,60q�$y�Hidden=�$
��Left=�$	��Top=�$	��512��$	��512���� -   $��
cbx_Color1��$
��Left=� �� 
IsEditable .  $��	95,0,0,20q�$��86��$��22��$��Bottom=��� -   $��label1��$��Outline color:��$
��Left=�$��	10,0,0,18q�$��95��$��27��$��Bottom=��� -   $��chb_Annotation1��$��Annotations��$��22��$
��Left=�$��
429,0,0,20q�$��Bottom=�$��93���� -   $��chb_Outline1��$��Outline�� d� 	IsChecked$
 TrueY�$��22��$
��Left=�$��
354,0,0,20q�$��Bottom=�$��69���� -   $��
chb_Image1��$��Image��$
 TrueY�$
��Left=�$��
296,0,0,20q�$��61��$��22��$��Bottom=��� -   $��lbx_Segment1��$	��478��$��6,44,6,0q� �� SelectionMode$ Multiple=�$	��Top=�$��Right=�$��63���� -   $��label5��$��Segment:��$��27��$��Right=�$��6,10,6,0q�$	��Top=�$��63��� -   $��grd_Display2��$��1��.+��� -   $��
pnl_Image2��$
��Fill=�$��	75,10,0,0q�$
��Left=�$	��Top=�$	��512��$	��512��  -   $��
pnl_Panel2��
������ $��	75,10,0,0q�$
��Left=�$	��Top=�$	��512��$	��512��  -   $��pnl_Annotation2��
������ $��	75,10,0,0q�$y�Hidden=�$
��Left=�$	��Top=�$	��512��$	��512���� -   $��
cbx_Color2��$
��Left=� .  $��
161,0,0,20q�$��86��$��22��$��Bottom=��� -   $��label2��$��Outline color:��$
��Left=�$��	76,0,0,18q�$��95��$��27��$��Bottom=��� -   $��chb_Annotation2��$��Annotations��$��22��$
��Left=�$��
495,0,0,20q�$��Bottom=�$��93���� -   $��chb_Outline2��$��Outline��$
 TrueY�$��22��$
��Left=�$��
420,0,0,20q�$��Bottom=�$��69���� -   $��
chb_Image2��$��Image��$
 TrueY�$
��Left=�$��
362,0,0,20q�$��61��$��22��$��Bottom=��� -    $��lbx_Segment2��$	��478��$��6,44,0,0q�$ Multiple=�$	��Top=�$
��Left=�$��63���� -!   $��label4��$��Segment:��$��27��$
��Left=�$��6,10,0,0q�$	��Top=�$��63��� -"   $��
grd_Footer��$��2��.+��� -#   $��btnClose��$��Ok��$��30��$
��Left=�$��1053,20,0,0q�$	��Top=�$	��104��