
using SAPCD.DSD.MobileClient.Pricing.Common.Interfaces.Core.ContainerObjects;
using SAPCD.DSD.MobileClient.Pricing.Common.Interfaces.Core.DocumentObjects;
using SAPCD.DSD.MobileClient.Pricing.Common.Interfaces.Core.Routines;

namespace ZZ.MobilePricing.UserExits
{
    public class Requirement959 : IRequirement
    {
        public bool CheckRequirement(IPricingInputDocumentItem inputDocumentItem,
                                     IPricingInputDocument inputDocument,
                                     ICommunicationWorkStructure communicationWorkStructure)
        {
            //this.LogTrace("*** ZZ.MobilePricing.UserExits:RequirementRoutine959 - start");

            foreach (IManualConditionRecord condRecord in inputDocumentItem.ManualConditionRecords)
            {
                //this.LogTrace(condRecord.CondTypeName);

                if (condRecord.CondTypeName.Equals("YPRC") || condRecord.CondTypeName.Equals("YPRN"))
                {
                    return false;
                }
            }

            return true;
        }
    }
}