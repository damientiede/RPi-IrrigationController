﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrrigationController.Data
{
    public class CommandHistory
    {
        public int Id;
        public int CommandId;
        public string Params;
        public DateTime Issued;
        public DateTime Actioned;
    }
}
