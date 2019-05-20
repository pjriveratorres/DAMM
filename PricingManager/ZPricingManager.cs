using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAPCD.DSD.MobileClient.Business;
using SAPCD.DSD.MobileClient.Business.Components;
using SAPCD.DSD.MobileClient.Business.Entities.Common;
using SAPCD.DSD.MobileClient.Business.Entities.Delivery;
using SAPCD.DSD.MobileClient.Business.Entities.Document;
using SAPCD.DSD.MobileClient.Business.Entities.Order;
using SAPCD.DSD.MobileClient.Business.Entities.Visit;
using SAPCD.DSD.MobileClient.Business.Interfaces;
using SAPCD.DSD.MobileClient.Business.Interfaces.Managers;
using SAPCD.DSD.MobileClient.Business.Managers;
using SAPCD.DSD.MobileClient.Business.Objects;
using SAPCD.DSD.MobileClient.Business.Objects.Common;
using SAPCD.DSD.MobileClient.Common;
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Extensions.BusinessObjects;
using SAPCD.DSD.MobileClient.Pricing.Interfaces.Core.DocumentObjects;

namespace Capgemini.DSD.Pricing
{
    [ApplicationExtension]
    public class ZPricingManager : PricingManager, ICanLog
    {
        /*
        // this method is used for loading data asynchronously
        public async override Task InitializeAsync()
        {
            // load data from base InvoiceReport
            await base.InitializeAsync();
        }
        */

        /* Add header ttribute 
        public async override Task<IPricingInputDocumentBase> PrepareInputDocumentAsync(PricingParameters pricingParameters) 
        {
            var inputDocument = await base.PrepareInputDocumentAsync(pricingParameters);

            inputDocument
            return inputDocument;
        }
        */

        /*
        public override void FillInputDocumentItem2(IPricingInputDocumentBase inputDocument, 
                                                   IDocumentItemEntity documentItem, 
                                                   ActivityEntity activity, 
                                                   CustomizingSalesAreaBO customizingSalesArea, 
                                                   IList<MaterialBO> materials, 
                                                   IList<MaterialAltUomBO> materialAltUoms, 
                                                   IList<MaterialSalesOrgBO> materialSalesOrgs, 
                                                   IList<MaterialTaxBO> materialTaxes, 
                                                   CustomerSalesAreaBO customerSalesArea,
                                                   CustomerBO customer, 
                                                   bool isRunningOnIsoCode, 
                                                   IList<DocumentCondBO> conditionsForItems, 
                                                   IDictionary<string, string> materialCampaigns, 
                                                   IDictionary<string, string> physicalUnitDictionary, 
                                                   IDictionary<string, CustomizingBO> reasonCodesCustomizingDictionary)

        {
            base.FillInputDocumentItem(inputDocument, documentItem, activity, customizingSalesArea, materials,
                                                   materialAltUoms, materialSalesOrgs, materialTaxes, customerSalesArea,
                                                   customer, isRunningOnIsoCode, conditionsForItems, materialCampaigns,
                                                   physicalUnitDictionary, reasonCodesCustomizingDictionary);


            this.LogTrace("*** ZPricingManager:FillInputDocumentItem - start");

            ((IPricingInputDocumentItemBase)documentItem).AddAttribute("ZZPRODH4", "00RF40");
            ((IPricingInputDocumentItemBase)documentItem).AddAttribute("ZZMVGR1_P", "SR");
            ((IPricingInputDocumentItemBase)documentItem).AddAttribute("ZZMVGR2_P", "16");
            ((IPricingInputDocumentItemBase)documentItem).AddAttribute("ZZMVGR3_P", "03");


            foreach(DocumentCondBO condBO in conditionsForItems) {
                this.LogTrace("" + condBO.ConditionType + ", " + condBO.DealConditionNumber);
            }

            this.LogTrace("*** ZPricingManager:FillInputDocumentItem - end"); 
        }
        */

        public override void FillInputDocumentItem(IPricingInputDocumentBase inputDocument, IDocumentItemEntity documentItem, ActivityEntity activity, CustomizingSalesAreaBO customizingSalesArea, IList<MaterialBO> materials, IList<MaterialAltUomBO> materialAltUoms, IList<MaterialSalesOrgBO> materialSalesOrgs, IList<MaterialTaxBO> materialTaxes, CustomerSalesAreaBO customerSalesArea, CustomerBO customer, bool isRunningOnIsoCode, IList<DocumentCondBO> conditionsForItems, IDictionary<string, string> materialCampaigns, IDictionary<string, string> physicalUnitDictionary, IDictionary<string, CustomizingBO> reasonCodesCustomizingDictionary)
        {
            IPricingInputDocumentItemBase inputItem = inputDocument.AddItem(documentItem.DocumentItemNumber, documentItem.MaterialNumber);
            inputItem.AddAttribute("SHKZG", (object)documentItem.IsReturn.ToAbapString());
            inputItem.AddAttribute("RETPO", (object)documentItem.IsReturn.ToAbapString());
            string str1 = activity.ActivityType == SAPCD.DSD.MobileClient.Business.Objects.Common.Enums.ActivityType.FreeGoodsDelivery || activity.ActivityType == SAPCD.DSD.MobileClient.Business.Objects.Common.Enums.ActivityType.FreeGoodsOrder ? this.DeterminePricingIndicatorForFGDocument(activity, documentItem, customizingSalesArea) : this.DeterminePricingIndicator(documentItem, reasonCodesCustomizingDictionary);
            inputItem.AddAttribute("PRSFD", (object)str1);
            inputItem.AddAttribute("IS_EMPTIES_ITEM", (object)documentItem.IsUntiedEmpty.ToAbapString());
            MaterialBO material = materials.First<MaterialBO>((Func<MaterialBO, bool>)(m => m.MaterialNumber == documentItem.MaterialNumber));
            inputItem.AddMaterialInfo(material);
            inputItem.AddQuantities(documentItem);
            inputItem.AddCustomerSalesAreaInfo(customerSalesArea, customer);
            MaterialSalesOrgBO materialSalesOrg = materialSalesOrgs.First<MaterialSalesOrgBO>((Func<MaterialSalesOrgBO, bool>)(m => m.MaterialNumber == documentItem.MaterialNumber));
            inputItem.AddMaterialSalesAreaInfo(materialSalesOrg);


            this.AddAltUoms(inputItem, documentItem.MaterialNumber, (IEnumerable<MaterialAltUomBO>)materialAltUoms, isRunningOnIsoCode, physicalUnitDictionary);
            string uom = !isRunningOnIsoCode ? documentItem.ActualUnitOfMeasure : this.ConvertToCommercialCode(documentItem.ActualUnitOfMeasure, physicalUnitDictionary);
            string volumeUom = !isRunningOnIsoCode ? material.VolumeUom : this.ConvertToCommercialCode(material.VolumeUom, physicalUnitDictionary);
            string weightUom = !isRunningOnIsoCode ? material.WeightUom : this.ConvertToCommercialCode(material.WeightUom, physicalUnitDictionary);
            string baseUom = !isRunningOnIsoCode ? material.BaseUom : this.ConvertToCommercialCode(material.BaseUom, physicalUnitDictionary);
            inputItem.AddUoms(uom, volumeUom, weightUom, baseUom);

            if (conditionsForItems != null)
            {
                IEnumerable<DocumentCondBO> documentCondBos = conditionsForItems.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => x.ItemNumber == inputItem.ItemId));
                if (documentCondBos != null)
                {
                    IEnumerator<DocumentCondBO> enumerator = documentCondBos.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            DocumentCondBO current = enumerator.Current;
                            inputItem.AddManualCondRecord(current.ConditionType, current.Amount, current.Currency);
                        }
                    }
                    finally
                    {
                        if (enumerator != null)
                            enumerator.Dispose();
                    }
                }
            }

            MaterialTaxBO materialTax = materialTaxes.FirstOrDefault<MaterialTaxBO>((Func<MaterialTaxBO, bool>)(mt =>
           {
               if (mt.MaterialNumber == documentItem.MaterialNumber)
                   return mt.Country == customer.Country;
               return false;
           }));

            if (materialTax != null)
                inputItem.AddMaterialTaxInfo(materialTax);
            string str2;
            if (materialCampaigns.TryGetValue(documentItem.MaterialNumber, out str2))
                inputItem.AddAttribute("CMPGN_ID", (object)str2);
            else
                inputItem.AddAttribute("CMPGN_ID", (object)string.Empty);
            inputItem.AddAttribute("SDATE", (object)DateTime.Now);

            try
            {
                string prdh4 = materialSalesOrg.ProductHierarchy.Substring(6, 2);

                inputItem.AddAttribute("ZZPRODH4", prdh4);
                inputItem.AddAttribute("ZZMVGR1_P", materialSalesOrg.MaterialGroup1);
                inputItem.AddAttribute("ZZMVGR2_P", materialSalesOrg.MaterialGroup2);
                inputItem.AddAttribute("ZZMVGR3_P", materialSalesOrg.MaterialGroup3);
            }
            catch (Exception)
            { }


            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - start");

            if (conditionsForItems.Count > 1) {
                this.LogWarn("Mas de una condicion de ventas activa a la misma vez.");        
            }

            foreach (DocumentCondBO condBO in conditionsForItems)
            {
                this.LogDebug("" + condBO.ConditionType + ", " + condBO.DealConditionNumber);
            }

            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - end");

        }


        private void AddAltUoms(IPricingInputDocumentItemBase inputItem, string materialNumber, IEnumerable<MaterialAltUomBO> allMaterialAltUoms, bool isRunningOnIsoCode, IDictionary<string, string> physicalUnitDictionary)
        {
            List<MaterialAltUomBO> list = allMaterialAltUoms.Where<MaterialAltUomBO>((Func<MaterialAltUomBO, bool>)(ma => ma.MaterialNumber == materialNumber)).ToList<MaterialAltUomBO>();
            if (!list.Any<MaterialAltUomBO>())
                return;
            List<MaterialAltUomBO>.Enumerator enumerator = list.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    MaterialAltUomBO current = enumerator.Current;
                    string altUom;
                    if (!isRunningOnIsoCode)
                    {
                        altUom = current.AltUom;
                    }
                    else
                    {
                        altUom = this.ConvertToCommercialCode(current.AltUom, physicalUnitDictionary);
                        if (string.IsNullOrEmpty(altUom))
                            continue;
                    }
                    inputItem.AddAltUom(altUom, current.CNumerator, current.CDenominator);
                }
            }
            finally
            {
                enumerator.Dispose();
            }
        }


        private string ConvertToCommercialCode(string isoUom, IDictionary<string, string> physicalUnitDictionary)
        {
            if (isoUom == null)
                throw new ArgumentNullException(nameof(isoUom));
            string str;
            physicalUnitDictionary.TryGetValue(isoUom, out str);
            return str ?? string.Empty;
        }

        private async Task<string> GetCustomizingValue(string custKey, string keyValue)
        {
            string custValue = "";

            try
            {
                this.LogTrace("ZPricingManager:GetCustomizingValue:{0}", keyValue);
                CustomizingBO customizingBO = await this.Customizing.FindCustomizingAsync("PRICING", keyValue).ConfigureAwait(false);

                if (customizingBO != null)
                {
                    if (!string.IsNullOrEmpty(customizingBO.KeyValueCode))
                    {
                        custValue = customizingBO.KeyValueCode;
                    }
                }
            }
            catch (Exception ex)
            {
                custValue = keyValue + " " + ex.Message;
            }

            return custValue;
        }

    }
}
