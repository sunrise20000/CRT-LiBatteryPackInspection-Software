﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class DoubleColumn : EditorDataGridTemplateColumnBase
    {
        public double Minimun { get; set; }
        public double Maximun { get; set; }
        public int Resolution { get; set; }
        public string InputMode { get; set; }
    }
}
