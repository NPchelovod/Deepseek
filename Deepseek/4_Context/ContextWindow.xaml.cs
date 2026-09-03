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
        public ContextWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            if (mainWindow.chatData.AnswerPromptVector != null)
            {
                string answer = mainWindow.chatData.AnswerPromptVector.GetAnswerText();
                answer += "\n" + mainWindow.chatData.AnswerPromptVector.GetTime;
                ContextTextBox.Text = answer;
            }
        }
    }
}
