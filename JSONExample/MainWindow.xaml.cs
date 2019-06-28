using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace JSONExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<SomeObject> SomeObjects = new ObservableCollection<SomeObject>() {
            new SomeObject() {
                AlistofObjects =new List<AnotherObject>() {
                    new AnotherObject() {ANumber = 10, AString = "Hello"},
                    new AnotherObject() {ANumber = 20, AString = "You"},
                    new AnotherObject() {ANumber = 30, AString = "Person"},
                },
                 Name = "I'm a test Object",
                 ANumber = 50,
                 Someobject = new AnotherObject(){ ANumber = 20, AString = "More Tests" }
            }
        };
        ObservableCollection<SomeObject> ConvertedObjects = new ObservableCollection<SomeObject>();
        public MainWindow()
        {
            InitializeComponent();
            DataIN.ItemsSource = SomeObjects;
            DataOUT.ItemsSource = ConvertedObjects;
        }

        private void ToJSONClick(object sender, RoutedEventArgs e)
        {
            var o = (SomeObject)DataIN.SelectedItem;
            Output.Text = JSON.ToJSONString(o);
        }

        private void FromJSONClick(object sender, RoutedEventArgs e)
        {
            var json =  JSON.Parse<SomeObject>(Output.Text);
            if(json != null)
            {
                ConvertedObjects.Add(json);
            }
        }
    }
    public class SomeObject
    {
        public string Name { get; set; }
        public string SomethingElse { get; set; }
        public int ANumber { get; set; }
        public AnotherObject Someobject { get; set; }
        public List<AnotherObject> AlistofObjects { get; set; }
    }
    public class AnotherObject
    {
        public string AString { get; set; }
        public int ANumber { get; set; }
    }
}
