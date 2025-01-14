﻿namespace PsMidiProfiler.Controls
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using PsMidiProfiler.Enums;
    using PsMidiProfiler.Helpers;
    using PsMidiProfiler.Models;
    using radio42.Multimedia.Midi;

    /// <summary>
    /// Interaction logic for FourLaneDrumsMonitor.xaml
    /// </summary>
    public partial class FourLaneDrumsMonitor : UserControl, IControllerMonitor, IButtonHighlighter, INotifyPropertyChanged
    {
        private PsDevice device;

        private List<MonitorButton> monitorButtons;

        private Visibility redVisibility;

        private Visibility yellowVisibility;

        private Visibility blueVisibility;

        private Visibility greenVisibility;

        private Visibility bassVisibility;

        public FourLaneDrumsMonitor()
        {
            this.InitializeComponent();
            var buttons = new List<ButtonName>();
            buttons.Add(ButtonName.Red);
            buttons.Add(ButtonName.Yellow_Tom);
            buttons.Add(ButtonName.Blue_Tom);
            buttons.Add(ButtonName.Green_Tom);
            buttons.Add(ButtonName.Bass);
            buttons.Add(ButtonName.Bass);

            this.device = new PsDevice("Four Lane Drums Midi Profile", DeviceType.Drums);
            this.device.ProfileButtons = new List<PsProfileButton>();
            this.monitorButtons = new List<MonitorButton>();

            foreach (var button in buttons)
            {
                var profileButton = new PsProfileButton(button, 0, 0, 0);
                var monitorButton = new MonitorButton(profileButton);

                this.device.ProfileButtons.Add(profileButton);
                this.monitorButtons.Add(monitorButton);
            }

            this.OnPropertyChanged("MonitorButtons");

            this.RedVisibility = Visibility.Hidden;
            this.YellowVisibility = Visibility.Hidden;
            this.BlueVisibility = Visibility.Hidden;
            this.GreenVisibility = Visibility.Hidden;
            this.BassVisibility = Visibility.Hidden;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Controller Controller
        {
            get
            {
                return new Controller(ControllerType.FiveLaneDrums, ControllerCategory.Drums);
            }
        }

        public PsDevice Device
        {
            get { return this.device; }
        }

        public IEnumerable<MonitorButton> MonitorButtons
        {
            get { return this.monitorButtons; }
        }

        public Visibility RedVisibility
        {
            get
            {
                return this.redVisibility;
            }

            set
            {
                this.redVisibility = value;
                this.OnPropertyChanged("RedVisibility");
            }
        }

        public Visibility YellowVisibility
        {
            get
            {
                return this.yellowVisibility;
            }

            set
            {
                this.yellowVisibility = value;
                this.OnPropertyChanged("YellowVisibility");
            }
        }

        public Visibility BlueVisibility
        {
            get
            {
                return this.blueVisibility;
            }

            set
            {
                this.blueVisibility = value;
                this.OnPropertyChanged("BlueVisibility");
            }
        }

        public Visibility GreenVisibility
        {
            get
            {
                return this.greenVisibility;
            }

            set
            {
                this.greenVisibility = value;
                this.OnPropertyChanged("GreenVisibility");
            }
        }

        public Visibility BassVisibility
        {
            get
            {
                return this.bassVisibility;
            }

            set
            {
                this.bassVisibility = value;
                this.OnPropertyChanged("BassVisibility");
            }
        }

        public void Highlight(ButtonName button, bool isNoteOn, byte velocity)
        {
            Visibility result = Convert.ToVisibility(isNoteOn);
            MidiNote note = MidiNote.None;
            var status = isNoteOn ? MIDIStatus.NoteOn : MIDIStatus.NoteOff;

            if (button == ButtonName.Red)
            {
                this.RedVisibility = result;
                note = MidiNote.AcousticSnare;
            }
            else if (button == ButtonName.Yellow_Tom)
            {
                this.YellowVisibility = result;
                note = MidiNote.LowTom;
            }
            else if (button == ButtonName.Blue_Tom)
            {
                this.BlueVisibility = result;
                note = MidiNote.HighFloorTom;
            }
            else if (button == ButtonName.Green_Tom)
            {
                this.GreenVisibility = result;
                note = MidiNote.LowFloorTom;
            }
            else if (button == ButtonName.Bass)
            {
                this.BassVisibility = result;
                note = MidiNote.BassDrum1;
            }

            if (note != MidiNote.None)
            {
                MidiModel.Send(new MidiShortMessage(status, 9, (byte)note, velocity, 0));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
