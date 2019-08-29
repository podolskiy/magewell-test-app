using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using ExternalAPI.Magewell;

namespace Magewell
{
    public class MagewellInputsUpdater : InputsUpdater<Input>
    {

        public MagewellInputsUpdater(string pluginName, string ext) : base(pluginName, ext)
        {
        }

        protected override string GetInputName(Input input)
        {
            return "Input " + (input.OrderId);
        }

        public override Input[] GetInputs()
        {
            MWCapture.RefreshDevices();
            var videoChannelCount = MWCapture.GetChannelCount();
            var newInputs = new List<Input>();
            for (int i = 0; i < videoChannelCount; i++)
            {
                LibMWCapture.MWCAP_CHANNEL_INFO channelInfo = new LibMWCapture.MWCAP_CHANNEL_INFO();
                MWCapture.GetChannelInfobyIndex(i, ref channelInfo);

                var hasSignal = false;
                Size channelSize = new Size();

                MWCapture.GetSignalStatus(channelInfo.byChannelIndex, out var audioStatus)
                    ?.HasSignal(out hasSignal)
                    ?.GetChannelSize(out channelSize);

                var audioVolume = audioStatus?.GetAudioVolume() ?? 0;
                var mute = audioStatus?.GetMute() ?? false;

                newInputs.Add(new Input
                {
                    OrderId = channelInfo.byChannelIndex,
                    Available = hasSignal,
                    Width = channelSize.Width,
                    Height = channelSize.Height,
                    Muted = mute,
                    Volume = audioVolume
                });
            }
            return newInputs.ToArray();
        }

        protected override void UpdateVmfu(Input input, string pathToInputVmfuFile)
        {
            var doc = XDocument.Load(pathToInputVmfuFile);
            var enabledParam =
            (from t in doc.Root.Elements("parameter")
             where t.Attribute("name").Value.Equals("enabled")
             select t).FirstOrDefault();

            if (!enabledParam.Value.Equals(input.Available.ToString())) //обновляем по необходимости
            {
                enabledParam.Value = input.Available.ToString();
                doc.Save(pathToInputVmfuFile);
            }
        }

        protected override string GetVmfuXml(Input input, string roomId, string displayId, string pluginName, string displayname)
        {
            return $@"<?xml version='1.0' encoding='utf-8'?>
                                            <meta-info type='videowall' content-url='{displayname}' displayname='{displayname}' file-content-size='0' file-content-modified-date='2012-05-21 16:19:03 UTC+4'>
                                            <parameter name='comment'></parameter>
                                            <parameter name='inputID'>{input.OrderId}</parameter>
                                            <parameter name='displayID'>{displayId}</parameter>
                                            <parameter name='roomID'>{roomId}</parameter>
                                            <parameter name='icon'></parameter>
                                            <parameter name='preview'></parameter>
                                            <parameter name='mute'>{input.Muted}</parameter>
                                            <parameter name='volume'>{input.Volume}</parameter>
                                            <parameter name='media-size'>{input.Width}x{input.Height}</parameter>
                                            <parameter name='enabled'>{input.Available}</parameter>
                                            <parameter name='piugin'>MagewellPlugin</parameter>
                                            </meta-info>";
        }
    }

    public abstract class InputsUpdater<T> : IInputsUpdater
    {
        protected readonly string PluginName;
        protected readonly string VmfuExt;

        protected InputsUpdater(string pluginName, string ext)
        {
            PluginName = pluginName;
            VmfuExt = GetVmfuExt(ext);
        }

        protected string InputsPath => Path.Combine(Environment.CurrentDirectory, "inputs");
        protected bool AppendCardExtention { get; private set; }
        public void Init()
        {
            AppendCardExtention = false;
            StartRefreshThread();
        }

        private void StartRefreshThread()
        {
            var thread = new Thread(() => //Запускаем поток на обновление источников
            {
                BeforeRefreshInputs();

                try
                {
                    while (true)
                    {
                        // обновляет только процесс с наименьшим id, сделано для уменьшения нагрузки + нет необходимости делать одно и тоже нескольким процессам
                        if (ThisInstanceShouldUpdate())
                        {
                            RefreshInputs();
                        }
                        Thread.Sleep(5000);
                    }
                }
                finally
                {
                    AfterRefreshInputs();
                }
            })
            { IsBackground = true };
            thread.Start();
        }

        private bool ThisInstanceShouldUpdate()
        {
            var currentProcess = Process.GetCurrentProcess();
            var first = Process.GetProcessesByName(currentProcess.ProcessName)
                .OrderBy(process => process.Id)
                .First();
            return first.Id == currentProcess.Id;
        }

        protected virtual void AfterRefreshInputs()
        {
        }

        protected virtual void BeforeRefreshInputs()
        {
        }

        protected virtual void RefreshInputs()
        {
            try
            {
                if (!Directory.Exists(InputsPath))
                    Directory.CreateDirectory(InputsPath);

                var inputs = GetInputs();

                foreach (var input in inputs)
                {
                    string inputName = GetInputName(input);

                    string pathToInputVmfuFile = InputsPath + "/" + inputName + VmfuExt;

                    if (File.Exists(pathToInputVmfuFile))
                    {
                        UpdateVmfu(input, pathToInputVmfuFile);
                    }
                    else
                    {

                        var xml = GetVmfuXml(input, "1", "1",
                            PluginName, inputName);

                        File.WriteAllBytes(pathToInputVmfuFile, Encoding.UTF8.GetBytes(xml));
                    }
                }

            }
            catch (Exception ex)
            {
               // Logger.ErrorException(nameof(RefreshInputs), ex);
            }
        }

        protected abstract string GetInputName(T input);

        public abstract T[] GetInputs();

        protected abstract void UpdateVmfu(T input, string pathToInputVmfuFile);

        protected abstract string GetVmfuXml(T input, string roomId, string displayId, string pluginName,
            string displayname);

        protected string GetVmfuExt(string card)
        {
            return AppendCardExtention ? $".{card}.vmfu" : ".vmfu";
        }
    }

    public interface IInputsUpdater
    {
        void Init();
    }
    public class Input
    {
        public int OrderId { get; set; }
        public bool Available { get; set; }
        public bool Muted { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Volume { get; set; }
    }
}