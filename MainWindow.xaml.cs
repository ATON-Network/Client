using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;
using System.Windows;
using Dark.Net;
using Microsoft.Web.WebView2.Core;
using Steamworks;

namespace AtonClient {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            DarkNet.Instance.SetWindowThemeWpf(this, Theme.Dark);

            try {
                SteamClient.Init(3067830);
            } catch {
                MessageBox.Show("You must own ATON on Steam in order to launch the game.", "ERROR", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
                Close();
            }
            
            Loaded += OnLoaded;
        }

        async void OnLoaded(object sender, RoutedEventArgs e) {
            await SetupWebview();
        }

        private async Task SetupWebview() {
            string versionString = Assembly.GetAssembly(typeof(MainWindow))!.GetName().Version!.ToString();

            await WebView.EnsureCoreWebView2Async();

            WebView.CoreWebView2.NewWindowRequested += (sender, args) => {
                Process.Start(args.Uri);

                args.Handled = true;
            };

            WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            WebView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            
            WebView.CoreWebView2.AddWebResourceRequestedFilter("https://localhost:7017/*",
                CoreWebView2WebResourceContext.All);
            
            WebView.CoreWebView2.WebResourceRequested +=
                async delegate (object sender, CoreWebView2WebResourceRequestedEventArgs args) {
                    args.Request.Headers.SetHeader("aton-user-agent", $"atonium client {versionString}");
                    
                    CoreWebView2Deferral deferral = args.GetDeferral();
                    
                    if (args.Request.Uri == "https://localhost:7017/account/register" && args.Request.Method == "POST") {
                        args.Request.Headers.SetHeader("aton-user-id", SteamClient.SteamId.Value.ToString());
                        
                        AuthTicket ticket = await SteamUser.GetAuthSessionTicketAsync();
                        string authTicket = HttpUtility.HtmlEncode(Convert.ToBase64String(ticket.Data));
                        
                        args.Request.Headers.SetHeader("aton-user-ticket", authTicket);
                        
                        Console.WriteLine("Submitted proper headers");
                    }
                    
                    deferral.Complete();
                };

            WebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            WebView.NavigationCompleted += (sender, args) => {
                if (!args.IsSuccess) {
                    WebView.CoreWebView2.Navigate("file://" + Directory.GetCurrentDirectory() + @"\Error.html");
                }
            };
            
            WebView.CoreWebView2.Navigate("https://localhost:7017");
        }
    }
}