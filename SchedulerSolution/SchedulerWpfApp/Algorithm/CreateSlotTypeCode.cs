namespace SchedulerWpfApp.Algorithm
{
    public class CreateSlotTypeCode
    {
        private static readonly Dictionary<int, string> Slot1Map = new()
        {
            { 2, "24" },
            { 3, "35" },
            { 4, "42" },
            { 5, "53" },
            { 6, "C" }
        };

        private static readonly Dictionary<int, string> Slot2Map = new()
        {
            { 2, "42" },
            { 3, "53" },
            { 4, "24" },
            { 5, "35" },
            { 6, "C" }
        };

        /// Sinh mã loại slot (slotTypeCode) dựa trên ngày, slot và buổi học (AM/PM).
        /// Quy tắc:
        /// - Nếu là slot 1, các ngày 2, 3, 4, 5 sẽ trả về mã tương ứng (A24, A35, A42, A53 hoặc P24, P35, P42, P53).
        /// - Nếu là slot 2, các ngày 2, 3, 4, 5 sẽ trả về mã đảo ngược với slot 1 (A42, A53, A24, A35 hoặc P42, P53, P24, P35).
        /// - Các ngày khác hoặc slot khác sẽ trả về chuỗi rỗng.
        /// </summary>
        /// <param name="day">Thứ trong tuần (1=Chủ nhật, 2=Thứ 2, ..., 7=Thứ 7).</param>
        /// <param name="slot">Slot trong buổi học (1 hoặc 2).</param>
        /// <param name="sessionFilter">Buổi học ("A" cho AM, "P" cho PM).</param>
        /// <returns>Mã loại slot (slotTypeCode) hoặc chuỗi rỗng nếu không khớp quy tắc.</returns>
        public string GetSlotTypeCode(int day, int slot, string sessionFilter)
        {
            if ((slot != 1 && slot != 2) || string.IsNullOrWhiteSpace(sessionFilter))
                return string.Empty;

            var map = slot == 1 ? Slot1Map : Slot2Map;

            return map.TryGetValue(day, out var code)
                ? $"{sessionFilter.ToUpperInvariant()}{code}"
                : string.Empty;
        }
    }
}
