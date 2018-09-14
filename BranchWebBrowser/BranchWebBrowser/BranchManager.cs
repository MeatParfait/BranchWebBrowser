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
    public static class BranchManager
    {
      public static MainWindow mainWindow;

      public static Branch currentMainBranch;

      public static Branch currentBranchNode;

      public static string filePath;
    }
}
