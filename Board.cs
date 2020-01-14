using System.Collections.Generic;

namespace Tetris
{
    class Board
    {
        public List<Piece> pieces;
        public Piece currPiece;
        public Piece hold;

        public int x, y;

        public Board(int x, int y)
        {
            pieces = new List<Piece>();
            this.x = x;

            // Height has leeway
            this.y = y + 2;
        }

        public void Reset()
        {
            currPiece = null;
            pieces = new List<Piece>();
        }

        public bool AddPiece(Piece p)
        {
            if (currPiece != null)
            {
                return false;
            }
            else if (p == null)
            {
                return false;
            }

            int insertX = x / 2;
            int insertY = y - 3;
            p.x = insertX;
            p.y = insertY;
            bool cont = true;
            while (cont)
            {
                cont = false;
                foreach (int[] curr in p.segments)
                {
                    int currX = curr[0] + p.x;
                    int currY = curr[1] + p.y;

                    if (GetPieceAt(currX, currY) != null)
                    {
                        return false;
                    }
                    else if (currY < 0 || y <= currY)
                    {
                        p.y--;
                        cont = true;
                        break;
                    }
                }
            }

            currPiece = p;
            pieces.Add(p);
            return true;
        }

        // Check if position is free
        public bool IsOpenPos(int x, int y, bool checkCurr)
        {
            // Out of bounds
            if (x < 0 || this.x <= x || y < 0 || this.y-1 <= y)
            {
                return false;
            }

            // Check every piece
            foreach (Piece piece in pieces)
            {
                // Check current piece as well if specified
                if (!checkCurr)
                {
                    if (piece == currPiece)
                    {
                        continue;
                    }
                }

                // Check segments in piece
                foreach (int[] seg in piece.segments)
                {
                    if (x == seg[0] + piece.x && y == seg[1] + piece.y)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public Piece GetPieceAt(int x, int y)
        {
            foreach (Piece piece in pieces)
            {
                foreach (int[] seg in piece.segments)
                {
                    if (x == seg[0] + piece.x && y == seg[1] + piece.y)
                    {
                        return piece;
                    }
                }
            }

            return null;
        }

        // Returns the current piece, put in held position
        public Piece Hold()
        {
            if (currPiece == null)
                return null;
            else
            {
                Piece oldHold = hold;
                hold = currPiece;
                pieces.Remove(currPiece);
                currPiece = null;
                AddPiece(oldHold);
                return hold;
            }
        }

        // Rotate piece without any translation
        public bool Rotate(int[][] rotMatrix)
        {
            if (currPiece == null) { return false; }

            int[][] tempSeg = CopySegments(currPiece.segments);

            // For each segment
            for (int i = 0; i < tempSeg.Length; i++)
            {
                int[] segment = currPiece.segments[i];

                // Matrix multiplication (rot * segment)
                tempSeg[i][0] = rotMatrix[0][0] * segment[0] + rotMatrix[1][0] * segment[1];
                tempSeg[i][1] = rotMatrix[0][1] * segment[0] + rotMatrix[1][1] * segment[1];
            }

            foreach (int[] segment in tempSeg)
            {
                int currX = segment[0] + currPiece.x;
                int currY = segment[1] + currPiece.y;

                if (!IsOpenPos(currX, currY, false))
                {
                    // Try moving it left/right first
                    // Move left
                    int[] dV = new int[] { -1, 0 };
                    if (Rotate(rotMatrix, dV))
                    {
                        return true;
                    }
                    // Move right
                    dV = new int[] { 1, 0 };
                    if (Rotate(rotMatrix, dV))
                    {
                        return true;
                    }

                    // No new center rotation worked
                    return false;
                }
            }

            currPiece.segments = tempSeg;

            return true;
        }

        // Rotate piece directly, with translation of segments
        public bool Rotate(int[][] rotMatrix, int[] dV)
        {
            if (currPiece == null) { return false; }

            int[][] tempSeg = new int[currPiece.segments.Length][];
            for (int i = 0; i < currPiece.segments.Length; i++)
            {
                tempSeg[i] = new int[currPiece.segments[i].Length];
            }

            // For each segment
            for (int i = 0; i < tempSeg.Length; i++)
            {
                int[] segment = currPiece.segments[i];

                // Matrix multiplication (rot * segment)
                tempSeg[i][0] = rotMatrix[0][0] * segment[0] + rotMatrix[1][0] * segment[1];
                tempSeg[i][1] = rotMatrix[0][1] * segment[0] + rotMatrix[1][1] * segment[1];
            }

            foreach (int[] segment in tempSeg)
            {
                int currX = segment[0] + currPiece.x + dV[0];
                int currY = segment[1] + currPiece.y + dV[1];

                if (!IsOpenPos(currX, currY, false))
                {
                    // Stop test if not an open position
                    return false;
                }
            }

            currPiece.x += dV[0];
            currPiece.y += dV[1];
            currPiece.segments = tempSeg;
            return true;
        }

        // Current piece goes downwards once, returns del if (del && finalized current piece)
        public bool Move(int[] dir, bool del)
        {

            // Test if no piece to move
            if (currPiece == null)
            {
                return false;
            }

            // List of bottom segments to test descent
            List<int[]> moveable = new List<int[]>();
            foreach (int[] seg in currPiece.segments)
            {

                // Whether to add to moveable list
                bool add = true;
                foreach (int[] otherSeg in currPiece.segments)
                {
                    // Can't compare to same segment
                    if (seg == otherSeg)
                    {
                        continue;
                    }
                    else if (seg[0] + dir[0] == otherSeg[0] && seg[1] + dir[1] == otherSeg[1])
                    {
                        add = false;
                        break;
                    }
                }

                // Add if successful
                if (add)
                {
                    moveable.Add(seg);
                }
            }

            // Test if new spot is out of bounds or taken by another piece
            foreach (int[] seg in moveable)
            {
                int[] newPos = { seg[0] + currPiece.x + dir[0], seg[1] + currPiece.y + dir[1] };
                if (newPos[0] < 0 || newPos[1] < 0 || x <= newPos[0] || y-1 <= newPos[1])
                {
                    currPiece = (del) ? null : currPiece;
                    return del;
                }
                else if (!IsOpenPos(newPos[0], newPos[1], false))
                {
                    currPiece = (del) ? null : currPiece;
                    return del;
                }
            }

            currPiece.x += dir[0];
            currPiece.y += dir[1];
            return false;
        }

        public bool Fall()
        {
            // Test if has piece
            if (currPiece == null)
            {
                return false;
            }

            // Move downwards until no piece to move
            while (currPiece != null)
            {
                Move(new int[] { 0, -1 }, true);
            }
            return true;
        }

        // Copies segments into new memory address
        public static int[][] CopySegments(int[][] seg)
        {
            int[][] newSeg = new int[seg.Length][];
            for (int i = 0; i < seg.Length; i++)
            {
                newSeg[i] = new int[seg[i].Length];
                for (int j = 0; j < seg[i].Length; j++)
                {
                    newSeg[i][j] = seg[i][j];
                }
            }

            return newSeg;
        }

        // Test if every piece is below top line
        public bool IsValid()
        {
            // If a piece is falling, then it is still redeemable
            if (currPiece != null)
            {
                return true;
            }
            else
            {
                // No piece falling, make sure everything is below leeway
                foreach (Piece p in pieces)
                {
                    foreach (int[] seg in p.segments)
                    {
                        if (seg[1] + p.y >= y-2)
                        {
                            // Out of the top of the board, then you have lost.
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void LowerAllAbove(int height)
        {
            List<Piece> piecesToLower = new List<Piece>();

            // Check every piece's segment's position
            foreach (Piece p in pieces)
            {
                foreach (int[] seg in p.segments)
                {
                    if (seg[1] + p.y >= height)
                    {
                        // If not accounted for and above, add to list
                        if (!piecesToLower.Contains(p))
                        {
                            piecesToLower.Add(p);
                        }
                    }
                }
            }

            // Lower pieces in list
            foreach (Piece p in piecesToLower)
            {
                p.y--;
            }
        }

        // Clear lines and return whether it did
        public bool Clean()
        {
            // Can't empty lines if a piece is falling
            if (currPiece != null)
            {
                return false;
            }

            bool removed = false;

            // Count how many spots are filled each line
            ClearLine[] lineCount = new ClearLine[y];
            for (int i = 0; i < lineCount.Length; i++)
            {
                lineCount[i] = new ClearLine();
            }

            foreach (Piece p in pieces)
            {
                foreach (int[] seg in p.segments)
                {
                    // Add segment to line count
                    lineCount[seg[1] + p.y].Add(seg, p);
                }
            }

            // Go through each line
            for (int i = 0; i < lineCount.Length; i++)
            {
                // If line is full, remove
                if (lineCount[i].count == x)
                {
                    removed = true;

                    lineCount[i].Clear();
                    LowerAllAbove(i);

                    for (int j = 0; j < pieces.Count; j++)
                    {
                        if (pieces[j].segments.Length == 0)
                        {
                            pieces.RemoveAt(j);
                            j--;
                        }
                    }
                    break;
                }
            }

            return removed;
        }
    }

    // Pairs segments with the line count
    class ClearLine
    {
        public int count;
        public List<SegmentPiecePair> pairs;

        public ClearLine()
        {
            pairs = new List<SegmentPiecePair>();
        }

        public void Add(int[] s, Piece p)
        {
            count++;
            pairs.Add(new SegmentPiecePair(s, p));
        }

        // Remove segments from pieces in line
        public void Clear()
        {
            foreach(SegmentPiecePair spp in pairs)
            {
                spp.Remove();
            }
        }
    }

    // Pair a specific segment in a piece with its piece
    class SegmentPiecePair
    {
        int[] segment;
        Piece piece;

        // Constructor
        public SegmentPiecePair(int[] s, Piece p)
        {
            segment = s;
            piece = p;
        }

        // Remove segment from piece
        public void Remove()
        {
            List<int[]> addSegs = new List<int[]>();
            foreach (int[] seg in piece.segments)
            {
                // Add to list if not removed piece
                // Also, if below removed piece, lift.
                if (seg != segment)
                {
                    addSegs.Add(seg);
                    if (seg[1] < segment[1])
                    {
                        seg[1]++;
                    }
                }
            }
            
            // Turn list into array of pieces not removed
            piece.segments = addSegs.ToArray();
        }
    }
}
