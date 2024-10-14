using Godot;
using System;

namespace GodotMultiplayer.Client
{
    public partial class ConsolePage : Control
    {
        private TextEdit _consoleOutput;
        private LineEdit _textInput;
        private Button _sendButton;

        public override void _Ready()
        {
            _consoleOutput = GetNode<TextEdit>("VBoxContainer/ConsoleOutput");
            _textInput = GetNode<LineEdit>("VBoxContainer/HBoxContainer/HBoxContainer");
            _sendButton = GetNode<Button>("VBoxContainer/HBoxContainer/HBoxContainer2");
            
            _sendButton.Connect("pressed", new Callable(this, nameof(OnSendButtonPressed)));
            
            _textInput.Connect("text_submitted", new Callable(this, nameof(OnTextInputSubmitted)));
        }
        
        private void OnSendButtonPressed()
        {
            ClearConsole();
        }
        
        private void OnTextInputSubmitted(string newText)
        {
            SubmitText();
        }
        
        private void SubmitText()
        {
            string newText = _textInput.Text.Trim();
            
            if (!string.IsNullOrEmpty(newText))
            {
                _consoleOutput.Text += $"> {newText}\n";
                _textInput.Clear();
            }
        }
        
        private void ClearConsole()
        {
            _consoleOutput.Text = "";  
        }
    }
}
