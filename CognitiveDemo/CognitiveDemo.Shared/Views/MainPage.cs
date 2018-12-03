using System;

using Xamarin.Forms;

namespace CognitiveDemo
{
    public class MainPage : TabbedPage
    {
        public MainPage()
        {
            Page demoPage, aboutPage = null;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    demoPage = new NavigationPage(new DemoPage())
                    {
                        Title = "Cognitive Demos"
                    };

                    aboutPage = new NavigationPage(new AboutPage())
                    {
                        Title = "About"
                    };
                    demoPage.Icon = "tab_feed.png";
                    aboutPage.Icon = "tab_about.png";
                    break;
                default:
                    demoPage = new DemoPage()
                    {
                        Title = "Cognitive Demos"
                    };

                    aboutPage = new AboutPage()
                    {
                        Title = "About"
                    };
                    break;
            }

            Children.Add(demoPage);
            Children.Add(aboutPage);

            Title = Children[0].Title;
        }

        protected override void OnCurrentPageChanged()
        {
            base.OnCurrentPageChanged();
            Title = CurrentPage?.Title ?? string.Empty;
        }
    }
}
