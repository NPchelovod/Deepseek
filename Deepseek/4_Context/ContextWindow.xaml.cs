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
            //перестраиваем запрос
            ContextTextBox.Text = GetAnswer(true);
        }

        private string GetAnswer(bool allPromptAndQ=false)
        {
            ChatElement chatElement = null;
            if (!allPromptAndQ) 
            {
                chatElement = _mainWindow.chatData.AnswerPromptVector;
            }
            else
            {
                chatElement = _mainWindow.chatData.AnswerAndQuestionsPromptVector;
            }
               
            if (chatElement != null)
            {
                string answer = _mainWindow.chatData.AnswerPromptVector.GetAnswerText();
                answer += "\n" + _mainWindow.chatData.AnswerPromptVector.GetTime;
                
                return answer;
            }
            return "";
        }
       
    }
}
