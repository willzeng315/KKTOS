using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using KKTOS.Resources;
using System.Diagnostics;

namespace KKTOS
{
    public partial class MainPage : PhoneApplicationPage
    {
        // 建構函式
        public MainPage()
        {
            InitializeComponent();
            beedPanel.ComboCompleted += OnComboCompleted;
        }

        private void OnComboCompleted(Int32 combo)
        {
            ComboText.Text = String.Format("{0} Combo !!", combo);
        }

        private void beedPanel1_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {

            
        }
    }
}