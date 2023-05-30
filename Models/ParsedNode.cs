using System.Collections.Generic;
using System.ComponentModel;

namespace FigmaReader
{
    public class ParsedNode
    {
        public string Prefix { get; set; }
        public string Name { get; set;  }
        public string[] Attributes {get; set; }
    }
}
