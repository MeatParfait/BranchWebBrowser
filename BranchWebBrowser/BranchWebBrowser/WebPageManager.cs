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
   public static class WebPageManager
   {
      static List<WebPage> webpageList = new List<WebPage>();

      public static WebPage currentWebPage;

      public static MainWindow mainWindow;


      public static void ActivateOneWebpage(WebPage target)
      {
         foreach (WebPage webpage in webpageList)
         {
            if(webpage == target)
            {
               webpage.visibility = Visibility.Visible;
            }
            else
            {
               webpage.visibility = Visibility.Hidden;
            }
         }
      }

      public static void AddWebpage(WebPage webPage)
      {
         webpageList.Add(webPage);

         currentWebPage = webPage;
         currentWebPage.showWebPageButton_Click(null, null);
      }

      public static void RemoveWebpage(WebPage webPage)
      {
         //활성화되있던 창이 닫히면
         if (webpageList.IndexOf(webPage) + 1 < webpageList.Count)
         {
            currentWebPage = webpageList[webpageList.IndexOf(webPage) + 1];
            ActivateOneWebpage(currentWebPage);
         }
         else if (webpageList.IndexOf(webPage) - 1 >= 0)
         {
            currentWebPage = webpageList[webpageList.IndexOf(webPage) - 1];
            ActivateOneWebpage(currentWebPage);
         }
         else
         {
            currentWebPage = null;
            mainWindow.urlTextBox.Text = null;
         }

         webpageList.Remove(webPage);

         mainWindow.webpagePanel.Children.Remove(webPage.webPageTab);
         mainWindow.webBrowserPanel.Children.Remove(webPage.webBrowser);
      }
   }
}
