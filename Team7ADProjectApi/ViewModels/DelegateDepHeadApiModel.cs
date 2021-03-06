﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Team7ADProjectApi.ViewModels
{
    public class DelegateDepHeadApiModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UserId { get; set; }
        public string DelegatedDepartmentHeadName { get; set; }
        public string DepartmentName { get; set; }
        public List<EmployeeDto> Employees { get; set; }
    }
}