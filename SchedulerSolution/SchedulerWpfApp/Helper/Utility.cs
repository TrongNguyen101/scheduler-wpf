using System.IO;
using System.Text;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Provides utility methods for the scheduler application.
    /// </summary>
    public static class Utility
    {
        private static string message = string.Empty;
        private static List<int> errorRows = new List<int>();
        private static HashSet<string> rowsDuplicated = new HashSet<string>();

        /// <summary>
        /// Asynchronously identifies rows in an Excel file that contain null or empty values.
        /// </summary>
        /// <param name="filePath">The path to the Excel file to be analyzed.</param>
        /// <returns>A list of row numbers (1-based) that contain at least one null or empty cell.</returns>
        /// <remarks>
        /// This method checks rows starting from row 2 (skipping the header row),
        /// and identifies rows where at least one cell contains a null or empty value.
        /// </remarks>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the application does not have the required permissions to read the file.</exception>
        public static async Task IsEmptyExcelRowAsync(string filePath)
        {
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = application.Workbooks.Open(fileStream);
                    IWorksheet worksheet = workbook.Worksheets[0];

                    int rowCount = worksheet.UsedRange.LastRow;
                    int colCount = worksheet.UsedRange.LastColumn;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        bool hasNull = false;
                        for (int col = 1; col <= colCount; col++)
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
                }

                if (errorRows.Count == 0)
                {
                    errorRows.Clear();
                }
                else if (errorRows.Count == 1)
                {
                    message = $"Phát hiện dòng trống tại dòng: {string.Join(", ", errorRows)}";
                    errorRows.Clear();
                    throw new Exception(message);
                }
                else if (errorRows.Count > 1)
                {
                    message = $"Phát hiện dòng trống tại các dòng: {string.Join(", ", errorRows)}";
                    errorRows.Clear();
                    throw new Exception(message);
                }
            }
        }

        public static void IsDuplicatedExcelRow(string filePath)
        {
            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int duplicateRow, int originalRow)>();

            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var workbook = application.Workbooks.Open(fileStream);
            var worksheet = workbook.Worksheets[0];

            int rowCount = worksheet.UsedRange.LastRow;
            int colCount = worksheet.UsedRange.LastColumn;

            for (int row = 2; row <= rowCount; row++)
            {
                var rowData = new StringBuilder();
                for (int col = 1; col <= colCount; col++)
                {
                    rowData.Append(worksheet[row, col].Value);
                    rowData.Append('|');
                }

                string rowKey = rowData.ToString();

                if (duplicatedMap.TryGetValue(rowKey, out int originalRow))
                {
                    // Dòng này trùng với originalRow
                    duplicates.Add((row, originalRow));
                }
                else
                {
                    duplicatedMap[rowKey] = row;
                }
            }

            if (duplicates.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Phát hiện các dòng trùng lặp:");

                foreach (var (dupRow, origRow) in duplicates)
                {
                    sb.AppendLine($"- Dòng {dupRow} trùng với dòng {origRow}");
                }

                throw new Exception(sb.ToString());
            }
        }

    }
}
}
