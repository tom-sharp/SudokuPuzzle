using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syslib;


/*
 * SudokuPuzzle solver provide two algorithms to solve sudoku puzzles;
 * 
 *	1.	Rule based Algorithm uses three rules to solve puzzle and is the fastest algorithm.
 *		The rules are: Cell Singles, Cluster Singles, Cluster cells Traverse Exclusions
 *		
 *	2.	NumPass Algorithm. Have same type of behaivior as backtrack, but have a lot
 *		better performance. Make use of logic rules, but guesses qualified selected numbers
 *		and will solve any valid puzzle, that might not be solved by the logic rule algorithm.
 *		
 *		
 *	ver	
 *	0.11	Added puzzle Validation and check for multi-solution
 *	0.10	Added GetNumber() to return string with positions for a specific number in puzzle
 *	0.09	Added additional support to create puzzles from a base solution (not only random)
 *	0.08	Improved NumPass performance
 *			Renamed BackTrack Algorithm back to NumPass as it deviates from backtrack to much
 *			BugFix in Numpass - possible Out of index issue if puzzle was constructed in a special way
 *	0.07	Added extended check for Unsolvable puzzles
 *	0.06	Added another three random numbers to generate random sudoku, lower chance of dublicate puzzles
 *	0.05	Improved performance of sudoku generation
 *			Added countinh of numbers in puzzle
 *	0.04	Added Create sudoku puzzles
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


	public class SudokuPuzzle {

		public SudokuPuzzle() {
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
				while (col < 9) {
					clusters.Add(this.puzzle.ClusterCells(col, row, width: 3, height: 3));   // square clusters
					col += 3;
				}
				row += 3;
			}

			// reset all cells
			foreach (var cell in this.puzzle) {
				cell.BitMask = BitMask[(int)Bit.all];
			}
		}

		public SudokuPuzzle(string puzzle) : this() {
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
				cell2.BitMask = cell1.BitMask;
				cell1 = sudokupuzzle.puzzle.NextCell();
				cell2 = this.puzzle.NextCell();
			}
			return this;
		}

		/// <summary>
		/// Make a copy of current puzzle
		/// </summary>
		/// <returns></returns>
		public SudokuPuzzle Copy() {
			var newPuzzle = new SudokuPuzzle();
			SudokuCell cell1 = this.puzzle.FirstCell(), cell2 = newPuzzle.puzzle.FirstCell();
			while (cell1 != null) {
				cell2.BitMask = cell1.BitMask;
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
		public SudokuPuzzle SetPuzzle(string puzzle) {
			int count = 0;
			if (puzzle != null) {
				foreach (var ch in puzzle) {
					if ((ch >= '0' && ch <= '9') || (ch == 'x') || (ch == 'X') || (ch == '.')) {
						if ((ch == '0') || (ch == 'x') || (ch == 'X') || (ch == '.')) this.puzzle.Cell(count).BitMask = BitMask[(int)Bit.all];
						else this.puzzle.Cell(count).BitMask = BitMask[ch - '0'];
						count++;
					}
					if (count == 81) break;
				}
			}
			while (count < 81) this.puzzle.Cell(count++).BitMask = BitMask[(int)Bit.all];
			foreach (var cell in this.puzzle) if ((cell.BitMask & BitMask[(int)Bit.undefined]) == 0) UpdateMask(cell);
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
				if ((cellvalue = cell.Number) > 0) temp.Append((byte)(cellvalue + '0'));
				else temp.Append('.');
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
			foreach (var cell in this.puzzle) {
				if (((cell.BitMask & cellvalue) != 0) && (cell.BitMask & BitMask[(int)Bit.undefined]) != 0) {
					temp.Append((byte)(number + '0'));
				}
				else temp.Append('.');
			}
			return temp.ToString();
		}

		/// <summary>
		/// return number positions for requested number as a string with 81 characters
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		public string GetNumber(int number)
		{
			if ((number <= 0) || (number > 9)) return "bad number";
			var temp = new CStr(82);
			int cellvalue = BitMask[number];
			foreach (var cell in this.puzzle) {
				if (cell.BitMask == cellvalue) {
					temp.Append((byte)(number + '0'));
				}
				else temp.Append('.');
			}
			return temp.ToString();
		}


		/// <summary>
		///  count number of occurances  of number in puzzle
		/// </summary>
		/// <param name="number"></param>
		/// <returns>returns count of number found in puzzle</returns>
		public int GetNumberCount(int number = 0) {
			if ((number < 0) || (number > 9)) return 0;
			int count = 0, cellvalue;
			if (number == 0) {
				// count all defined numbers
				cellvalue = BitMask[(int)Bit.undefined];
				foreach (var cell in this.puzzle) {
					if ((cell.BitMask & cellvalue) == 0) count++;
				}
			}
			else {
				// count only requested number
				cellvalue = BitMask[number];
				foreach (var cell in this.puzzle) {
					if (cell.BitMask == cellvalue) count++;
				}
			}
			return count;
		}

		/// <summary>
		/// use three rules solving algorithm to resolve sudoku puzzle
		/// 1. find single cells in puzzle that can hold only a single number
		/// 2. find single cells within a cluster (row / column / square) that can hold only a single number
		/// 3. exclude numbers mask from clusters where traverse cells forces numbers
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
		/// use NumPass algorithm to solve puzzle (it still use logic rules for performance increase)
		/// </summary>
		/// <returns>returns true if puzzle is solved or false if puzzle is invalid or unsolvable</returns>
		public bool ResolveNumPass() {
			this.ResolveRules();
			if (!this.IsValid()) return false;
			if (this.IsSolved()) return true;
			
			SudokuPuzzle NumPassPuzzle = new SudokuPuzzle();
			SudokuCell cell = null;
			int cellnumber;
			int[] numpass = new int[81];    // current numberguess
			var puzzlestring = new CStr(this.GetPuzzle());

			// select what cells should be target for number guesses (0) and which to be ignored to (-1)
			cellnumber = 0;
			while (cellnumber < 81) {
				cell = this.puzzle.Cell(cellnumber);
				// if number is undefined - test possible numbers
				if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) numpass[cellnumber] = 0;
				else numpass[cellnumber] = -1;
				cellnumber++;
			}

			cellnumber = 0;
			while ((cellnumber >= 0) && (cellnumber < 81)) {
				if (numpass[cellnumber] >= 0) {
					cell = this.puzzle.Cell(cellnumber);
					numpass[cellnumber]++;
					while (numpass[cellnumber] <= 9) {
						if ((cell.BitMask & BitMask[numpass[cellnumber]]) != 0) {
							puzzlestring.Set(cellnumber, (byte)(numpass[cellnumber] + '0'));
							NumPassPuzzle.SetPuzzle(puzzlestring.ToString());
							NumPassPuzzle.ResolveRules();
							if (NumPassPuzzle.IsValid()) { cellnumber++; break; }   // stepup 1
						}
						numpass[cellnumber]++;
					}
					if (((cellnumber < 81) && (numpass[cellnumber] > 9))) {
						// this number resulted in invalid puzzle position at previous cell
						while (cellnumber >= 0) {
							if (numpass[cellnumber] > 9) {
								puzzlestring.Set(cellnumber, '.');  // reset current cell
								numpass[cellnumber] = 0;
								cellnumber--;
							}
							else if (numpass[cellnumber] >= 0) break;
							else { cellnumber--; }
						}
					}
					if (NumPassPuzzle.IsSolved()) { this.SetPuzzle(NumPassPuzzle.GetPuzzle()); return true; }
				}
				else cellnumber++;
			}
			return false;
		}


		/// <summary>
		///  Validate make extended checks and look for multiple solutions to the puzzle using backtrack.
		///  Return true if there is at least one solution to the puzzle or false if puzzle is not valid or unsolvable
		/// </summary>
		/// <param name="validation"></param>
		/// <returns>returns true if there is at least one solution to the puzzle or false if puzzle is unsolveable or</returns>
		public SudokuValidation ValidatePuzzle() {
			return ValidateMultiSolutionPuzzle(new SudokuValidation(this.GetPuzzle()));
		}

		/// <summary>
		/// Check if puzzle has more than one solution
		/// return true if there exist more than one solution to the puzzle else false is returned
		/// </summary>
		/// <returns></returns>
		public bool IsMultiSolutionPuzzle() {
			 if (this.ValidateMultiSolutionPuzzle(new SudokuValidation(this.GetPuzzle()).SetLimit(maxsolutions: 2)).IsMultiSolution) return true;
			return false;
		}

		/// <summary>
		/// check that puzzle is valid and solvable
		/// </summary>
		/// <returns></returns>
		public bool IsValid() {
			if (this.IsInvalid()) return false;
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
				if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) return false;
			}
			return this.IsValid();
		}

		// check if same number occure at least twich within a cluster (row/column/square)
		bool IsInvalid() {
			int checkMask;
			foreach (var cluster in this.clusters) {
				checkMask = 0;
				foreach (var cell in cluster) {
					if ((cell.BitMask & BitMask[(int)Bit.undefined]) == 0) {
						if ((cell.BitMask & checkMask) != 0) return true;
						checkMask |= cell.BitMask;
					}
				}
			}
			return false;
		}

		bool IsUnSolvable() {
			int possibleMask = 0x0000;
			foreach (var cell in this.puzzle) {
				// check if there is a cell that is not defined, and have no possible numbers available
				if (cell.BitMask == BitMask[(int)Bit.undefined]) return true;
				if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) possibleMask |= cell.BitMask;
			}
			// check each number to see there is available possible positions if not all numbers are defined
			int number = 0x0001;
			int count = 0;
			while (count++ < 9) {
				if ((number & possibleMask) == 0) {
					if (this.GetNumberCount(count) < 9) return true;
				}
				number <<= 1;
			}
			return false;
		}

		// check cell for one only possible number
		bool ResolveCellSingle() {
			int bitvalue;
			int issingle;
			foreach (var cell in this.puzzle) {
				if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) {
					issingle = 0;
					bitvalue = 0;
					while (bitvalue++ < 9) {
						if ((cell.BitMask & BitMask[bitvalue]) != 0) {
							if (issingle != 0) { issingle = 0; break; }
							issingle = BitMask[bitvalue];
						}
					}
					if (issingle != 0) { cell.BitMask = issingle; UpdateMask(cell); return true; }
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
					if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) {
						filterMask |= accMask & cell.BitMask;
						accMask |= cell.BitMask;
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
						if ((cell.BitMask & singleMask) != 0) { cell.BitMask = singleMask; this.UpdateMask(cell); return true; }
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
						if (((cell.BitMask & maskBit) != 0) && ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0)) {    // count occurances of in cluster
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
										if ((tcell != cell1) && (tcell != cell2) && ((tcell.BitMask & maskBit) != 0)) {
											tcell.BitMask ^= maskBit;
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
						if (((cell.BitMask & maskBit) != 0) && ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0)) {    // count occurances of in cluster
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
										if ((tcell != cell1) && (tcell != cell2) && (tcell != cell3) && ((tcell.BitMask & maskBit) != 0)) {
											tcell.BitMask ^= maskBit;
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
							if (cell.BitMask == applyMask) {
								if (cell1 == null) cell1 = cell;
								else cell2 = cell;
							}
						}
						if (cell2 != null) {
							// a pair was found - apply mask filter for all other cells in cluster
							foreach (var cell in cluster) {
								if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) {
									if ((cell != cell1) && (cell != cell2) && ((cell.BitMask & (applyMask & BitMask[(int)Bit.allNumbers])) != 0)) {
										cell.BitMask &= (applyMask ^ BitMask[(int)Bit.allNumbers]); updated = true;
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
		bool ResolveClusterPairMask() {
			bool updated = false;
			int count, maskCellPair;
			SudokuCell cell1, cell2;
			int bit1, bit2;

			// loop through all clusters (row / col / square)
			foreach (var cluster in this.clusters) {
				// find numbers that occure exactly twice within cluster
				maskCellPair = 0;
				bit1 = BitMask[(int)Bit.no1];
				while (bit1 != BitMask[(int)Bit.undefined]) {
					count = 0;
					foreach (var cell in cluster) {
						if ((cell.BitMask & bit1) != 0) count++;
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
										if ((cell.BitMask & applyMask) == applyMask) {
											if (cell1 == null) cell1 = cell;
											else cell2 = cell;
										}
									}
									if ((cell1 != null) && (cell2 != null)) {
										// found a matching pair - apply filter
										applyMask |= BitMask[(int)Bit.undefined];
										if (cell1.BitMask != applyMask) { cell1.BitMask = applyMask; updated = true; }
										if (cell2.BitMask != applyMask) { cell2.BitMask = applyMask; updated = true; }
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


		// Update BitMask for cells included in same cluster as the cell provided.
		// Removing the bit number for cells defined number
		void UpdateMask(SudokuCell cell) {
			if (cell == null) return;
			if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) return;
			bool applyToCluster;
			int applyMask;
			foreach (var cluster in this.clusters) {
				applyToCluster = false;
				applyMask = BitMask[(int)Bit.undefined];
				foreach (var c in cluster) {
					if (c == cell) applyToCluster = true;
					if ((c.BitMask & BitMask[(int)Bit.undefined]) == 0) applyMask |= c.BitMask; 
				}
				if (applyToCluster) {
					applyMask ^= BitMask[(int)Bit.allNumbers];
					foreach (var c in cluster) {
						if ((c.BitMask & BitMask[(int)Bit.undefined]) != 0) c.BitMask &= applyMask;
					}
				}
			}
		}


		// Validate puzzle using backtrack to run through all valid combinations of not set numbers in puzzle
		SudokuValidation ValidateMultiSolutionPuzzle(SudokuValidation validation) {
			if (validation == null) return new SudokuValidation(puzzle:"");
			this.ResolveRules();
			if (this.IsSolved()) {
				// puzzle solved compleately using rules - this is a true sudoko puzzle
				validation.IsValidated = true;
				validation.IsValid = true;
				validation.IsSolvable = true;
				validation.IsMultiSolution = false;
				validation.Solutions.Add(this.GetPuzzle());
				this.SetPuzzle(validation.Puzzle);  // restore puzzle
				return validation;
			}
			if (!this.IsValid()) {
				validation.IsValidated = true;
				validation.IsValid = !this.IsInvalid();
				validation.IsSolvable = !this.IsUnSolvable();
				validation.IsMultiSolution = false;
				this.SetPuzzle(validation.Puzzle);  // restore puzzle
				return validation;
			}
			// puzzle did not solve using rules - continue and check all qualified cominations using backtrack
			SudokuPuzzle BackTrackPuzzle = new SudokuPuzzle();
			SudokuCell cell = null;
			int cellnumber;
			int[] backtrack = new int[81];    // current numberguess
			var puzzlestring = new CStr(this.GetPuzzle());
			bool limitreached = false;

			// select what cells should be target for number guesses (0) and which to be ignored to (-1)
			cellnumber = 0;
			while (cellnumber < 81) {
				cell = this.puzzle.Cell(cellnumber);
				if ((cell.BitMask & BitMask[(int)Bit.undefined]) != 0) backtrack[cellnumber] = 0;
				else backtrack[cellnumber] = -1;
				cellnumber++;
			}

			// backtrack routine - will move through all undefined cells and test them until the last cell
			// undefined. and then it will backtrack down to cellnumber -1;
			cellnumber = 0;
			while ((cellnumber >= 0) && (!limitreached)) {
				if (backtrack[cellnumber] >= 0) {
					cell = this.puzzle.Cell(cellnumber);
					backtrack[cellnumber]++;
					while (backtrack[cellnumber] <= 9) {
						if ((cell.BitMask & BitMask[backtrack[cellnumber]]) != 0) {
							puzzlestring.Set(cellnumber, (byte)(backtrack[cellnumber] + '0'));
							BackTrackPuzzle.SetPuzzle(puzzlestring.ToString());
							if (BackTrackPuzzle.IsValid()) {
								BackTrackPuzzle.ResolveRules();
								if (BackTrackPuzzle.IsSolved()) {
									validation.Solutions.Add(BackTrackPuzzle.GetPuzzle());  // add the valid solution to solutions
									if ((validation.SolutionsMax > 0) && (validation.SolutionCount >= validation.SolutionsMax)) { limitreached = true; break; }
								}
								else { cellnumber++; break; }
							}
						}
						backtrack[cellnumber]++;
					}
					if (((cellnumber < 81) && (backtrack[cellnumber] > 9)) || (cellnumber == 81)) {
						// this number resulted in invalid puzzle position at previous cell
						if (cellnumber == 81) cellnumber = 80;
						while (cellnumber >= 0) {
							if (backtrack[cellnumber] > 9) {
								puzzlestring.Set(cellnumber, '.');  // reset current cell
								backtrack[cellnumber] = 0;
							}
							else if (backtrack[cellnumber] >= 0) break;
							cellnumber--;
							validation.BackTrackCounter++;
						}
					}
				}
				else cellnumber++;  // cell is defined move on to next
			}
			// sum up validation
			validation.IsValidated = true;
			if (validation.SolutionCount > 0) {
				validation.IsValid = true;
				validation.IsSolvable = true;
				if (validation.SolutionCount > 1) validation.IsMultiSolution = true;
				else validation.IsMultiSolution = false;
			}
			else {
				validation.IsValid = false;
				validation.IsSolvable = false;
				validation.IsMultiSolution = false;
			}
			this.SetPuzzle(validation.Puzzle);  // restore puzzle
			return validation;
		}


		enum Bit { empty = 0, no1, no2, no3, no4, no5, no6, no7, no8, no9, undefined, all, allNumbers };

		static int[] BitMask =  { 0x00000000, 0x00000001, 0x00000002, 0x00000004, 0x00000008, 0x00000010, 0x00000020, 0x00000040, 0x00000080, 0x00000100, 0x00000200, 0x000003ff, 0x000001ff };

		CCellObjects<SudokuCell> puzzle = null;
		CList<CList<SudokuCell>> clusters = null;
	}

}
