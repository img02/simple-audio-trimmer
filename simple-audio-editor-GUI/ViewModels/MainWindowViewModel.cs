﻿using Microsoft.Win32;
using simple_audio_editor;
using simple_audio_editor_GUI.Annotations;
using simple_audio_editor_GUI.Commands;
using simple_audio_editor_GUI.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace simple_audio_editor_GUI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly string _default_Input = "";
        private readonly string _default_Output = "";
        private readonly double _default_Volume = 1.0;
        private readonly int _default_Bit_Rate = 128;
        private readonly double _default_Trim_Time = 0;
        private readonly int _default_Font_Size = 12;

        private string _input;
        private string _output;
        private double _volume;
        private int _bitRate;
        private double _trimStart;
        private double _trimEnd;
        private bool _notNotProcessingJobs;
        private int _fontSize;

        private FFmpegProcess FFmpegProcesses { get; set; }
        private ObservableCollection<ConversionResult> _results;

        public ObservableCollection<Job> Jobs { get; set; }
        public ObservableCollection<TrimTime> TrimList { get; set; }


        public string Input
        {
            get => _input;
            set
            {
                if (value == _input) return;
                _input = value;
                OnPropertyChanged(nameof(Input));
            }
        }

        public string Output
        {
            get => _output;
            set
            {
                if (value == _output) return;
                _output = value;
                OnPropertyChanged(nameof(Output));
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (value.Equals(_volume)) return;
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public int BitRate
        {
            get => _bitRate;
            set
            {
                if (value == _bitRate) return;
                _bitRate = value;
                OnPropertyChanged(nameof(BitRate));
            }
        }

        public double TrimStart
        {
            get => _trimStart;
            set
            {
                if (value == _trimStart) return;
                _trimStart = value;
                OnPropertyChanged(nameof(TrimStart));
            }
        }

        public double TrimEnd
        {
            get => _trimEnd;
            set
            {
                if (value == _trimEnd) return;
                _trimEnd = value;
                OnPropertyChanged(nameof(TrimEnd));
            }
        }

        public bool NotProcessingJobs
        {
            get => _notNotProcessingJobs;
            set
            {
                if (value == _notNotProcessingJobs) return;
                _notNotProcessingJobs = value;
                OnPropertyChanged(nameof(NotProcessingJobs));
            }
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                Trace.WriteLine(value);
                if (value == _fontSize || value is < 8 or > 30) return;
                _fontSize = value;
                OnPropertyChanged(nameof(FontSize));
            }
        }

        public ICommand AddJobButtonClicked { get; set; }
        public ICommand AddTrimTimeButtonClicked { get; set; }
        public ICommand OpenFileButtonClicked { get; set; }
        public ICommand RemoveTrimClicked { get; set; }
        public ICommand RemoveJobClicked { get; set; }
        public ICommand StartJobClicked { get; set; }
        public ICommand RemoveFinishedJobsClicked { get; set; }
        public ICommand RemoveAllJobsClicked { get; set; }

        public ICommand IncreaseFontClicked { get; set; }
        public ICommand DecreaseFontClicked { get; set; }


        public MainWindowViewModel()
        {
            Jobs = new ObservableCollection<Job>();

            FFmpegProcesses = new FFmpegProcess();
            _results = FFmpegProcesses.Results;
            _results.CollectionChanged += SetStatusResults;

            TrimList = new ObservableCollection<TrimTime>();

            Input = _default_Input;
            Output = _default_Output;
            Volume = _default_Volume;
            BitRate = _default_Bit_Rate;
            TrimStart = _default_Trim_Time;
            TrimEnd = _default_Trim_Time;
            NotProcessingJobs = true;
            FontSize = _default_Font_Size;

            AddJobButtonClicked = new CustomCommand(AddJobButton_Executed);
            OpenFileButtonClicked = new CustomCommand(OpenFile_Executed);
            AddTrimTimeButtonClicked = new CustomCommand(AddTrimTime_Executed);
            StartJobClicked = new CustomCommand(StartJob_Executed);
            RemoveFinishedJobsClicked = new CustomCommand(RemoveFinishedJobs_Executed);
            RemoveAllJobsClicked = new CustomCommand(RemoveAllJobs_Executed);
            IncreaseFontClicked = new CustomCommand(IncreaseFont_Executed);
            DecreaseFontClicked = new CustomCommand(DecreaseFont_Executed);

            RemoveTrimClicked = new GenericCommand<TrimTime>(RemoveTrimTime_Executed);
            RemoveJobClicked = new GenericCommand<Job>(RemoveJob_Executed);
        }


        #region Command stuff

        public void AddJobButton_Executed()
        {
            //if file doesn't exist, or same file already queued for job, ignore. // maybe throw popup 
            if (!File.Exists(Input)) return;
            if (Jobs.FirstOrDefault(j => j.Options.Input == Input) != null)
            {
                MessageBox.Show($"File: {Input} \nAlready exists in job queue. Please remove it before trying to add it again.",
                    "Job already exists for file.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            };

            //create new ffmpegoptions here.
            var opt = new FFmpegOptions(Input, Output, new List<TrimTime>(TrimList), Volume, BitRate);
            Jobs.Add(new Job(opt));

            //clear current input, output, etc to default values.
            ResetAllInputs();

            #region Debug stuff delete later idk

            /*var optionAsText = "";
            var argAsText = "";

            foreach (var f in Jobs)
            {
                optionAsText += $"Options: In: {f.Options.Input}, Out: {f.Options.Output}, Volume: {f.Options.Volume}, BitRate: {f.Options.BitRate}";
                argAsText = FFmpegArgsBuilder.Create(opt);
            }

            var win = new Window();
            win.Content = new TextBox { Text = "\n\n" + optionAsText + "\n\n" + argAsText + "\n\n" };
            win.SizeToContent = SizeToContent.WidthAndHeight;
            win.ShowDialog();*/

            #endregion

        }

        public void OpenFile_Executed()
        {
            var open = new OpenFileDialog();
            open.Filter = "All Files |*.*|" +
                          "Audio (*.mp3, *.wav, *.flac, *.m4a, *.aac, *.ogg)|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg|" +
                          "Video (*.mkv, *.mp4, *.webm, *.wmv, *.mov, *.m4v; )|*.mkv;*.mp4;*.webm;*.wmv;*.mov;*.m4v";
            open.ShowDialog();
            if (open.FileName != string.Empty)
            {
                ResetAllInputs();
                Input = open.FileName;
                Output = GenerateOutputPath(Input);
            }
        }

        public void AddTrimTime_Executed()
        {
            if (Input == _default_Input) return;

            if (TrimStart < TrimEnd)
            {
                var trimTime = new TrimTime(TrimStart, TrimEnd);
                TrimList.Add(trimTime);
            }
            TrimStart = 0;
            TrimEnd = 0;
        }

        public void RemoveTrimTime_Executed(TrimTime time)
        {
            TrimList.Remove(time);
        }

        public void RemoveJob_Executed(Job job)
        {
            Jobs.Remove(job);
        }

        public void StartJob_Executed()
        {
            StartFFmpegProcess();
        }

        public void RemoveFinishedJobs_Executed()
        {
            var tempList = new List<Job>();

            foreach (var job in Jobs)
            {
                if (job.Status == JobStatus.Success.ToString())
                {
                    tempList.Add(job);
                }
            }

            tempList.ForEach(job => Jobs.Remove(job));
        }

        public void RemoveAllJobs_Executed()
        {
            Jobs.Clear();
        }

        #region Font Sizing

        public void IncreaseFont_Executed()
        {
            Trace.WriteLine("HELLO?");
            FontSize += 2;
        }
        public void DecreaseFont_Executed()
        {
            FontSize -= 2;
        }


        #endregion

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        #region Helpers

        private string GenerateOutputPath(string inputPath)
        {
            var inputSplit = inputPath.Split('\\');
            var outputPath = "";
            for (int i = 0; i <= inputSplit.Length - 2; i++)
            {
                outputPath += inputSplit[i] + "\\";
            }

            outputPath += "Output" + "\\" + inputSplit[^1];

            return OutputPathAppendMp3(outputPath);
        }

        private string OutputPathAppendMp3(string outputPath)
        {
            var outputSplit = outputPath.Split(".");
            outputSplit[^1] = "mp3";
            return string.Join(".", outputSplit);
        }
        private async void StartFFmpegProcess()
        {
            //where job status != Success, select the ffmpeg options.
            var optList = Jobs.Where(j => j.Status != JobStatus.Success.ToString()).Select(j => j.Options).ToList();

            Trace.WriteLine(optList.Count);
            Trace.WriteLine(JobStatus.Success.ToString());

            FFmpegProcesses.OptionsQueue = optList;

            NotProcessingJobs = false;
            SetStatusToProcessing();
            await FFmpegProcesses.Start();
            NotProcessingJobs = true;
        }

        private void SetStatusToProcessing()
        {
            foreach (var job in Jobs)
            {
                if (job.Status == JobStatus.Success.ToString()) continue;
                job.SetStatus(JobStatus.Processing);
            }
        }

        private void SetStatusResults(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems == null) return;

            foreach (var item in args.NewItems)
            {
                var result = item as ConversionResult;
                if (result == null) return;

                var j = Jobs.First(j => j.Options.Input == result.Input);

                j.SetStatus(result.Succeeded ? JobStatus.Success : JobStatus.Failed);
            }
        }

        private void ResetAllInputs()
        {
            Input = _default_Input;
            Output = _default_Output;
            Volume = _default_Volume;
            BitRate = _default_Bit_Rate;
            TrimStart = _default_Trim_Time;
            TrimEnd = _default_Trim_Time;
            TrimList.Clear();
        }

        #endregion
    }
}