using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WpfApp5
{
    // Класс, реализующий пользовательскую обработку оконных сообщений операционной системы Windows
    public class WMReceiver : NativeWindow
    {
        // Объявление типа пользовательского метода, ответственного за обработку сообщения в форме
        public delegate void MessageHandler(System.Windows.Forms.Message Msg);

        // Ссылка на пользовательский метод-обработчик сообщений
        private MessageHandler MessageHandlerMethod;

        // Конструктор класса обработчика сообщений
        public WMReceiver(MessageHandler MessageHandlerMethod)
        {
            this.MessageHandlerMethod = MessageHandlerMethod;
            // Создание дескриптора (this.Handle), его нужно указать в вызове 
            // системной функции PostMessage, чтобы именно данный объект получил сообщение
            CreateHandle(new CreateParams());
        }

        // Метод класса, ответственный за получение сообщений
        protected override void WndProc(ref System.Windows.Forms.Message Msg)
        {
            // Вызов пользовательского обработчика сообщения
            MessageHandlerMethod(Msg);
            // Вызов обработчика сообщения родительского класса
            base.WndProc(ref Msg);
        }
    }
}
