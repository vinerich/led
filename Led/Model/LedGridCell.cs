﻿using Newtonsoft.Json;

namespace Led.Model
{
    [JsonObject]
    class LedGridCell : INPC
    {
        [JsonProperty]
        public bool Status;

        [JsonProperty]
        public LedViewArrowDirection Direction;

        public LedGridCell()
        {
            Status = true;
            Direction = LedViewArrowDirection.None;
        }
    }
}