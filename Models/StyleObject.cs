using System.Collections.Generic;
using System.ComponentModel;
using System.Xaml;

namespace FigmaReader
{
    public class StyleObject
    {
        public List<string> Properties { get; set; } // ex: backgroundColor

        public string StyleName { get; set; } // ex: buttonStyle

        public NodeModel Node { get; set; }

        private static Dictionary<string, int> StyleCount = new Dictionary<string, int>();

        public StyleObject(NodeModel node)
        {
            this.Node = node;
            this.Properties = new List<string>();
        }
    }
}
