using Atlas.Core.Logic.DTOs;
using PinPayObjects.BaseObjects.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.WCF.TicketsService.Responses
{
    public class GetTicketsResponse:BaseResponse
    {
        public GetTicketsResponse(string pRequestId):base(pRequestId)
        {
            Tickets = new List<TicketObject>();
        }
        public List<TicketObject> Tickets { get; set; }
    }
}