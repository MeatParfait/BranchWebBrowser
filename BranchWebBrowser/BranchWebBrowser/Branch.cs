using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace BranchWebBrowser
{
   [Serializable]
   public class Branch
   {
      public string title, url;
      
      public List<Branch> children = new List<Branch>();

      [NonSerialized]
      Button _titleButton;
      public Button TitleButton
      {
         get
         {
            return _titleButton;
         }
         set
         {
            _titleButton = value;
            _titleButton.Content = title;
            _titleButton.ToolTip = string.Format("[제목]  {0}\n\n[URL]  {1}", title, url);

            _titleButton.Click += titleButton_Click;
         }
      }


      public Branch(string title, string url)
      {
         this.title = title;
         this.url = url;
      }


      private void titleButton_Click(object sender, RoutedEventArgs e)
      {
         if(BranchManager.currentBranchNode == this)
         {
            BranchManager.mainWindow.OpenUrl(url, true);
         }
         else
         {
            if (BranchManager.currentBranchNode != null)
               BranchManager.currentBranchNode.TitleButton.Background = Brushes.White;
            BranchManager.currentBranchNode = this;
            TitleButton.Background = Brushes.Yellow;
         }
      }


      public override string ToString()
      {
         return title;
      }
   }
}
