﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YawGLAPI;

namespace IL2Plugin
{
    public struct Config
    {
        [Info(Description = "UDP port the game is using")]
        public int Port = 4321;

        [Info(Description = "UDP port the game is using")]
        public int TelemetryPort = 4322;

        [Info(Description = "(ms)")]
        public int EventTime = 500;

        [Info(Description = "(degree)")]
        public float YawJumpLimit = 0.05f;

        [Info(Description = "(degree)")]
        public float RollLimitMin = -15.0f;

        [Info(Description = "(degree)")]
        public float RollLimitMax = 15.0f;

        public Config()
        {
        }
    }
}
