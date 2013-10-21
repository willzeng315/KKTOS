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
        private List<List<Int32>> colorBeedCollection;
        // 建構函式
        public MainPage()
        {
            InitializeComponent();
            Debug.WriteLine("MainPage");

            for (int i = 100; i < 10; i++)
            {
                Debug.WriteLine(i);
            }
        }

        private void beedPanel1_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            Debug.WriteLine(beedPanel.test);
        }
    }
}