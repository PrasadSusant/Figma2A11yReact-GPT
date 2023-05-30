using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace FigmaReader
{
    public class MainViewModel : BaseModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the Figma API access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the Figma document url.
        /// </summary>
        public string FigmaUrl { get; set; }

        /// Gets or sets the app feedback message.
        /// </summary>
        public string Message { get; set; }

        /// Gets or sets the Accessibility Issues.
        /// </summary>
        public List<string> AccessibilityIssues { get; set; }

        /// <summary>
        /// Gets or sets the deserialize response message from figma API.
        /// </summary>
        private RootModel RootNode { get; set; }

        /// <summary>
        /// Gets or sets the Visibility for showing the progress bar.
        /// </summary>
        public Visibility ShowLoading { get; set; }

        /// <summary>
        /// Gets or sets the Visibility for showing the files generated link.
        /// </summary>
        public Visibility FilesGenerated { get; set; }

        /// <summary>
        /// Generated output folder path.
        /// </summary>
        private string OutputFolderPath { get; set; }        

        /// Gets or sets the Accessibility Issues.
        /// </summary>
        public string getAccessibilityIssues { 
            get
            {
                if (AccessibilityIssues != null && AccessibilityIssues.Count > 0)
                {
                    var bulletPoints = AccessibilityIssues.Select(item => "• " + item);
                    return string.Join(Environment.NewLine, bulletPoints);
                }

                return null;
            }
        }

        /// Gets or sets the whether figam has any Accessibility Issues.
        /// </summary>
        public Visibility hasAccessibilityIssues
        {
            get
            {
                if (AccessibilityIssues != null && AccessibilityIssues.Count > 0)
                {
                    return Visibility.Visible;
                }

                return Visibility.Hidden;
            }
        }
        

        #endregion Properties

        /// <summary>
        /// Constructor. Do some initialization stuff.
        /// </summary>
        public MainViewModel() : base()
        {
            // default access token
            this.AccessToken = "figd_7tX87I8s4IbNGGHL_-QDJwrt-SXYjHdmv4YPNJZk";
            this.OnPropertyChanged("AccessToken");

            this.ShowLoading = Visibility.Hidden;
            this.FilesGenerated = Visibility.Collapsed;
        }

        #region Public Methods

        /// <summary>
        /// Load figma data.
        /// </summary>
        public void LoadFigma()
        {
            this.ShowLoading = Visibility.Visible;
            this.OnPropertyChanged("ShowLoading");

            this.FilesGenerated = Visibility.Collapsed;
            this.OnPropertyChanged("FilesGenerated");

            this.Message = string.Empty;
            this.OnPropertyChanged("Message");

            this.AccessibilityIssues = new List<string>();
            this.OnPropertyChanged("AccessibilityIssues");
            this.OnPropertyChanged("getAccessibilityIssues");
            this.OnPropertyChanged("hasAccessibilityIssues");

            // Load this asynchronously...
            Task.Run(async () =>
            {
                try
                {
                    var url = this.GetFigmaApiUrl(this.FigmaUrl);
                    var result = await this.RequestWithPATAsync(url);
                    this.OutputFolderPath = $"{Resource1.OutputPath}";

                    Directory.CreateDirectory(Resource1.OutputPath);
                    Directory.CreateDirectory(this.OutputFolderPath);
                    this.CreateResponseLog(result);
                    this.RootNode = Newtonsoft.Json.JsonConvert.DeserializeObject<RootModel>(result);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // get first child with children (i.e. Page related to the url).
                        var figmaPageNode = this.RootNode.Document.Children.Where(n => n.Children.Length > 0).FirstOrDefault();
                        var docNode = figmaPageNode.Children[0];
                        var docName = docNode.Name;
                        this.Message = $"Retrieved '{docName}'";
                        this.OnPropertyChanged("Message");

                        //this.ShowLoading = Visibility.Hidden;
                        //this.OnPropertyChanged("ShowLoading");

                        try
                        {
                            this.OutputFolderPath = new Generator(this).Generate(docNode);

                            //this.Message = $"Generated '{docName}'";
                            //this.OnPropertyChanged("Message");

                            //this.FilesGenerated = Visibility.Visible;
                            //this.OnPropertyChanged("FilesGenerated");
                        }
                        catch (Exception ex)
                        {
                            this.Message = $"{ex.Message}";
                            this.ShowLoading = Visibility.Hidden;
                            this.OnPropertyChanged("ShowLoading");
                            this.OnPropertyChanged("Message");
                        }
                    });
                }
                catch (Exception ex)
                {
                    this.Message = $"{ex.Message}";
                    this.OnPropertyChanged("Message");
                    this.ShowLoading = Visibility.Hidden;
                    this.OnPropertyChanged("ShowLoading");
                }
            });
        }

        /// <summary>
        /// Open the generated folder location.
        /// </summary>
        public void OpenFolder()
        {
            try
            {
                var path = $"{Environment.CurrentDirectory}\\{this.OutputFolderPath}";
                Process.Start("explorer.exe", $"{Environment.CurrentDirectory}\\{this.OutputFolderPath}");
            }
            catch (Exception ex)
            {
                this.Message = $"{ex.Message}";
                this.OnPropertyChanged("Message");
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Get the figma api url based off of the figma document url.
        /// </summary>
        /// <returns>A FigmaUrlModel object.</returns>
        private string GetFigmaApiUrl(string figmaDocUrl)
        {
            // Example diagram url: https://www.figma.com/file/E9Wjl1QqyBijo8ccih0dPk/Upload-Diagram?node-id=1213%3A2307
            // New diagram URL : https://www.figma.com/file/qnm5C6UbGHgkiq2FO8PEok/Prototype?type=design&node-id=59-474&t=4B7NCZFE7AFJlXqh-0
            // Example API url: https://api.figma.com/v1/files/E9Wjl1QqyBijo8ccih0dPk?ids=1213%3A2307

            if (!string.IsNullOrWhiteSpace(figmaDocUrl))
            {
                const string file = "/file/";
                const string nodeId = "node-id=";
                var filesIndex = figmaDocUrl.IndexOf(file) + file.Length;
                var idsIndex = figmaDocUrl.IndexOf(nodeId);

                var str = figmaDocUrl.Substring(filesIndex);
                var fileId = str.Substring(0, str.IndexOf("/"));
                var docId = figmaDocUrl.Substring(idsIndex + nodeId.Length);

                return $"https://api.figma.com/v1/files/{fileId}?ids={docId}";
            }

            return string.Empty;
        }

        /// <summary>
        /// Make http request with access token to Figma API.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<string> RequestWithPATAsync(string url)
        {
            this.Message = $"Requesting {url}";
            this.OnPropertyChanged("Message");

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Figma-Token", this.AccessToken);

                    using (var response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode();
                        var data = response.Content.ReadAsStringAsync().Result;
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Message = $"{ex.Message}";
                this.OnPropertyChanged("Message");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logText"></param>
        private void CreateResponseLog(string logText)
        {
            try
            {
                var fileName = $"{Resource1.OutputPath}\\Logs.txt";
                using (var sw = File.CreateText(fileName))
                {
                    sw.WriteLine(logText);
                }
            }
            catch (Exception ex)
            {
                this.Message = $"{ex.Message}";
                this.OnPropertyChanged("Message");
            }
        }

        #endregion Private Methods
    }
}