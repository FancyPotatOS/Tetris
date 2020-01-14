using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    class Piece
    {
        public int x, y;
        public int[][] segments;
        public Color color;

        public Piece(int x, int y, int[][] seg, Color c)
        {
            this.x = x;
            this.y = y;
            segments = seg;
            color = c;
        }

        public Piece(int[][] seg, Color c)
        {
            segments = seg;
            color = c;
        }

        public Piece Clone(int x, int y)
        {
            int[][] newSeg = new int[4][];
            // Copy segments
            for (int i = 0; i < segments.Length; i++)
            {
                newSeg[i] = new int[segments[i].Length];
                for (int j = 0; j < segments[i].Length; j++)
                {
                    newSeg[i][j] = segments[i][j];
                }
            }

            return new Piece(x, y, newSeg, color);
        }
    }
}
