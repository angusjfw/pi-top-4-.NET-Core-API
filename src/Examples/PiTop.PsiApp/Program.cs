﻿using System;

using Microsoft.Psi;

using PiTop.MakerArchitecture.Foundation;
using PiTop.MakerArchitecture.Foundation.Components;
using PiTop.MakerArchitecture.Foundation.Psi;
using PiTop.MakerArchitecture.Foundation.Sensors;

namespace PiTop.PsiApp
{
    class Program
    {
        static void Main(string[] args)
        {
            using var pipeline = Pipeline.Create("PiTop", DeliveryPolicy.Unlimited);
            using var module = PiTop4Board.Instance;
            var plate = module.GetOrCreatePlate<FoundationPlate>();

            var threshold = plate
                .GetOrCreateDevice<Potentiometer>(AnaloguePort.A0)
                .CreateComponent(pipeline, TimeSpan.FromSeconds(0.5));

            var distance = plate
                .GetOrCreateDevice<UltrasonicSensor>(DigitalPort.D3)
                .CreateComponent(pipeline, TimeSpan.FromSeconds(0.2));

            var alert = new ValueAlertComponent(pipeline,
                new[]
                {
                   plate.GetOrCreateDevice<Led>(DigitalPort.D0),
                   plate.GetOrCreateDevice<Led>(DigitalPort.D1),
                   plate.GetOrCreateDevice<Led>(DigitalPort.D2)
                });

            threshold
                .Select(t => t * 50)
                .PipeTo(alert.Threshold);

            distance
                .Select(d => d.Value)
                .PipeTo(alert.Value);

            pipeline.Run();
        }
    }
}
