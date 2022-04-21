﻿using System;
using System.Windows.Input;

namespace simple_audio_editor_GUI.Commands
{
    public class AddToQueueCommand : ICommand
    {
        private Action _action;

        public AddToQueueCommand(Action action)
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
            //Executed?.Invoke(null, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;
    }
}