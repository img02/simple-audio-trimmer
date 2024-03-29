﻿using System;
using System.Windows.Input;

namespace simple_audio_editor_GUI.Commands
{
    public class CustomCommand : ICommand
    {
        private Action _action;

        public CustomCommand(Action action)
        {
            _action = action;
        }
        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _action?.Invoke();
        }

        public event EventHandler CanExecuteChanged;
    }
}