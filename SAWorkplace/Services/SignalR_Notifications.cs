using Microsoft.AspNetCore.SignalR;
using SAWorkplace.Models;
using System.Threading.Tasks;

namespace SAWorkplace.Services
{
    public class SignalR_Notifications : Hub
    {

        public async Task NotifyFeasibilityReviewAdded(int TicketNumber)
        {
            await Clients.All.SendAsync("FeasibilityReview_Added", new MessageModel() { TicketNumber = TicketNumber, MessageType = "New" });
        }
    }
}
