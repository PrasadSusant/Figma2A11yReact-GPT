using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FigmaReader.Constants
{
    internal class Controls
    {
        public const string Button = "Button";
        public const string DefaultButton = "DefaultButton";
        public const string PrimaryButton = "PrimaryButton";
        public const string Dropdown = "Dropdown";

        public static readonly Dictionary<string, List<string>> AttributeKeyValue = new Dictionary<string, List<string>>
        {
            {"disabled", new List<string> { "disabled","disable" } }
        };
    }
}
