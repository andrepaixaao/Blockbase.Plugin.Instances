using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
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

         public static FormViewModel From(Object obj)
        {
            var list = new List<InputObjectModel>();
            var DisplayClassName = (DisplayNameAttribute)obj.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault();


            foreach (var prop in obj.GetType().GetProperties())
            {
                var display = prop;
                var dp = display.GetCustomAttribute<DisplayNameAttribute>();
                var reg = display.GetCustomAttribute<RegularExpressionAttribute>();
                string type = "text";
                if (prop.PropertyType == typeof(Int32)) type = "number";
                list.Add(new InputObjectModel()
                {
                    Name = prop.Name,
                    Type = type,
                    DisplayName = dp.DisplayName,
                    Pattern = reg.Pattern,
                    Title = reg.ErrorMessage
                });

            }
            return new FormViewModel() { Inputs = list, ClassName = obj.GetType().Name, DisplayClassName = DisplayClassName.DisplayName };
        }
        
    }
}
