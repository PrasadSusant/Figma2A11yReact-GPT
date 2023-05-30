using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Windows.Media.Animation;

namespace FigmaReader
{
    // These models are needed to deserialize the Figma API response.
    
    public class NodeModel : BaseModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("backGroundColor")]
        public Color BackGroundColor { get; set; }

        [JsonProperty("fills")]
        public Paint[] Fills { get; set; }

        [JsonProperty("background")]
        public Paint[] Background { get; set; }

        [JsonProperty("children")]
        public NodeModel[] Children { get; set; }

        public NodeModel Parent { get; set; }

        //[JsonProperty("exportSettings")]
        //public NodeModel[] ExportSettings { get; set; }

        //[JsonProperty("absoluteBoundingBox")]
        //public AbsoluteBoundingBox BoundingBox { get; set; }

        //[JsonProperty("absoluteRenderBounds")]
        //public AbsoluteRenderBounds RenderBounds { get; set; }

        [JsonProperty("characters")]
        public string Text { get; set; }

        [JsonProperty("style")]
        public Style Style { get; set; }
    }

    public class FullNodeModel : NodeModel
    {

        [JsonProperty("exportSettings")]
        public NodeModel[] ExportSettings { get; set; }

        [JsonProperty("absoluteBoundingBox")]
        public AbsoluteBoundingBox BoundingBox { get; set; }

        //[JsonProperty("absoluteRenderBounds")]
        //public AbsoluteRenderBounds RenderBounds { get; set; }

        [JsonProperty("opacity")]
        public int Opacity { get; set; }
    }

    public class Style
    {
        [JsonProperty("fontSize")]
        public string FontSize { get; set; }
    }

    public class Paint
    {
        //[JsonProperty("blendMode")]
        //public string BlendMode { get; set; }
        //[JsonProperty("type")]
        //public string Type { get; set; }
        [JsonProperty("color")]
        public Color Color { get; set; }


        [JsonProperty("opacity")]
        public int Opacity { get; set; }
    }

    public class RootModel : BaseModel
    {
        [JsonProperty("document")]
        public NodeModel Document { get; set; }
    }

    public class Color 
    {
        public double a { get; set; }
        public double b { get; set; }
        public double g { get; set; }
        public double r { get; set; }
    }

    public class AbsoluteBoundingBox
    {
        public float x { get; set; }
        public float y { get; set; }
        public float width { get; set; }
        public float height { get; set; }
    }

    public class AbsoluteRenderBounds : AbsoluteBoundingBox
    {
    }
}
