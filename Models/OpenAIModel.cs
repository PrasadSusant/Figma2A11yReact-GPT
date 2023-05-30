using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Windows.Media.Animation;

namespace FigmaReader
{
    // These models are needed to deserialize the Chat GPT Open AI response.
    
    public class ModelPrompt
    {
        [JsonProperty("prompt")]
        public string Prompt
        {
            get;
            set;
        }

        [JsonProperty("model")]
        public string Model
        {
            get;
            set;
        }

        [JsonProperty("max_tokens")]
        public int MaxTokens
        {
            get;
            set;
        }

        [JsonProperty("temperature")]
        public double Temperature
        {
            get;
            set;
        }

        [JsonProperty("top_p")]
        public int TopP
        {
            get;
            set;
        }

        [JsonProperty("n")]
        public int N
        {
            get;
            set;
        }

        [JsonProperty("stream")]
        public bool Stream
        {
            get;
            set;
        }

        //[JsonProperty("logprobs")]
        //public object LogProbs
        //{
        //    get;
        //    set;
        //}

        [JsonProperty("stop")]
        public string Stop
        {
            get;
            set;
        }
    };

    public class Choice
    {
        [JsonProperty("text")]
        public string Text
        {
            get;
            set;
        }

        [JsonProperty("index")]
        public int Index
        {
            get;
            set;
        }

        [JsonProperty("logprobs")]
        public object LogProbs
        {
            get;
            set;
        }

        [JsonProperty("finish_reason")]
        public string FinishReason
        {
            get;
            set;
        }
    }

    public class StreamResponse
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonProperty("object")]
        public string Object
        {
            get;
            set;
        }

        [JsonProperty("created")]
        public int Created
        {
            get;
            set;
        }

        [JsonProperty("choices")]
        public List<Choice> Choices
        {
            get;
            set;
        }

        [JsonProperty("model")]
        public string Model
        {
            get;
            set;
        }
    }

    public class UploadFileResponse
    {
        [JsonProperty("id")]
        public string Id
        {
            get;
            set;
        }

        [JsonProperty("object")]
        public string Object
        {
            get;
            set;
        }

        [JsonProperty("bytes")]
        public double Bytes
        {
            get;
            set;
        }

        //[JsonProperty("created_at")]
        //public string CreatedAt
        //{
        //    get;
        //    set;
        //}

        [JsonProperty("filename")]
        public string Filename
        {
            get;
            set;
        }

        [JsonProperty("purpose")]
        public string Purpose
        {
            get;
            set;
        }
    }
}
