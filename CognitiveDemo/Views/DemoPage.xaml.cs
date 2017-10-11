using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace CognitiveDemo
{
    public partial class DemoPage : ContentPage
    {
        DemoViewModel viewModel;

        public DemoPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new DemoViewModel();
        }
    }
}
