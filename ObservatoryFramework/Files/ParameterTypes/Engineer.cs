﻿using System.Text.Json.Serialization;

namespace Observatory.Framework.Files.ParameterTypes
{
    public class EngineerType
    {
        public string Engineer { get; init; }
        public ulong EngineerID { get; init; }
        public int Rank { get; init; }
        public int RankProgress { get; init; }
        public Progress Progress { get; init; }
    }
}
