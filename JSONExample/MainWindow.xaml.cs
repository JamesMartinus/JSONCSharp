using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace JSONExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            var _test = new ClassForConversion()
            {
                GiveMeANewName = "foo",
                IgnoreMe = "You, won't see this",
                IgnoreMeToJSON = "You won't see this either",
                IgnoreMeFromJSON = "Converting from JSON Won't see this",
                PropertyUsingConverterBothWays = "This was converted from the converter",
                PropertyUsingConverterFromJSON = "This was converted from the string",
                PropertyUsingConverterToJSON = "This was converted from the object"
            };

            // Converting the Object
            var str = JSONSharp.ToJSON(_test);

            Console.WriteLine(str);


            string JsonString = "{"
                + "\"NewAlias\" : \"Some Value\","
                + "\"IgnoreMe\" : \"This will be ignored completely\","
                + "\"IgnoreMeToJSON\" : \"this won't be ignored\","
                + "\"IgnoreMeFromJSON\" : \"This is going to be ignored\","
                + "\"PropertyUsingConverterBothWays\" : \"This was passed through the ConvertFromJSON Method\","
                + "\"PropertyUsingConverterFromJSON\" : \"This was also passed through the ConvertFromJSON Method\","
                + "\"PropertyUsingConverterToJSON\" : \"This won't be converted\""
                + "}";

            ClassForConversion ConvertedObject = JSONSharp.FromString<ClassForConversion>(JsonString);

            Console.WriteLine(string.Join("\n", ConvertedObject.GetType().GetProperties().Select(x => x.Name + " : " + x.GetValue(ConvertedObject))));

        }

    }
    
}
