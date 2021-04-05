using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blockbase.Plugin.Instances.Models
{
    public class OperationResult
    {
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

    }
        public class OperationResult<T> : OperationResult
        {
            public T Result { get; set; }
           
        }

    }

