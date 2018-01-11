using Atlas.Core.Logic.DTOs;
using PinPayObjects.BaseObjects.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.WCF.TicketsService.Responses
{
    public class GetTicketInfo:BaseResponse
    {
        public GetTicketInfo(string pRequestId):base (pRequestId)
        {

        }
        public FullTicketInfo Ticket { get; set; }
    }
}