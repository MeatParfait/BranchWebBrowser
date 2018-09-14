using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Microsoft.Win32;

namespace BranchWebBrowser
{
/// <summary>
/// MainWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class MainWindow : Window
   {
      Regex domainRegex = new Regex(@"\.[a-zA-Z]+");

      enum TileType
      {
         None,
         Node,
         Line_One,
         Line_Next,
         Line_Some,
         Line_Another,
         Line_TheOther
      }
      struct Tile
      {
         public UIElement uIElement;
         public TileType tileType;
      }

      //branchGrid
      int scaleX = 150, scaleY = 100;
      int offset = 50;

      //branchScrollViewer
      Point initialPnt = new Point();
      Point initialOffset = new Point();

      Stopwatch stopwatch = new Stopwatch();


      public MainWindow()
      {
         InitializeComponent();

         this.KeyDown += branchGrid_KeyDown;

         //manager에 연결
         WebPageManager.mainWindow = this;
         BranchManager.mainWindow = this;
         
         //branchScrollViewer 이벤트 핸들러에 연결
         branchScrollViewer.PreviewMouseLeftButtonDown += branchScrollViewer_PreviewMouseLeftButtonDown;
         branchScrollViewer.PreviewMouseLeftButtonUp += branchScrollViewer_PreviewMouseLeftButtonUp;
         branchScrollViewer.PreviewMouseMove += branchScrollViewer_PreviewMouseMove;

         //prefab 등록
         UIElementUtil.RegisterPrefab(webBrowser_prefab);
         UIElementUtil.RegisterPrefab(webPageTab_prefab);
         UIElementUtil.RegisterPrefab(BranchNode_prefab);
         UIElementUtil.RegisterPrefab(BranchLine_prefab);
         //prefab 제거
         webBrowserPanel.Children.Remove(webBrowser_prefab);
         webpagePanel.Children.Remove(webPageTab_prefab);
         branchGrid.Children.Remove(BranchNode_prefab);
         branchGrid.Children.Remove(BranchLine_prefab);

         //IE11으로 설정
         Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", "BranchWebBrowser.exe", 11000);
      }


      private void EraseUrl_Click(object sender, RoutedEventArgs e)
      {
         urlTextBox.Text = "";
      }

      private void NewBranchButton_Click(object sender, RoutedEventArgs e)
      {
         SaveFileDialog saveFileDialog = new SaveFileDialog();
         saveFileDialog.AddExtension = true;
         saveFileDialog.DefaultExt = ".brc";
         saveFileDialog.Filter = "brc files (*.brc)|*.brc";
         saveFileDialog.Title = "가지 파일(.brc) 저장";
         if (saveFileDialog.ShowDialog() == true)
         {
            BranchManager.filePath = saveFileDialog.FileName;
            
            //빈 파일 하나 만들기
            File.WriteAllText(BranchManager.filePath, "");
            
            if(WebPageManager.currentWebPage == null)
            {
               BranchManager.currentMainBranch = new Branch("빈 가지", "");
            }
            else
            {
               BranchManager.currentMainBranch = new Branch(WebPageManager.currentWebPage.currentTitle,
                                                            WebPageManager.currentWebPage.currentUrl);
            }
            SaveTree();

            LoadTree();
            DrawTree();
            ShowTreePanel();
         }
      }
      private void OpenBranchButton_Click(object sender, RoutedEventArgs e)
      {
         OpenFileDialog openFileDialog = new OpenFileDialog();
         openFileDialog.DefaultExt = ".brc";
         openFileDialog.Filter = "brc files (*.brc)|*.brc";
         openFileDialog.Title = "가지 파일(.brc) 열기";
         if (openFileDialog.ShowDialog() == true)
         {
            BranchManager.filePath = openFileDialog.FileName;

            LoadTree();
            DrawTree();
            ShowTreePanel();
         }
      }
      private void AddBranchButton_Click(object sender, RoutedEventArgs e)
      {
         Branch newBranch;
         if(WebPageManager.currentWebPage == null)
         {
            newBranch = new Branch("빈 가지", "");
         }
         else
         {
            newBranch = new Branch(WebPageManager.currentWebPage.currentTitle,
                                   WebPageManager.currentWebPage.currentUrl);
         }
         //부모 자식 연결
         if (BranchManager.currentBranchNode != null)
            BranchManager.currentBranchNode.children.Add(newBranch);

         BranchManager.currentBranchNode = newBranch;

         DrawTree();
         ShowTreePanel();
         ShowAddBrcPanel();
      }
      private void EditBranchButton_Click(object sender, RoutedEventArgs e)
      {
         //열린 페이지가 없으면 나가기
         if (WebPageManager.currentWebPage == null)
            return;

         DrawTree();
         ShowTreePanel();
         ShowAddBrcPanel();
         addBrcTitleTextBox.Text = WebPageManager.currentWebPage.currentTitle;
         addBrcUrlTextBox.Text = WebPageManager.currentWebPage.currentUrl;
      }

      public void DrawTree()
      {
         //디버깅용
         //Branch testBranch = new Branch("a", "");
         //Branch cb1 = new Branch("b", "");
         //Branch cb2 = new Branch("c", "");
         //Branch cb3 = new Branch("d", "");
         //Branch cb4 = new Branch("e", "");
         //testBranch.children.Add(cb1);
         //testBranch.children.Add(cb2);
         //testBranch.children.Add(cb3);
         //testBranch.children.Add(cb4);
         //BranchManager.currentMainBranch = testBranch;

         int currentStep = 0, maxLayer = 10, maxStep = 10;
         
         //지우고 다시 그리기
         EraseTree();

         //map 배열 크기 계산
         void CalculateTileSize(Branch branch)
         {
            if (branch.children.Count == 0)
            {
               maxStep++;
               return;
            }
            else
            {
               maxLayer++;
         
               for (int i = 0; i < branch.children.Count; i++)
               {
                  CalculateTileSize(branch.children[i]);
               }
            }
         }
         CalculateTileSize(BranchManager.currentMainBranch);
         Tile[,] map = new Tile[maxLayer * 2 - 1, maxStep];
         branchGrid.Width = maxLayer * scaleX * 2;
         branchGrid.Height = maxStep * scaleY * 2;


         //타일 등록
         void SetTiles(Branch branch, int layer)
         {
            int currentLayer = layer;

            if (branch == BranchManager.currentBranchNode)
            {
               SetTile(map, currentLayer, currentStep, TileType.Node,
                     branch: branch, isCurrentNode: true);
            }
            else
            {
               SetTile(map, currentLayer, currentStep, TileType.Node,
                     branch: branch);
            }

            if(branch.children.Count == 0)
            {
               currentStep++;
               return;
            }

            currentLayer++;
            for (int i = 0; i < branch.children.Count; i++)
            {
               int previousStep = currentStep;

               SetTiles(branch.children[i], currentLayer + 1);
               
               //첫번째 가지
               if (i == 0)
               {
                  if(branch.children.Count == 1)
                  {
                     SetTile(map, currentLayer, previousStep, TileType.Line_One);
                  }
                  else
                  {
                     SetTile(map, currentLayer, previousStep, TileType.Line_Some);
                  }
               }
               //마지막 가지
               else if (i == branch.children.Count - 1)
               {
                  SetTile(map, currentLayer, previousStep, TileType.Line_TheOther);
               }
               //처음과 마지막 사이 가지
               else
               {
                  SetTile(map, currentLayer, previousStep, TileType.Line_Another);
               }

               //가지와 가지 사이 연결
               if(i != branch.children.Count - 1)
               {
                  for (; previousStep < currentStep; previousStep++)
                  {
                     SetTile(map, currentLayer, previousStep, TileType.Line_Next);
                  }
               }
            }
         }
         SetTiles(BranchManager.currentMainBranch, 0);


         //타일 그리기
         for (int y = 0; y < map.GetLength(1); y++)
         {
            for (int x = 0; x < map.GetLength(0); x++)
            {
               DrawTile(map, x, y);
            }
         }
      }
      private void SetTile(Tile[,] map, int x, int y, 
                           TileType tileType,
                           Branch branch = null, bool isCurrentNode = false)
      {
         //이미 쓴 공간이면 돌아가기
         if (map[x, y].tileType != TileType.None)
         {
            return;
         }

         //branch node
         if(tileType == TileType.Node)
         {
            //branchNode 복제
            Grid newBranchNode = UIElementUtil.ClonePrefab(BranchNode_prefab) as Grid;
            UIElementCollection childrenOfNode = newBranchNode.Children;
            branch.TitleButton = childrenOfNode[0] as Button;

            if(isCurrentNode)
               branch.TitleButton.Background = Brushes.Yellow;

            map[x, y].uIElement = newBranchNode;
            map[x, y].tileType = tileType;
         }
         //branch line
         else
         {
            //branchLine 복제
            Grid newBranchNodeSpace = UIElementUtil.ClonePrefab(BranchLine_prefab) as Grid;

            map[x, y].uIElement = newBranchNodeSpace;
            map[x, y].tileType = tileType;
         }
      }
      private void DrawTile(Tile[,] map, int x, int y)
      {
         Tile tile = map[x, y];

         //Node
         if(tile.tileType == TileType.Node)
         {
            branchGrid.Children.Add(tile.uIElement);
            ((Grid)tile.uIElement).Margin = new Thickness(x * scaleX + offset, 
                                                          y * scaleY + offset + 15, 0.0, 0.0);
         }
         //Line
         else if(tile.tileType == TileType.Line_One ||
                 tile.tileType == TileType.Line_Next ||
                 tile.tileType == TileType.Line_Some ||
                 tile.tileType == TileType.Line_Another ||
                 tile.tileType == TileType.Line_TheOther)
         {
            ImageBrush image = new ImageBrush();

            switch (tile.tileType)
            {
               case TileType.Line_One:
                  image.ImageSource = 
                     new BitmapImage(new Uri(@"pack://application:,,,/Resources/One.png"));
                  break;
               case TileType.Line_Next:
                  image.ImageSource =
                     new BitmapImage(new Uri(@"pack://application:,,,/Resources/Next.png"));
                  break;
               case TileType.Line_Some:
                  image.ImageSource = 
                     new BitmapImage(new Uri(@"pack://application:,,,/Resources/Some.png"));
                  break;
               case TileType.Line_Another:
                  image.ImageSource = 
                     new BitmapImage(new Uri(@"pack://application:,,,/Resources/Another.png"));
                  break;
               case TileType.Line_TheOther:
                  image.ImageSource = 
                     new BitmapImage(new Uri(@"pack://application:,,,/Resources/TheOther.png"));
                  break;
            }

            branchGrid.Children.Add(tile.uIElement);
            ((Grid)tile.uIElement).Margin = new Thickness(x * scaleX + offset + 50,
                                                          y * scaleY + offset, 0.0, 0.0);
            ((Grid)tile.uIElement).Background = image;
         }
      }

      private void EraseTree()
      {
         branchGrid.Children.Clear();
      }
      private void SaveTree()
      {
         BinaryFormatter binaryFormatter = new BinaryFormatter();
         Stream fs = new FileStream(BranchManager.filePath, FileMode.Open, FileAccess.Write);
         binaryFormatter.Serialize(fs, BranchManager.currentMainBranch);
         fs.Close();
      }
      private void LoadTree()
      {
         BinaryFormatter binaryFormatter = new BinaryFormatter();
         Stream fs = new FileStream(BranchManager.filePath, FileMode.Open, FileAccess.Read);
         BranchManager.currentMainBranch = (Branch)binaryFormatter.Deserialize(fs);
         fs.Close();
         
         addBranchButton.Visibility = Visibility.Visible;
         editBranchButton.Visibility = Visibility.Visible;
         ShowBranchButton.Visibility = Visibility.Visible;
         saveBranchButton.Visibility = Visibility.Visible;

         //처음에는 메인 가지가 활성화됨
         BranchManager.currentBranchNode = BranchManager.currentMainBranch;
      }

      private void urlTextBox_KeyDown(object sender, KeyEventArgs e)
      {
         //focus 지우기
         if (e.Key == Key.Escape)
         {
            Keyboard.ClearFocus();
         }

         //입력한 url 검색
         if (e.Key == Key.Enter)
         {
            OpenUrl(urlTextBox.Text);
         }
      }
      public void OpenUrl(string url, bool createNewWebPage = false)
      {
         Keyboard.ClearFocus();

         //빈 주소면 변경 안함
         if (url == "" || url == null)
            return;

         if (domainRegex.IsMatch(url) == false)
         {
            url = string.Format("https://www.google.co.kr/search?hl=ko&ei=R5AcW42SDoO20QSphpSoAg&q={0}", url);
         }
         else if (url.StartsWith("http://") == false && url.StartsWith("https://") == false)
         {
            url = "http://" + url;
         }

         //열려있는 창이 없으면 하나 만들기
         if (WebPageManager.currentWebPage == null || createNewWebPage)
         {
            newWebPageButton_Click(null, null);
         }

         WebPageManager.currentWebPage.webBrowser.Navigate(url);
         HideTreePanel();
      }

      private void GoForwardButton_Click(object sender, RoutedEventArgs e)
      {
         //열려있는 창이 없으면 나가기
         if (WebPageManager.currentWebPage == null)
            return;

         if (WebPageManager.currentWebPage.webBrowser.CanGoForward)
         {
            WebPageManager.currentWebPage.webBrowser.GoForward();
         }
      }
      private void GoBackwardButton_Click(object sender, RoutedEventArgs e)
      {
         //열려있는 창이 없으면 나가기
         if (WebPageManager.currentWebPage == null)
            return;

         if (WebPageManager.currentWebPage.webBrowser.CanGoBack)
         {
            WebPageManager.currentWebPage.webBrowser.GoBack();
         }
      }

      public void ShowTreePanel()
      {
         WebPageManager.ActivateOneWebpage(null);
         branchScrollViewer.Visibility = Visibility.Visible;
         helpTextBlock.Visibility = Visibility.Visible;
      }
      public void HideTreePanel()
      {
         WebPageManager.ActivateOneWebpage(WebPageManager.currentWebPage);
         branchScrollViewer.Visibility = Visibility.Hidden;
         helpTextBlock.Visibility = Visibility.Hidden;
      }

      private void ShowBranchButton_Click(object sender, RoutedEventArgs e)
      {
         //열려있으면
         if(branchScrollViewer.Visibility == Visibility.Visible)
         {
            HideTreePanel();
         }
         //닫혀있으면
         else
         {
            ShowTreePanel();
         }
      }
      
      void ShowAddBrcPanel()
      {
         addBranchGrid.Visibility = Visibility.Visible;
         addBrcTitleTextBox.Text = BranchManager.currentBranchNode.title;
         addBrcUrlTextBox.Text = BranchManager.currentBranchNode.url;
         Keyboard.Focus(addBrcTitleTextBox);
      }
      void HideAddBrcPanel()
      {
         addBranchGrid.Visibility = Visibility.Hidden;
      }

      private void newWebPageButton_Click(object sender, RoutedEventArgs e)
      {
         //webpage tab 생성
         Grid newWebPage = UIElementUtil.ClonePrefab(webPageTab_prefab) as Grid;
         UIElementCollection children = newWebPage.Children;
         webpagePanel.Children.Add(newWebPage);

         //web browser 생성
         WebBrowser newWebBrowser = UIElementUtil.ClonePrefab(webBrowser_prefab) as WebBrowser;
         webBrowserPanel.Children.Add(newWebBrowser);
         
         //생성 및 등록
         new WebPage(newWebBrowser, 
                     newWebPage,
                     children[0] as Button, 
                     children[1] as Button);

         //시작 페이지를 구글로 설정
         WebPageManager.currentWebPage.webBrowser.Navigate("https://www.google.com");

      }
      
      private void branchGrid_KeyDown(object sender, KeyEventArgs e)
      {
         if (BranchManager.currentBranchNode == null)
            return;

         //추가
         if(e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
         {
            AddBranchButton_Click(null, null);
         }

         //수정
         if (e.Key == Key.E && Keyboard.IsKeyDown(Key.LeftCtrl))
         {
            ShowAddBrcPanel();
         }

         //제거
         if (e.Key == Key.Delete)
         {
            bool isDone = false;
            void FindMeAndKill(Branch branch)
            {
               if(branch.children.Count == 0 || isDone)
               {
                  return;
               }
               else
               {
                  for (int i = 0; i < branch.children.Count; i++)
                  {
                     if(branch.children[i] == BranchManager.currentBranchNode)
                     {
                        branch.children.Remove(BranchManager.currentBranchNode);
                        BranchManager.currentBranchNode = branch;
                        isDone = true;
                        return;
                     }
                     else
                     {
                        FindMeAndKill(branch.children[i]);
                     }
                  }
               }
            }
            FindMeAndKill(BranchManager.currentMainBranch);

            DrawTree();
         }
      }
      private void AddBranchGrid_KeyDown(object sender, KeyEventArgs e)
      {
         if(e.Key == Key.Enter)
         {
            admitAddbranchButton_Click(null, null);
         }
      }

      private void admitAddbranchButton_Click(object sender, RoutedEventArgs e)
      {
         HideAddBrcPanel();

         BranchManager.currentBranchNode.title = addBrcTitleTextBox.Text;
         BranchManager.currentBranchNode.url = addBrcUrlTextBox.Text;

         DrawTree();
      }

      private void SaveBranchButton_Click(object sender, RoutedEventArgs e)
      {
         SaveTree();
      }
      
      private void branchScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
      {
         if (e.Source as Button != null)
         {
            //더블클릭이라면
            if(stopwatch.IsRunning && stopwatch.ElapsedMilliseconds < 300)
            {
               stopwatch.Reset();

               ((Button)e.Source).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else
            {
               stopwatch.Reset();
               stopwatch.Start();

               if (BranchManager.currentBranchNode.TitleButton != (Button)e.Source)
               {
                  ((Button)e.Source).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
               }
            }
         }

         initialPnt = e.GetPosition(branchScrollViewer);
         initialOffset.X = branchScrollViewer.HorizontalOffset;
         initialOffset.Y = branchScrollViewer.VerticalOffset;

         branchScrollViewer.CaptureMouse();
      }

      private void branchScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
      {
         Point currentPoint = e.GetPosition(branchScrollViewer);

         if (branchScrollViewer.IsMouseCaptured)
         {
            branchScrollViewer.ScrollToHorizontalOffset(initialOffset.X + (initialPnt.X - currentPoint.X));
            branchScrollViewer.ScrollToVerticalOffset(initialOffset.Y + (initialPnt.Y - currentPoint.Y));
         }
      }

      private void branchScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
      {
         branchScrollViewer.ReleaseMouseCapture();
      }
   }
}
