using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syslib;

namespace Sudoku.Puzzle
{
	public class CreateSudoku {

		public SudokuPuzzle GetNewSudoku() {
			return FillRandomSudoku();
		}

		public SudokuPuzzle GetSudokuPuzzle() {
			return GetSudokuPuzzle2();	// this version generate a little better puzzles (easy puzzles)
		}

		// easy puzzles
		SudokuPuzzle GetSudokuPuzzle1() {
			var sourcepuzzle = new CStr(FillRandomSudoku().GetPuzzle());    // source sudoku (solved)
			var workpuzzle = sourcepuzzle.Copy();               // numbers that can be uncovered
			var targetsudoku = new SudokuPuzzle();              // new empty target puzzle
			var newpuzzle = new CStr().Fill(81,(byte)'.');		// new puzzle that builds up
			var temppuzzle = new CStr();
			var rnd = new CRandom();
			int num, counter;

			// uncover numbers until puzzle can be solved with rules
			while (true) {
				while (true) {
					num = rnd.RandomNumber(0, 80);
					if (workpuzzle.Get(num) != '.') {
						newpuzzle.Set(num, sourcepuzzle.Get(num));	// unfold number in puzzle
						workpuzzle.Set(num, '.');   // block position as its already given in the puzzle
						break;
					}
				}
				if (targetsudoku.SetPuzzle(newpuzzle.ToString()).ResolveRules() > 0) {
					// find the numbers that was resolved
					temppuzzle.Str(targetsudoku.GetPuzzle());
					counter = 0;
					while (counter < 81) {
						if ((temppuzzle.Get(counter) != newpuzzle.Get(counter)) && (workpuzzle.Get(counter) != '.') ) {
							workpuzzle.Set(counter, '.');
						}   
						counter++;
					}
				}
				if (targetsudoku.IsSolved()) break;
			}
			return targetsudoku.SetPuzzle(newpuzzle.ToString());
		}

		// easy puzzles
		SudokuPuzzle GetSudokuPuzzle2() {
			var sourcepuzzle = new CStr(FillRandomSudoku().GetPuzzle());    // source sudoku (solved)
			var workpuzzle = sourcepuzzle.Copy();               // numbers that can be uncovered
			var targetsudoku = new SudokuPuzzle();              // new empty target puzzle
			var newpuzzle = new CStr().Fill(81, (byte)'.');     // new puzzle that builds up
			var temppuzzle = new CStr();
			var rnd = new CRandom();
			int num, counter;

			// uncover numbers until puzzle can be solved with rules
			while (true) {
				while (true) {
					temppuzzle.Str(targetsudoku.GetPossible(rnd.RandomNumber(1, 9))); // get possible positions for random number
					num = rnd.RandomNumber(0, 80);  // get random start position
					if (rnd.RandomBool()) {
						while (num < 81) {
							if ((temppuzzle.Get(num) != '.') && (workpuzzle.Get(num) != '.')) break;
							num++;
						}
					}
					else {
						while (num >= 0) {
							if ((temppuzzle.Get(num) != '.') && (workpuzzle.Get(num) != '.')) break;
							num--;
						}
					}
					if ((num >= 0) && (num <= 80)) {
						newpuzzle.Set(num, sourcepuzzle.Get(num));  // unfold number in puzzle
						workpuzzle.Set(num, '.');   // block position as its already given in the puzzle
						break;
					}
				}
				if (targetsudoku.SetPuzzle(newpuzzle.ToString()).ResolveRules() > 0) {
					// find the numbers that was resolved
					temppuzzle.Str(targetsudoku.GetPuzzle());
					counter = 0;
					while (counter < 81) {
						if ((temppuzzle.Get(counter) != newpuzzle.Get(counter)) && (workpuzzle.Get(counter) != '.')) {
							workpuzzle.Set(counter, '.');
						}
						counter++;
					}
				}
				if (targetsudoku.IsSolved()) break;
			}
			return targetsudoku.SetPuzzle(newpuzzle.ToString());
		}


		// generate a random sudoku by filling random numbers to 
		//
		//	rrr ... ...
		//	rrr ... ...
		//	rrr ... ...
		//	... r.. ...
		//	... .r. ...
		//	... ..r ...
		//	... ... r..
		//	... ... .r.
		//	... ... ..r
		SudokuPuzzle FillRandomSudoku() {
			var newsudoku = new SudokuPuzzle();
			var workpuzzle = new CStr();
			byte[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var rnd = new CRandom();
			workpuzzle.Fill(81, (byte)'.');
			while (true) {
				rnd.Shuffle<byte>(numbers);
				workpuzzle.Set(0, (byte)('0' + numbers[0]));
				workpuzzle.Set(1, (byte)('0' + numbers[1]));
				workpuzzle.Set(2, (byte)('0' + numbers[2]));

				workpuzzle.Set(9, (byte)('0' + numbers[3]));
				workpuzzle.Set(10, (byte)('0' + numbers[4]));
				workpuzzle.Set(11, (byte)('0' + numbers[5]));

				workpuzzle.Set(18, (byte)('0' + numbers[6]));
				workpuzzle.Set(19, (byte)('0' + numbers[7]));
				workpuzzle.Set(20, (byte)('0' + numbers[8]));

				workpuzzle.Set(30, (byte)('0' + rnd.RandomNumber(1, 9)));
				workpuzzle.Set(40, (byte)('0' + rnd.RandomNumber(1, 9)));
				workpuzzle.Set(50, (byte)('0' + rnd.RandomNumber(1, 9)));
				workpuzzle.Set(60, (byte)('0' + rnd.RandomNumber(1, 9)));
				workpuzzle.Set(70, (byte)('0' + rnd.RandomNumber(1, 9)));
				workpuzzle.Set(80, (byte)('0' + rnd.RandomNumber(1, 9)));

				if (newsudoku.SetPuzzle(workpuzzle.ToString()).ResolveNumPass()) break;
			}
			return newsudoku;
		}

	}


}
