using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace RhythmHit
{
    class Hazard
    {
        public enum Sign
        {
            Perfect,
            Good,
            Miss,
            Undecided
        }
        public Vector2 Position;
        public bool Visible = true;
        public Sign sign = Sign.Undecided;
        public double SignDisplayTime;
        public Hazard()
        {
        }
    }
}

