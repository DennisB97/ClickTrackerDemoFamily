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

namespace ClickTracker
{
    /// <summary>
    /// Interaction logic for LoadUniqueStringWindow.xaml
    /// </summary>
    public partial class LoadUniqueStringWindow : Window
    {
        //If add button is clicked the given input from the textbox is added to this string which is then received to the one who called for this window.
        public string uniqueIDString = "";

        public bool bLoadUniqueString = false;

        public LoadUniqueStringWindow()
        {
            InitializeComponent();

            if(BTN_AddUniqueID != null)
            {
                BTN_AddUniqueID.Click += new RoutedEventHandler(OnAddUniqueIDClick);
            }

            if(BTN_CancelAddUniqueID != null)
            {
                BTN_CancelAddUniqueID.Click += new RoutedEventHandler(OnCancelAddUniqueID);
            }


        }


        ~LoadUniqueStringWindow()
        {
            if (BTN_AddUniqueID != null)
            {
                BTN_AddUniqueID.Click -= OnAddUniqueIDClick;
            }

            if (BTN_CancelAddUniqueID != null)
            {
                BTN_CancelAddUniqueID.Click -= OnCancelAddUniqueID;
            }
        }

        private void OnAddUniqueIDClick(object sender, RoutedEventArgs e)
        {
            if(TBo_LoadUniqueStringID != null)
            {
                uniqueIDString = TBo_LoadUniqueStringID.Text;
            }

            bLoadUniqueString = true;
            Close();
        }

        private void OnCancelAddUniqueID(object sender, RoutedEventArgs e)
        {
            Close();
        }


    }
}
