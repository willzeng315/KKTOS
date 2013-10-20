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
            colorBeedCollection = new List<List<Int32>>();
            for (int i = 0; i < 5; i++)
            {
                List<Int32> a = new List<Int32>();
                colorBeedCollection.Add(a);
            }
            colorBeedCollection[0].Add(25345);
            colorBeedCollection[4].Add(223);

            // 將 ApplicationBar 當地語系化的程式碼範例
            //BuildLocalizedApplicationBar();
            Debug.WriteLine(colorBeedCollection[0][0]);
            Debug.WriteLine(colorBeedCollection[4][0]);

        }

        private void beedPanel1_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            Debug.WriteLine(beedPanel.test);
        }
    }
}