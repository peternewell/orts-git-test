   M S B A M L   `   `   `  ����  MenuWPF    MenuWPF.ProgressionWindow   5      \ XPresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    8x,http://schemas.microsoft.com/winfx/2006/xaml    5      >d2http://schemas.microsoft.com/expression/blend/2008    5      Hmc;http://schemas.openxmlformats.org/markup-compatibility/2006    6?   -   5          �Name$  Window��5       � Title$ Loading��5      $	��800��5      $	��150��6    � WindowStartupLocation$ CenterScreen=�6    � 
ResizeMode$ NoResize=�6@    � Icon$ ORTS.ico��6V   #��� ��6f   ���	#00000000 � WindowStyle$
 None=�6�    � AllowsTransparency . .��5
      �� -   5         ��Name$ brdMain��5
   
   
���   �$��1q�6/   ���	#7FFFFFFF �� CornerRadius$ 20��6Z   $��	0,0,20,20q�6l   	 J� Effect	 5      3  -System.Windows.Media.Effects.DropShadowEffect  5      
   
BlurRadius$
 10Q�6      ShadowDepth$ 10Q�6&   ._�5      � $��0,0,-5,0q�6	    � RowDefinitions 5      �� 5      $��40 �6   �� 5       � ColumnDefinitions 5      �� 5      $	��120 �6   .+�5      �� -   5         ��Name$ lblPath��5      $ ��Loading train database ...��6   $��Right=�6A   $��
0,0,-200,0q�6]   {�.  #��� ��6�   $
��Bold"�6�   
���   �$��16(�6�   $��Bottom=�6  $	��300��6  � 5      -   5         �Name$ progBar��5      $��30��6"   $��20,8,-637,0q�6.   $��1��6C   $	��Top=�6P   ��5      �� 5       �� EndPoint$ 0.5,1&�6    �� 
StartPoint$ 0.5,0&�6,   .��5      �  � Color$ 	#FF0090FF��6    � Offset$ 0Q�6'   � 5      $ 	#FF65FF00��6   $ 1Q�6'   5      5      5      5       5!      