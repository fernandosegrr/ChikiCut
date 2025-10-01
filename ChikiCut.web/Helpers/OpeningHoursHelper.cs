using System.Text.Json;

namespace ChikiCut.web.Helpers
{
    public static class OpeningHoursHelper
    {
        private static readonly Dictionary<string, string> DayNames = new()
        {
            { "mon", "Lunes" },
            { "tue", "Martes" },
            { "wed", "Miércoles" },
            { "thu", "Jueves" },
            { "fri", "Viernes" },
            { "sat", "Sábado" },
            { "sun", "Domingo" }
        };

        private static readonly string[] DayOrder = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

        // Instancia estática para reutilizar JsonSerializerOptions
        private static readonly JsonSerializerOptions IndentedJsonOptions = new JsonSerializerOptions { WriteIndented = true };
        
        // Opciones de deserialización con naming policy
        private static readonly JsonSerializerOptions DeserializationOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };

        public static string FormatOpeningHours(string? jsonHours)
        {
            if (string.IsNullOrEmpty(jsonHours) || jsonHours == "{}")
            {
                return "No definido";
            }

            try
            {
                var hoursDict = JsonSerializer.Deserialize<Dictionary<string, List<TimeSlot>>>(jsonHours, DeserializationOptions);
                if (hoursDict == null || hoursDict.Count == 0)
                {
                    return "No definido";
                }

                var result = new List<string>();
                
                foreach (var day in DayOrder)
                {
                    if (hoursDict.TryGetValue(day, out var timeSlots) && timeSlots != null && timeSlots.Count > 0)
                    {
                        var dayName = DayNames[day];

                        var times = timeSlots.Select(slot => 
                        {
                            var openTime = string.IsNullOrEmpty(slot.Open) ? "?" : slot.Open;
                            var closeTime = string.IsNullOrEmpty(slot.Close) ? "?" : slot.Close;
                            return $"{openTime} - {closeTime}";
                        });
                        result.Add($"{dayName}: {string.Join(", ", times)}");
                    }
                    else
                    {
                        // Si el día no existe en el diccionario o está vacío
                        result.Add($"{DayNames[day]}: Cerrado");
                    }
                }

                return string.Join("<br/>", result);
            }
            catch (JsonException ex)
            {
                return $"Error de formato: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static string FormatOpeningHoursCompact(string? jsonHours)
        {
            if (string.IsNullOrEmpty(jsonHours) || jsonHours == "{}")
            {
                return "No definido";
            }

            try
            {
                var hoursDict = JsonSerializer.Deserialize<Dictionary<string, List<TimeSlot>>>(jsonHours, DeserializationOptions);
                if (hoursDict == null || hoursDict.Count == 0)
                {
                    return "No definido";
                }

                // Agrupar días con el mismo horario
                var groups = new Dictionary<string, List<string>>();
                
                foreach (var day in DayOrder)
                {
                    if (hoursDict.TryGetValue(day, out var timeSlots) && timeSlots != null && timeSlots.Count > 0)
                    {
                        var times = timeSlots.Select(slot => 
                        {
                            var openTime = string.IsNullOrEmpty(slot.Open) ? "?" : slot.Open;
                            var closeTime = string.IsNullOrEmpty(slot.Close) ? "?" : slot.Close;
                            return $"{openTime}-{closeTime}";
                        });
                        var schedule = string.Join(", ", times);

                        if (!groups.ContainsKey(schedule))
                        {
                            groups[schedule] = new List<string>();
                        }
                        groups[schedule].Add(DayNames[day]);
                    }
                    else
                    {
                        // Si el día no existe o está vacío, agregarlo como cerrado
                        if (!groups.ContainsKey("Cerrado"))
                        {
                            groups["Cerrado"] = new List<string>();
                        }
                        groups["Cerrado"].Add(DayNames[day]);
                    }
                }

                var result = groups.Select(group =>
                {
                    var days = group.Value;
                    var schedule = group.Key;
                    
                    if (days.Count == 1)
                    {
                        return $"{days[0]}: {schedule}";
                    }
                    else if (days.Count > 1)
                    {
                        return $"{string.Join(", ", days)}: {schedule}";
                    }
                    return "";
                });

                return string.Join(" | ", result.Where(r => !string.IsNullOrEmpty(r)));
            }
            catch (JsonException ex)
            {
                return $"Error de formato: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public class TimeSlot
        {
            public string Open { get; set; } = "";
            public string Close { get; set; } = "";
        }
    }
}