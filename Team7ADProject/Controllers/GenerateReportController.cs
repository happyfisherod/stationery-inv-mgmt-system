﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Team7ADProject.Entities;
using Team7ADProject.ViewModels.GenerateReport;
using Newtonsoft.Json;

namespace Team7ADProject.Controllers
{
    //For SS to generate reports
    //Author: Elaine Chan
    public class GenerateReportController : Controller
    {
        #region Author: Elaine Chan
        // GET: GenerateReport
        [Authorize(Roles = "Store Manager, Store Supervisor")]
        public ActionResult GenerateDashboard()
        {
            LogicDB context = new LogicDB();

            var grvm = new GenerateReportViewModel
            {
                fDate = new DateTime(2017, 1, 1),
                tDate = DateTime.Today,
                module = "Disbursements",
                statcategory = new List<string>(),
                entcategory = new List<string>(),
                selectentcategory = new List<string>(),
                selectstatcategory = new List<string>()

            };
            var slist = context.Stationery.GroupBy(x => x.Category).Select(y => y.Key);
            foreach (var l in slist)
            {
                grvm.statcategory.Add(l);
            }

            grvm.selectstatcategory = grvm.statcategory;
            var elist = context.Department.GroupBy(x => x.DepartmentId).Select(y => y.Key);
            foreach (var l in elist)
            {
                grvm.entcategory.Add(l);
            }
            grvm.selectentcategory = grvm.entcategory;

            #region Disbursement by DeptID
            List<StringDoubleDPViewModel> deptdataPoints = new List<StringDoubleDPViewModel>();

            var gendeptRpt = context.TransactionDetail.Where(x=> x.Disbursement.AcknowledgedBy != null).
                GroupBy(x => new { x.Disbursement.DepartmentId }).
                Select(y => new { DeptID = y.Key.DepartmentId, TotalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

            foreach (var i in gendeptRpt)
            {
                deptdataPoints.Add(new StringDoubleDPViewModel(i.DeptID, (double)i.TotalAmt));
            }

            ViewBag.deptDataPoints = JsonConvert.SerializeObject(deptdataPoints);
            #endregion

            #region Disbursement by Stationery Category

            List<StringDoubleDPViewModel> statdataPoints = new List<StringDoubleDPViewModel>();

            var genstatRpt = context.TransactionDetail.Where(x => x.Disbursement.AcknowledgedBy != null).
                    GroupBy(y => new { y.Stationery.Category }).
                    Select(z => new { itemCat = z.Key.Category, totalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

            foreach (var i in genstatRpt)
            {
                statdataPoints.Add(new StringDoubleDPViewModel(i.itemCat, (double)i.totalAmt));
            }

            ViewBag.statDataPoints = JsonConvert.SerializeObject(statdataPoints);

            #endregion

            #region Disbursements over time

            List<StringDoubleDPViewModel> timedataPoints = new List<StringDoubleDPViewModel>();

            var timeRpt = context.TransactionDetail.Where(x => x.Disbursement.AcknowledgedBy != null).
                OrderBy(x => x.TransactionDate).
                GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToArray().
                Select(y => new { dateval = string.Format("{0} {1}", Enum.Parse(typeof(EnumMonth), y.Key.Month.ToString()), y.Key.Year), totalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

            foreach (var i in timeRpt)
            {
                timedataPoints.Add(new StringDoubleDPViewModel(i.dateval, (double)i.totalAmt));
            }

            ViewBag.timedataPoints = JsonConvert.SerializeObject(timedataPoints);
            #endregion

            return View(grvm);
        }

        [Authorize(Roles = "Store Manager, Store Supervisor")]
        [HttpPost]
        public ActionResult GenerateDashboard(DateTime? fromDateTP, DateTime? toDateTP, string module, List<string> selstatcat, List<string> seldeptcat)
        {
          
                LogicDB context = new LogicDB();

            #region Build VM
            var grvm = new GenerateReportViewModel
            {
                fDate = (DateTime)fromDateTP,
                tDate = (DateTime)toDateTP,
                module = module,
                statcategory = new List<string>(),
                entcategory = new List<string>(),
                selectentcategory = new List<string>(),
                selectstatcategory = new List<string>()

            };
            var slist = context.Stationery.GroupBy(x => x.Category).Select(y => y.Key);
            foreach (var l in slist)
            {
                grvm.statcategory.Add(l);
            }

            if(selstatcat.Count ==0)
            {
                grvm.selectstatcategory = grvm.statcategory;
            }
            else
            {
                foreach (var l in selstatcat)
                {
                    grvm.selectstatcategory.Add(l);
                }
            }
           
            var elist = context.Department.GroupBy(x => x.DepartmentId).Select(y => y.Key);
            foreach (var l in elist)
            {
                grvm.entcategory.Add(l);
            }
            if (seldeptcat.Count == 0) {

                grvm.selectentcategory = grvm.entcategory;
            }
            else { 
            foreach (var l in seldeptcat)
            {
                grvm.selectentcategory.Add(l);
            }
            }
            #endregion

            if (module =="Disbursements")
            { 

                #region Disbursement by DeptId
                List<StringDoubleDPViewModel> deptdataPoints = new List<StringDoubleDPViewModel>();
                var gendeptRpt = context.TransactionDetail.
                    Where(x => x.Disbursement.AcknowledgedBy != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    GroupBy(y => new { y.Disbursement.DepartmentId }).
                    Select(z => new { DeptID = z.Key.DepartmentId, TotalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in gendeptRpt)
                {
                    deptdataPoints.Add(new StringDoubleDPViewModel(i.DeptID, (double)i.TotalAmt));
                }

                ViewBag.deptDataPoints = JsonConvert.SerializeObject(deptdataPoints);
                #endregion

                #region Disbursement by Stationery Category

                List<StringDoubleDPViewModel> statdataPoints = new List<StringDoubleDPViewModel>();
                var genstatRpt = context.TransactionDetail.
                    Where(x => x.Disbursement.AcknowledgedBy != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                        GroupBy(y => new { y.Stationery.Category }).
                        Select(z => new { itemCat = z.Key.Category, totalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in genstatRpt)
                {
                    statdataPoints.Add(new StringDoubleDPViewModel(i.itemCat, (double)i.totalAmt));
                }

                ViewBag.statDataPoints = JsonConvert.SerializeObject(statdataPoints);

                #endregion

                #region Disbursements over time

                List<StringDoubleDPViewModel> timedataPoints = new List<StringDoubleDPViewModel>();

                var timeRpt = context.TransactionDetail.Where(x => x.Disbursement.AcknowledgedBy != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    OrderBy(x => x.TransactionDate).
                    GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToArray().
                    Select(y => new { dateval = string.Format("{0} {1}", Enum.Parse(typeof(EnumMonth), y.Key.Month.ToString()), y.Key.Year), totalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

                foreach (var i in timeRpt)
                {
                    timedataPoints.Add(new StringDoubleDPViewModel(i.dateval, (double)i.totalAmt));
                }

                ViewBag.timedataPoints = JsonConvert.SerializeObject(timedataPoints);

                #endregion
            }

            if (module == "Requests")
            {
                #region Requests by Dept
                List<StringDoubleDPViewModel> deptdataPoints = new List<StringDoubleDPViewModel>();
                var gendeptRpt = context.TransactionDetail.
                    Where(x => x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    GroupBy(y=> new { y.StationeryRequest.DepartmentId}).
                    Select(z => new { DeptID = z.Key.DepartmentId, TotalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });
                foreach (var i in gendeptRpt)
                {
                    deptdataPoints.Add(new StringDoubleDPViewModel(i.DeptID, (double)i.TotalAmt));
                }

                ViewBag.deptDataPoints = JsonConvert.SerializeObject(deptdataPoints);
                #endregion

                #region Requests by Stationery Category

                List<StringDoubleDPViewModel> statdataPoints = new List<StringDoubleDPViewModel>();
                var genstatRpt = context.TransactionDetail.
                    Where(x => x.StationeryRequest.Status == "Completed" && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                        GroupBy(y => new { y.Stationery.Category }).
                        Select(z => new { itemCat = z.Key.Category, totalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in genstatRpt)
                {
                    statdataPoints.Add(new StringDoubleDPViewModel(i.itemCat, (double)i.totalAmt));
                }

                ViewBag.statDataPoints = JsonConvert.SerializeObject(statdataPoints);

                #endregion

                #region Requests over time

                List<StringDoubleDPViewModel> timedataPoints = new List<StringDoubleDPViewModel>();

                var timeRpt = context.TransactionDetail.Where(x => x.StationeryRequest.Status == "Completed" && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    OrderBy(x => x.TransactionDate).
                    GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToArray().
                    Select(y => new { dateval = string.Format("{0} {1}", Enum.Parse(typeof(EnumMonth), y.Key.Month.ToString()), y.Key.Year), totalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

                foreach (var i in timeRpt)
                {
                    timedataPoints.Add(new StringDoubleDPViewModel(i.dateval, (double)i.totalAmt));
                }

                ViewBag.timedataPoints = JsonConvert.SerializeObject(timedataPoints);

                #endregion

            }

            if(module == "ChargeBack")
            {
                #region Charge back by DeptId
                List<StringDoubleDPViewModel> deptdataPoints = new List<StringDoubleDPViewModel>();
                var gendeptRpt = context.TransactionDetail.
                    Where(x => x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    GroupBy(y => new { y.Disbursement.DepartmentId }).
                    Select(z => new { DeptID = z.Key.DepartmentId, TotalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in gendeptRpt)
                {
                    deptdataPoints.Add(new StringDoubleDPViewModel(i.DeptID, (double)i.TotalAmt));
                }

                ViewBag.deptDataPoints = JsonConvert.SerializeObject(deptdataPoints);
                #endregion

                #region Charge back by Stationery Category

                List<StringDoubleDPViewModel> statdataPoints = new List<StringDoubleDPViewModel>();
                var genstatRpt = context.TransactionDetail.
                    Where(x => x.Disbursement.DepartmentId != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                        GroupBy(y => new { y.Stationery.Category }).
                        Select(z => new { itemCat = z.Key.Category, totalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in genstatRpt)
                {
                    statdataPoints.Add(new StringDoubleDPViewModel(i.itemCat, (double)i.totalAmt));
                }

                ViewBag.statDataPoints = JsonConvert.SerializeObject(statdataPoints);

                #endregion

                #region Charge back over time

                List<StringDoubleDPViewModel> timedataPoints = new List<StringDoubleDPViewModel>();

                var timeRpt = context.TransactionDetail.Where(x => x.Disbursement.DepartmentId != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    OrderBy(x => x.TransactionDate).
                    GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToArray().
                    Select(y => new { dateval = string.Format("{0} {1}", Enum.Parse(typeof(EnumMonth), y.Key.Month.ToString()), y.Key.Year), totalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

                foreach (var i in timeRpt)
                {
                    timedataPoints.Add(new StringDoubleDPViewModel(i.dateval, (double)i.totalAmt));
                }

                ViewBag.timedataPoints = JsonConvert.SerializeObject(timedataPoints);

                #endregion
            }

            if (module == "Purchases")
            {
                #region Purchases by SupplierID
                List<StringDoubleDPViewModel> deptdataPoints = new List<StringDoubleDPViewModel>();
                var gendeptRpt = context.TransactionDetail.
                    Where(x => x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    GroupBy(y => new { y.PurchaseOrder.SupplierId }).
                    Select(z => new { DeptID = z.Key.SupplierId, TotalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in gendeptRpt)
                {
                    deptdataPoints.Add(new StringDoubleDPViewModel(i.DeptID, (double)i.TotalAmt));
                }

                ViewBag.deptDataPoints = JsonConvert.SerializeObject(deptdataPoints);
                #endregion

                #region Purchases by Stationery Category

                List<StringDoubleDPViewModel> statdataPoints = new List<StringDoubleDPViewModel>();
                var genstatRpt = context.TransactionDetail.
                    Where(x => x.PurchaseOrder.Status =="Completed" && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                        GroupBy(y => new { y.Stationery.Category }).
                        Select(z => new { itemCat = z.Key.Category, totalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in genstatRpt)
                {
                    statdataPoints.Add(new StringDoubleDPViewModel(i.itemCat, (double)i.totalAmt));
                }

                ViewBag.statDataPoints = JsonConvert.SerializeObject(statdataPoints);

                #endregion

                #region Purchases over time

                List<StringDoubleDPViewModel> timedataPoints = new List<StringDoubleDPViewModel>();

                var timeRpt = context.TransactionDetail.Where(x => x.PurchaseOrder.Status == "Completed" && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    OrderBy(x => x.TransactionDate).
                    GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToArray().
                    Select(y => new { dateval = string.Format("{0} {1}", Enum.Parse(typeof(EnumMonth), y.Key.Month.ToString()), y.Key.Year), totalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

                foreach (var i in timeRpt)
                {
                    timedataPoints.Add(new StringDoubleDPViewModel(i.dateval, (double)i.totalAmt));
                }

                ViewBag.timedataPoints = JsonConvert.SerializeObject(timedataPoints);

                #endregion
            }

            if (module == "Retrieval")
            {
                #region Retrieval by Employee
                List<StringDoubleDPViewModel> deptdataPoints = new List<StringDoubleDPViewModel>();
                var gendeptRpt = context.TransactionDetail.
                    Where(x => x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    GroupBy(y => new { y.StationeryRetrieval.AspNetUsers.EmployeeName }).
                    Select(z => new { DeptID = z.Key.EmployeeName, TotalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in gendeptRpt)
                {
                    deptdataPoints.Add(new StringDoubleDPViewModel(i.DeptID, (double)i.TotalAmt));
                }

                ViewBag.deptDataPoints = JsonConvert.SerializeObject(deptdataPoints);
                #endregion

                #region Retrieval by Stationery Category

                List<StringDoubleDPViewModel> statdataPoints = new List<StringDoubleDPViewModel>();
                var genstatRpt = context.TransactionDetail.
                    Where(x => x.StationeryRetrieval.RetrievalId != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                        GroupBy(y => new { y.Stationery.Category }).
                        Select(z => new { itemCat = z.Key.Category, totalAmt = z.Sum(a => (a.Quantity * a.UnitPrice)) });

                foreach (var i in genstatRpt)
                {
                    statdataPoints.Add(new StringDoubleDPViewModel(i.itemCat, (double)i.totalAmt));
                }

                ViewBag.statDataPoints = JsonConvert.SerializeObject(statdataPoints);

                #endregion

                #region Retrieval over time

                List<StringDoubleDPViewModel> timedataPoints = new List<StringDoubleDPViewModel>();

                var timeRpt = context.TransactionDetail.Where(x => x.StationeryRetrieval.RetrievalId != null && x.TransactionDate >= fromDateTP && x.TransactionDate <= toDateTP).
                    OrderBy(x => x.TransactionDate).
                    GroupBy(x => new { x.TransactionDate.Year, x.TransactionDate.Month }).ToArray().
                    Select(y => new { dateval = string.Format("{0} {1}", Enum.Parse(typeof(EnumMonth), y.Key.Month.ToString()), y.Key.Year), totalAmt = y.Sum(z => (z.Quantity * z.UnitPrice)) });

                foreach (var i in timeRpt)
                {
                    timedataPoints.Add(new StringDoubleDPViewModel(i.dateval, (double)i.totalAmt));
                }

                ViewBag.timedataPoints = JsonConvert.SerializeObject(timedataPoints);
            }
                #endregion
                return View(grvm);
            
        }
    }

     

        #endregion
    }
