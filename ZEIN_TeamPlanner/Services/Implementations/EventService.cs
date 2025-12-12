using Ical.Net.DataTypes;
using Microsoft.EntityFrameworkCore;
using TeamPlanner.Data;
using ZEIN_TeamPlanner.DTOs.EventsDto;
using ZEIN_TeamPlanner.Models;
using ZEIN_TeamPlanner.Services.Interfaces;

namespace ZEIN_TeamPlanner.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGroupService _groupService;

        public EventService(ApplicationDbContext context, IGroupService groupService)
        {
            _context = context;
            _groupService = groupService;
        }

        public async Task<CalendarEvent> CreateEventAsync(EventCreateDto dto, string userId)
        {
            // Check Admin permission or group creator
            if (!await _groupService.IsUserAdminAsync(dto.GroupId, userId))
                throw new UnauthorizedAccessException("You do not have permission to create events in this group.");

            // Validate time
            if (dto.EndTime.HasValue && dto.EndTime <= dto.StartTime)
                throw new InvalidOperationException("End time must be after start time.");

            // Validate recurrence rule
            if (!string.IsNullOrEmpty(dto.RecurrenceRule))
            {
                try
                {
                    var icalEvent = new Ical.Net.CalendarComponents.CalendarEvent();
                    icalEvent.RecurrenceRules.Add(new RecurrencePattern(dto.RecurrenceRule));
                }
                catch
                {
                    throw new InvalidOperationException("Invalid recurrence rule (must follow iCal RRULE format).");
                }
            }

            // Validate time zone
            if (!string.IsNullOrEmpty(dto.TimeZoneId) &&
                !TimeZoneInfo.GetSystemTimeZones().Any(tz => tz.Id == dto.TimeZoneId) &&
                !NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(dto.TimeZoneId))
                throw new InvalidOperationException("Invalid time zone.");

            var calendarEvent = new CalendarEvent
            {
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TimeZoneId = dto.TimeZoneId ?? "Asia/Ho_Chi_Minh",
                GroupId = dto.GroupId,
                IsAllDay = dto.IsAllDay,
                RecurrenceRule = dto.RecurrenceRule,
                Type = dto.Type
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();
            return calendarEvent;
        }

        public async Task<CalendarEvent> UpdateEventAsync(EditEventDto dto, string userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Group)
                .FirstOrDefaultAsync(e => e.CalendarEventId == dto.CalendarEventId);

            if (calendarEvent == null)
                throw new KeyNotFoundException("Event does not exist.");

            if (!await _groupService.IsUserAdminAsync(calendarEvent.GroupId, userId))
                throw new UnauthorizedAccessException("You do not have permission to edit this event.");

            // Validate time
            if (dto.EndTime.HasValue && dto.EndTime <= dto.StartTime)
                throw new InvalidOperationException("End time must be after start time.");

            // Validate recurrence rule
            if (!string.IsNullOrEmpty(dto.RecurrenceRule))
            {
                try
                {
                    var icalEvent = new Ical.Net.CalendarComponents.CalendarEvent();
                    icalEvent.RecurrenceRules.Add(new RecurrencePattern(dto.RecurrenceRule));
                }
                catch
                {
                    throw new InvalidOperationException("Invalid recurrence rule (must follow iCal RRULE format).");
                }
            }

            // Validate time zone
            if (!string.IsNullOrEmpty(dto.TimeZoneId) &&
                !TimeZoneInfo.GetSystemTimeZones().Any(tz => tz.Id == dto.TimeZoneId) &&
                !NodaTime.DateTimeZoneProviders.Tzdb.Ids.Contains(dto.TimeZoneId))
                throw new InvalidOperationException("Invalid time zone.");

            calendarEvent.Title = dto.Title;
            calendarEvent.Description = dto.Description;
            calendarEvent.StartTime = dto.StartTime;
            calendarEvent.EndTime = dto.EndTime;
            calendarEvent.TimeZoneId = dto.TimeZoneId ?? "Asia/Ho_Chi_Minh";
            calendarEvent.IsAllDay = dto.IsAllDay;
            calendarEvent.RecurrenceRule = dto.RecurrenceRule;
            calendarEvent.Type = dto.Type;

            await _context.SaveChangesAsync();
            return calendarEvent;
        }

        public async Task<bool> CanAccessEventAsync(int eventId, string userId)
        {
            var calendarEvent = await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.CalendarEventId == eventId);

            if (calendarEvent == null)
                return false;

            return await _groupService.CanAccessGroupAsync(calendarEvent.GroupId, userId);
        }
    }
}