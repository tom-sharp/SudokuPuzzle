using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syslib;


/*
 * SudokuPuzzle solver provide three algorithms to solve sudoku puzzles;
 * 
 *	1.	Rule based Algorithm uses three rules to solve puzzle and is the fastest algorithm.
 *		The rules are: Cell Singles, Cluster Singles, Cluster cells Traverse Exclusions
 *	
 *	2.	NumPass Algorithm, utililizes the rule base algorithm with add-on to guess numbers.
 *		This is the second fastest algorithm, that sholund not be needed if the puzzle is made for humans
 *	
 *	3.	BackTrack Algorithm (enhanched). This is the slowest algorithm and will test all possible numbers
 *		and will finally solve the puzzle if it's a valid puzzle. However, it utilize Rule based algorithm as
 *		a starting point and may never kick-in as the rule based algorithm probably already solved the puzzle.
 *		Still, backtrack algorithm is usable if need to solve a 'dirty' sudoku puzzle with multiple solutions.
 *		Rule based algorithm will not be able to do that.
 *		
 */


namespace Sudoku.Puzzle
{

	class SudokuCell {
		public int Value { get; set; }
	}

	public class SudokuPuzzle
	{

		public SudokuPuzzle()
		{
			puzzle = new CCellObjects<SudokuCell>( columns:9, rows:9);   // create grid of 9x9 cells
			clusters = new CList<CList<SudokuCell>>();

			// setup column clusters
			int col = 0, row = 0;
			while (col < 9) {
				clusters.Add(this.puzzle.ClusterCells(col++, row, width: 1, height: 9));   // column clusters
			}

			// setup row clusters
			col = 0; row = 0;
			while (row < 9) {
				clusters.Add(this.puzzle.ClusterCells(col, row++, width: 9, height: 1));   // row clusters
			}

			// setup square clusters
			row = 0;
			while (row < 9) {
				col = 0;
				while (col < 9)
				{
					clusters.Add(this.puzzle.ClusterCells(col, row, width: 3, height: 3));   // square clusters
					col += 3;
				}
				row += 3;
			}

			// reset all cells
			foreach (var cell in this.puzzle) {
				cell.Value = BitMask[(int)Bit.all];
			}
		}

		public SudokuPuzzle(string puzzle) : this()
		{
			this.SetPuzzle(puzzle);
		}

		/// <summary>
		/// Setup puzzle to be solved. string of numbers 1-9. undefined cells are represented by '0', 'x', 'X' or '.'
		/// other characters such as CF/LF or space are ignored. if puzzle string is shorter than board size of 81 cells, the board filled out with undefined cells
		/// </summary>
		/// <param name="puzzle"></param>
		public void SetPuzzle(string puzzle)
		{
			int count = 0;
			if (puzzle != null)
			{
				foreach (var ch in puzzle)
				{
					if ((ch >= '0' && ch <= '9') || (ch == 'x') || (ch == 'X') || (ch == '.'))
					{
						if ((ch == '0') || (ch == 'x') || (ch == 'X') || (ch == '.')) this.puzzle.Cell(count).Value = BitMask[(int)Bit.all];
						else this.puzzle.Cell(count).Value = BitMask[ch - '0'];
						count++;
					}
					if (count == 81) break;
				}
			}
			while (count < 81) this.puzzle.Cell(count++).Value = BitMask[(int)Bit.all];
			foreach (var cell in this.puzzle) if ((cell.Value & BitMask[(int)Bit.undefined]) == 0) UpdateMask(cell);
		}

		/// <summary>
		/// return sudokupuzzle as a string with 81 characters
		/// </summary>
		/// <returns></returns>
		public string GetPuzzle() {
			var temp = new CStr(82);
			int cellvalue;
			foreach (var cell in this.puzzle) {
				cellvalue = BitMaskValue(cell.Value);
				if ((cellvalue == 0) || (cellvalue == 10)) temp.Append('.');
				else temp.Append((byte)(cellvalue + '0'));
			}
			return temp.ToString();
		}

		/// <summary>
		/// return possible positions for requested number as a string with 81 characters
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public string GetPossible(int number) {
			if ((number <= 0) || (number > 9)) return "bad number";
			var temp = new CStr(82);
			int cellvalue = BitMask[number];
			foreach (var cell in this.puzzle)
			{
				if (((cell.Value & cellvalue) != 0) && (cell.Value & BitMask[(int)Bit.undefined]) != 0) {
					temp.Append((byte)(number + '0'));
				}
				else temp.Append('.');
			}
			return temp.ToString();
		}

		/// <summary>
		/// use three rules solving algorithm to resolve sudoku puzzle
		/// 1. find single cells in puzzle that can hold only a single number
		/// 2. find single cells withing a cluster (row / column / square) that can hold only a single number
		/// 3. exclude numbers mask from clusters where traverse cells forces numbers (based on two numbers)
		/// </summary>
		/// <param name="requestedcount">requestedcount is number of undefined cells to resolve before return. if set to 0 all numbers will be solved before return</param>
		/// <returns>return number of undefined cells resolved</returns>
		public int ResolveMask(int requestedcount = 0) {
			int count = 0;
			bool resolved = false;
			while (this.IsValid()) {
				if ((requestedcount > 0) && (requestedcount == count)) return count;
				if (!resolved) resolved = this.ResolveCellSingle();
				if (!resolved) resolved = this.ResolveClusterSingle();
				if ((!resolved) && (!ResolveExcludeMaskPair())) return count;    // could not resolve anything
				if (resolved) count++;
				resolved = false;
			}
			return count;
		}

		/// <summary>
		/// use NumPass solving algorithm. will resolve puzzle by testing numbers numbers. and try to solve puzzle.
		/// utilize three rules algorithm to evaluate numbers
		/// </summary>
		/// <returns>return true if puzzle is solved</returns>
		public bool ResolveNumPass()
		{
			this.ResolveMask(); // resolve puzzle as far as possible with known rules
			if (this.IsSolved()) return true;
			if (!this.IsValid()) return false;

			SudokuPuzzle NumPassPuzzle = new SudokuPuzzle();
			SudokuCell cell = null;
			int cellnumber;
			int numpass;    // current numberguess
			var puzzlestring = new CStr(this.GetPuzzle());

			cellnumber = 0;
			while (cellnumber < 81)
			{
				cell = this.puzzle.Cell(cellnumber);
				if ((cell.Value & BitMask[(int)Bit.undefined]) != 0)
				{
					numpass = 0;
					while (numpass++ < 9)
					{
						if ((cell.Value & BitMask[numpass]) != 0)
						{
							puzzlestring.Set(cellnumber, (byte)(numpass + '0'));
							NumPassPuzzle.SetPuzzle(puzzlestring.ToString());
							NumPassPuzzle.ResolveMask();
							if (NumPassPuzzle.IsSolved()) { this.SetPuzzle(NumPassPuzzle.GetPuzzle()); return true; }
							else if (!NumPassPuzzle.IsValid()) { cell.Value ^= BitMask[numpass]; }   // unmask number if not valid
						}
					}
					puzzlestring.Set(cellnumber, (byte)('0'));
				}
				if ((cellnumber == 80) && (this.ResolveMask() > 0))
				{
					if (this.IsSolved()) return true;
					puzzlestring.Str(this.GetPuzzle());
					cellnumber = 0;
				}
				else cellnumber++;
			}
			return false;
		}


		/// <summary>
		/// use backtrack algorithm to solve puzzle
		/// </summary>
		/// <returns>returns true if puzzle is solved or false if puzzle is invalid or unsolvable</returns>
		public bool ResolveBacktrack() {
			this.ResolveMask(); // resolve puzzle as far as possible with known rules
			if (this.IsSolved()) return true;
			if (!this.IsValid()) return false;

			SudokuPuzzle NumPassPuzzle = new SudokuPuzzle();
			SudokuCell cell = null;
			int cellnumber;
			int[] numpass = new int[81];    // current numberguess
			var puzzlestring = new CStr(this.GetPuzzle());

			cellnumber = 0;
			while (cellnumber < 81)
			{
				cell = this.puzzle.Cell(cellnumber);
				// if number is undefined - test possible numbers
				if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) numpass[cellnumber] = 0;
				else numpass[cellnumber] = -1;
				cellnumber++;
			}

			cellnumber = 0;
			while ((cellnumber >= 0) && (cellnumber < 81))
			{
				if (numpass[cellnumber] >= 0)
				{
					cell = this.puzzle.Cell(cellnumber);
					numpass[cellnumber]++;
					while (numpass[cellnumber] <= 9)
					{
						if ((cell.Value & BitMask[numpass[cellnumber]]) != 0)
						{
							puzzlestring.Set(cellnumber, (byte)(numpass[cellnumber] + '0'));
							NumPassPuzzle.SetPuzzle(puzzlestring.ToString());
							NumPassPuzzle.ResolveMask();
							if (NumPassPuzzle.IsValid()) { cellnumber++; break; }   // stepup 1
						}
						numpass[cellnumber]++;
					}
					if (numpass[cellnumber] > 9)
					{
						// this number resulted in invalid puzzle position at previous cell
						while (cellnumber >= 0)
						{
							if (numpass[cellnumber] > 9)
							{
								puzzlestring.Set(cellnumber, '.');  // reset current cell
								numpass[cellnumber] = 0;
								cellnumber--;
							}
							else if (numpass[cellnumber] >= 0) break;
							else cellnumber--;
						}
					}
				}
				else cellnumber++;
				if (NumPassPuzzle.IsSolved()) { this.SetPuzzle(NumPassPuzzle.GetPuzzle()); return true; }
			}
			return false;
		}


		/// <summary>
		/// check that puzzle is valid and solvable
		/// </summary>
		/// <returns></returns>
		public bool IsValid() {
			int checkMask;
			foreach (var cluster in this.clusters) {
				checkMask = 0;
				foreach (var cell in cluster) {
					if ((cell.Value & BitMask[(int)Bit.undefined]) == 0) {
						if ((cell.Value & checkMask) != 0) return false;
						checkMask |= cell.Value;
					}
				}
			}
			if (this.IsUnSolvable()) return false;
			return true;
		}

		/// <summary>
		/// check if puzzle is solved and valid
		/// </summary>
		/// <returns></returns>
		public bool IsSolved()
		{
			foreach (var cell in this.puzzle) {
				if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) return false;
			}
			return this.IsValid();
		}

		bool IsUnSolvable() {
			foreach (var cell in this.puzzle)
			{
				if (cell.Value == BitMask[(int)Bit.undefined]) return true;
			}
			return false;
		}

		bool ResolveCellSingle() {
			int bitvalue;
			int issingle;
			foreach (var cell in this.puzzle) {
				if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) {
					issingle = 0;
					bitvalue = 0;
					while (bitvalue++ < 9)
					{
						if ((cell.Value & BitMask[bitvalue]) != 0) {
							if (issingle != 0) { issingle = 0; break; }
							issingle = BitMask[bitvalue];
						}
					}
					if (issingle != 0) { cell.Value = issingle; UpdateMask(cell); return true; }
				}
			}
			return false;
		}

		bool ResolveClusterSingle() {
			int singleMask, accMask, filterMask;
			foreach (var cluster in this.clusters)
			{
				accMask = 0; filterMask = 0;
				foreach (var cell in cluster)
				{
					if ((cell.Value & BitMask[(int)Bit.undefined]) != 0)
					{
						filterMask |= accMask & cell.Value;
						accMask |= cell.Value;
					}
				}

				singleMask = (accMask ^ filterMask) & BitMask[(int)Bit.allNumbers];
				if (singleMask != 0) {
					// single mask may hold multiple singles - find the first single 
					var numberbit = 1;
					while (numberbit <= 9) {
						if ((singleMask & BitMask[numberbit]) != 0) { singleMask = BitMask[numberbit]; break; }
						numberbit++;
					}
					foreach (var cell in cluster)
					{
						if ((cell.Value & singleMask) != 0) { cell.Value = singleMask; this.UpdateMask(cell); return true; }
					}
				}
			}
			return false;
		}

		bool ResolveExcludeMaskPair() {
			bool updated = false;
			int count, numberbit, maskBit;
			SudokuCell cell1 = null, cell2 = null;

			// loop through all clusters (row / col / square)
			foreach (var cluster in this.clusters) {
				numberbit = 1;
				// loop through all numbers
				while (numberbit <= 9) {
					count = 0;
					maskBit = BitMask[numberbit];
					foreach (var cell in cluster) {
						if ((cell.Value & maskBit) != 0) {    // count occurances of in cluster
							count++;
							if (count == 1) cell1 = cell;
							else if (count == 2) cell2 = cell;
						}
					}
					if (count == 2) {
						foreach (var tcluster in this.clusters) {   // find traverse cluster
							count = 0;
							if (cluster != tcluster) { // do not compare itself
								foreach (var tcell in tcluster)
								{
									if ((tcell == cell1) || (tcell == cell2)) count++;
								}
								if (count == 2) {
									foreach (var tcell in tcluster)
									{
										if ((tcell != cell1) && (tcell != cell2) && ((tcell.Value & maskBit) != 0)) {
											tcell.Value ^= maskBit;
											updated = true;
										}
									}

								}
							}
						}
					}
					numberbit++;
				}
			}

			return updated;
		}

		void UpdateMask(SudokuCell cell) {
			if (cell == null) return;
			if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) return;
			bool applyToCluster;
			int applyMask;
			foreach (var cluster in this.clusters) {
				applyToCluster = false;
				applyMask = BitMask[(int)Bit.undefined];
				foreach (var c in cluster) {
					if (c == cell) applyToCluster = true;
					if ((c.Value & BitMask[(int)Bit.undefined]) == 0) applyMask |= c.Value; 
				}
				if (applyToCluster) {
					applyMask ^= BitMask[(int)Bit.allNumbers];
					foreach (var c in cluster) {
						if ((c.Value & BitMask[(int)Bit.undefined]) != 0) c.Value &= applyMask;
					}
				}
			}
		}

		int BitMaskValue(int mask)
		{
			int count = (int)Bit.undefined;
			while (count > 0)
			{
				if ((mask & BitMask[count]) != 0) return count;
				count--;
			}
			return 0;
		}

		enum Bit { empty = 0, no1, no2, no3, no4, no5, no6, no7, no8, no9, undefined, all, allNumbers };

		static int[] BitMask =  { 0x00000000, 0x00000001, 0x00000002, 0x00000004, 0x00000008, 0x00000010, 0x00000020, 0x00000040, 0x00000080, 0x00000100, 0x00000200, 0x000003ff, 0x000001ff };

		CCellObjects<SudokuCell> puzzle = null;
		CList<CList<SudokuCell>> clusters = null;
	}

}
