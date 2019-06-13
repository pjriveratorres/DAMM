using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAPCD.DSD.MobileClient.Business.Entities.DealCondition;
using SAPCD.DSD.MobileClient.Business.Entities.Visit;
using SAPCD.DSD.MobileClient.Business.Interfaces.ApplicationCustomizing;
using SAPCD.DSD.MobileClient.Business.Managers;
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Data.Interfaces;
using SAPCD.DSD.MobileClient.Extensions.BusinessObjects;

namespace Capgemini.DSD.DealConditions
{
    [ApplicationExtension]
    [Singleton(true)]
    public class ZDealConditionManager : DealConditionManager, ICanLog
    {
        public ZDealConditionManager(IDealConditionsDAL dealConditionDAL, IMaterialDAL materialDAL, ICustomizing customizing) :
            base(dealConditionDAL, materialDAL, customizing)
        {
            this.LogDebug("*ZDealConditionManager:Const.()");
        }

        public async override Task<IList<DealConditionEntity>> FindDealConditionsByCustomerAsync(VisitEntity visit, string paymentTerm, DateTime documentDate)
        {
            this.LogDebug("*ZDealConditionManager:FindDealConditionsByCustomerAsync()");

            IList <DealConditionEntity> dealContionList = await base.FindDealConditionsByCustomerAsync(visit, paymentTerm, documentDate);

            return (IList<DealConditionEntity>)dealContionList.OrderBy(dc => dc.CheckedActiveDC);
        }
    }
}
