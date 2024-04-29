using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Linq;
using Zezoprice.Dtos;
using Zezoprice.Models;
using Zezoprice.Services;

namespace Zezoprice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly rsc_v2Context _context;
        private readonly IServices services;

        public RequestController(rsc_v2Context context, IServices services)
        {
            _context = context;
            this.services = services;
        }


        [HttpGet]
        [Route("LiveAllData")]
        public ActionResult<AfterDto> AllData(int pagenumber,string? requestnumber,
             int? requesttype, int? usagetype, DateTime? addeddatefilter)
        {




            List<DataDto> Data = new List<DataDto>();
            List<decimal> Areas = new List<decimal>();
            decimal Price = 0;
            var query = from f in _context.FieldDataV2s
                        join u in _context.UsageStatuses on f.Usage equals u.Id
                        join l in _context.LkupUnittypes on f.SuvSubtype equals l.Id into suvTypes
                        from suvType in suvTypes.DefaultIfEmpty()

                        join a in _context.Assignements on f.Requestnumber equals a.Requestnumber into assignments
                        from assignment in assignments.DefaultIfEmpty()
                        join r in _context.Requests on f.Requestnumber equals r.Requestnumber into requests
                        from request in requests.DefaultIfEmpty()

                        join bl in _context.LkupUnittypes on request.Unittype equals bl.Id into unitTypes
                        from unitType in unitTypes.DefaultIfEmpty()
                        join bu in _context.UsageStatuses on request.Subunittype equals bu.Id into usageStatuses
                        from usageStatus in usageStatuses.DefaultIfEmpty()

                        join p in _context.Requestpricedifferences on (int?)request.Id equals p.Requestid into priceDifferences
                        from priceDifference in priceDifferences.DefaultIfEmpty()

                        join so in _context.Addresses on (int?)request.Id equals so.Requestid into address
                        from Address in address.DefaultIfEmpty()

                        join o in _context.Regions on Address.Regionid equals o.Id into regions
                        from region in regions.DefaultIfEmpty()

                        join gov in _context.Governorates on (region != null ? region.Governorateid : (int?)null) equals gov.Id into governorates
                        from governorate in governorates.DefaultIfEmpty()




                        where assignment.PrintStatus == 1 &&
                               (addeddatefilter == null || (assignment.PrintDate.HasValue && assignment.PrintDate.Value == DateOnly.FromDateTime(addeddatefilter.Value)))
                                 && (requesttype == null || f.SuvSubtype == requesttype) && (usagetype == null || f.Usage == usagetype)



                        select new
                        {
                            requestnumber = f.Requestnumber,
                            total_area_building = f.TotalAreaBuilding,
                            area_apartment = (f.AreaApartment ?? 0) + (f.AreaApartment2 ?? 0) + (f.AreaApartment3 ?? 0) + (f.AreaApartment4 ?? 0),
                            area_buildings = f.AreaBuildings,
                            usage = f.Usage,
                            name = u.Name,
                            suv_subtype = f.SuvSubtype,
                            Lkb_name = suvType.Name,
                            print_status = assignment.PrintStatus,
                            Price = (request != null ? request.Price : 0) +
                                    ((priceDifference != null && priceDifference.Orderstatus == 1) ? priceDifference.Price : 0),
                            area = request.Area,
                            area_g=f.AreaG,
                            Unittype = unitType.Name,
                            UsageStatus = usageStatus.Name,
                            Addeddate = request.Addeddate,
                            governorateid = governorate.Id != null ? governorate.Id : 0
                        };


            if (!string.IsNullOrEmpty(requestnumber))
            {
                var listfive = requestnumber.Length >= 6 ? requestnumber.Substring(requestnumber.Length - 6) : requestnumber;
                query = query.Where(item => item.requestnumber.EndsWith(listfive));
            }

            var after = query.Skip((pagenumber - 1) * 15).Take(15).ToList();
            var totalcount = query.Count();

            foreach (var item in after)  
            {
                #region inCase2Area

                if (item.suv_subtype == 5 || item.suv_subtype ==13 || item.suv_subtype == 15)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)

                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = item.suv_subtype == 13 ? 13 : 3;
                           
                            if (item.area_g == null)
                                areaDto.area_gardens = 0;
                            else
                                areaDto.area_gardens = (decimal)item.area_g;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;

                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = item.suv_subtype==13 ? 13:3,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid

                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();

                            areaDto.Type = 1;
                            if (item.area_g == null)
                                areaDto.area_gardens = 0;
                            else
                                areaDto.area_gardens = (decimal)item.area_g;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid,
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };

                    Data.Add(dataDto);
                }


                else if (item.suv_subtype == 24)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)
                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 0;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 0,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid,
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }


                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 1;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };

                    Data.Add(dataDto);
                }
                #endregion
                #region inCaseOneArea
                else
                {
                    TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                    areaDto.Type = (int)item.suv_subtype;

                    if (item.total_area_building == null)
                        areaDto.totalAreaBulding = 0;
                    else
                        areaDto.totalAreaBulding = (decimal)item.total_area_building;

                    if (item.area_buildings == null)
                        areaDto.areaBulding = 0;
                    else
                        areaDto.areaBulding = (decimal)item.area_buildings;

                    if (item.area_apartment == null)
                        areaDto.areaApartent = 0;
                    else
                        areaDto.areaApartent = (decimal)item.area_apartment;
                    List<decimal> area = services.GetArea(areaDto);
                    CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                    {
                        TypeId = (int)item.suv_subtype,
                        usageTypeId = (int)item.usage,
                        Area = area,
                        goverid = item.governorateid,
                    };
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = area,
                        Price = services.CalculatePriceAfter(calculatePrice, item.Addeddate)
                    };
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor 
                    };

                    Data.Add(dataDto);
                }
                #endregion



            }

            

            return Ok(new { totalcount, Data});


        }



        [HttpGet]
        [Route("ValidCertificate")]
        public ActionResult<AfterDto> ValidCertificate(int pagenumber, string? requestnumber,
             int? requesttype, int? usagetype, DateTime? addeddatefilter)
        {




            List<DataDto> Data = new List<DataDto>();
            List<decimal> Areas = new List<decimal>();
            decimal Price = 0;
            var query = from f in _context.FieldDataV2s
                        join u in _context.UsageStatuses on f.Usage equals u.Id
                        join l in _context.LkupUnittypes on f.SuvSubtype equals l.Id into suvTypes
                        from suvType in suvTypes.DefaultIfEmpty()

                        join a in _context.Assignements on f.Requestnumber equals a.Requestnumber into assignments
                        from assignment in assignments.DefaultIfEmpty()
                        join r in _context.Requests on f.Requestnumber equals r.Requestnumber into requests
                        from request in requests.DefaultIfEmpty()

                        join bl in _context.LkupUnittypes on request.Unittype equals bl.Id into unitTypes
                        from unitType in unitTypes.DefaultIfEmpty()
                        join bu in _context.UsageStatuses on request.Subunittype equals bu.Id into usageStatuses
                        from usageStatus in usageStatuses.DefaultIfEmpty()

                        join p in _context.Requestpricedifferences on (int?)request.Id equals p.Requestid into priceDifferences
                        from priceDifference in priceDifferences.DefaultIfEmpty()

                        join so in _context.Addresses on (int?)request.Id equals so.Requestid into address
                        from Address in address.DefaultIfEmpty()

                        join o in _context.Regions on Address.Regionid equals o.Id into regions
                        from region in regions.DefaultIfEmpty()

                        join gov in _context.Governorates on (region != null ? region.Governorateid : (int?)null) equals gov.Id into governorates
                        from governorate in governorates.DefaultIfEmpty()




                        where assignment.PrintStatus == 1 &&
                               (addeddatefilter == null || (assignment.PrintDate.HasValue && assignment.PrintDate.Value == DateOnly.FromDateTime(addeddatefilter.Value)))
                                 && (requesttype == null || f.SuvSubtype == requesttype) && (usagetype == null || f.Usage == usagetype)



                        select new
                        {
                            requestnumber = f.Requestnumber,
                            total_area_building = f.TotalAreaBuilding,
                            area_apartment = (f.AreaApartment ?? 0) + (f.AreaApartment2 ?? 0) + (f.AreaApartment3 ?? 0) + (f.AreaApartment4 ?? 0),
                            area_buildings = f.AreaBuildings,
                            usage = f.Usage,
                            name = u.Name,
                            suv_subtype = f.SuvSubtype,
                            Lkb_name = suvType.Name,
                            print_status = assignment.PrintStatus,
                            Price = (request != null ? request.Price : 0) +
                                    ((priceDifference != null && priceDifference.Orderstatus == 1) ? priceDifference.Price : 0),
                            area = request.Area,
                            area_g = f.AreaG,
                            Unittype = unitType.Name,
                            UsageStatus = usageStatus.Name,
                            Addeddate = request.Addeddate,
                            governorateid = governorate.Id != null ? governorate.Id : 0
                        };


            if (!string.IsNullOrEmpty(requestnumber))
            {
                var listfive = requestnumber.Length >= 6 ? requestnumber.Substring(requestnumber.Length - 6) : requestnumber;
                query = query.Where(item => item.requestnumber.EndsWith(listfive));
            }

            var after = query.Skip((pagenumber - 1) * 100).Take(100).ToList();


            foreach (var item in after)
            {
                #region inCase2Area

                if (item.suv_subtype == 5 || item.suv_subtype == 13 || item.suv_subtype == 15)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)

                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = item.suv_subtype == 13 ? 13 : 3;

                            if (item.area_g == null)
                                areaDto.area_gardens = 0;
                            else
                                areaDto.area_gardens = (decimal)item.area_g;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;

                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = item.suv_subtype == 13 ? 13 : 3,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid

                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();

                            areaDto.Type = 1;
                            if (item.area_g == null)
                                areaDto.area_gardens = 0;
                            else
                                areaDto.area_gardens = (decimal)item.area_g;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid,
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };


                    if (dataDto.priceDefernce <= 0)
                        Data.Add(dataDto);

                }


                else if (item.suv_subtype == 24)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)
                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 0;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 0,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid,
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }


                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 1;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };
                    if(dataDto.priceDefernce <=0 )
                        Data.Add(dataDto);

                }
                #endregion
                #region inCaseOneArea
                else

                {
                    TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                    areaDto.Type = (int)item.suv_subtype;

                    if (item.total_area_building == null)
                        areaDto.totalAreaBulding = 0;
                    else
                        areaDto.totalAreaBulding = (decimal)item.total_area_building;

                    if (item.area_buildings == null)
                        areaDto.areaBulding = 0;
                    else
                        areaDto.areaBulding = (decimal)item.area_buildings;

                    if (item.area_apartment == null)
                        areaDto.areaApartent = 0;
                    else
                        areaDto.areaApartent = (decimal)item.area_apartment;
                    List<decimal> area = services.GetArea(areaDto);
                    CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                    {
                        TypeId = (int)item.suv_subtype,
                        usageTypeId = (int)item.usage,
                        Area = area,
                        goverid = item.governorateid,
                    };
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = area,
                        Price = services.CalculatePriceAfter(calculatePrice, item.Addeddate)
                    };
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };
                   

                    if(dataDto.priceDefernce <= 0)
                        Data.Add(dataDto);

                }
                #endregion



            }

          

           
           
            var totalcount = Data.Count(dto => dto.priceDefernce <= 0);

            return Ok(new { Count = totalcount,Data = Data.Take(15) });


        }

        [HttpGet]
        [Route("inValidCertificate")]
        public ActionResult<AfterDto> inValidCertificate (int pagenumber, string? requestnumber,
             int? requesttype, int? usagetype, DateTime? addeddatefilter)
        {




            List<DataDto> Data = new List<DataDto>();
            List<decimal> Areas = new List<decimal>();
            decimal Price = 0;
            var query = from f in _context.FieldDataV2s
                        join u in _context.UsageStatuses on f.Usage equals u.Id
                        join l in _context.LkupUnittypes on f.SuvSubtype equals l.Id into suvTypes
                        from suvType in suvTypes.DefaultIfEmpty()

                        join a in _context.Assignements on f.Requestnumber equals a.Requestnumber into assignments
                        from assignment in assignments.DefaultIfEmpty()
                        join r in _context.Requests on f.Requestnumber equals r.Requestnumber into requests
                        from request in requests.DefaultIfEmpty()

                        join bl in _context.LkupUnittypes on request.Unittype equals bl.Id into unitTypes
                        from unitType in unitTypes.DefaultIfEmpty()
                        join bu in _context.UsageStatuses on request.Subunittype equals bu.Id into usageStatuses
                        from usageStatus in usageStatuses.DefaultIfEmpty()

                        join p in _context.Requestpricedifferences on (int?)request.Id equals p.Requestid into priceDifferences
                        from priceDifference in priceDifferences.DefaultIfEmpty()

                        join so in _context.Addresses on (int?)request.Id equals so.Requestid into address
                        from Address in address.DefaultIfEmpty()

                        join o in _context.Regions on Address.Regionid equals o.Id into regions
                        from region in regions.DefaultIfEmpty()

                        join gov in _context.Governorates on (region != null ? region.Governorateid : (int?)null) equals gov.Id into governorates
                        from governorate in governorates.DefaultIfEmpty()




                        where assignment.PrintStatus == 1 &&
                               (addeddatefilter == null || (assignment.PrintDate.HasValue && assignment.PrintDate.Value == DateOnly.FromDateTime(addeddatefilter.Value)))
                                 && (requesttype == null || f.SuvSubtype == requesttype) && (usagetype == null || f.Usage == usagetype)



                        select new
                        {
                            requestnumber = f.Requestnumber,
                            total_area_building = f.TotalAreaBuilding,
                            area_apartment = (f.AreaApartment ?? 0) + (f.AreaApartment2 ?? 0) + (f.AreaApartment3 ?? 0) + (f.AreaApartment4 ?? 0),
                            area_buildings = f.AreaBuildings,
                            usage = f.Usage,
                            name = u.Name,
                            suv_subtype = f.SuvSubtype,
                            Lkb_name = suvType.Name,
                            print_status = assignment.PrintStatus,
                            Price = (request != null ? request.Price : 0) +
                                    ((priceDifference != null && priceDifference.Orderstatus == 1) ? priceDifference.Price : 0),
                            area = request.Area,
                            area_g = f.AreaG,
                            Unittype = unitType.Name,
                            UsageStatus = usageStatus.Name,
                            Addeddate = request.Addeddate,
                            governorateid = governorate.Id != null ? governorate.Id : 0
                        };


            if (!string.IsNullOrEmpty(requestnumber))
            {
                var listfive = requestnumber.Length >= 6 ? requestnumber.Substring(requestnumber.Length - 6) : requestnumber;
                query = query.Where(item => item.requestnumber.EndsWith(listfive));
            }

            var after = query.Skip((pagenumber - 1) * 1000).Take(1000).ToList();


            foreach (var item in after)
            {
                #region inCase2Area

                if (item.suv_subtype == 5 || item.suv_subtype == 13 || item.suv_subtype == 15)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)

                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = item.suv_subtype == 13 ? 13 : 3;

                            if (item.area_g == null)
                                areaDto.area_gardens = 0;
                            else
                                areaDto.area_gardens = (decimal)item.area_g;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;

                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = item.suv_subtype == 13 ? 13 : 3,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid

                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();

                            areaDto.Type = 1;
                            if (item.area_g == null)
                                areaDto.area_gardens = 0;
                            else
                                areaDto.area_gardens = (decimal)item.area_g;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid,
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };

                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };


                    if (dataDto.priceDefernce > 0)
                        Data.Add(dataDto);

                }


                else if (item.suv_subtype == 24)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)
                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 0;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 0,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid,
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }


                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 1;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };
                   
                    if (dataDto.priceDefernce > 0)
                        Data.Add(dataDto);

                }
                #endregion
                #region inCaseOneArea
                else

                {
                    TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                    areaDto.Type = (int)item.suv_subtype;

                    if (item.total_area_building == null)
                        areaDto.totalAreaBulding = 0;
                    else
                        areaDto.totalAreaBulding = (decimal)item.total_area_building;

                    if (item.area_buildings == null)
                        areaDto.areaBulding = 0;
                    else
                        areaDto.areaBulding = (decimal)item.area_buildings;

                    if (item.area_apartment == null)
                        areaDto.areaApartent = 0;
                    else
                        areaDto.areaApartent = (decimal)item.area_apartment;
                    List<decimal> area = services.GetArea(areaDto);
                    CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                    {
                        TypeId = (int)item.suv_subtype,
                        usageTypeId = (int)item.usage,
                        Area = area,
                        goverid = item.governorateid,
                    };
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = area,
                        Price = services.CalculatePriceAfter(calculatePrice, item.Addeddate)
                    };
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor 
                    };

                    if (dataDto.priceDefernce > 0)
                        Data.Add(dataDto);

                }
                #endregion



            }
           
          var priceDifferenceCount = Data.Count(dto => dto.priceDefernce > 0);

            //while (Data.Count < 15)
            //{
            //    Data.Add(new DataDto());
            //}

            //var filter = Data.Count();

            return Ok(new { Count = priceDifferenceCount, Data = Data.Take(15) });

        }

        [HttpPost("FromExcel")]
        public ActionResult<List<DataDto>> AllDataFromExcel(IFormFile excelFile, int pageNumber)
        {
            var list = new List<string>();

            using (var stream = new MemoryStream())
            {
                excelFile.CopyTo(stream);
                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count > 0)
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                        if (worksheet != null)
                        {
                            var rowCount = worksheet.Dimension?.Rows ?? 0;

                            for (int row = 2; row <= rowCount; row++)
                            {
                                var cellValue = worksheet.Cells[row, 1]?.Value?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(cellValue))
                                {
                                    list.Add(cellValue);
                                }
                            }
                        }
                    }
                }
            }

            List<DataDto> data = GetFilteredAllData(list, pageNumber);
            return Ok(data);
        }

        private List<DataDto> GetFilteredAllData(List<string> requestNumbers, int pagenumber)
        {
            List<DataDto> Data = new List<DataDto>();
            List<decimal> Areas = new List<decimal>();
            decimal Price = 0;
            var query = from f in _context.FieldDataV2s
                        join u in _context.UsageStatuses on f.Usage equals u.Id
                        join l in _context.LkupUnittypes on f.SuvSubtype equals l.Id into suvTypes
                        from suvType in suvTypes.DefaultIfEmpty()
                        join a in _context.Assignements on f.Requestnumber equals a.Requestnumber into assignments
                        from assignment in assignments.DefaultIfEmpty()
                        join r in _context.Requests on f.Requestnumber equals r.Requestnumber into requests
                        from request in requests.DefaultIfEmpty()
                        join bl in _context.LkupUnittypes on request.Unittype equals bl.Id into unitTypes
                        from unitType in unitTypes.DefaultIfEmpty()
                        join bu in _context.UsageStatuses on request.Subunittype equals bu.Id into usageStatuses

                        from usageStatus in usageStatuses.DefaultIfEmpty()
                        join p in _context.Requestpricedifferences on request.Id equals p.Requestid.GetValueOrDefault() into priceDifferences
                        from priceDifference in priceDifferences.DefaultIfEmpty()
                        join so in _context.Shippingorders on request.Id equals so.Requestid.GetValueOrDefault() into shippingOrders
                        from shippingOrder in shippingOrders.DefaultIfEmpty()


                        join reg in _context.Regions on shippingOrder.Regionid equals reg.Id into regions
                        from region in regions.DefaultIfEmpty()
                        join gov in _context.Governorates on (region != null ? region.Governorateid : (int?)null) equals gov.Id into governorates
                        from governorate in governorates.DefaultIfEmpty()


                        where assignment.PrintStatus == 1
                        select new
                        {
                            requestnumber = f.Requestnumber,
                            total_area_building = f.TotalAreaBuilding,
                            area_apartment = (f.AreaApartment ?? 0) + (f.AreaApartment2 ?? 0) + (f.AreaApartment3 ?? 0) + (f.AreaApartment4 ?? 0),
                            area_buildings = f.AreaBuildings,
                            usage = f.Usage,
                            name = u.Name,
                            suv_subtype = f.SuvSubtype,
                            Lkb_name = suvType.Name,
                            print_status = assignment.PrintStatus,
                            Price = request.Price + (priceDifference != null ? priceDifference.Price : 0),
                            area = request.Area,
                            Unittype = unitType.Name,
                            UsageStatus = usageStatus.Name,
                            Addeddate = request.Addeddate,
                            governorateid = governorate.Id
                        };



            query = query.Where(item => requestNumbers.Contains(item.requestnumber));



            var after = query.Skip((pagenumber - 1) * 1000).Take(1000).ToList();

            foreach (var item in after)
            {
                #region inCase2Area
                if (item.suv_subtype == 5)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)
                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 3;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 3,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 1;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };

                    Data.Add(dataDto);
                }
                else if (item.suv_subtype == 24)
                {
                    Price = 0;
                    for (int count = 0; count < 2; count++)
                    {
                        if (count == 0)
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 0;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 0,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                        else
                        {
                            TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                            areaDto.Type = 1;
                            if (item.total_area_building == null)
                                areaDto.totalAreaBulding = 0;
                            else
                                areaDto.totalAreaBulding = (decimal)item.total_area_building;

                            if (item.area_buildings == null)
                                areaDto.areaBulding = 0;
                            else
                                areaDto.areaBulding = (decimal)item.area_buildings;

                            if (item.area_apartment == null)
                                areaDto.areaApartent = 0;
                            else
                                areaDto.areaApartent = (decimal)item.area_apartment;
                            List<decimal> area = services.GetArea(areaDto);
                            Areas.Add(area[0]);
                            CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                            {
                                TypeId = 1,
                                usageTypeId = (int)item.usage,
                                Area = area,
                                goverid = item.governorateid
                            };
                            Price += services.CalculatePriceAfter(calculatePrice, item.Addeddate);
                        }
                    }
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = Areas,

                    };
                    afterDto.Price = Price;
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };

                    Data.Add(dataDto);
                }
                #endregion
                #region inCaseOneArea
                else
                {
                    TypesToGetAreaDto areaDto = new TypesToGetAreaDto();
                    areaDto.Type = (int)item.suv_subtype;
                    if (item.total_area_building == null)
                        areaDto.totalAreaBulding = 0;
                    else
                        areaDto.totalAreaBulding = (decimal)item.total_area_building;

                    if (item.area_buildings == null)
                        areaDto.areaBulding = 0;
                    else
                        areaDto.areaBulding = (decimal)item.area_buildings;

                    if (item.area_apartment == null)
                        areaDto.areaApartent = 0;
                    else
                        areaDto.areaApartent = (decimal)item.area_apartment;
                    List<decimal> area = services.GetArea(areaDto);
                    CalculatePriceAfterDto calculatePrice = new CalculatePriceAfterDto
                    {
                        TypeId = (int)item.suv_subtype,
                        usageTypeId = (int)item.usage,
                        Area = area,
                        goverid = item.governorateid
                    };
                    AfterDto afterDto = new AfterDto
                    {
                        RequestNumber = item.requestnumber,
                        RequestType = item.Lkb_name,
                        UsageType = item.name,
                        Area = area,
                        Price = services.CalculatePriceAfter(calculatePrice, item.Addeddate)
                    };
                    BeforDto beforDto = new BeforDto
                    {
                        PriceBfor = (int)item.Price,
                        Areabefor = (decimal)item.area,
                        TypeBefor = item.Unittype,
                        UsageTypeBefor = item.UsageStatus
                    };

                    DataDto dataDto = new DataDto
                    {
                        Before = beforDto,
                        After = afterDto,
                        priceDefernce = afterDto.Price - beforDto.PriceBfor
                    };

                    Data.Add(dataDto);

                }

                #endregion



            }

            return Data;
        }
    }
}



