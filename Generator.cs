using FigmaReader.Constants;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FigmaReader
{
    public class Generator
    {
        private string OutputFolderPath;

        private string DocumentName;

        private string[] GPTResponse;

        private MainViewModel viewModel;

        private NodeModel RootNode;

        private readonly string AppName;

        private readonly string AppVersion;

        private readonly Dictionary<string, object> PrefixLookupTable; // TODO: use this table to check for valid element names.

        private readonly Dictionary<string, StyleObject> NodeModelStyles;

        private readonly Dictionary<string, string> ValidGPTNodes;

        private enum RenderMode
        {
            Component = 0,
            Style,
            Test,
            Type,
            GPTComponent
        };

        /// <summary>
        /// 
        /// </summary>
        public Generator(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.AppName = Assembly.GetEntryAssembly().GetName().Name;
            this.AppVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            this.PrefixLookupTable = new Dictionary<string, object>();
            this.NodeModelStyles = new Dictionary<string, StyleObject>();
            this.ValidGPTNodes = new Dictionary<string, string>();
            this.DocumentName = "";
            
            var prefixList = Resource1.ValidNodePrefixList;
            var prefixWords = prefixList.Split(new char[] { ',', ';' });

            foreach (var word in prefixWords)
            {
                this.PrefixLookupTable.Add(word.ToUpper(), true);
            }
        }

        /// <summary>
        /// Generate output.
        /// </summary>
        public string Generate(NodeModel docNode)
        {
            try
            {
                var docName = docNode.Name;
                docName = docName.Replace(" ", String.Empty);
                docName = this.ParseName(docName).Name;
                this.DocumentName = docName;
                this.OutputFolderPath = $"{Resource1.OutputPath}\\{docName}";
                this.RootNode = docNode;

                Directory.CreateDirectory(Resource1.OutputPath);
                Directory.CreateDirectory(this.OutputFolderPath);

                this.CreateComponentFile(docName);
                this.CreateComponentStyleFile(docName);
                this.CreateComponentTypeFile(docName);
                this.CreateComponentTestFile(docName);

                return this.OutputFolderPath;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Cleanup node name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ParsedNode ParseName(string name)
        {
            // get string after ':'
            var i = name.IndexOf(":");
            var newName = (i >= 0) ? name.Substring(i + 1).Trim() : name.Trim();

            var attributeName = string.Empty;
            // TODO : For now taking Last index of "/", need to take all "/" separated attribute values.
            var attributeIndex = newName.LastIndexOf("/");
            if (attributeIndex >= 0)
            {
                attributeName = newName.Substring(attributeIndex + 1).Trim().ToLower();
            }

            newName = (attributeIndex >= 0) ? newName.Substring(0, attributeIndex + 1).Trim() : newName;

            // replace white spaces
            newName = newName.Replace(" ", string.Empty);

            // replace special characters
            newName = newName.Replace("/", string.Empty).Replace(".", string.Empty).Replace("_", string.Empty);

            return new ParsedNode()
            {
                Prefix = (i >= 0) ? name.Substring(0, i) : string.Empty,
                Name = newName,
                Attributes = new string[]{ attributeName }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        private void ParseNodeStyles(NodeModel node)
        {
            var parsedNode = this.ParseName(node.Name);
            var styleObject = new StyleObject(node);
            styleObject.StyleName = $"{parsedNode.Name.ToLower()}Style";

            if (node.BackGroundColor != null)
            {
                if (!this.NodeModelStyles.ContainsKey(node.Id))
                {                    
                    styleObject.Properties.Add("backgroundColor");                    
                }
            }

            if (!this.NodeModelStyles.ContainsKey(node.Id))
            {
                this.NodeModelStyles.Add(node.Id, styleObject);
            }
        }

        /// <summary>
        /// Create styles. Currently only supports colors.
        /// </summary>
        /// <returns></returns>
        private string GenerateStyles()
        {
            var sb = new StringBuilder();

            foreach (var key in this.NodeModelStyles.Keys)
            {                
                var styleObject = this.NodeModelStyles[key];
                sb.Append($"export const {styleObject.StyleName}: React.CSSProperties = {{");

                foreach (var style in styleObject.Properties)
                {
                    sb.Append(Environment.NewLine);

                    // emit color as rgba.
                    if (style.ToUpper().IndexOf("COLOR") >= 0)
                    {
                        var color = styleObject.Node.BackGroundColor;
                        sb.Append($"    {style}: rgba({color.r}, {color.g}, {color.b}, {color.a});");
                    }                    
                }

                sb.Append(Environment.NewLine);
                sb.Append("}");
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Recursively generate XML string for render body.
        /// </summary>
        /// <returns></returns>
        private string GenerateComponentXml(string leftPadding)
        {
           var sb = new StringBuilder();
           this.RootNode.Parent = null;
           this.GenerateRecursively(this.RootNode, leftPadding, sb);
           this.GenerateCodeUsingOpenAI();
           return sb.ToString();
        }


        private async void GenerateCodeUsingOpenAI()
        {

            //var resp = await OpenAIService.UploadFile();
            var fileUploadId = "file-MiRiSVHd1c7r2SRT4u6Ls8n8";
            var fineTuneModelId = "ft-DkqvS3XcVouDC3aDMGJe4TNi";
            //var result = await OpenAIService.CreateFineTuneModel(fileUploadId);
            //var resultTuneModelId = result.id;
            //var result1 = await OpenAIService.GetFineTuneModelById(fineTuneModelId);
            //var resp1 = await OpenAIService.DeleteFile();
            //var resp = await OpenAIService.DeleteFineTuneModel();
            try
            {
                if (string.IsNullOrWhiteSpace(OpenAIService.ApiKey))
                {
                    throw new Exception("Please provide GPT Model API Key.");
                }

                var results = await this.ExecuteTaskAsync(this.ValidGPTNodes);
                this.GPTResponse = results;

                CreateComponentGPTFile(this.DocumentName);

                viewModel.Message = string.Empty;
                viewModel.OnPropertyChanged("Message");

                viewModel.FilesGenerated = Visibility.Visible;
                viewModel.OnPropertyChanged("FilesGenerated");

                viewModel.ShowLoading = Visibility.Collapsed;
                viewModel.OnPropertyChanged("ShowLoading");
            }

            catch(Exception ex)
            {
                viewModel.Message = $"{ex.Message}";
                viewModel.ShowLoading = Visibility.Hidden;
                viewModel.OnPropertyChanged("ShowLoading");
                viewModel.OnPropertyChanged("Message");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }  
        }

        private async Task<string[]> ExecuteTaskAsync(Dictionary<string, string> validNodes)
        {
            viewModel.Message = "Generating files using Open API..";
            viewModel.OnPropertyChanged("Message");

            List<Task<string>> responses = new List<Task<string>>();

            // Reverse the list so that 1st figma node is populated first.
            var validNodesValues = validNodes.Values.Reverse().ToList(); 
            var tasks = validNodesValues.Select(async nodeValue =>
            {
                var result = await OpenAIService.CallOpenAPI(nodeValue);

                // Covert response to response model
                StreamResponse reponseModel = Newtonsoft.Json.JsonConvert.DeserializeObject<StreamResponse>(result);
                if(reponseModel != null && reponseModel.Choices != null)
                {
                    if(reponseModel.Choices.Count > 0)
                    return reponseModel.Choices[0]?.Text;
                }
                return string.Empty;
            });

            var results = await Task.WhenAll(tasks);

            Console.WriteLine("All tasks completed. Task count:");

            return results;
        }

        /// <summary>
        /// Checks if a node is a valid node for XML generation.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool EvaluateNodeValidity(NodeModel node)
        {
            var parsedNode = this.ParseName(node.Name);
            if (this.PrefixLookupTable.ContainsKey(node.Name.ToUpper()))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(parsedNode.Prefix))
            {
                if (this.PrefixLookupTable.ContainsKey(parsedNode.Prefix.ToUpper()))
                {
                    return true;
                }
            }

            return false;
        }

        private  NodeModel[] GetNodeModelInstance(NodeModel[] nodeModels)
        {
            //NodeModel[] baseModels = { };
            //for (int i =0; i< nodeModels.Length; i++)
            //{
            //    var model = new NodeModel();
            //    model = nodeModels[i];
            //    baseModels.Append(model);
            //}

            var models = nodeModels.Select(a => new NodeModel()
            {
                Background = a.Background,
                BackGroundColor = a.BackGroundColor,
                Children = a.Children,
                Fills = a.Fills,
                Name = a.Name,
                Style = a.Style,
                Text = a.Text,
                Type = a.Type
            });


            return models.ToArray();
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="leftPadding"></param>
        /// <param name="sb"></param>
        /// <param name="level"></param>
        private void GenerateRecursively(NodeModel node, string leftPadding, StringBuilder sb, int level = 0)
        {
           
            var isTextElement = node.Type == "TEXT";
            var isValidElement = this.EvaluateNodeValidity(node);

            var tabs = new StringBuilder();
            for (var i = 0; i < level - 1; ++i)
            {
                tabs.Append("   ");
            }

            var tabString = tabs.ToString(); // indention string
            var nodeName = this.ParseName(node.Name).Name;
            var prefixString = string.Empty;

            if (isValidElement)
            {
                // TODO: 
                if (node.Type.ToUpper() == "GROUP")
                {
                    if (!this.ValidGPTNodes.ContainsKey(node.Id))
                    {
                        var jsonObject = JsonConvert.SerializeObject(node.Children);
                        this.ValidGPTNodes.Add(node.Id, jsonObject);
                    }
                }
                this.ParseNodeStyles(node);
            }

            if (level > 0)
            {
                var styleAttribute = string.Empty;
                if (this.NodeModelStyles.ContainsKey(node.Id))
                {
                    var styleObject = this.NodeModelStyles[node.Id];
                    styleAttribute = $" style={{{styleObject.StyleName}}}";
                }

                if (isTextElement && isValidElement)
                {
                    prefixString = (sb.Length == 0) ? string.Empty : $"{leftPadding}{tabString}";
                    sb.Append($"{prefixString}<Label{styleAttribute}>{node.Text}</Label>");
                    sb.Append(Environment.NewLine);
                }
                else if (isValidElement)
                {
                    prefixString = (sb.Length == 0) ? string.Empty : $"{leftPadding}{tabString}";
                    sb.Append($"{prefixString}<{nodeName}{styleAttribute}>");
                    sb.Append(Environment.NewLine);
                }
            }

            if (!isTextElement)
            {
                // recurse...
                if (node.Children != null)
                {
                    // TODO: need to order by X & Y components. The below does not order things horizontally.
                    var sortedChildren = node.Children; //.OrderBy(c => c.BoundingBox.y).ToArray();
                    foreach (var n in sortedChildren)
                    {
                        n.Parent = node;
                        this.GenerateRecursively(n, leftPadding, sb, level + 1);
                    }
                }

                if (isValidElement && level > 0)
                {
                    CheckColorContrastAccessibility(node);
                    this.AppendAttributes(node, sb);
                    sb.Append($"{leftPadding}{tabString}</{nodeName}>");
                    sb.Append(Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Append Attributes.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sb"></param>
        private void AppendAttributes(NodeModel node, StringBuilder sb)
        {
            var nodeName = this.ParseName(node.Name);
            switch (nodeName.Name)
            {
                case Controls.DefaultButton:
                case Controls.PrimaryButton:
                case Controls.Button:
                    // Loop upto 2 children level for any text values. If found, append to the control as attributes.
                    var textChildren = node.Children.Where(c => c.Type == "TEXT").FirstOrDefault();
                    string defaultAttributetext = "onClick={{_clickEvent}}>";
                    //NodeModel parentBackGround = node;
                    if (textChildren == null && node.Children != null)
                    {
                        textChildren = node.Children.FirstOrDefault();
                        textChildren = textChildren.Children.Where(c => c.Type == "TEXT").FirstOrDefault();
                        //parentBackGround = node.Children.FirstOrDefault();
                    }
                    if (textChildren != null)
                    {
                        defaultAttributetext = String.Format("text={{'{0}'}} {1}", textChildren.Text, defaultAttributetext);
                        // Compute color contrast for accessibility.
                        //ColorContrast(parentBackGround);
                    }
                    AddAttributes(nodeName, sb, defaultAttributetext);
                    break;
                case Controls.Dropdown:
                    defaultAttributetext = "onChange={{_changeEvent}}>";
                    AddAttributes(nodeName, sb, defaultAttributetext);
                    break;
                default:
                    break;
            }           
        }

        private void CheckColorContrastAccessibility(NodeModel node)
        {
            // Check for any text and if exists, check for color contarst.
            var textNode = node.Type == "TEXT" ? node : null;
            if (ValidBackground(node.Parent))
            {
                NodeModel parentBackGround = node.Parent;
                if (textNode == null && node?.Children != null)
                {
                    textNode = node.Children.Where(c => c.Type == "TEXT").FirstOrDefault();
                    if (textNode == null && node.Children != null)
                    {
                        textNode = node.Children.FirstOrDefault();
                        textNode = textNode.Children.Where(c => c.Type == "TEXT").FirstOrDefault();
                        parentBackGround = node.Children.FirstOrDefault();
                    }

                    if (textNode != null)
                    {
                        // Compute color contrast for accessibility.
                        ColorContrast(parentBackGround);
                    }
                }
            }
        }

        private bool ValidBackground(NodeModel node)
        {
            return (node.Type.ToUpper() == "RECTANGLE") || (node.Type.ToUpper() == "VECTOR") || (node.Type.ToUpper() == "FRAME") || (node.Type.ToUpper() == "GROUP");

        }

        //private bool CompletelyVisible(NodeModel node)
        //{
        //    var fullNodeModel= node as FullNodeModel;
        //    if (node.Parent != null && node.Parent.Type == "PAGE")
        //    {
        //        return node.Visible;
        //    }
        //    else if ((!node.Visible) || (fullNodeModel.Opacity < 1))
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        return CompletelyVisible(node.Parent);
        //    }
        //}

        //private bool HasSolidFill(NodeModel node)
        //{
        //    if (node.Type == "FRAME")
        //    {
        //        return (node.Background.Length == 1);
        //    }
        //    else
        //    {
        //        return (node.Fills.Length == 1) && (node.Fills[0].Opacity == 1);
        //    }
        //}

        private void ColorContrast(NodeModel node)
        {
            var backGroundColor = node.BackGroundColor;
            if(backGroundColor == null)
            {
                if (node.Fills != null && node.Fills.Length > 0)
                {
                    backGroundColor = node.Fills[0].Color;
                }
                else
                {
                    if (node.Background != null && node.Background.Length > 0)
                    {
                        backGroundColor = node.Background[0].Color;
                    }
                }
            }

            var textNode = node.Children.FirstOrDefault();

            if (textNode != null) {
                var textColor = textNode.BackGroundColor;

                if (textNode.Fills != null && textNode.Fills.Length > 0)
                {
                    textColor = textNode.Fills[0].Color;
                }
                else
                {
                    if (textNode.Background != null && textNode.Background.Length > 0)
                    {
                        textColor = textNode.Background[0].Color;
                    }
                }

                double textFontSize = 0;
                if(double.TryParse(textNode?.Style?.FontSize, out textFontSize))
                {
                    var textLuminance = GetRelativeLuminance(textColor);
                    var backGroundLuminance = GetRelativeLuminance(backGroundColor);
                    var rawContrastRatio = (Math.Max(textLuminance, backGroundLuminance) + 0.05) / (Math.Min(textLuminance, backGroundLuminance) + 0.05);
                    var contrastRatio = Math.Round(rawContrastRatio, 4);
                    if (((textFontSize >= 19) && (contrastRatio < 3)) || ((textFontSize < 19) && (contrastRatio < 4.5)))
                    {
                        if (this.viewModel.AccessibilityIssues == null) this.viewModel.AccessibilityIssues = new List<string>();
                        var contratsRatio = textFontSize > 19 ? 3 : 4.5;
                        this.viewModel.AccessibilityIssues.Add($"'{textNode.Text}' contrast ratio is {contrastRatio} against the background but it should be at least {contratsRatio} for font size {textFontSize}");
                        this.viewModel.OnPropertyChanged("getAccessibilityIssues");
                        this.viewModel.OnPropertyChanged("hasAccessibilityIssues");
                    }
                }
            }
        }

        private double GammaCorrect(double colorValue)
        {
            if (colorValue <= 0.04045) 
            { 
                return colorValue / 12.92; 
            }
            else 
            {
                return Math.Pow((colorValue + 0.055) / 1.055, 2.4); 
            }

        }

        private double GetRelativeLuminance(Color color)
        {
            var relativeLuminance = 0.2126 * GammaCorrect(color.r) + 0.7152 * GammaCorrect(color.g) + 0.0722 * GammaCorrect(color.b);
            return relativeLuminance;
            //  var textLuminance =
            //convertRGB(textColor.r) * 0.2126 + convertRGB(textColor.g) * 0.7152 + convertRGB(textColor.b) * 0.0722;
            //  var bgLuminance = convertRGB(bgColor.r) * 0.2126 + convertRGB(bgColor.g) * 0.7152 + convertRGB(bgColor.b) * 0.0722;
            //  var contrastRatio = (Math.max(textLuminance, bgLuminance) + 0.05) / (Math.min(textLuminance, bgLuminance) + 0.05);
            //  return parseFloat(contrastRatio.toFixed(2));
        }

        /// <summary>
        /// Add Attributes
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sb"></param>
        /// <param name="specificControlAttribute"></param>
        private void AddAttributes(ParsedNode node, StringBuilder sb, string controlSpecificAttribute)
        {
            var index = sb.ToString().LastIndexOf('>');
            if (index >= 0)
            {
                string attributeValue = string.Empty;
                string attributeKey = string.Empty;
                if (node.Attributes.Length > 0)
                {
                    attributeValue = node.Attributes[0];
                    attributeKey = Controls.AttributeKeyValue.FirstOrDefault(v => v.Value.Contains(attributeValue.ToLower())).Key;
                    controlSpecificAttribute = String.Format(" {0} {1}", attributeKey, controlSpecificAttribute);
                }
                sb.Replace(">", controlSpecificAttribute, index, 1);
            }
        }

        /// <summary>
        /// Write Files Using Template
        /// </summary>
        /// <param name="filePath">File Path Name</param>
        /// <param name="templateFile">Template File Name</param>
        /// <param name="docName">Document Name</param>

        private void WriteFileUsingTemplate(string filePath, string templateFile, string docName, RenderMode mode)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var sw = File.CreateText(filePath))
            {
                using (var sourceTemplateFile = File.OpenText(templateFile))
                {
                    string line;
                    // read lines till the end of the file.
                    while ((line = sourceTemplateFile.ReadLine()) != null)
                    {
                        // component name
                        if (line.IndexOf("[componentName]") >= 0)
                        {
                            line = line.Replace("[componentName]", docName);
                        }

                        // auto-generated text
                        if (line.IndexOf("[auto-generated-text]") >= 0)
                        {
                            line = line.Replace("[auto-generated-text]", string.Format(Resource1.AutoGeneratedTemplate, this.AppName, this.AppVersion));
                        }

                        if (mode == RenderMode.Component)
                        {
                            // render body
                            if (line.IndexOf("[component-render-body]") >= 0)
                            {
                                var leftStringSpace = line.Substring(0, line.IndexOf("[component-render-body]"));
                                line = line.Replace("[component-render-body]", this.GenerateComponentXml(leftStringSpace));
                            }
                        }

                        if (mode == RenderMode.GPTComponent)
                        {
                            // render body
                            if (line.IndexOf("[render-component]") >= 0)
                            {
                                var leftStringSpace = line.Substring(0, line.IndexOf("[render-component]"));
                                line = line.Replace("[render-component]", string.Join(" ", this.GPTResponse));
                            }
                        }

                        else if (mode == RenderMode.Style)
                        {
                            if (line.IndexOf("[style-classes]") >= 0)
                            {
                                line = line.Replace("[style-classes]", this.GenerateStyles());
                            }
                        }

                        // Write the modified line to the new file
                        sw.WriteLine(line);
                    }
                }
            }

            // Sanitize it.
            if (mode == RenderMode.GPTComponent)
                RemoveAllDuplicateImportLines(filePath);
        }


        private void RemoveAllDuplicateImportLines(string templateFile)
        {
           
             // Read all lines from the file
            string[] lines = File.ReadAllLines(templateFile);

            List<string> uniqueLines = new List<string>();
            
            // Create a HashSet to store unique lines
            HashSet<string> unqiueComponent = new HashSet<string>();

            // Filter out duplicate lines
            foreach (string line in lines)
                {
                    if (line.TrimStart().StartsWith("import") && !uniqueLines.Contains(line))
                    {
                        uniqueLines.Insert(1, line);
                    }
                    else if (!line.TrimStart().StartsWith("export default") && !line.TrimStart().StartsWith("import"))
                    {
                        uniqueLines.Add(line);
                    }
               
                    if (line.TrimStart().StartsWith("export default"))
                    {
                        string keyword = "export default";
                        string componentName = line.Substring(line.IndexOf(keyword) + keyword.Length);
                        componentName = componentName.Replace(";", "");
                        unqiueComponent.Add(componentName.Trim());
                    }

                    // TODO: Extract exprt { comp1, comp2 } from this sentence as well.

                    if (line.TrimStart().StartsWith("<> {/* Render */}"))
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (string comp in unqiueComponent)
                            {
                                sb = sb.Append("\t\t\t\t {" + comp + "()}\n");
                            }
                            uniqueLines.Add(sb.ToString());
                        }
                    }

                // Write the filtered lines back to the file
                File.WriteAllLines(templateFile, uniqueLines);
        }

        /// <summary>
        /// Create component TSX file.
        /// </summary>
        /// <param name="docName">Document Name</param>
        private void CreateComponentFile(string docName)
        {
            string filePath = $"{this.OutputFolderPath}/{docName}.tsx";
            string templateFile = Environment.CurrentDirectory + @"\Templates\component.txt";
            this.WriteFileUsingTemplate(filePath, templateFile, docName, RenderMode.Component);
        }

        /// <summary>
        /// Create component TSX file by GPT.
        /// </summary>
        /// <param name="docName">Document Name</param>
        private void CreateComponentGPTFile(string docName)
        {
            string filePath = $"{this.OutputFolderPath}/{docName}_GPT.tsx";
            string templateFile = Environment.CurrentDirectory + @"\Templates\componentGPT.txt";
            this.WriteFileUsingTemplate(filePath, templateFile, docName, RenderMode.GPTComponent);
        }

        /// <summary>
        /// Create Component Type file.
        /// </summary>
        /// <param name="docName">Document Name</param>
        private void CreateComponentTypeFile(string docName)
        {
            string fileName = $"{this.OutputFolderPath}/{docName}.types.ts";
            string templateFile = Environment.CurrentDirectory + @"\Templates\type.txt";
            this.WriteFileUsingTemplate(fileName, templateFile, docName, RenderMode.Type);
        }

        /// <summary>
        /// Create component style file.
        /// </summary>
        /// <param name="fileName"></param>
        private void CreateComponentStyleFile(string docName)
        {
            string fileName = $"{this.OutputFolderPath}/{docName}.styles.ts";
            string templateFile = Environment.CurrentDirectory + @"\Templates\style.txt";
            this.WriteFileUsingTemplate(fileName, templateFile, docName, RenderMode.Style);
        }

        /// <summary>
        /// Create component test file.
        /// </summary>
        /// <param name="fileName"></param>
        private void CreateComponentTestFile(string docName)
        {
            string fileName = $"{this.OutputFolderPath}/{docName}.test.tsx";
            string templateFile = Environment.CurrentDirectory + @"\Templates\test.txt";
            this.WriteFileUsingTemplate(fileName, templateFile, docName, RenderMode.Test);
        }
    }
}
