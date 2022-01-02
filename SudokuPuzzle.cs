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
 *	2.	BackTrack Algorithm (enhanched). This is the slowest algorithm, testing all possible numbers
 *		and will solve any valid puzzle.
 *		
 *		
 *	ver	
 *	0.03	Added cluster twin cell mask exclusion rule
 *			Added cluster traverse tripple cell mask exclusion rule
 *			
 *	0.02	Removed pre-run of rule based algorithm from BackTrack algorithm 
 *			Improved rule based algorithm
 *			Retired NumPass algorithm as it has about same performance as backtrack with rules algorithm
 *			
 *	0.01	Initial
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
		/// Copy sudokupuzzle to this puzzle
		/// </summary>
		/// <param name="sudokupuzzle"></param>
		public SudokuPuzzle Copy(SudokuPuzzle sudokupuzzle) {
			if (sudokupuzzle == null) { this.SetPuzzle(""); return this; }
			SudokuCell cell1 = sudokupuzzle.puzzle.FirstCell(), cell2 = this.puzzle.FirstCell();
			while (cell1 != null) {
				cell2.Value = cell1.Value;
				cell1 = sudokupuzzle.puzzle.NextCell();
				cell2 = this.puzzle.NextCell();
			}
			return this;
		}

		/// <summary>
		/// Make a copy of current puzzle
		/// </summary>
		/// <returns></returns>
		public SudokuPuzzle Copy()
		{
			var newPuzzle = new SudokuPuzzle();
			SudokuCell cell1 = this.puzzle.FirstCell(), cell2 = newPuzzle.puzzle.FirstCell();
			while (cell1 != null)
			{
				cell2.Value = cell1.Value;
				cell1 = this.puzzle.NextCell();
				cell2 = newPuzzle.puzzle.NextCell();
			}
			return newPuzzle;
		}

		/// <summary>
		/// Setup puzzle to be solved. string of numbers 1-9. undefined cells are represented by '0', 'x', 'X' or '.'
		/// other characters such as CF/LF or space are ignored. if puzzle string is shorter than board size of 81 cells, the board filled out with undefined cells
		/// </summary>
		/// <param name="puzzle"></param>
		public SudokuPuzzle SetPuzzle(string puzzle)
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
			return this;
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
		/// <returns>return number of undefined cells resolved or -1 if invalid puzzle</returns>
		public int ResolveRules(int requestedcount = 0) {
			int count = 0;
			bool resolved = false;
			while (this.IsValid()) {
				if ((requestedcount > 0) && (requestedcount == count)) return count;
				if (!resolved) resolved = this.ResolveCellSingle();
				if (!resolved) resolved = this.ResolveClusterSingle();
				if ((!resolved) && (!ResolveExcludeMask())) return count;    // could not resolve anything
				if (resolved) count++;
				resolved = false;
			}
			if (!this.IsValid()) return -1;
			return count;
		}

		/// <summary>
		/// use backtrack algorithm to solve puzzle
		/// </summary>
		/// <returns>returns true if puzzle is solved or false if puzzle is invalid or unsolvable</returns>
		public bool ResolveBacktrack() {
			if (this.IsSolved()) return true;
			if (!this.IsValid()) return false;

			SudokuPuzzle NumPassPuzzle = new SudokuPuzzle();
			SudokuCell cell = null;
			int cellnumber;
			int[] numpass = new int[81];    // current numberguess
			var puzzlestring = new CStr(this.GetPuzzle());

			cellnumber = 0;
			while (cellnumber < 81) {
				cell = this.puzzle.Cell(cellnumber);
				// if number is undefined - test possible numbers
				if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) numpass[cellnumber] = 0;
				else numpass[cellnumber] = -1;
				cellnumber++;
			}

			cellnumber = 0;
			while ((cellnumber >= 0) && (cellnumber < 81)) {
				if (numpass[cellnumber] >= 0) {
					cell = this.puzzle.Cell(cellnumber);
					numpass[cellnumber]++;
					while (numpass[cellnumber] <= 9) {
						if ((cell.Value & BitMask[numpass[cellnumber]]) != 0) {
							puzzlestring.Set(cellnumber, (byte)(numpass[cellnumber] + '0'));
							NumPassPuzzle.SetPuzzle(puzzlestring.ToString());
							NumPassPuzzle.ResolveRules();
							if (NumPassPuzzle.IsValid()) { cellnumber++; break; }   // stepup 1
						}
						numpass[cellnumber]++;
					}
					if (numpass[cellnumber] > 9) {
						// this number resulted in invalid puzzle position at previous cell
						while (cellnumber >= 0) {
							if (numpass[cellnumber] > 9) {
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
			foreach (var cell in this.puzzle) {
				if (cell.Value == BitMask[(int)Bit.undefined]) return true;
			}
			return false;
		}

		// check cell for one only possible number
		bool ResolveCellSingle() {
			int bitvalue;
			int issingle;
			foreach (var cell in this.puzzle) {
				if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) {
					issingle = 0;
					bitvalue = 0;
					while (bitvalue++ < 9) {
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

		// check cluster cells for one only possible number
		bool ResolveClusterSingle() {
			int singleMask, accMask, filterMask;
			foreach (var cluster in this.clusters) {
				accMask = 0; filterMask = 0;
				foreach (var cell in cluster) {
					if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) {
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
					foreach (var cell in cluster) {
						if ((cell.Value & singleMask) != 0) { cell.Value = singleMask; this.UpdateMask(cell); return true; }
					}
				}
			}
			return false;
		}

		// This will not resolve any number, but check for possibility to exclude possible number masks
		// return true if any cell mask bits has been updated or false if it was not able to exclude any mask bits
		bool ResolveExcludeMask() {
			if (ResolveClusterTraversePairMask()) return true;
			if (ResolveClusterTraverseTrippleMask()) return true;
			if (ResolveClusterPairMask()) return true;
			if (ResolveClusterTwinMask()) return true;
			return false;
		}

		// check for pair cells within a cluster that can hold same number & check traverse cluster for same cells
		bool ResolveClusterTraversePairMask() {
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
						if (((cell.Value & maskBit) != 0) && ((cell.Value & BitMask[(int)Bit.undefined]) != 0)) {    // count occurances of in cluster
							count++;
							if (count == 1) cell1 = cell;
							else if (count == 2) cell2 = cell;
						}
					}
					if (count == 2) {
						foreach (var tcluster in this.clusters) {   // find traverse cluster
							count = 0;
							if (cluster != tcluster) { // do not compare itself
								foreach (var tcell in tcluster) {
									if ((tcell == cell1) || (tcell == cell2)) count++;
								}
								if (count == 2) {
									foreach (var tcell in tcluster) {
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

		// check for three cells within a cluster with possible same number & check traverse cluster for same cells
		bool ResolveClusterTraverseTrippleMask() {
			bool updated = false;
			int counter, numberbit, maskBit;
			SudokuCell cell1 = null, cell2 = null, cell3 = null;

			// loop through all clusters (row / col / square)
			foreach (var cluster in this.clusters) {
				numberbit = 1;
				// loop through all numbers 
				while (numberbit <= 9) {
					counter = 0;
					maskBit = BitMask[numberbit];
					foreach (var cell in cluster) {
						if (((cell.Value & maskBit) != 0) && ((cell.Value & BitMask[(int)Bit.undefined]) != 0)) {    // count occurances of in cluster
							counter++;
							if (counter == 1) cell1 = cell;
							else if (counter == 2) cell2 = cell;
							else if (counter == 3) cell3 = cell;
						}
					}
					if (counter == 3) {
						foreach (var tcluster in this.clusters)	{   // find traverse cluster
							counter = 0;
							if (cluster != tcluster) { // do not compare itself
								foreach (var tcell in tcluster)	{
									if ((tcell == cell1) || (tcell == cell2) || (tcell == cell3)) counter++;
								}
								if (counter == 3)	{
									foreach (var tcell in tcluster) {
										if ((tcell != cell1) && (tcell != cell2) && (tcell != cell3) && ((tcell.Value & maskBit) != 0)) {
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

		// check for pair of cell that look exacly the same with two possible numbers
		bool ResolveClusterTwinMask() {
			bool updated = false;
			SudokuCell cell1, cell2;
			int bit1, bit2;

			// loop through all clusters (row / col / square)
			foreach (var cluster in this.clusters) {
				// test all combos of two numbers
				bit1 = BitMask[(int)Bit.no1];
				while (bit1 != BitMask[(int)Bit.undefined]) {
					bit2 = bit1 << 1;
					while (bit2 != BitMask[(int)Bit.undefined]) {
						var applyMask = bit1 | bit2 | BitMask[(int)Bit.undefined];
						cell1 = null; cell2 = null;
						// look through all cells in cluster to see if there is twin pair
						foreach (var cell in cluster) {
							if (cell.Value == applyMask) {
								if (cell1 == null) cell1 = cell;
								else cell2 = cell;
							}
						}
						if (cell2 != null) {
							// a pair was found - apply mask filter for all other cells in cluster
							foreach (var cell in cluster) {
								if ((cell.Value & BitMask[(int)Bit.undefined]) != 0) {
									if ((cell != cell1) && (cell != cell2) && ((cell.Value & (applyMask & BitMask[(int)Bit.allNumbers])) != 0)) {
										cell.Value &= (applyMask ^ BitMask[(int)Bit.allNumbers]); updated = true;
									}
								}
							}
						}
						bit2 <<= 1;
					}
					bit1 <<= 1;
				}
			}
			return updated;
		}


		// check for pair cells within a cluster that hold same two numbers
		bool ResolveClusterPairMask()
		{
			bool updated = false;
			int count, maskCellPair;
			SudokuCell cell1, cell2;
			int bit1, bit2;

			// loop through all clusters (row / col / square)
			foreach (var cluster in this.clusters)
			{
				// find numbers that occure exactly twice within cluster
				maskCellPair = 0;
				bit1 = BitMask[(int)Bit.no1];
				while (bit1 != BitMask[(int)Bit.undefined]) {
					count = 0;
					foreach (var cell in cluster) {
						if ((cell.Value & bit1) != 0) count++;
					}
					if (count == 2) maskCellPair |= bit1;    // store numbers that occure twice within cluster
					bit1 <<= 1;
				}

				// find for matching cells and filter any additional mask bits
				if (maskCellPair != 0) {
					bit1 = BitMask[(int)Bit.no1];
					while (bit1 != BitMask[(int)Bit.undefined])	{
						if ((bit1 & maskCellPair) != 0)	{
							bit2 = bit1 << 1;
							while (bit2 != BitMask[(int)Bit.undefined])	{
								if ((bit2 & maskCellPair) != 0) {
									// found two numbers that occure twice in cluster
									var applyMask = bit1 | bit2;
									cell1 = null; cell2 = null;
									foreach (var cell in cluster) {
										if ((cell.Value & applyMask) == applyMask) {
											if (cell1 == null) cell1 = cell;
											else cell2 = cell;
										}
									}
									if ((cell1 != null) && (cell2 != null)) {
										// found a matching pair - apply filter
										applyMask |= BitMask[(int)Bit.undefined];
										if (cell1.Value != applyMask) { cell1.Value = applyMask; updated = true; }
										if (cell2.Value != applyMask) { cell2.Value = applyMask; updated = true; }
									}
								}
								bit2 <<= 1;
							}
						}
						bit1 <<= 1;
					}
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
