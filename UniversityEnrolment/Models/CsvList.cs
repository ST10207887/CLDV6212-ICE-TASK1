namespace UniversityEnrolment.Models
{
    /// <summary>
    /// Azure Table storage cannot store lists, so lists are kept as
    /// comma-separated strings. These helpers convert both ways.
    /// </summary>
    public static class CsvList
    {
        public static List<string> Split(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        public static string Join(IEnumerable<string> values) =>
            string.Join(",", values.Where(v => !string.IsNullOrWhiteSpace(v))
                                   .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
