﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TheRuleOfSilvester.Core;

namespace TheRuleOfSilvester.Runtime.Cells
{
    [Guid("C37255B3-2C41-431C-AB26-10B740837FA3")]
    public class CornerLeftDown : MapCell
    {
        public override string CellName => nameof(CornerLeftDown);
        public override ConnectionPoint ConnectionPoint => ConnectionPoint.Left | ConnectionPoint.Down;

        public CornerLeftDown(Map map, bool movable = true) : base(map, movable)
        {
            Lines[4, 2] = Movable ? '│' : '║';
            Lines[0, 2] = Movable ? '┐' : '╗';
            Lines[4, 1] = Movable ? '│' : '║';
            Lines[4, 0] = Movable ? '┐' : '╗';
            for (int i = 0; i < 4; i++)
                Lines[i, 0] = Movable ? '─' : '═';
        }

        
    }
}