using Atlas.Core.Logic.DTOs;
using PinPayObjects.BaseObjects.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.WCF.TicketsService.Requests
{
    public class CreateTicketRequest:BaseRequest
    {
        public TicketObject Ticket { get; set; }
    }
}