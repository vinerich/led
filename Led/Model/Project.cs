﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace Led.Model
{
    [JsonObject]
    class Project : INPC
    {
        [JsonProperty]
        public string ProjectName { get; set; }

        [JsonProperty]
        public byte FramesPerSecond { get; set; }

        [JsonProperty]
        public AudioProperty AudioProperty { get; set; }

        [JsonProperty]
        public List<LedEntity> LedEntities { get; set; }

        public Project(string projectName)
        {
            ProjectName = projectName;
            LedEntities = new List<LedEntity>();
        }
    }
}
