using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syslib;

namespace Sudoku.Puzzle
{
	public class SudokuValidation {

		public bool IsValidated { get; internal set; }
		public bool IsValid { get; internal set; }
		public bool IsSolvable { get; internal set; }
		public bool IsMultiSolution { get; internal set; }
		public int SolutionCount { get { return Solutions.Count(); } }
		public CList<string> Solutions { get; internal set; }
		public string Puzzle { get; private set; }
		public int SolutionsMax { get; private set; }   // break iteration when Max is reached
		public int BackTrackCounter { get; internal set; }


		public SudokuValidation(string puzzle) {
			this.Solutions = new CList<string>();
			this.validationPuzzle = new SudokuPuzzle();
			ResetValidation();
			this.Puzzle = puzzle;
		}

		/// <summary>
		/// Change default limit of 1000 puzzle solutions to maxsolutions
		/// if limit is reached there may be more solutions
		/// if limit is set to 0, it will be no limit and it will iterate through all combinations
		/// if limit should be at least 2 to be able to decide if it is a multisolution puzzle
		/// </summary>
		/// <param name="maxsolutions"></param>
		/// <returns></returns>
		public SudokuValidation SetLimit(int maxsolutions) {
			if (maxsolutions < 0) this.SolutionsMax = 0;
			else this.SolutionsMax = maxsolutions;
			return this;
		}

		void ResetValidation() {
			this.IsValidated = false;
			this.IsValid = false;
			this.IsSolvable = false;
			this.IsMultiSolution = false;
			this.Puzzle = "";
			this.Solutions.Clear();
			this.SolutionsMax = 1000;
			this.BackTrackCounter = 0;
		}

		SudokuPuzzle validationPuzzle;

	}
}
