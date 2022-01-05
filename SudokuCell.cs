using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sudoku.Puzzle 
{
	internal class SudokuCell {

		/// <summary>
		/// BitMask value for sudoku cell
		/// If cell value is defined appropiate bit is set and undefined bit 0x0200 is not set
		/// </summary>
		public int Value { get; set; }

		/// <summary>
		/// Check if the cellvalue is defined or not.
		/// </summary>
		/// <returns> return true if cell has a defiend value or false if cell is not defined</returns>
		public bool IsDefined() { 
			return ((this.Value & 0x00000200) == 0);
		}

		/// <summary>
		/// Check if cell is defined and return the number value for the cell or 0 if cell is not defined
		/// or there is an invalid cell value
		/// </summary>
		/// <returns></returns>
		public int Number() {
			int count = 0;
			if (this.IsDefined()) {
				int temp = 0x00000001;
				while (temp != 0x00000200) {
					count++;
					if (this.Value == temp) return count;
					temp <<= 1;
				}
				count = 0;
			}
			return count;
		}
	}


}
