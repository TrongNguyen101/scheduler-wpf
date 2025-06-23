using AutoMapper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;
namespace SchedulerWpfApp.ServiceRefactor.RoomService
{
    /// <summary>
    /// 
    /// </summary>
    public class RoomService : IRoomService
    {
        #region Fields
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Contractors
        /// <summary>
        /// Initializes a new instance of the <see cref="RoomService"/> class.
        /// </summary>
        /// <param name="mapper">The AutoMapper instance used for object mapping.</param>
        /// <param name="unitOfWork">The unit of work instance for database operations.</param>
        public RoomService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Method
        /// <summary>
        /// Reads a list of rooms from an Excel file.
        /// This method opens the specified Excel file, reads the header row to map columns, 
        /// validates required headers, and then iterates through each row to create Room objects.
        /// Returns a list of Room objects populated from the Excel data.
        /// </summary>
        /// <param name="filePath">The path to the Excel file to read.</param>
        /// <returns>A list of Room objects read from the Excel file.</returns>
        public List<Room> ReadRoomListFromExcel(string filePath)
        {
            var rooms = new List<Room>();
            // key: RoomName, value: list of line numbers where this room appears
            var roomLineMap = new Dictionary<string, List<int>>();
            using ExcelEngine excelEngine = new();
            var app = excelEngine.Excel;
            app.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = app.Workbooks.Open(filePath);
            var sheet = workbook.Worksheets[0];

            int rowCount = sheet.UsedRange.LastRow;
            int colCount = sheet.UsedRange.LastColumn;

            Dictionary<string, int> headerMap = new();
            for (int c = 1; c <= colCount; c++)
            {
                string header = sheet[1, c].Value?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(header))
                    headerMap[header] = c;
            }
            string[] requiredHeaders = {"RoomName", "Loại phòng", "Tầng", "Tòa", "SLSV", "Status" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                // get RoomName from the header map
                string roomName = sheet[r, headerMap["RoomName"]].Value;
                var room = new Room
                {
                    Building = sheet[r, headerMap["Tòa"]].Value?.Trim(),
                    Floor = int.TryParse(sheet[r, headerMap["Tầng"]].Value, out var floor) ? floor : 0,
                    RoomName = sheet[r, headerMap["RoomName"]].Value?.Trim(),
                    Status = sheet[r, headerMap["Status"]].Value?.Trim() ?? "available", // Default to "available" if not specified
                    TotalPersons = int.TryParse(sheet[r, headerMap["SLSV"]].Value, out var totalPersons) ? totalPersons : 0,
                    TypeOfRoom = sheet[r, headerMap["Loại phòng"]].Value,
                };
                rooms.Add(room);
                // If roomName already exists, create a new position in its place.
                if (!roomLineMap.ContainsKey(roomName))
                {
                    roomLineMap[roomName] = new List<int>();
                }
                // Add the current row number to the list for this roomName
                roomLineMap[roomName].Add(r);
            }
            var duplicateRooms = roomLineMap
            //Filter out RoomNames that appear more than once.
            .Where(r => r.Value.Count > 1)
            // get key and value as a dictionary
            .ToDictionary(r => r.Key, r => r.Value);
            if (duplicateRooms.Count > 0)
            {
                // Create an error message for each duplicate room.
                var errorMessage = duplicateRooms
                    .Select(dlc => $"RoomName '{dlc.Key}' trùng tại các dòng: {string.Join(", ", dlc.Value)}");
                throw new Exception("Phát hiện dữ liệu trùng trong file Excel:\n " + string.Join("\n", errorMessage));
            }
            return rooms;
        }

        /// <summary>
        /// Exports a list of Room objects to an Excel file.
        /// This method creates a new Excel workbook, writes the header row, and populates each row with room data.
        /// The resulting Excel file is saved to the specified file path.
        /// </summary>
        /// <param name="room">The list of Room objects to export.</param>
        /// <param name="filePath">The file path where the Excel file will be saved.</param>
        public void ExportRoomToExcel(List<Room> room, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];
            // Header
            sheet[1, 1].Text = "RoomName";
            sheet[1, 2].Text = "Loại phòng";
            sheet[1, 3].Text = "Tầng";
            sheet[1, 4].Text = "Tòa";
            sheet[1, 5].Text = "SLSV";
            sheet[1, 6].Text = "Status";
            int row = 2;
            foreach (var rooms in room)
            {
                sheet[row, 1].Text = rooms.RoomName ?? "";
                sheet[row, 2].Text = rooms.TypeOfRoom ?? "";
                sheet[row, 3].Number = rooms.Floor;
                sheet[row, 4].Text = rooms.Building ?? "";
                sheet[row, 5].Number = rooms.TotalPersons;
                sheet[row, 6].Text = rooms.Status ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Imports a list of rooms from an Excel file into the database.
        /// This method iterates through the provided list of rooms and adds each room to the database context.
        /// </summary>
        public async Task ImportRoomFromExcel(List<Room> listRoomFromExcel)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var room in listRoomFromExcel)
                {
                    var existing = await _unitOfWork.RoomRepository.CheckRoomNameExistsAsync(room.RoomName);
                    if (!existing)
                    {
                        await _unitOfWork.RoomRepository.AddAsync(room);
                    }
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Có lỗi trong quá trình import dữ liệu", ex);
            }
        }

        /// <summary>
        /// Retrieves all rooms from the database.
        /// This method returns a list of all rooms stored in the database.
        /// </summary>
        public async Task<List<Room>> GetAllAsync()
        {
            var rooms = await _unitOfWork.Repository<Room>().GetAllAsync();
            return rooms;
        }

        /// <summary>
        /// Retrieves number of rooms from the database.
        /// This method returns a list of all rooms stored in the database.
        /// </summary>
        public async Task<List<Room>> GetNumberOfRoom(int numberOfRoom)
        {
            return await _unitOfWork.RoomRepository.GetNumberOfRoom(numberOfRoom);
        }

        /// <summary>
        /// Retrieves a room by its ID.
        /// This method searches for a room in the database using its unique identifier.
        /// </summary>
        /// <param name="id"></param>
        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<Room>().GetByIdAsync(id);
        }

        /// <summary>
        /// Adds a new room to the database.
        /// This method takes a Room object as input and adds it to the database context, then saves the changes asynchronously.
        /// </summary>
        /// <param name="room"></param>
        public async Task AddRoom(Room room)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Room>().AddAsync(room);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Retrieves a specific room by their ID
        /// </summary>
        /// <param name="id">The ID of the room to retrieve</param>
        /// <returns>The subject with the specified ID, or null if not found</returns>
        public async Task<Room?> GetByRoomIdAsync(int roomid)
        {
            return await _unitOfWork.Repository<Room>().GetByIdAsync(roomid);
        }

        /// <summary>
        /// Updates an existing room in the database.
        /// This method takes a Room object as input, updates the corresponding record in the database, and saves the changes asynchronously.
        /// </summary>
        /// <param name="room"></param>
        public async Task UpdateRoom(Room room)
        {
            var existingRoom = await GetByRoomIdAsync(room.RoomId);
            if (existingRoom != null)
            {
                _mapper.Map(room, existingRoom);
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Room>().UpdateAsync(existingRoom);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Deletes a room from the database by its ID.
        /// This method retrieves the room by its ID, removes it from the database context, and saves the changes asynchronously.
        /// </summary>
        public async Task DeleteRoom(int id)
        {

            var room = await GetByIdAsync(id);
            if (room != null)
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Room>().DeleteAsync(id);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Searches for rooms in the database based on a search term.
        /// This method filters the rooms whose names contain the specified search term, ignoring case.
        /// </summary>
        public async Task<List<Room>> SearchRoomsAsync(string searchTerm)
        {
            return await _unitOfWork.RoomRepository.SearchRoomsAsync(searchTerm);
        }

        /// <summary>
        /// Checks if a room ID exists in the database.
        /// This method checks if there is any room in the database with the specified room name.
        /// </summary>
        /// <param name="roomname"></param>
        public async Task<bool> CheckRoomNameExistsAsync(string roomname)
        {
            return await _unitOfWork.RoomRepository.CheckRoomNameExistsAsync(roomname);
        }
        #endregion
    }
}
