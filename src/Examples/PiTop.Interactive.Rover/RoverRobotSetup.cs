using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using lobe;

using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using PiTop.Abstractions;
using PiTop.Algorithms;
using PiTop.Camera;
using PiTop.Interactive.Rover.ML;
using PiTop.MakerArchitecture.Expansion;
using PiTop.MakerArchitecture.Expansion.Rover;
using PiTop.MakerArchitecture.Foundation;
using PiTop.MakerArchitecture.Foundation.Components;
using PiTop.MakerArchitecture.Foundation.Sensors;
using Pocket;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

using UnitsNet;

using static Pocket.Logger;
namespace PiTop.Interactive.Rover
{
    public static class RoverRobotSetup
    {
        public static async Task SetupKernelEnvironment(CSharpKernel csharpKernel)
        {
            using var _ = Log.OnEnterAndExit();

            await ConfigureNamespaces(csharpKernel);
            await ConfigureImageSharp(csharpKernel);
           
            await ConfigurePiTop(csharpKernel);
            await ConfigureLobe(csharpKernel);
            await ConfigureRover(csharpKernel);
        }

        private static async Task ConfigureRover(CSharpKernel csharpKernel)
        {
            using var _ =  Log.OnEnterAndExit();
            await LoadAssemblyAndAddNamespace<RoverRobot>(csharpKernel);
            await LoadAssemblyAndAddNamespace<ResourceScanner>(csharpKernel);
            await AddNamespace(csharpKernel, typeof(ImageProcessing.ImageExtensions));

            var roverBody = new RoverRobot(PiTop4Board.Instance.GetOrCreateExpansionPlate(),
                PiTop4Board.Instance.GetOrCreateCamera<StreamingCamera>(0),
                RoverRobotConfiguration.Default);
            var roverBrain = new RoverRobotStrategies();

            var resourceScanner = new ResourceScanner();

            roverBody.AllLightsOn();
            roverBody.BlinkAllLights();

            await csharpKernel.SetVariableAsync(nameof(roverBody), roverBody);
            await csharpKernel.SetVariableAsync(nameof(roverBrain), roverBrain);
            await csharpKernel.SetVariableAsync(nameof(resourceScanner), resourceScanner);

            var source = new CancellationTokenSource();

            var robotLoop = Task.Run(() =>
            {
                using var operation = Log.OnEnterAndExit("roverBrainLoop");
                while (!source.IsCancellationRequested)
                {
                    var localCancellationSource = new CancellationTokenSource();
                    if (!source.IsCancellationRequested && !localCancellationSource.IsCancellationRequested)
                    {
                        using var __ = operation.OnEnterAndExit("Perceive");
                        try
                        {
                            roverBrain.Perceive?.Invoke(roverBody, DateTime.Now, localCancellationSource.Token);
                        }
                        catch (Exception e)
                        {
                            __.Error(e);
                        }
                    }

                    if (!source.IsCancellationRequested && !localCancellationSource.IsCancellationRequested)
                    {
                        var planResult = PlanningResult.NoPlan;
                        using var ___ = operation.OnEnterAndExit("Plan");
                        try
                        {
                            planResult = roverBrain.Plan?.Invoke(roverBody, DateTime.Now, localCancellationSource.Token) ??
                                             PlanningResult.NoPlan;
                        }
                        catch (Exception e)
                        {
                            ___.Error(e);
                            planResult = PlanningResult.NoPlan;
                        }

                        if (!source.IsCancellationRequested && planResult != PlanningResult.NoPlan && !localCancellationSource.IsCancellationRequested)
                        {
                            using var ____ = operation.OnEnterAndExit("Act");
                            roverBrain.Act?.Invoke(roverBody, DateTime.Now, localCancellationSource.Token);
                        }
                    }
                }

                roverBody.MotionComponent.Stop();
            }, source.Token);

            var reactLoop = Task.Run(() =>
            {
                using var operation = Log.OnEnterAndExit("roverBrainReactLoop");
                while (!source.IsCancellationRequested)
                {
                    var localCancellationSource = new CancellationTokenSource();
                    if (!source.IsCancellationRequested && !localCancellationSource.IsCancellationRequested)
                    {
                        using var __ = operation.OnEnterAndExit("React");
                        try
                        {
                            roverBrain.React?.Invoke(roverBody, DateTime.Now, localCancellationSource.Token);
                        }
                        catch (Exception e)
                        {
                            __.Error(e);
                        }
                    }
                }

                roverBody.MotionComponent.Stop();
            }, source.Token);

            csharpKernel.RegisterForDisposal(() =>
            {
                source.Cancel(false);
                Task.WaitAll(new[] {robotLoop, reactLoop}, TimeSpan.FromSeconds(10));
                roverBody.Dispose();
            });
        }

        private static async Task ConfigureNamespaces(CSharpKernel csharpKernel)
        {
            await AddNamespace(csharpKernel, typeof(Task));
            await AddNamespace(csharpKernel, typeof(Directory));
            await AddNamespace(csharpKernel, typeof(List<>));
            await AddNamespace(csharpKernel, typeof(System.Linq.Enumerable));
          
        }

        private static async Task ConfigureLobe(CSharpKernel csharpKernel)
        {
            ImageClassifier.Register("onnx", () => new OnnxImageClassifier());
            await LoadAssemblyAndAddNamespace<ImageClassifier>(csharpKernel);
            await LoadAssemblyAndAddNamespace<OnnxImageClassifier>(csharpKernel);
            await AddNamespace(csharpKernel, typeof(ClassificationResults));
        }

        private static async Task ConfigurePiTop(CSharpKernel csharpKernel)
        {
            PiTop4Board.Instance.UseCamera();

            var piTopExtension = new InteractiveExtension.KernelExtension();
            await piTopExtension.OnLoadAsync(csharpKernel);

            var cameraExtension = new Camera.InteractiveExtension.KernelExtension();
            await cameraExtension.OnLoadAsync(csharpKernel);

            var foundationExtension = new MakerArchitecture.Foundation.InteractiveExtension.KernelExtension();
            await foundationExtension.OnLoadAsync(csharpKernel);

            var expansionExtension = new MakerArchitecture.Expansion.InteractiveExtension.KernelExtension();
            await expansionExtension.OnLoadAsync(csharpKernel);

            await LoadAssemblyAndAddNamespace<PiTop4Board>(csharpKernel);
            await LoadAssemblyAndAddNamespace<StreamingCamera>(csharpKernel);
            await LoadAssemblyAndAddNamespace<FoundationPlate>(csharpKernel);
            await LoadAssemblyAndAddNamespace<ExpansionPlate>(csharpKernel);
            await LoadAssemblyAndAddNamespace<Speed>(csharpKernel);

            await AddNamespace(csharpKernel, typeof(Button));
            await AddNamespace(csharpKernel, typeof(Led));
            await AddNamespace(csharpKernel, typeof(IFilter));
            await AddNamespace(csharpKernel, typeof(Display));
        }

        private static async Task ConfigureImageSharp(CSharpKernel csharpKernel)
        {
            await LoadAssemblyAndAddNamespace<Image>(csharpKernel);
            await LoadAssemblyAndAddNamespace<Color>(csharpKernel);
            await LoadAssemblyAndAddNamespace<ComplexPolygon>(csharpKernel);
            await LoadAssemblyAndAddNamespace<Font>(csharpKernel);
            await AddNamespace(csharpKernel, typeof(ImageExtensions));

        }

        private static async Task LoadAssemblyAndAddNamespace<T>(CSharpKernel csharpKernel)
        {
            await csharpKernel.SendAsync(new SubmitCode(@$"#r ""{typeof(T).Assembly.Location}""
using {typeof(T).Namespace};"));
        }

        private static async Task AddNamespace(CSharpKernel csharpKernel, Type type)
        {
            await csharpKernel.SendAsync(new SubmitCode(@$"using {type.Namespace};"));
        }
    }
}