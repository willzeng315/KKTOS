using System;
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
using System.Threading;
using System.Windows.Media.Animation;

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

    public class BeedChain
    {
        public BeedChain()
        {
            startPos = new Position();
            chainLength = 0;
            belongs = -1;
        }

        public Position startPos;
        public Int32 chainLength;
        public Int32 belongs;
    }

    public class BeedFall
    {
        public BeedFall()
        {
            Pos = new Position();
            FallCount = 0;
        }
        public Position Pos;
        public Int32 FallCount;
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

        private const Int32 BLOCK_ROW_COUNT = 5;
        private const Int32 BLOCK_COLUMN_COUNT = 6;
        private const Int32 MIN_CHAIN_LEN = 3;
        public Int32 BLOCK_SIZE;
        public Int32 BLOCK_RADIUS;
        public const String BEED_PATH_TEMPLATE = "Image/bead00{0}.png";
        private Position mPosPrevious = new Position(-1, -1);
        private Position mPosCurrent = new Position(-1, -1);
        private Int32[,] mVirtualMap = new Int32[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT];
        private Int32[,] mFallCountMap = new Int32[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT];
        private Position[,] mVisualMap = new Position[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT];
        private Image[,] mBeedsMap = new Image[BLOCK_ROW_COUNT, BLOCK_COLUMN_COUNT]; // 記下每個格子的圖片
        private Double offsetX;
        private Double offsetY;
        private Point offsetPoint;
        private BitmapImage[] beedImages;
        private const Int32 mBeedsMaxCount = 5;
        private DispatcherTimer Timer;
        private Boolean TimerStart = true;
        private const Int32 CountDownSecond = 20;
        private Int32 CountDown = CountDownSecond;
        private Boolean firstClick = true;
        private Image cursorImage;
        private UInt32 cursorShift = 20;
        private Random mRandom = new Random();
        private List<List<BeedChain>> mBeedChainSet;
        private List<List<BeedChain>> mBeedChainHorizontal;
        private List<List<BeedChain>> mBeedChainVertical;
        public List<List<Int32>> EachBeedChainCount;
        private List<List<Int32>> mEachBeedFallCountMap;
        private Int32[] mVHChain = new Int32[mBeedsMaxCount];
        private List<Boolean> mIsBeedChecked;
        private List<BeedFall> mBeedFallCollection = new List<BeedFall>();


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

        private void OnUserControlLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OnUserControlLoaded");
            Debug.WriteLine(LayoutRoot.ActualWidth);
            BLOCK_SIZE = (Int32)(LayoutRoot.ActualWidth / 6.0);
            BLOCK_RADIUS = (BLOCK_SIZE / 2);

            TimeLineBar.Width = (Int32)(LayoutRoot.ActualWidth);
            InitBeedChainSet();
            InitBeedImages();
            InitMapPosition();
            InitVirtualMap();
            ResetFallCountMap();
            CreateBeedsMap();
            InitBeedsMap();
            InitEachBeedFallCountMap();
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
            Debug.WriteLine(CountDown);
            //TimeLineBar.Width -= 50;
            CountDown--;
            if (CountDown == 0)
            {
                ManipulationComplete = Visibility.Visible;
                StartEliminateBeed();
            }
            Debug.WriteLine(CountDown);
        }

        private void InitEachBeedFallCountMap()
        {
            mEachBeedFallCountMap = new List<List<Int32>>();
            for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
            {
                List<Int32> nullBeedFallCount = new List<Int32>();
                mEachBeedFallCountMap.Add(nullBeedFallCount);
            }
        }

        private void InitBeedChainSet()
        {
            mBeedChainSet = new List<List<BeedChain>>();
            mBeedChainHorizontal = new List<List<BeedChain>>();
            mBeedChainVertical = new List<List<BeedChain>>();
            EachBeedChainCount = new List<List<Int32>>();
            for (int i = 0; i < mBeedsMaxCount; i++)
            {
                List<BeedChain> nullBeedChainHorizontal = new List<BeedChain>();
                List<BeedChain> nullBeedChainVertical = new List<BeedChain>();
                List<BeedChain> nullBeedChain = new List<BeedChain>();
                List<Int32> nullBeedChainCount = new List<Int32>();

                EachBeedChainCount.Add(nullBeedChainCount);
                mBeedChainSet.Add(nullBeedChain);
                mBeedChainHorizontal.Add(nullBeedChainHorizontal);
                mBeedChainVertical.Add(nullBeedChainVertical);
            }
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
                    int nLeft = (col * (BLOCK_SIZE));
                    int nTop = (row * (BLOCK_SIZE));
                    mVisualMap[row, col] = new Position(nTop, nLeft);
                }
            }
        }

        /// <summary>
        /// 產生足夠的種子圖片元件到 GameSpace 裡面
        /// </summary>
        private void CreateBeedsMap()
        {
            // 把 mBeansMap 依 mVisualMap 的座標排好
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    mBeedsMap[row, col] = new Image();
                    mBeedsMap[row, col].Width = BLOCK_SIZE;
                    mBeedsMap[row, col].Height = BLOCK_SIZE;
                    mBeedsMap[row, col].RenderTransformOrigin = new Point(0.5, 0.5);
                    ScaleTransform trans = new ScaleTransform();
                    mBeedsMap[row, col].RenderTransform = trans;
                    TosSpace.Children.Add(mBeedsMap[row, col]);
                    Canvas.SetLeft(mBeedsMap[row, col], mVisualMap[row, col].X);
                    Canvas.SetTop(mBeedsMap[row, col], mVisualMap[row, col].Y);
                }
            }
        }

        /// <summary>
        /// 將每個格子記錄的種子類型清空
        /// </summary>
        /// 
        private void ResetFallCountMap()
        {
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    mFallCountMap[row, col] = 0;
                }
            }
        }

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
        private void InitBeedsMap()
        {
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    Int32 rand = mRandom.Next(mBeedsMaxCount) + 1;
                    mBeedsMap[row, col].Source = GetBeedImage(rand);
                    mVirtualMap[row, col] = rand;
                }
            }
        }

        private void InitBeedImages()
        {
            beedImages = new BitmapImage[5];
            for (int i = 0; i < 5; i++)
            {
                String strPath = String.Format(BEED_PATH_TEMPLATE, i + 1);
                beedImages[i] = new BitmapImage(new Uri(strPath, UriKind.Relative));
            }
        }

        private BitmapImage GetBeedImage(Int32 type)
        {
            return beedImages[type - 1];
        }

        private Position GetCoordinate(Point pt)
        {
            Boolean bFind = false;
            Position ptRes = new Position(-1, -1);

            Int32 FUZZY_BLOCK_SIZE = BLOCK_SIZE + 1;
            for (Int32 row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                for (Int32 col = 0; col < BLOCK_COLUMN_COUNT; ++col)
                {
                    // 讀出此列此欄的 Rect
                    Int32 nLeft = (Int32)mVisualMap[row, col].X;
                    Int32 nTop = (Int32)mVisualMap[row, col].Y;
                    Int32 nRight = nLeft + FUZZY_BLOCK_SIZE;
                    Int32 nBottom = nTop + FUZZY_BLOCK_SIZE;
                    // 是否在範圍內
                    if (pt.X >= nLeft && pt.Y >= nTop && pt.X <= nRight && pt.Y <= nBottom)
                    {
                        //mPosPrevious.Row = mPosCurrent.Row;
                        //mPosPrevious.Column = mPosCurrent.Column;
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

        private void FindBeedChainHorizontal()
        {
            for (int row = 0; row < BLOCK_ROW_COUNT; ++row)
            {
                Int32 LastLength =0;
                for (int col = 0; col <= BLOCK_COLUMN_COUNT - MIN_CHAIN_LEN; ++col)
                {
                    if (LastLength > 1)
                    {
                        LastLength--;
                        continue;
                    }
                    Int32 type = mVirtualMap[row, col];
                    Int32 chainLength = 1;

                    for (int i = col + 1; i < BLOCK_COLUMN_COUNT; ++i)
                    {
                        if(mVirtualMap[row, i] == type)
                        {
                            chainLength++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (chainLength >= MIN_CHAIN_LEN)
                    {
                        LastLength = chainLength;
                        BeedChain beedChain = new BeedChain();
                        beedChain.startPos = new Position(row,col);
                        beedChain.chainLength = chainLength;
                        mBeedChainHorizontal[type-1].Add(beedChain);
                    }
                     
                }
            }
        }

        private void FindBeedChainVertical()
        {
            for (int col = 0; col < BLOCK_COLUMN_COUNT ; ++col)
            {
                Int32 LastLength = 0;
                for (int row = 0; row <= BLOCK_ROW_COUNT - MIN_CHAIN_LEN; ++row)
                {
                    if (LastLength > 1)
                    {
                        LastLength--;
                        continue;
                    }
                    Int32 type = mVirtualMap[row, col];
                    Int32 chainLength = 1;

                    for (int i = row + 1; i < BLOCK_ROW_COUNT; ++i)
                    {
                        if (mVirtualMap[i, col] == type)
                        {
                            chainLength++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (chainLength >= MIN_CHAIN_LEN)
                    {
                        LastLength = chainLength;
                        BeedChain beedChain = new BeedChain();
                        beedChain.startPos = new Position(row, col);
                        beedChain.chainLength = chainLength;
                        mBeedChainVertical[type - 1].Add(beedChain);
                    }

                }
            }
        }

        private void FindIntersectionHV()
        {
            for (int type = 0; type < mBeedsMaxCount; type++)
            {
                mVHChain[type] = 0;
                Int32 chainCount = 0;
                for (int i = 0; i < mBeedChainHorizontal[type].Count; i++)
                {
                    for (int j = 0; j < mBeedChainVertical[type].Count; j++)
                    {
                        Int32 vStartRow = mBeedChainVertical[type][j].startPos.Row;
                        Int32 vEndRow = mBeedChainVertical[type][j].startPos.Row + mBeedChainVertical[type][j].chainLength-1;
                        Int32 hStartCol = mBeedChainHorizontal[type][i].startPos.Column;
                        Int32 hEndCol = mBeedChainHorizontal[type][i].startPos.Column + mBeedChainHorizontal[type][i].chainLength - 1;

                        if (hStartCol <= mBeedChainVertical[type][j].startPos.Column && mBeedChainVertical[type][j].startPos.Column <= hEndCol
                            && vStartRow <= mBeedChainHorizontal[type][i].startPos.Row && mBeedChainHorizontal[type][i].startPos.Row <= vEndRow)
                        {
                            if (mBeedChainVertical[type][j].belongs == -1)
                            {
                                if (mBeedChainHorizontal[type][i].belongs == -1)
                                {
                                    mBeedChainVertical[type][j].belongs = chainCount;
                                    mBeedChainHorizontal[type][i].belongs = chainCount;
                                    EachBeedChainCount[type].Add(mBeedChainVertical[type][j].chainLength + mBeedChainHorizontal[type][i].chainLength - 1);
                                    chainCount++;
                                }
                                else
                                {
                                    mBeedChainVertical[type][j].belongs = mBeedChainHorizontal[type][i].belongs;
                                    EachBeedChainCount[type][mBeedChainVertical[type][j].belongs] += mBeedChainVertical[type][j].chainLength - 1;
                                }
                            }
                            else
                            {
                                mBeedChainHorizontal[type][i].belongs = mBeedChainVertical[type][j].belongs;
                                EachBeedChainCount[type][mBeedChainVertical[type][j].belongs] += mBeedChainHorizontal[type][i].chainLength - 1;
                            }

                        }
                    }
                }
                mVHChain[type] = chainCount;
            }
        }

        private void CountRemainChain()
        {
            for (int type = 0; type < mBeedsMaxCount; type++)
            {
                for (int i = 0; i < mBeedChainHorizontal[type].Count; i++)
                {
                    if (mBeedChainHorizontal[type][i].belongs == -1)
                    {
                        EachBeedChainCount[type].Add(mBeedChainHorizontal[type][i].chainLength);
                    }
                }
                for (int i = 0; i < mBeedChainVertical[type].Count; i++)
                {
                    if (mBeedChainVertical[type][i].belongs == -1)
                    {
                        EachBeedChainCount[type].Add(mBeedChainVertical[type][i].chainLength);
                    }
                }
            }

        }

        private Int32 delayMilliSecond = 350;
        private Int32 eliminateMilliSecond = 200;

        private void EliminateBeedAnimation(ScaleTransform imageTransForm, Int32 bMilliSecond)
        {
            DoubleAnimation animX = new DoubleAnimation();
            animX.From = 1;
            animX.To = 0;
            animX.Duration = new Duration(TimeSpan.FromMilliseconds(eliminateMilliSecond));
            DoubleAnimation animY = new DoubleAnimation();
            animY.From = 1;
            animY.To = 0;
            animY.Duration = new Duration(TimeSpan.FromMilliseconds(eliminateMilliSecond));
            Storyboard.SetTarget(animX, imageTransForm);
            Storyboard.SetTarget(animY, imageTransForm);
            Storyboard.SetTargetProperty(animX, new PropertyPath(ScaleTransform.ScaleXProperty));
            Storyboard.SetTargetProperty(animY, new PropertyPath(ScaleTransform.ScaleYProperty));
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animX);
            storyboard.Children.Add(animY);
            storyboard.BeginTime = new TimeSpan(0, 0, 0, 0, bMilliSecond);
            storyboard.Begin();
        }


        private void EliminateBeed()
        {
            Int32 beginMilliSecond = 0;
            for (int type = 0; type < mBeedsMaxCount; type++)
            {
                for (int i = 0; i < mBeedChainHorizontal[type].Count; i++)
                {
                    if (mBeedChainHorizontal[type][i].belongs == -1)
                    {
                        ScaleTransform imageTrans = new ScaleTransform();
                        for (int j = mBeedChainHorizontal[type][i].startPos.Column; j < mBeedChainHorizontal[type][i].startPos.Column + mBeedChainHorizontal[type][i].chainLength; j++)
                        {
                            mBeedsMap[mBeedChainHorizontal[type][i].startPos.Row, j].RenderTransform = imageTrans;
                            mVirtualMap[mBeedChainHorizontal[type][i].startPos.Row, j] = -1;
                        }
                        EliminateBeedAnimation(imageTrans, beginMilliSecond);
                    }
                    beginMilliSecond += delayMilliSecond;
                }
                for (int i = 0; i < mBeedChainVertical[type].Count; i++)
                {
                    if (mBeedChainVertical[type][i].belongs == -1)
                    {
                        ScaleTransform imageTrans = new ScaleTransform();
                        for (int j = mBeedChainVertical[type][i].startPos.Row; j < mBeedChainVertical[type][i].startPos.Row + mBeedChainVertical[type][i].chainLength; j++)
                        {
                            mBeedsMap[j, mBeedChainVertical[type][i].startPos.Column].RenderTransform = imageTrans;
                            mVirtualMap[j, mBeedChainVertical[type][i].startPos.Column] = -1;
                        }
                        EliminateBeedAnimation(imageTrans, beginMilliSecond);
                    }
                    beginMilliSecond += delayMilliSecond;
                }
                for (int belong = 0; belong < mVHChain[type]; belong++)
                {
                    ScaleTransform imageTrans = new ScaleTransform();
                    for (int i = 0; i < mBeedChainHorizontal[type].Count; i++)
                    {
                        if (mBeedChainHorizontal[type][i].belongs == belong)
                        {
                            for (int j = mBeedChainHorizontal[type][i].startPos.Column; j < mBeedChainHorizontal[type][i].startPos.Column + mBeedChainHorizontal[type][i].chainLength; j++)
                            {
                                mBeedsMap[mBeedChainHorizontal[type][i].startPos.Row, j].RenderTransform = imageTrans;
                                mVirtualMap[mBeedChainHorizontal[type][i].startPos.Row, j] = -1;
                            }
                        }
                    }
                    for (int i = 0; i < mBeedChainVertical[type].Count; i++)
                    {
                        if (mBeedChainVertical[type][i].belongs == belong)
                        {
                            for (int j = mBeedChainVertical[type][i].startPos.Row; j < mBeedChainVertical[type][i].startPos.Row + mBeedChainVertical[type][i].chainLength; j++)
                            {
                                mBeedsMap[j, mBeedChainVertical[type][i].startPos.Column].RenderTransform = imageTrans;
                                mVirtualMap[j, mBeedChainVertical[type][i].startPos.Column] = -1;
                            }
                        }
                    }
                    EliminateBeedAnimation(imageTrans, beginMilliSecond);
                    beginMilliSecond += delayMilliSecond;
                }
            }
        }

        private void MoveRemainBeeds(List<Position> mPath)
        {
            Position posSource = mPath[0];
            int nSourceRow = (int)posSource.Row;
            int nSourceColumn = (int)posSource.Column;
            int nSourcePositionX = (int)mVisualMap[nSourceRow, nSourceColumn].X - BLOCK_RADIUS;
            int nSourcePositionY = (int)mVisualMap[nSourceRow, nSourceColumn].Y - BLOCK_RADIUS;

            PointAnimationUsingKeyFrames frames = new PointAnimationUsingKeyFrames();
            Double dblSeconePosition = 0.0;
            for (int i = 0; i < mPath.Count; ++i)
            {
                KeyTime kTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(dblSeconePosition));
                Position posNew = mVisualMap[mPath[i].Row, mPath[i].Column];
                Point ptNew = new Point(posNew.X - nSourcePositionX, posNew.Y - nSourcePositionY);
                frames.KeyFrames.Add(new LinearPointKeyFrame() { KeyTime = kTime, Value = ptNew });
                dblSeconePosition += 1;
            }


            Image testImage = new Image();
            testImage.Source = mBeedsMap[nSourceRow,nSourceColumn].Source;
            CompositeTransform imageComposite = new CompositeTransform();
            testImage.RenderTransform = imageComposite;

            //RectangleGeometry image = new RectangleGeometry();
            Storyboard.SetTarget(frames, imageComposite);
            Storyboard.SetTargetProperty(frames, new PropertyPath(CompositeTransform.TranslateYProperty));
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(frames);
            //image.fill
            // 把 EllipseGeometry 貼上正確的圖片並秀出來、移到正確的位置
            //RoleBeanElement.Fill = new ImageBrush() { ImageSource = GetBeanImagePath(beanType) };
            //Canvas.SetLeft(RoleBeanElement, mVisualMap[nSourceRow, nSourceColumn].X);
            //Canvas.SetTop(RoleBeanElement, mVisualMap[nSourceRow, nSourceColumn].Y);
            // 把原本在底下的圖片清掉
            //mBeansMap[nSourceRow, nSourceColumn].Source = null;

            // 開始跑
            //mAnimationRunning = true;
            storyboard.Begin();
        }

        private void ComputeEachBeedFallCount()
        {
            for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
            {
                Int32 ignoreLen = 0;
                Int32 count = 0;
                for (int row = BLOCK_ROW_COUNT - 2; row >= 0; --row)
                {
                    if (ignoreLen > 1)
                    {
                        ignoreLen--;
                        continue;
                    }
                    
                    if (mVirtualMap[row + 1, col] == -1 && mVirtualMap[row, col] != -1)
                    {
                        for (int i = row; i >= 0; --i)
                        {
                            if (mVirtualMap[i, col] != -1)
                            {
                                ignoreLen++;
                                mBeedFallCollection.Add(new BeedFall()
                                {
                                    Pos = new Position(i, col),
                                    FallCount = mEachBeedFallCountMap[col][count]
                                });
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (ignoreLen == 1)
                        {
                            ignoreLen = 0;
                        }
                        count++;
                    }
                }
            }
            for (int i = 0; i < mBeedFallCollection.Count; i++)
            {
                Debug.WriteLine(String.Format("{0},{1}  {2}", mBeedFallCollection[i].Pos.Row, mBeedFallCollection[i].Pos.Column, mBeedFallCollection[i].FallCount));
            }
            Debug.WriteLine(mBeedFallCollection.Count);
        }

        private void CheckBeedBoardHoles()
        {
            for (int col = 0; col < BLOCK_COLUMN_COUNT; ++col)
            {
                Int32 fallCounts = 0;
                for (int row = BLOCK_ROW_COUNT - 1; row >= 0; --row)
                {
                    if (mVirtualMap[row, col] == -1)
                    {
                        fallCounts++;
                    }
                    else if (fallCounts != 0)
                    {
                        mEachBeedFallCountMap[col].Add(fallCounts);
                    }
                    if (row == 0 && mVirtualMap[row, col] == -1)
                    {
                        mEachBeedFallCountMap[col].Add(fallCounts);
                    }
                }
            }
        }

        private void StartEliminateBeed()
        {
            ManipulationComplete = Visibility.Collapsed;
            FindBeedChainHorizontal();
            FindBeedChainVertical();
            FindIntersectionHV();
            CountRemainChain();
            EliminateBeed();
            CheckBeedBoardHoles();
            ComputeEachBeedFallCount();
            List<Position> mPath = new List<Position>();
            mPath.Add(mBeedFallCollection[0].Pos);
            mPath.Add(new Position(mBeedFallCollection[0].Pos.Row + mBeedFallCollection[0].FallCount, mBeedFallCollection[0].Pos.Column));
            MoveRemainBeeds(mPath);
        }

        private void OnTosPanelManipulationStarted(Object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            offsetPoint = new Point(offsetX - e.ManipulationOrigin.X, offsetY - e.ManipulationOrigin.Y);
        }

        private void OnTosPanelMouseEnter(Object sender, System.Windows.Input.MouseEventArgs e)
        {
            Debug.WriteLine("OnTosPanelMouseEnter");

            if (firstClick == false)
            {
                GetCoordinate(e.GetPosition(TosSpace));
                mBeedsMap[mPosCurrent.Row, mPosCurrent.Column].Source = null;
                offsetX = e.GetPosition(TosSpace).X;
                offsetY = e.GetPosition(TosSpace).Y;
                mPosPrevious.Row = mPosCurrent.Row;
                mPosPrevious.Column = mPosCurrent.Column;
                CreateCursorImage(e.GetPosition(TosSpace));
                Debug.WriteLine(mVirtualMap[mPosCurrent.Row, mPosCurrent.Column]);
            }
        }

        private void CreateCursorImage(Point pos)
        {
            cursorImage = new Image();
            cursorImage.Width = BLOCK_SIZE;
            cursorImage.Height = BLOCK_SIZE;
            cursorImage.RenderTransformOrigin = new Point(0.5, 0.5);
            ScaleTransform trans = new ScaleTransform();
            cursorImage.RenderTransform = trans;
            cursorImage.Source = GetBeedImage((mVirtualMap[mPosCurrent.Row, mPosCurrent.Column]));
            TosSpace.Children.Add(cursorImage);
            Canvas.SetLeft(cursorImage, pos.X - cursorShift);
            Canvas.SetTop(cursorImage, pos.Y - cursorShift);
        }

        private void ManipulationCompletedState()
        {
            mBeedsMap[mPosCurrent.Row, mPosCurrent.Column].Source = cursorImage.Source;
            TimeLineBar.Width = (Int32)(LayoutRoot.ActualWidth);
            Timer.Stop();
            TimerStart = true;
            CountDown = CountDownSecond;
            ManipulationComplete = Visibility.Visible;
            StartEliminateBeed();
            TosSpace.Children.Remove(cursorImage);
        }


        private void OnTosPanelManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            Debug.WriteLine("OnTosPanelManipulationCompleted");
            if (firstClick == false)
            {
                ManipulationCompletedState();
                Debug.WriteLine("TimerStop");
            }
            firstClick = false;
        }

        private void OnTosPanelManipulationDelta(Object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (CountDown <= 0)
            {
                ManipulationCompletedState();
            }
            else if (cursorImage != null)
            {
                if (TimerStart)
                {
                    Debug.WriteLine("TimerStart");
                    TimerStart = false;
                    Timer.Start();
                }
                Point realPoint = new Point(offsetPoint.X + e.ManipulationOrigin.X, offsetPoint.Y + e.ManipulationOrigin.Y);
                GetCoordinate(realPoint);

                Canvas.SetLeft(cursorImage, realPoint.X - cursorShift);
                Canvas.SetTop(cursorImage, realPoint.Y - cursorShift);

                if (Math.Abs(mPosPrevious.Row - mPosCurrent.Row) == 1 || Math.Abs(mPosPrevious.Column - mPosCurrent.Column) == 1)
                {
                    Int32 nBeanTypeTarget = mVirtualMap[mPosPrevious.Row, mPosPrevious.Column];
                    mVirtualMap[mPosPrevious.Row, mPosPrevious.Column] = mVirtualMap[mPosCurrent.Row, mPosCurrent.Column];
                    mVirtualMap[mPosCurrent.Row, mPosCurrent.Column] = nBeanTypeTarget;

                    ImageSource iSource = mBeedsMap[mPosPrevious.Row, mPosPrevious.Column].Source;
                    mBeedsMap[mPosPrevious.Row, mPosPrevious.Column].Source = mBeedsMap[mPosCurrent.Row, mPosCurrent.Column].Source;
                    mBeedsMap[mPosCurrent.Row, mPosCurrent.Column].Source = iSource;

                    mPosPrevious.Row = mPosCurrent.Row;
                    mPosPrevious.Column = mPosCurrent.Column;
                }
            }
        }


    }
}
