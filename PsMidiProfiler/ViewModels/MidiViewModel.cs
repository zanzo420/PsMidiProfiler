﻿namespace PsMidiProfiler.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Data;
    using System.Windows.Input;
    using PsMidiProfiler.Commands;
    using PsMidiProfiler.Controls;
    using PsMidiProfiler.Enums;
    using PsMidiProfiler.Helpers;
    using PsMidiProfiler.Models;
    using radio42.Multimedia.Midi;

    public class MidiViewModel : INotifyPropertyChanged
    {
        public const int UnhighlightDelayInMilliseconds = 150;

        private MidiModel midi;

        private Controller controller;

        private IControllerMonitor controllerMonitor;

        private ICommand clearMidiMonitorHistoryCommand;

        private ICommand generateProfileCommand;

        private bool waitForNoteOff;

        public MidiViewModel()
        {
            this.midi = new MidiModel();
            this.midi.MessageReceived += this.OnMidiMessageReceived;
            this.CurrentController = new Controller(ControllerType.FourLaneDrums, ControllerCategory.Drums);
            this.WaitForNoteOff = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<ProfileGeneratedEventArgs> ProfileGenerated;

        public MidiModel MidiModel
        {
            get
            {
                return this.midi;
            }
        }

        public IEnumerable<string> MidiInDevices
        {
            get
            {
                return this.midi.MidiInDevices;
            }
        }

        public string CurrentMidiInDevice
        {
            get
            {
                return this.midi.CurrentDevice;
            }

            set
            {
                this.midi.CurrentDevice = value;
            }
        }

        public string NoteHistory
        {
            get
            {
                return this.midi.NoteHistory;
            }
        }

        public IControllerMonitor ControllerMonitor
        {
            get
            {
                return this.controllerMonitor;
            }

            private set
            {
                this.controllerMonitor = value;
                this.OnPropertyChanged("ControllerMonitor");
            }
        }

        public ListCollectionView Controllers
        {
            get
            {
                var controllers = Helpers.ControllersHelper.GetControllers();
                var list = new ListCollectionView(controllers);
                list.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                return list;
            }
        }

        public Controller CurrentController
        {
            get
            {
                return this.controller;
            }

            set
            {
                this.controller = value;
                this.controllerMonitor = Helpers.ControllersHelper.CreaterMonitor(this.controller);

                foreach (var button in this.controllerMonitor.MonitorButtons)
                {
                    button.Cleared += this.OnMonitorButtonCleared;
                }

                this.ControllerMonitor = this.controllerMonitor;
                this.OnPropertyChanged("CurrentController");
            }
        }

        public bool WaitForNoteOff
        {
            get
            {
                return this.waitForNoteOff;
            }

            set
            {
                this.waitForNoteOff = value;
                this.OnPropertyChanged("WaitForNoteOff");
            }
        }

        public IEnumerable<bool> Booleans
        {
            get
            {
                return new List<bool>()
                {
                    true,
                    false
                };
            }
        }

        public ICommand ClearMidiMonitorHistoryCommand
        {
            get
            {
                if (this.clearMidiMonitorHistoryCommand == null)
                {
                    this.clearMidiMonitorHistoryCommand = new RelayCommand(
                        this.OnClearMonitorHistoryRequested);
                }

                return this.clearMidiMonitorHistoryCommand;
            }
        }

        public ICommand GenerateProfileCommand
        {
            get
            {
                if (this.generateProfileCommand == null)
                {
                    this.generateProfileCommand = new RelayCommand(this.OnGenerateProfileRequested);
                }

                return this.generateProfileCommand;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void OnClearMonitorHistoryRequested(object obj)
        {
            this.midi.ClearNoteHistory();
            this.OnPropertyChanged("NoteHistory");
        }

        private void OnGenerateProfileRequested(object obj)
        {
            string error;
            string midiProfile = PsDeviceSerializer.Serialize(this.controllerMonitor.Device, out error);
            if (this.ProfileGenerated != null)
            {
                this.ProfileGenerated(this, new ProfileGeneratedEventArgs(midiProfile, error));
            }
        }

        private void OnMidiMessageReceived(object sender, MidiMessageEventArgs e)
        {
            this.OnPropertyChanged("NoteHistory");
            if (e.IsShortMessage)
            {
                byte controller = e.ShortMessage.Controller;
                byte velocity = e.ShortMessage.Velocity;
                bool isMSB = controller == 4;
                bool isLSB = controller == 36;

                if (this.controllerMonitor is ICC4HiHatPedalMonitor)
                {
                    if (isMSB || (isLSB && velocity == 0))
                    {
                        var pedalMonitor = this.controllerMonitor as ICC4HiHatPedalMonitor;
                        pedalMonitor.HiHatPedalVelocity = velocity;
                        return;
                    }
                }

                var buttons = this.ControllerMonitor.MonitorButtons.Where(
                    button => button.ProfileButton.Note == e.ShortMessage.Note &&
                    button.ProfileButton.Channel == e.ShortMessage.Channel);

                bool shouldHightlight = e.ShortMessage.StatusType != MIDIStatus.NoteOff;
                foreach (var button in buttons)
                {
                    this.ControllerMonitor.Highlight(button.ProfileButton.Name, shouldHightlight);
                }

                if (shouldHightlight && !this.WaitForNoteOff)
                {
                    var task = Task.Run(async delegate
                    {
                        await Task.Delay(MidiViewModel.UnhighlightDelayInMilliseconds);
                        foreach (var button in buttons)
                        {
                            this.ControllerMonitor.Highlight(button.ProfileButton.Name, false);
                        }
                    });
                }
            }
        }

        private void OnMonitorButtonCleared(object sender, EventArgs e)
        {
            var button = sender as MonitorButton;
            if (button != null)
            {
                this.controllerMonitor.Highlight(button.ProfileButton.Name, false);
            }
        }
    }
}