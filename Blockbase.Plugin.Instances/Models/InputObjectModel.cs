using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Blockbase.Plugin.Instances.Models
{
    public class InputObjectModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Pattern { get; set; }
        public string Title { get; set; }
    }
}
