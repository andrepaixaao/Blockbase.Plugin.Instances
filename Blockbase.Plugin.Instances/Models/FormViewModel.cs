using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blockbase.Plugin.Instances.Models
{

    public class FormViewModel
    {
        public List<InputObjectModel> Inputs { get; set; }
        public string ClassName { get; internal set; }
        public string DisplayClassName { get; set; }

        public FormViewModel(List<InputObjectModel> inputs)
        {
            Inputs = inputs;
        }
        public FormViewModel()
        {
        }
    }
}
