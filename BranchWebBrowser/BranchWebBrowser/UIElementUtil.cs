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
   public static class UIElementUtil
   {
      static Dictionary<int, string> prefabDic = new Dictionary<int, string>();


      public static void RegisterPrefab(UIElement Source)
      {
         string XAML = System.Windows.Markup.XamlWriter.Save(Source);
         prefabDic.Add(Source.GetHashCode(), XAML);
      }

      public static UIElement ClonePrefab(UIElement Source)
      {
         if(prefabDic.ContainsKey(Source.GetHashCode()) == false)
         {
            throw new Exception("등록된 Prefab이 아닙니다. 먼저 Prefab을 등록해주세요.");
         }

         string XAML = prefabDic[Source.GetHashCode()];
         StringReader StringReader = new StringReader(XAML);
         System.Xml.XmlReader xmlReader = System.Xml.XmlTextReader.Create(StringReader);

         UIElement newUI = System.Windows.Markup.XamlReader.Load(xmlReader) as UIElement;
         newUI.Visibility = Visibility.Visible;
         return newUI;
      }
   }
}
