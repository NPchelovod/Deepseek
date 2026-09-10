using OllamaChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Deepseek
{
    /// <summary>
    /// Логика взаимодействия для ContextWindow.xaml
    /// </summary>
    public partial class ContextWindow : Window
    {
        MainWindow _mainWindow;
        public ContextWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
            this.Loaded += SettingsWindow_Loaded;
        }
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ContextTextBox.Text = GetAnswer();
        }

        private void GetAllContextCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (UseContextCheckBox.IsChecked == true)
            {
                //перестраиваем запрос
                ContextTextBox.Text = GetAnswer(true);
            }
            else
            {
                ContextTextBox.Text = GetAnswer();
            }
        }

        private string GetAnswer(bool allPromptAndQ=false)
        {
            ChatElement chatElement = null;
            ChatElement aiMessageAnswer = _mainWindow.chatData.ConversationHistory.Where(x => x.Id == _mainWindow.chatData.Id && x.Senders == ESenders.AI_Chat).LastOrDefault();
            if (!allPromptAndQ) 
            {
                chatElement = _mainWindow.chatData.AnswerPromptVector;
            }
            else
            {
                chatElement = _mainWindow.chatData.AnswerPromptVector;
                
            }
               
            if (chatElement != null)
            {
                string answer = _mainWindow.chatData.AnswerPromptVector.GetAnswerText();
                if(allPromptAndQ && aiMessageAnswer!=null)
                {
                    answer += "\n" + aiMessageAnswer.GetAnswerText();
                }
                answer += "\n" + _mainWindow.chatData.AnswerPromptVector.GetTime;
                
                return answer;
            }
            if (!allPromptAndQ)
            {
                return "=>Пусто 1";
            }
            return "=>Пусто 2";
        }
       
    }
}
