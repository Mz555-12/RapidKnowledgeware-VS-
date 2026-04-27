using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RapidKnowledgeware.Base
{
    public class MessageBlockTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate CodeTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is MessageBlock block)
            {
                return block.Type == "Code" ? CodeTemplate : TextTemplate;
            }
            return TextTemplate;
        }
    }
}