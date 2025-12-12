using ZEIN_TeamPlanner.DTOs.TasksDto;
using ZEIN_TeamPlanner.Models;

namespace ZEIN_TeamPlanner.Services.Interfaces
{
    public interface ITaskService
    {
        Task<TaskItem> CreateTaskAsync(TaskCreateDto dto, string userId);
        Task<TaskItem> UpdateTaskAsync(TaskEditDto dto, string userId);
        Task<bool> CanAccessTaskAsync(int taskId, string userId);
        Task DeleteTaskAsync(int taskId, string userId);
        Task UpdateTaskStatusAsync(int taskId, TaskItem.TaskStatus status, string userId);
    }
}