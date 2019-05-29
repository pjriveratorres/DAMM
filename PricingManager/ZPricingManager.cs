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
using SAPCD.DSD.MobileClient.Business.Exceptions;
using SAPCD.DSD.MobileClient.Business.Interfaces;
using SAPCD.DSD.MobileClient.Business.Interfaces.Managers;
using SAPCD.DSD.MobileClient.Business.Managers;
using SAPCD.DSD.MobileClient.Business.Objects;
using SAPCD.DSD.MobileClient.Business.Objects.Common;
using SAPCD.DSD.MobileClient.Data;
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Extensions.BusinessObjects;
using SAPCD.DSD.MobileClient.Pricing.Interfaces.Core.DocumentObjects;

namespace Capgemini.DSD.Pricing
{
    [ApplicationExtension]
    public class ZPricingManager : PricingManager, ICanLog
    {
        private IList<FieldExtensionBO> _materialExt;

        private IList<FieldExtensionBO> MaterialExt
        {
            get
            {
                if (_materialExt == null)
                {
                    _materialExt = FieldExtensionDAL.FindFieldExtensionsForBusinessObjectAsync("MSE_MATERIAL_HD").Result;
                }

                return _materialExt;
            }
        }

        [Resolve]
        public DealConditionsDAL DealConditionDAL { get; set; }

        [Resolve]
        public FieldExtensionDAL FieldExtensionDAL { get; set; }

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
                    this.LogDebug("*** ZPricingManager:FillInputDocumentItem - count " + (documentCondBos.Count()));

                    if (documentCondBos.Count() > 1)
                    {
                        throw new PricingException("No es permitido mas de una condición de ventas activa para el producto [" + material.MaterialNumber + "]");
                    }

                    IEnumerator<DocumentCondBO> enumerator = documentCondBos.GetEnumerator();
                    try
                    {
                        // This loop will assume there is only one deal condition applied to this line item
                        while (enumerator.MoveNext())
                        {
                            DocumentCondBO current = enumerator.Current;
                            inputItem.AddManualCondRecord(current.ConditionType, current.Amount, current.Currency);

                            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - Add manual deal cond for " + inputItem.ItemId + "/" + current.DealConditionNumber + "/" + current.ConditionType + "/" + current.Amount + " " + current.Currency);
                            IList<string> conditionIDs = new List<string>(){current.DealConditionNumber};
     
                            var dealConditionHeaderBOList = DealConditionDAL.FindDealConditionHeadersByDealConditionIDWithoutDateCheckAsync(conditionIDs).Result;

                            if (dealConditionHeaderBOList.Count > 0) {
                                this.LogDebug("*** ZPricingManager:FillInputDocumentItem - DC Type: " + dealConditionHeaderBOList[0].DealConditionType);
                                inputItem.AddAttribute("ZZTYPE", dealConditionHeaderBOList[0].DealConditionType);  // value to be used by routine 959.                               
                            }
                            break;
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
                this.LogDebug("*** ZPricingManager:ZZIIEE count " + MaterialExt.Count());
                              
                if (MaterialExt.Count > 0) 
                {
                    FieldExtensionBO fieldExtension = MaterialExt.First<FieldExtensionBO>((Func<FieldExtensionBO, bool>)(m => m.ElementKey == documentItem.MaterialNumber));
   
                    if (fieldExtension != null)
                    {
                        this.LogDebug("ZZIIEE: " + fieldExtension.ElementKey + "=" + fieldExtension.Value);
                        inputItem.AddAttribute("ZZIIEE", fieldExtension.Value);
                    }
                }

                if (materialSalesOrg.ProductHierarchy.Length > 7) {
                    string prdh4 = materialSalesOrg.ProductHierarchy.Substring(6, 2);
                    inputItem.AddAttribute("ZZPRODH4", prdh4);
                }

                inputItem.AddAttribute("ZZMVGR1_P", materialSalesOrg.MaterialGroup1);
                inputItem.AddAttribute("ZZMVGR2_P", materialSalesOrg.MaterialGroup2);
                inputItem.AddAttribute("ZZMVGR3_P", materialSalesOrg.MaterialGroup3); 
                inputItem.AddAttribute("UPMAT", material.MaterialNumber); 
            }
            catch (Exception ex)
            {
                this.LogWarn("ZPricingManager:FillInputDocumentItem - Linea " + inputItem.ItemId + " Material: " + material.MaterialNumber, ex);
            }

            if (inputDocument.GetStringHeaderAttribute("HIENR01") != null) 
            {
                inputDocument.AddAttribute("HIENR45", inputDocument.GetStringHeaderAttribute("HIENR01"));
            }

            if (inputDocument.GetStringHeaderAttribute("HIENR02") != null)
            {
                inputDocument.AddAttribute("HIENR46", inputDocument.GetStringHeaderAttribute("HIENR02"));
            }

           /*
            this.LogDebug("*** ZPricingManager:FillInputDocumentItemHIENR01 - " + inputItem.GetStringAttribute("HIENR01"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItemHIENR02 - " + inputItem.GetStringAttribute("HIENR02"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItemHIENR03 - " + inputItem.GetStringAttribute("HIENR03"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItemPMATN - " + inputItem.GetStringAttribute("PMATN"));
            */
        }

        /*
        public override PricingResultEntity ProcessOutputDocument(PricingParameters pricingParameters, IPricingOutputDocumentBase outputDocument, string currency)
        {
            var pricingResultEntity = base.ProcessOutputDocument(pricingParameters, outputDocument, currency);

            IList<DocumentCondBO> conditions = pricingParameters.Conditions;
            this.LogDebug("*** ZPricingManager:ProcessOutputDocument - conditions = " + (conditions.Count));

            if (conditions != null)
            {
                IList<DocumentCondBO> conditionsForHeader = (IList<DocumentCondBO>)null;
                IList<DocumentCondBO> conditionsForItems = (IList<DocumentCondBO>)null;

                conditionsForHeader = (IList<DocumentCondBO>)conditions.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => x.ItemNumber == 0)).ToList<DocumentCondBO>();
                conditionsForItems = (IList<DocumentCondBO>)conditions.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => (uint)x.ItemNumber > 0U)).ToList<DocumentCondBO>();

                this.LogDebug("*** ZPricingManager:ProcessOutputDocument - conditionsForHeader =" + (conditionsForHeader.Count));
                this.LogDebug("*** ZPricingManager:ProcessOutputDocument - conditionsForItems =" + (conditionsForItems.Count));
            }

            return pricingResultEntity;
        }
        */
 

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
