﻿using System;
using System.Threading;

namespace PiTop.MakerArchitecture.Expansion.Rover
{
    public class RoverRobotStrategies
    {
        public Action<RoverRobot, DateTime, CancellationToken> Perceive { get; set; }

        public Func<RoverRobot, DateTime, CancellationToken, PlanningResult> Plan { get; set; }

        public Action<RoverRobot, DateTime, CancellationToken> Act { get; set; }

        public Action<RoverRobot, DateTime, CancellationToken> React { get; set; }
    }
}