using System;

namespace FlsurfDesktop.Core.Models
{
    public class WorkSessionDto
    {
        public Guid Id { get; }
        public DateTime StartDate { get; }
        public DateTime? EndDate { get; }
        public string Status { get; }

        public WorkSessionDto(Guid id, DateTime start, DateTime? end, string status)
        {
            Id = id;
            StartDate = start;
            EndDate = end;
            Status = status;
        }

        public DateTime StartDateLocal => StartDate.ToLocalTime();
        public DateTime? EndDateLocal => EndDate?.ToLocalTime();
    }
}
