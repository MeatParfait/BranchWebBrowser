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
   public class WebPage
   {
      public string currentTitle;
      public string currentUrl = "";

      public WebBrowser webBrowser;
      public Grid webPageTab;
      Button showWebPageButton;
      Button closeWebPageButton;

      Visibility _visibility;
      public Visibility visibility
      {
         get
         {
            return _visibility;
         }
         set
         {
            _visibility = value;
            webBrowser.Visibility = visibility;
         }
      }

      
      public WebPage(WebBrowser webBrowser, Grid webPageTab, Button showWebPageButton, Button closeWebPageButton)
      {
         this.webBrowser = webBrowser;
         this.webPageTab = webPageTab;
         this.showWebPageButton = showWebPageButton;
         this.closeWebPageButton = closeWebPageButton;

         showWebPageButton.Click += showWebPageButton_Click;
         closeWebPageButton.Click += closeWebPageButton_Click;
         webBrowser.Navigating += webBrowser_Navigating;

         WebPageManager.AddWebpage(this);
      }


      public void showWebPageButton_Click(object sender, RoutedEventArgs e)
      {
         WebPageManager.currentWebPage = this;
         WebPageManager.ActivateOneWebpage(this);

         WebPageManager.mainWindow.urlTextBox.Text = currentUrl;

         WebPageManager.mainWindow.HideTreePanel();
      }

      public void closeWebPageButton_Click(object sender, RoutedEventArgs e)
      {
         WebPageManager.RemoveWebpage(this);
      }

      private void webBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
      {
         currentUrl = e.Uri.AbsoluteUri;
         WebPageManager.mainWindow.urlTextBox.Text = currentUrl;
         showWebPageButton.Content = currentUrl;
         currentTitle = showWebPageButton.Content.ToString();
      }
   }
}
