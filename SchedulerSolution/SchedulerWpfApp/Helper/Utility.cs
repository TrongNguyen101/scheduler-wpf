using System.Text;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Provides utility methods for the scheduler application.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Checks for empty cells in an Excel worksheet and throws an exception if any are found.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to be analyzed.</param>
        /// <remarks>
        /// This method checks rows starting from row 2 (skipping the header row),
        /// and identifies rows where at least one cell contains a null or empty value.
        /// If empty cells are found, an exception is thrown with details about which rows contain empty cells.
        /// </remarks>
        /// <exception cref="Exception">Thrown when empty cells are found in the Excel worksheet, with details about which rows have empty cells.</exception>
        public static void IsEmptyExcelRow(IWorksheet worksheet, int totalRow, int totalCol)
        {
            var errorRows = new List<int>();

            for (int row = 2; row <= totalRow; row++)
            {
                bool hasNull = false;
                for (int col = 1; col <= totalCol; col++)
                {
                    var cellValue = worksheet[row, col].Value;
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        hasNull = true;
                        break;
                    }
                }

                if (hasNull)
                {
                    errorRows.Add(row);
                }
            }

            if (errorRows.Count > 0)
            {
                var message = errorRows.Count == 1
                    ? $"Import thất bại phát hiện ô trống tại dòng: {string.Join(", ", errorRows)}"
                    : $"Import thất bại phát hiện ô trống tại các dòng: {string.Join(", ", errorRows)}";

                throw new Exception(message);
            }
        }

        /// <summary>
        /// Checks for duplicate rows in an Excel worksheet and throws an exception if any are found.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to be analyzed.</param>
        /// <remarks>
        /// This method checks rows starting from row 2 (skipping the header row),
        /// and identifies rows that have identical content to other rows.
        /// A row is considered a duplicate if all of its cells contain exactly the same values as another row.
        /// If duplicates are found, an exception is thrown with details about which rows are duplicates.
        /// </remarks>
        /// <exception cref="Exception">Thrown when duplicate rows are found in the Excel worksheet, with details about which rows are duplicates.</exception>
        public static void IsDuplicatedExcelRow(IWorksheet worksheet, int totalRow, int totalCol)
        {
            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int duplicateRow, int originalRow)>();

            for (int row = 2; row <= totalRow; row++)
            {
                var rowData = new StringBuilder();
                for (int col = 1; col <= totalCol; col++)
                {
                    rowData.Append(worksheet[row, col].Value);
                    rowData.Append('|');
                }

                string rowKey = rowData.ToString();

                if (duplicatedMap.TryGetValue(rowKey, out int originalRow))
                {
                    duplicates.Add((row, originalRow));
                }
                else
                {
                    duplicatedMap[rowKey] = row;
                }
            }

            if (duplicates.Any())
            {
                var message = new StringBuilder();
                message.AppendLine("Import thất bại phát hiện các dòng trùng lặp:");

                foreach (var (dupRow, origRow) in duplicates)
                {
                    message.AppendLine($"- Dòng {dupRow} trùng với dòng {origRow}");
                }

                throw new Exception(message.ToString());
            }
        }
    }
}

