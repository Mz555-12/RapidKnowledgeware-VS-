using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RapidKnowledgeware.Base
{
    public class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        // 添加无参构造函数以保持向后兼容
        public CommandBase()
        {
        }

        // 新的构造函数
        public CommandBase(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
                return _canExecute(parameter);

            return DoCanExecute == null || DoCanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute?.Invoke(parameter);
            DoExecute?.Invoke(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        // 为了向后兼容，保留原来的属性
        public Action<object> DoExecute { get; set; }
        public Func<object, bool> DoCanExecute { get; set; }
    }
}
