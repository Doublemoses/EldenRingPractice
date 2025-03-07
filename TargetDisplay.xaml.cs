using System;
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
using System.Windows.Shapes;

namespace EldenRingPractice
{
    /// <summary>
    /// Interaction logic for TargetDisplay.xaml
    /// </summary>
    public partial class TargetDisplay : Window
    {
        IntPtr pointer;
        public TargetDisplay(IntPtr entityPointer = 0, bool show = false)
        {
            InitializeComponent();
            pointer = entityPointer;
            if (show) { this.Show(); }
            if (entityPointer != 0)
            {
                this.Title = "Entity " + pointer.ToString("X16");
            }
        }

        public IntPtr getPointer()
        {
            return pointer;
        }
    }
}
