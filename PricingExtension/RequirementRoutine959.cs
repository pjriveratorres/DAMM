
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Pricing.Common.Interfaces.Core.ContainerObjects;
using SAPCD.DSD.MobileClient.Pricing.Common.Interfaces.Core.DocumentObjects;
using SAPCD.DSD.MobileClient.Pricing.Common.Interfaces.Core.Routines;

namespace ZZ.MobilePricing.UserExits
{
    public class Requirement959 : IRequirement, ICanLog
    {
        public bool CheckRequirement(IPricingInputDocumentItem inputDocumentItem,
                                     IPricingInputDocument inputDocument,
                                     ICommunicationWorkStructure communicationWorkStructure)
        {
            // Get Deal Condition Type
            string conditionType = inputDocumentItem.GetStringAttribute("ZZTYPE");

            this.LogDebug("* ZZ.MobilePricing.UserExit:CheckRequirement959-> " + conditionType + "=" + (conditionType.Equals("YPRC") || conditionType.Equals("YPRN")));

            if (conditionType.Equals("YPRC") || conditionType.Equals("YPRN"))
            {
                 return false;
            }

            return true;
        }
    }
}