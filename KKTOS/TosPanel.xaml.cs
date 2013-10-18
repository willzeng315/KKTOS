﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KKTOS
{
    public class Position
    {
        private Int32 row = -1;
        private Int32 column = -1;

        public static Position Empty
        {
            get
            {
                return new Position(-1, -1);
            }
        }

        public Int32 Row
        {
            get
            {
                return row;
            }
            set
            {
                row = value;
            }
        }

        public Int32 Column
        {
            get
            {
                return column;
            }
            set
            {
                column = value;
            }
        }

        public Int32 Y
        {
            get
            {
                return row;
            }
            set
            {
                row = value;
            }
        }

        public Int32 X
        {
            get
            {
                return column;
            }
            set
            {
                column = value;
            }
        }

        public Position()
        {
        }

        public Position(Int32 r, Int32 c)
        {
            row = r;
            column = c;
        }

        public Position(Position pos)
        {
            row = pos.row;
            column = pos.column;
        }
    }

    public partial class TosPanel : UserControl, INotifyPropertyChanged
    {
        #region OnPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public const Int32 OFFSET_BORDER = 3;
        public const Int32 BLOCK_ROW_COUNT = 5;
        public const Int32 BLOCK_COLUMN_COUNT = 6;
        public Int32 BLOCK_SIZE;
        public Int32 BLOCK_RADIUS;
        public const String BEAN_PATH_TEMPLATE = "Image/bead00{0}.png";
        private Position mPosPrevious = new Position(-1, -1);
        private Position mPosCurrent = new Position(-1, -1);
        private Int32[,] mVirtualMap = new Int32[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT];
        private Position[,] mVisualMap = new Position[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT];
        private Image[,] mBeansMap = new Image[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT]; // 記下每個格子的圖片
        private Double offsetX;
        private Double offsetY;
        private Point offsetPoint;
        Random mRandom = new Random();
        private Int32 nLastRow;
        private Int32 nLastCol;
        private BitmapImage[] beedImages;
        private Int32 mBeansMaxCount = 5;
        private DispatcherTimer Timer;
        private Boolean TimerStart = true;
        private const Int32 CountDownSecond = 2;
        private Int32 CountDown = CountDownSecond;

        public Int32 test;

        private Visibility manipulationComplete;
        public Visibility ManipulationComplete
        {
            get
            {
                return manipulationComplete;
            }
            set
            {
                SetProperty(ref manipulationComplete, value, "ManipulationComplete");
            }
        }

        public TosPanel()
        {
            InitializeComponent();

            ManipulationComplete = Visibility.Collapsed;
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += OnTimerTick;
            DataContext = this;

        }

        private void OnTimerTick(Object sender, EventArgs e)
        {

            //Debug.WriteLine(TimeLineBar.ActualWidth);
            Debug.WriteLine(CountDown);
            //TimeLineBar.Width -= 50;
            CountDown--;
            if (CountDown == 0)
            {
                ManipulationComplete = Visibility.Visible;
                TosSpace.ReleaseMouseCapture();
                LayoutRoot.ReleaseMouseCapture();
                //OnTosPanelManipulationCompleted(null, null);
            }
            Debug.WriteLine(CountDown);
        }

        /// <summary>
        /// 運算出每個格子的 Left、Top
        /// </summary>
        private void InitMapPosition()
        {
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    int nLeft = ((col + 3) * 5) + (col * BLOCK_SIZE) - 15;
                    int nTop = ((row + 3) * 5) + (row * BLOCK_SIZE) - 15;
                    mVisualMap[row, col] = new Position(nTop, nLeft);
                }
            }
        }

        /// <summary>
        /// 產生足夠的種子圖片元件到 GameSpace 裡面
        /// </summary>
        private void CreateBeansMap()
        {
            // 把 mBeansMap 依 mVisualMap 的座標排好
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    mBeansMap[row, col] = new Image();
                    mBeansMap[row, col].Width = BLOCK_SIZE;
                    mBeansMap[row, col].Height = BLOCK_SIZE;
                    mBeansMap[row, col].RenderTransformOrigin = new Point(0.5, 0.5);
                    ScaleTransform trans = new ScaleTransform();
                    mBeansMap[row, col].RenderTransform = trans;
                    TosSpace.Children.Add(mBeansMap[row, col]);
                    Canvas.SetLeft(mBeansMap[row, col], mVisualMap[row, col].X);
                    Canvas.SetTop(mBeansMap[row, col], mVisualMap[row, col].Y);
                }
            }
        }

        /// <summary>
        /// 將每個格子記錄的種子類型清空
        /// </summary>
        private void InitVirtualMap()
        {
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    mVirtualMap[row, col] = 0;
                }
            }
        }

        /// <summary>
        /// 將每個格子記錄的圖片清空
        /// </summary>
        private void InitBeansMap()
        {
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    int rand = mRandom.Next(mBeansMaxCount) + 1;
                    mBeansMap[row, col].Source = GetBeanImagePath(rand);
                    mVirtualMap[row, col] = rand;
                }
            }
        }

        private void InitialBitmapImage()
        {

            beedImages = new BitmapImage[5];
            for (int i = 0; i < 5; i++)
            {
                String strPath = String.Format(BEAN_PATH_TEMPLATE, i + 1);
                beedImages[i] = new BitmapImage(new Uri(strPath, UriKind.Relative));
            }

        }

        private BitmapImage GetBeanImagePath(Int32 type)
        {
            return beedImages[type - 1];
        }

        private Position GetCoordinate(Point pt)
        {
            Boolean bFind = false;
            Position ptRes = new Position(-1, -1);

            mPosPrevious.Row = 0;
            mPosPrevious.Column = 0;

            Int32 FUZZY_BLOCK_SIZE = BLOCK_SIZE + 2;
            for (Int32 row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (Int32 col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    // 讀出此列此欄的 Rect
                    Int32 nLeft = (Int32)mVisualMap[row, col].X - 1;
                    Int32 nTop = (Int32)mVisualMap[row, col].Y - 1;
                    Int32 nRight = nLeft + FUZZY_BLOCK_SIZE;
                    Int32 nBottom = nTop + FUZZY_BLOCK_SIZE;
                    // 是否在範圍內
                    if (pt.X >= nLeft && pt.Y >= nTop && pt.X <= nRight && pt.Y <= nBottom)
                    {
                        mPosPrevious.Row = mPosCurrent.Row;
                        mPosPrevious.Column = mPosCurrent.Column;
                        mPosCurrent.Row = row;
                        mPosCurrent.Column = col;
                        ptRes = mVisualMap[row, col];
                        bFind = true;
                        break;
                    }
                }
                if (bFind)
                {
                    break;
                }
            }

            return ptRes;
        }

        private void OnTosPanelManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            offsetPoint = new Point(offsetX - e.ManipulationOrigin.X, offsetY - e.ManipulationOrigin.Y);
        }

        private void OnTosPanelMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {

            Debug.WriteLine("GameSpace_MouseEnter");
            GetCoordinate(e.GetPosition(TosSpace));
            //Debug.WriteLine(e.GetPosition(GameSpace));
            //Debug.WriteLine(mVirtualMap[mPosCurrent.Row, mPosCurrent.Column]);
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Width = BLOCK_SIZE + 10;
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Height = BLOCK_SIZE + 10;
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform trans = new ScaleTransform();
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].RenderTransform = trans;

            //Debug.WriteLine(String.Format("{0},{1}", mPosCurrent.Y, mPosCurrent.X));
            offsetX = e.GetPosition(TosSpace).X;
            offsetY = e.GetPosition(TosSpace).Y;
            nLastRow = mPosCurrent.Row;
            nLastCol = mPosCurrent.Column;
        }

        private void OnTosPanelManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Width = BLOCK_SIZE;
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Height = BLOCK_SIZE;
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform trans = new ScaleTransform();
            mBeansMap[mPosCurrent.Row, mPosCurrent.Column].RenderTransform = trans;

            TimeLineBar.Width = (Int32)(LayoutRoot.ActualWidth);
            Timer.Stop();
            TimerStart = true;
            CountDown = CountDownSecond;
            Debug.WriteLine("TimerStop");
        }


        private void OnTosPanelManipulationDelta(Object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            UIElement el = (UIElement)sender;
            if (CountDown == 0)
            {
                el.ReleaseMouseCapture();
            }

            if (TimerStart)
            {
                Debug.WriteLine("TimerStart");
                TimerStart = false;
                Timer.Start();
            }
            Point realPoint = new Point(offsetPoint.X + e.ManipulationOrigin.X, offsetPoint.Y + e.ManipulationOrigin.Y);
            GetCoordinate(realPoint);

            if (Math.Abs(nLastRow - mPosCurrent.Row) == 1 || Math.Abs(nLastCol - mPosCurrent.Column) == 1)
            {
                //Debug.WriteLine("Change");
                int nBeanTypeTarget = mVirtualMap[nLastRow, nLastCol];
                mVirtualMap[nLastRow, nLastCol] = mVirtualMap[mPosCurrent.Row, mPosCurrent.Column];
                mVirtualMap[mPosCurrent.Row, mPosCurrent.Column] = nBeanTypeTarget;

                ImageSource iSource = mBeansMap[nLastRow, nLastCol].Source;

                mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Width = BLOCK_SIZE + 10;
                mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Height = BLOCK_SIZE + 10;
                mBeansMap[mPosCurrent.Row, mPosCurrent.Column].RenderTransformOrigin = new Point(0.5, 0.5);
                ScaleTransform trans = new ScaleTransform();
                mBeansMap[mPosCurrent.Row, mPosCurrent.Column].RenderTransform = trans;

                mBeansMap[nLastRow, nLastCol].Source = mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Source;
                mBeansMap[mPosCurrent.Row, mPosCurrent.Column].Source = iSource;


                mBeansMap[nLastRow, nLastCol].Width = BLOCK_SIZE;
                mBeansMap[nLastRow, nLastCol].Height = BLOCK_SIZE;
                mBeansMap[nLastRow, nLastCol].RenderTransformOrigin = new Point(0.5, 0.5);
                ScaleTransform trans2 = new ScaleTransform();
                mBeansMap[nLastRow, nLastCol].RenderTransform = trans;


                nLastRow = mPosCurrent.Row;
                nLastCol = mPosCurrent.Column;
            }
        }

        private void OnUserControlLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("UserControl_Loaded");
            Debug.WriteLine(LayoutRoot.ActualWidth);
            BLOCK_SIZE = (Int32)(LayoutRoot.ActualWidth/6.5);
            BLOCK_RADIUS = (BLOCK_SIZE / 2);

            TimeLineBar.Width = (Int32)(LayoutRoot.ActualWidth);
            InitialBitmapImage();
            InitMapPosition();
            InitVirtualMap();
            CreateBeansMap();
            InitBeansMap();
        }
    }
}