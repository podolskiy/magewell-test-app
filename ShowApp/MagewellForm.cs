using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ExternalAPI.Magewell;

namespace ShowApp
{
    public partial class MagewellForm : Form
    {
        private readonly int _inputId;

        public MagewellForm(int inputId)
        {
            _inputId = inputId;
            InitializeComponent();

            Load += OnLoad;

        }

        private void OnLoad(object sender, EventArgs e)
        {
            Init(_inputId, false, new Random().Next(1, 100), new Size(500, 500));
        }


        private readonly MWCapture _capture = new MWCapture();
        private readonly uint _frameDuration = 400000;
        private readonly uint _fourcc = MWCap_FOURCC.MWCAP_FOURCC_YUY2;

        public void Init(int inputId, bool muteAudio, int volume, Size mediaSize)
        {
            try
            {

                try
                {

                    this.FormClosing += VLCForm_FormClosing;

                    var channelInfo = MWCapture.GetChannelInfoByIndex(inputId);


                    if (!channelInfo.HasValue || !_capture.OpenVideoChannel(channelInfo.Value, _fourcc, mediaSize.Width,
                            mediaSize.Height, _frameDuration, Handle))
                    {
                        MessageBox.Show($"Error opening channel in Magewell plugin. " +
                                     $"_inputId: {inputId}{Environment.NewLine}" +
                                     $"HasVideoChannel: {_capture.HasVideoChannel} " +
                                     $"HasD3DRenderer: {_capture.HasD3DRenderer} " +
                                     $"HasVideo: {_capture.HasVideo} " +
                                     $"HasAudioRender: {_capture.HasAudioRender} " +
                                     $"HasAudio: {_capture.HasAudio}{Environment.NewLine}");

                        Close();
                        return;
                    }
                    _capture.SetMute(muteAudio);
                    _capture.SetAudioVolume(volume);

                    OnResize(EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening channel in Magewell plugin. {ex.Message}");
            }
        }

        void VLCForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _capture.Destory();
                MWCapture.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening channel in Magewell plugin. {ex.Message}");
            }
        }
    }
}
