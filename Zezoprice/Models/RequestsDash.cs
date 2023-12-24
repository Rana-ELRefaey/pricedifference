using System;
using System.Collections.Generic;

namespace Zezoprice.Models
{
    public partial class RequestsDash
    {
        public int? Id { get; set; }
        public string? Requestnumber { get; set; }
        public string? Arabicfullname { get; set; }
        public string? Sec { get; set; }
        public string? Ssec { get; set; }
        public string? Gov { get; set; }
        public string? PropertyNumber { get; set; }
        public string? FloorNumber { get; set; }
        public string? Streetname { get; set; }
        public string? ApartmentNumber { get; set; }
        public string? UniqueMark { get; set; }
        public string? Telephonenumber { get; set; }
        public DateOnly? DueDate { get; set; }
        public int? Collected { get; set; }
        public int? SurveyReview { get; set; }
        public string? Cert { get; set; }
        public string? SurveyReviewStatus { get; set; }
        public short? PrintStatus { get; set; }
        public int? Certt { get; set; }
        public string? Certtt { get; set; }
        public DateOnly? Tofedex { get; set; }
        public string? CollectStatus { get; set; }
        public string? Typee { get; set; }
    }
}
