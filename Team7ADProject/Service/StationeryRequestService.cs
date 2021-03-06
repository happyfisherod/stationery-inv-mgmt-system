﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Team7ADProject.Entities;
using Team7ADProject.ViewModels;

namespace Team7ADProject.Service
{
    public class StationeryRequestService
    {
        #region Singleton Design Pattern

        private static readonly StationeryRequestService instance = new StationeryRequestService();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static StationeryRequestService()
        {
        }

        private StationeryRequestService()
        {
        }

        public static StationeryRequestService Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion


        public List<RequestByItemViewModel> GetListRequestByItem()
        {
            LogicDB context = new LogicDB();
            List<RequestByItemViewModel> model = new List<RequestByItemViewModel>();
            var requests = context.RequestByItemView.OrderBy(x => x.ItemId).ToList();
            var disbList = (from x in context.DisbByDept
                        select new SimpleDisbViewModel
                        {
                            DepartmentId = x.DepartmentId,
                            DepartmentName = x.DepartmentName,
                            ItemId = x.ItemId,
                            Description = x.Description,
                            Quantity = x.Quantity
                        }).ToList();

            foreach (var req in requests)
            {
                var item = model.Find(x => x.ItemId == req.ItemId);

                var disb = disbList.FirstOrDefault(x => x.DepartmentId == req.DepartmentId && x.ItemId == req.ItemId);

                if (item != null)
                {
                    BreakdownByDeptViewModel newModel;
                    if (disb == null)
                    {
                        newModel = new BreakdownByDeptViewModel
                        {
                            DepartmentId = req.DepartmentId,
                            DepartmentName = req.DepartmentName,
                            Quantity = (int)req.Quantity
                        };
                    }
                    else
                    {
                        newModel = new BreakdownByDeptViewModel
                        {
                            DepartmentId = req.DepartmentId,
                            DepartmentName = req.DepartmentName,
                            Quantity = ((int)req.Quantity - (int)disb.Quantity)
                        };
                        disb.Quantity = 0;
                    }
                  
                    if (newModel.Quantity > 0)
                        item.requestList.Add(newModel);
                }
                else
                {
                    RequestByItemViewModel requestByItemViewModel = new RequestByItemViewModel();
                    requestByItemViewModel.ItemId = req.ItemId;
                    requestByItemViewModel.Description = req.Description;
                    requestByItemViewModel.requestList = new List<BreakdownByDeptViewModel>();
                    BreakdownByDeptViewModel newModel;
                    if (disb == null)
                    {
                        newModel = new BreakdownByDeptViewModel
                        {
                            DepartmentId = req.DepartmentId,
                            DepartmentName = req.DepartmentName,
                            Quantity = (int)req.Quantity
                        };
                    }
                    else
                    {
                        newModel = new BreakdownByDeptViewModel
                        {
                            DepartmentId = req.DepartmentId,
                            DepartmentName = req.DepartmentName,
                            Quantity = ((int)req.Quantity - (int)disb.Quantity)
                        };
                        disb.Quantity = 0;
                    }
                    
                    if (newModel.Quantity > 0)
                    {
                        requestByItemViewModel.requestList.Add(newModel);
                        model.Add(requestByItemViewModel);
                    }
                }
            }
            return model;
        }

        public string GetUserEmail(string id)
        {
            LogicDB context = new LogicDB();
            return context.AspNetUsers.FirstOrDefault(x => x.Id == id).Email;
        }

        public string GetNewRetrievalId()
        {
            LogicDB context = new LogicDB();
            string rid;
            var ret = context.StationeryRetrieval.OrderByDescending(x => x.Date).OrderByDescending(x => x.RetrievalId).FirstOrDefault();
            if (ret.Date.Year == DateTime.Now.Year)
            {
                rid = "R" + DateTime.Now.Year.ToString() + "-" + (Convert.ToInt32(ret.RetrievalId.Substring(6, 4)) + 1).ToString("0000");
            }
            else
            {
                rid = "R" + DateTime.Now.Year.ToString() + "-" + "0001";
            }
            return rid;
        }

        //Convert breakdown of stationery by items to breakdown of stationery by dept
        public List<DisbursementByDeptViewModel> GenerateDisbursement(List<RequestByItemViewModel> model)
        {
            LogicDB context = new LogicDB();
            List<DisbursementByDeptViewModel> disbList = new List<DisbursementByDeptViewModel>();

            for (int i = 0; i < model.Count; i++)
            {
                for (int j = 0; j < model[i].requestList.Count; j++)
                {
                    var disb = disbList.Find(x => x.DepartmentId == model[i].requestList[j].DepartmentId);
                    if (disb != null)
                    {
                        var item = disb.requestList.Find(x => x.ItemId == model[i].ItemId);
                        if (item != null)
                        {
                            item.Quantity += model[i].requestList[j].Quantity;
                        }
                        else
                        {
                            if (model[i].requestList[j].RetrievedQty > 0)
                            {
                                BreakdownByItemViewModel breakdown = new BreakdownByItemViewModel();
                                breakdown.ItemId = model[i].ItemId;
                                breakdown.Description = model[i].Description;
                                breakdown.RetrievedQty = model[i].requestList[j].RetrievedQty;
                                disb.requestList.Add(breakdown);
                            }
                        }
                    }
                    else
                    {
                        if (model[i].requestList[j].RetrievedQty > 0)
                        {
                            DisbursementByDeptViewModel disbModel = new DisbursementByDeptViewModel();
                            disbModel.DepartmentId = model[i].requestList[j].DepartmentId;
                            disbModel.DepartmentName = model[i].requestList[j].DepartmentName;
                            BreakdownByItemViewModel breakdown = new BreakdownByItemViewModel();
                            breakdown.RetrievedQty = model[i].requestList[j].RetrievedQty;
                            breakdown.ItemId = model[i].ItemId;
                            breakdown.Description = model[i].Description;
                            disbModel.requestList = new List<BreakdownByItemViewModel>();
                            disbModel.requestList.Add(breakdown);
                            disbList.Add(disbModel);
                        }
                    }
                }
            }
            return disbList;
        }


        public bool SaveAndDisburse(List<RequestByItemViewModel> model, string userId)
        {
            LogicDB context = new LogicDB();
            using(var dbContextTransaction = context.Database.BeginTransaction())
            {
                try
                {
                    StationeryRetrieval retrieval = new StationeryRetrieval();
                    string rid = GetNewRetrievalId();
                    retrieval.RetrievalId = rid;
                    retrieval.RetrievedBy = userId;
                    retrieval.Date = DateTime.Now;

                    foreach (var sr in model)
                    {
                        int retQty = sr.requestList.Sum(x => x.RetrievedQty);
                        if (retQty > 0)
                        {
                            TransactionDetail detail = new TransactionDetail();
                            detail.ItemId = sr.ItemId;
                            detail.Quantity = retQty;
                            detail.TransactionDate = DateTime.Now;
                            detail.Remarks = "Retrieved";
                            detail.TransactionRef = rid;
                            retrieval.TransactionDetail.Add(detail);

                            //Less off from stationery

                            var item = context.Stationery.FirstOrDefault(x => x.ItemId == sr.ItemId);
                            if (item != null)
                            {
                                item.QuantityWarehouse -= retQty;
                                item.QuantityTransit += retQty;
                            }


                        }
                    }
                    context.StationeryRetrieval.Add(retrieval);
                    context.SaveChanges();

                    List<RequestByIdViewModel> requests = CreateDisbHelpers.GetRequestQuery(context).OrderBy(x => x.RequestId).ToList();
                    
                    List<DisbursementByDeptViewModel> disbList = GenerateDisbursement(model);

                    foreach (var dept in disbList)
                    {
                        string currentDeptId = dept.DepartmentId;
                        string disbNo = CreateDisbHelpers.GetNewDisbNo(context, currentDeptId);
                        string OTP;
                        do
                        {
                            Random rand = new Random();
                            OTP = rand.Next(10000).ToString("0000");
                        } while (context.Disbursement.Where(x => x.OTP == OTP).FirstOrDefault() != null);

                        Dictionary<string, int> tempDict = new Dictionary<string, int>();
                        foreach(var retItem in dept.requestList)
                        {
                            tempDict.Add(retItem.Description, retItem.RetrievedQty);
                        }

                        var deptReqList = requests.Where(x => x.DepartmentId == dept.DepartmentId).ToList();

                        foreach (var req in deptReqList)
                        {
                            bool isComplete = true;
                            foreach (var item in req.ItemList)
                            {
                                var disbItem = dept.requestList.FirstOrDefault(x => x.ItemId == item.ItemId);
                                if (disbItem != null)
                                {
                                    if (disbItem.RetrievedQty >= item.Quantity)
                                    {
                                        disbItem.RetrievedQty -= item.Quantity;
                                    }
                                    else
                                    {
                                        item.Quantity = disbItem.RetrievedQty;
                                        disbItem.RetrievedQty = 0; //0
                                        isComplete = false;
                                    }
                                }
                                else
                                {
                                    item.Quantity = 0;
                                    isComplete = false;
                                }
                            }

                            foreach (var item in req.ItemList)
                            {
                                if (item.Quantity > 0)
                                {
                                    Disbursement newDisb = new Disbursement();
                                    TransactionDetail newDetail = new TransactionDetail();
                                    newDisb.DisbursementId = CreateDisbHelpers.GetNewDisbId(context);
                                    newDisb.DisbursementNo = disbNo;
                                    newDisb.DepartmentId = currentDeptId;
                                    newDisb.DisbursedBy = userId;
                                    newDisb.Date = DateTime.Now;
                                    newDisb.RequestId = req.RequestId;
                                    newDisb.Status = "In Transit";
                                    newDisb.OTP = OTP;
                                    newDetail.ItemId = item.ItemId;
                                    newDetail.Quantity = item.Quantity;
                                    newDetail.TransactionRef = newDisb.DisbursementId;
                                    newDetail.TransactionDate = DateTime.Now;
                                    newDetail.UnitPrice = item.UnitPrice;
                                    newDetail.Remarks = "In Transit";
                                    newDisb.TransactionDetail.Add(newDetail);
                                    context.Disbursement.Add(newDisb);
                                    context.SaveChanges();
                                }
                            }

                            var currentReq = context.StationeryRequest.FirstOrDefault(x => x.RequestId == req.RequestId);
                            if (isComplete)
                            {
                                currentReq.Status = "Completed";
                            }
                            else
                            {
                                currentReq.Status = "Partially Fulfilled";
                            }
                        }
                        //Send email to dept rep
                        string email = context.Department.FirstOrDefault(x => x.DepartmentId == dept.DepartmentId).AspNetUsers1.Email;
                        string subject = string.Format("Stationeries ready for collection (Disbursement No: {0})", disbNo);

                        string content = string.Format("Disbursement No: {0}{1}Please quote the OTP below when collecting your stationeries.{2}OTP: {3}{4}Collection Point: {5}{6}Time: {7}{8}Item\t\t\t\t\t\t\tQuantity{9}", 
                            disbNo, Environment.NewLine, Environment.NewLine, OTP, Environment.NewLine, dept.CollectionDescription, Environment.NewLine, dept.CollectionTime, Environment.NewLine, Environment.NewLine);
                        foreach(KeyValuePair<string, int> entry in tempDict)
                        {
                            content += string.Format("{0}\t\t\t\t\t\t{1}{2}", entry.Key, entry.Value, Environment.NewLine);
                        }
                        Email.Send(email, subject, content);
                    }

                    dbContextTransaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    dbContextTransaction.Rollback();
                    return false;
                }
                
            }
            
        }

        public List<DisbursementByDeptViewModel> GetListDisb()
        {
            LogicDB context = new LogicDB();
            var query = (from x in context.Disbursement
                         join y in context.TransactionDetail
                         on x.DisbursementId equals y.TransactionRef
                         join z in context.Department
                         on x.DepartmentId equals z.DepartmentId
                         join c in context.CollectionPoint
                         on z.CollectionPointId equals c.CollectionPointId
                         join i in context.Stationery
                         on y.ItemId equals i.ItemId
                         where x.Status == "In Transit"
                         select new
                         {
                             x.DepartmentId,
                             z.DepartmentName,
                             c.CollectionPointId,
                             c.CollectionDescription,
                             y.ItemId,
                             i.Description,
                             y.Quantity
                         }).ToList();

            List<DisbursementByDeptViewModel> model = new List<DisbursementByDeptViewModel>();

            foreach(var item in query)
            {
                var deptExists = model.FirstOrDefault(x => x.DepartmentId == item.DepartmentId);
                if (deptExists != null)
                {
                    var itemExists = deptExists.requestList.FirstOrDefault(x => x.ItemId == item.ItemId);
                    if (itemExists != null)
                    {
                        itemExists.Quantity += item.Quantity;
                    }
                    else
                    {
                        BreakdownByItemViewModel newItemModel = new BreakdownByItemViewModel();
                        newItemModel.ItemId = item.ItemId;
                        newItemModel.Description = item.Description;
                        newItemModel.Quantity = item.Quantity;
                        deptExists.requestList.Add(newItemModel);
                    }
                }
                else
                {
                    DisbursementByDeptViewModel newDeptModel = new DisbursementByDeptViewModel();
                    newDeptModel.DepartmentId = item.DepartmentId;
                    newDeptModel.DepartmentName = item.DepartmentName;

                    List<BreakdownByItemViewModel> newItemList = new List<BreakdownByItemViewModel>();

                    BreakdownByItemViewModel newItemModel = new BreakdownByItemViewModel();
                    newItemModel.ItemId = item.ItemId;
                    newItemModel.Description = item.Description;
                    newItemModel.Quantity = item.Quantity;

                    newItemList.Add(newItemModel);
                    newDeptModel.requestList = newItemList;
                    model.Add(newDeptModel);
                }
            }
            return model;
        }
    }

    

    public static class CreateDisbHelpers
    {
        public static String GetNewDisbId(LogicDB context)
        {
            var disbursement = context.Disbursement.OrderByDescending(x => x.DisbursementId).First();
            string did = "DISB" + (Convert.ToInt32(disbursement.DisbursementId.Substring(4, 6)) + 1).ToString("000000");
            return did;
        }

        public static string GetNewDisbNo(LogicDB context, string DepartmentId)
        {
            var disbursement = context.Disbursement.OrderByDescending(x => x.DisbursementId).First();
            string dno = "D" + DepartmentId + (Convert.ToInt32(disbursement.DisbursementNo.Substring(5, 5)) + 1).ToString("00000");
            return dno;
        }

        public static List<RequestByIdViewModel> GetRequestQuery(LogicDB context)
        {
            List<SimpleRequestViewModel> requests = (from x in context.RequestByReqIdView
                                                select new SimpleRequestViewModel
                                                {
                                                    RequestId = x.RequestId,
                                                    DepartmentId = x.DepartmentId,
                                                    ItemId = x.ItemId,
                                                    UnitPrice = x.UnitPrice,
                                                    Quantity = x.Quantity
                                                }).ToList();

            var offsets = (from x in context.Disbursement
                            join d in context.Department
                            on x.DepartmentId equals d.DepartmentId
                            join y in context.TransactionDetail
                            on x.DisbursementId equals y.TransactionRef
                            join i in context.Stationery
                            on y.ItemId equals i.ItemId
                            join z in context.StationeryRequest
                            on x.RequestId equals z.RequestId
                            where z.Status == "Partially Fulfilled"
                            select new SimpleDisbViewModel
                            {
                                RequestId = x.RequestId,
                                DepartmentId = x.DepartmentId,
                                DepartmentName = d.DepartmentName,
                                ItemId = y.ItemId,
                                Description = i.Description,
                                Quantity = y.Quantity
                            }).ToList();

            foreach(var req in requests)
            {
                var disb = offsets.FirstOrDefault(x => x.RequestId == req.RequestId && x.ItemId == req.ItemId);
                if (disb != null)
                {
                    req.Quantity -= (int)disb.Quantity;
                }
            }

            List<RequestByIdViewModel> model = new List<RequestByIdViewModel>();

            foreach (var item in requests)
            {
                var exist = model.FirstOrDefault(x => x.RequestId == item.RequestId && x.DepartmentId == item.DepartmentId);
                if (exist != null)
                {
                    DeptAndItemViewModel newVM = new DeptAndItemViewModel();

                    newVM.ItemId = item.ItemId;
                    newVM.UnitPrice = item.UnitPrice;
                    newVM.Quantity = item.Quantity;

                    exist.ItemList.Add(newVM);
                }
                else
                {
                    RequestByIdViewModel newRVM = new RequestByIdViewModel();
                    newRVM.RequestId = item.RequestId;
                    newRVM.DepartmentId = item.DepartmentId;

                    List<DeptAndItemViewModel> newList = new List<DeptAndItemViewModel>();
                    DeptAndItemViewModel newVM = new DeptAndItemViewModel();
                    newVM.ItemId = item.ItemId;
                    newVM.UnitPrice = item.UnitPrice;
                    newVM.Quantity = item.Quantity;
                    newList.Add(newVM);

                    newRVM.ItemList = newList;
                    model.Add(newRVM);
                }
            }


            return model;
        }
    }
}
