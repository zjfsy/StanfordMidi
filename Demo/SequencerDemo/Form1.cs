using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Sanford.Multimedia.Midi;
using Sanford.Multimedia.Midi.UI;

namespace SequencerDemo
{
    public partial class Form1 : Form
    {
        private bool scrolling = false;

        private bool playing = false;

        private bool closing = false;

        private OutputDevice outDevice;

        private int outDeviceID = 0;

        private OutputDeviceDialog outDialog = new OutputDeviceDialog();

        public Form1()
        {
            InitializeComponent();
            MoveControl();
            rbtn1.Select();
            playMode = 0;
            midiList = new Dictionary<string, string>();
        }

        protected override void OnLoad(EventArgs e)
        {
            if(OutputDevice.DeviceCount == 0)
            {
                MessageBox.Show("No MIDI output devices available.", "Error!",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);

                Close();
            }
            else
            {
                try
                {
                    outDevice = new OutputDevice(outDeviceID);

                    sequence1.LoadProgressChanged += HandleLoadProgressChanged;
                    sequence1.LoadCompleted += HandleLoadCompleted;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    Close();
                }
            }

            base.OnLoad(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            pianoControl.PressPianoKey(e.KeyCode);

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            pianoControl.ReleasePianoKey(e.KeyCode);

            base.OnKeyUp(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            closing = true;

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            sequence1.Dispose();

            if(outDevice != null)
            {
                outDevice.Dispose();
            }

            outDialog.Dispose();

            base.OnClosed(e);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openMidiFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openMidiFileDialog.FileName;
                string midiName = Path.GetFileName(fileName);
                if (midiList.ContainsKey(midiName))
                {
                    MessageBox.Show("您已添加过这首MIDI！", "提示");
                }
                else
                {
                    midiList.Add(midiName, fileName);
                    listBox1.Items.Add(midiName);
                    listBox1.SelectedItem = midiName;
                    Open(fileName);
                }
            }
        }

        public void Open(string fileName)
        {
            try
            {
                sequencer1.Stop();
                sequencer1.Position = 0;
                playing = false;
                sequence1.LoadAsync(fileName);
                this.Cursor = Cursors.WaitCursor;
                startButton.Enabled = false;
                continueButton.Enabled = false;
                stopButton.Enabled = false;
                openToolStripMenuItem.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void outputDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutDialog dlg = new AboutDialog();

            dlg.ShowDialog();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            try
            {
                playing = false;
                sequencer1.Stop();
                timer1.Stop();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (listBox1.Items.Count == 0)
                {
                    MessageBox.Show("请添加MIDI文件到播放列表中！", "提示");
                }
                else
                {
                    Open(midiList[listBox1.SelectedItem.ToString()]);
                    playing = true;
                    sequencer1.Start();
                    timer1.Start();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void continueButton_Click(object sender, EventArgs e)
        {
            try
            {
                playing = true;
                sequencer1.Continue();
                timer1.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void positionHScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if(e.Type == ScrollEventType.EndScroll)
            {
                sequencer1.Position = e.NewValue;

                scrolling = false;
            }
            else
            {
                scrolling = true;
            }
        }

        private void HandleLoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar.Value = e.ProgressPercentage;
        }

        private void HandleLoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            startButton.Enabled = true;
            continueButton.Enabled = true;
            stopButton.Enabled = true;
            openToolStripMenuItem.Enabled = true;
            toolStripProgressBar.Value = 0;

            if(e.Error == null)
            {
                positionHScrollBar.Value = 0;
                positionHScrollBar.Maximum = sequence1.GetLength();
            }
            else
            {
                MessageBox.Show(e.Error.Message);
            }
        }

        private void HandleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            if(closing)
            {
                return;
            }

            outDevice.Send(e.Message);
            pianoControl.Send(e.Message);
        }

        private void HandleChased(object sender, ChasedEventArgs e)
        {
            foreach(ChannelMessage message in e.Messages)
            {
                outDevice.Send(message);
            }
        }

        private void HandleSysExMessagePlayed(object sender, SysExMessageEventArgs e)
        {
       //     outDevice.Send(e.Message); Sometimes causes an exception to be thrown because the output device is overloaded.
        }

        private void HandleStopped(object sender, StoppedEventArgs e)
        {
            foreach(ChannelMessage message in e.Messages)
            {
                outDevice.Send(message);
                pianoControl.Send(message);
            }
        }

        private delegate void bik();
        private void HandlePlayingCompleted(object sender, EventArgs e)
        {
            timer1.Stop();
            switch (playMode)
            {
                case 1:
                    Invoke(new MethodInvoker(delegate ()
                    {
                        if (listBox1.SelectedIndex == listBox1.Items.Count - 1)
                        {
                            listBox1.SelectedIndex = 0;
                        }
                        else
                        {
                            listBox1.SelectedIndex++;
                        }
                    }));
                    break;
                case 2:
                    Invoke(new MethodInvoker(delegate ()
                    {
                        listBox1.SelectedIndex = new Random().Next(listBox1.Items.Count);
                    }));
                    break;
            }
            BeginInvoke(new bik(startButton.PerformClick));
        }

        private void pianoControl1_PianoKeyDown(object sender, PianoKeyEventArgs e)
        {
            #region Guard

            if(playing)
            {
                return;
            }

            #endregion

            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOn, 0, e.NoteID, 127));
        }

        private void pianoControl1_PianoKeyUp(object sender, PianoKeyEventArgs e)
        {
            #region Guard

            if(playing)
            {
                return;
            }

            #endregion

            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOff, 0, e.NoteID, 0));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(!scrolling)
            {
                positionHScrollBar.Value = Math.Min(sequencer1.Position, positionHScrollBar.Maximum);
            }
        }

        //protected override void OnResizeBegin(EventArgs e)
        //{
        //    base.OnResizeBegin(e);
        //    oldW = ClientSize.Width;
        //    oldH = ClientSize.Height;
        //}
        //protected override void OnResizeEnd(EventArgs e)
        //{
        //    base.OnResizeEnd(e);
        //    newW = ClientSize.Width;
        //    newH = ClientSize.Height;
        //    MoveControl();
        //}
        //protected override void OnResize(EventArgs e)
        //{
        //    base.OnResize(e);
        //    MovePianoControl();
        //}
        //private void MoveControl0()
        //{
        //    foreach(Control ctrl in Controls)
        //    {
        //        if(!(ctrl is DataGridView))
        //        {
        //            ctrl.Top = ctrl.Top * newH / oldH;
        //            ctrl.Left = ctrl.Left * newW / oldW;
        //            ctrl.Width = ctrl.Width * newW / oldW;
        //            ctrl.Height = ctrl.Height * newH / oldH;
        //        }
        //    }
        //    MovePianoControl();
        //}
        //private int oldW;
        //private int oldH;
        //private int newW;
        //private int newH;

        private void MovePianoControl()
        {
            pianoControl.Left = ClientSize.Width / 20;
            pianoControl.Width = ClientSize.Width * 9 / 10;
            pianoControl.Top = (ClientSize.Height - toolStripProgressBar.Height) * 2 / 3;
            pianoControl.Height = (ClientSize.Height - toolStripProgressBar.Height) / 4;
        }
        private void MoveButton(Button button, int x)
        {
            button.Left = ClientSize.Width * x / 10;
            button.Width = ClientSize.Width / 10;
            button.Height = (ClientSize.Height - toolStripProgressBar.Height) / 16;
            button.Top = (ClientSize.Height - toolStripProgressBar.Height) / 3;
        }
        private void MovePositionHScrollBar()
        {
            positionHScrollBar.Left = ClientSize.Width / 20;
            positionHScrollBar.Width = ClientSize.Width * 3 / 5;
            positionHScrollBar.Height = (ClientSize.Height - toolStripProgressBar.Height) / 20;
            positionHScrollBar.Top = (ClientSize.Height - toolStripProgressBar.Height) / 5;
        }
        private void MoveMidiList()
        {
            listBox1.Left = ClientSize.Width * 7 / 10;
            listBox1.Width = ClientSize.Width / 6;
            listBox1.Height = (ClientSize.Height - toolStripProgressBar.Height) * 2 / 5;
            listBox1.Top = (ClientSize.Height - toolStripProgressBar.Height) / 5;
        }
        private void MoveRadioButton(RadioButton button, int x)
        {
            button.Left = ClientSize.Width * 26 / 30;
            button.Width = ClientSize.Width / 10;
            button.Height = (ClientSize.Height - toolStripProgressBar.Height) / 20;
            button.Top = (ClientSize.Height - toolStripProgressBar.Height) * (x * 5 + 1) / 30;
        }
        private void MoveControl()
        {
            MoveMidiList();
            MovePianoControl();
            MovePositionHScrollBar();
            MoveButton(stopButton, 1);
            MoveButton(startButton, 3);
            MoveButton(continueButton, 5);
            MoveRadioButton(rbtn1, 1);
            MoveRadioButton(rbtn2, 2);
            MoveRadioButton(rbtn3, 3);
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            MoveControl();
        }
        private void rbtn1_CheckedChanged(object sender, EventArgs e)
        {
            playMode = 0;
        }
        private void rbtn2_CheckedChanged(object sender, EventArgs e)
        {
            playMode = 1;
        }
        private void rbtn3_CheckedChanged(object sender, EventArgs e)
        {
            playMode = 2;
        }
        private Dictionary<string, string> midiList;
        private int playMode;
    }
}