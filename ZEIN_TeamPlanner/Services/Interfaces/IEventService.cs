using ZEIN_TeamPlanner.DTOs.EventsDto;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services.Interfaces
{
    public interface IEventService
    {
        Task<CalendarEvent> CreateEventAsync(EventCreateDto dto, string userId);
        Task<CalendarEvent> UpdateEventAsync(EditEventDto dto, string userId);
        Task<bool> CanAccessEventAsync(int eventId, string userId);
    }
}