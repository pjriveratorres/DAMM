using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAPCD.DSD.MobileClient.Business.Components;
using SAPCD.DSD.MobileClient.Business.Entities.Common;
using SAPCD.DSD.MobileClient.Business.Entities.Document;
using SAPCD.DSD.MobileClient.Business.Entities.Order;
using SAPCD.DSD.MobileClient.Business.Entities.Visit;
using SAPCD.DSD.MobileClient.Business.Interfaces.Managers;
using SAPCD.DSD.MobileClient.Business.Managers;
using SAPCD.DSD.MobileClient.Business.Objects;
using SAPCD.DSD.MobileClient.Business.Objects.Common;
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Core.Interfaces.Services;
using SAPCD.DSD.MobileClient.Data;
using SAPCD.DSD.MobileClient.Extensions.BusinessObjects;
using SAPCD.DSD.MobileClient.Pricing.Interfaces.Core.DocumentObjects;

namespace Capgemini.DSD.Pricing
{
    [ApplicationExtension]
    public class ZPricingManager : PricingManager, ICanLog
    {
        private IList<FieldExtensionBO> _materialExt;

        // Generate list of deal conditions met:
        private IDictionary<string, DealConditionHeaderBO> dealConditionsMetList; 
        private IDictionary<int, string> lineItemConditionMet;

        
        private string WarningMessage { get; set; }
        private bool warningMet = false;


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

        [Resolve]
        public IDialogService Dialog { get; set; }

        /*
        // this method is used for loading data asynchronously
        public async override Task InitializeAsync()
        {
            // load data from base InvoiceReport
            await base.InitializeAsync();
        }
        */

        
        public async override Task<PricingResultEntity> PriceOrderAsync(VisitEntity visit, ActivityEntity activity, OrderEntity order, IList<OrderItemEntity> orderItems, IList<DocumentCondBO> conditions)
        {
            // init conditions met and message for every order been priced.
            WarningMessage = "Se han aplicado más de una promoción sobre los materiales:";
            warningMet = false;

            if (dealConditionsMetList != null)
            {
                dealConditionsMetList.Clear();
                lineItemConditionMet.Clear();
            }
            else
            {
                dealConditionsMetList = new Dictionary<string, DealConditionHeaderBO>();
                lineItemConditionMet = new Dictionary<int, string>();
            }

            // First add item level:
            if (conditions != null)
            {
                foreach (DocumentCondBO conditionBO in conditions)
                {
                    var item = orderItems.FirstOrDefault(it => it.DocumentItemNumber == conditionBO.ItemNumber);
                    if (item != null)
                    {
                        this.LogDebug("* ZPricingManager:PriceOrderAsync:ConditionItemLevel " + conditionBO.ItemNumber + "-" + conditionBO.DealConditionNumber);
                        await AddDealConditionMetAsync(item.DocumentItemNumber, conditionBO.DealConditionNumber);
                    }
                }
            }

            if (orderItems != null)
            {
                // Then add conditions met because of free goods:
                foreach (OrderItemEntity orderItem in orderItems)
                {
                    if (orderItem.IsPromotionResult)
                    {
                        this.LogDebug("* ZPricingManager:PriceOrderAsync:PromotionResultFG " + orderItem.DocumentItemNumber + "-" + orderItem.PromotionNumber);
                        await AddDealConditionMetAsync(orderItem.DocumentItemNumber, orderItem.PromotionNumber);
                    }
                }


                // Now lets check preconditions
                IEnumerator<OrderItemEntity> itemEnum = orderItems.GetEnumerator();

                // VALIDATION: Check if multiple DC were met because of same material using preconditions
                try
                {
                    this.LogDebug("* ZPricingManager:Check Pre-conditions for DC");

                    if (itemEnum != null)
                    {
                        while (itemEnum.MoveNext())
                        {
                            OrderItemEntity currentItem = itemEnum.Current;
                            int count = 0; // count conditions met

                            if (!currentItem.IsPromotionResult)
                            {
                                this.LogDebug("* ZPricingManager:PriceOrderAsync:CheckPre-Cond. " + currentItem.DocumentItemNumber + "-" + currentItem.MaterialNumber);

                                foreach (string conditionID in dealConditionsMetList.Keys)
                                {
                                    // Get Preconditions for DC
                                    IList<DealConditionPreconditionBO> preConditions = await DealConditionDAL.FindDealConditionPreconditionsByDealConditionIDAsync(conditionID);

                                    if (preConditions.Count > 0)
                                    {
                                        foreach (DealConditionPreconditionBO preCondition in preConditions)
                                        {
                                            this.LogDebug("*** DC:PreCondMaterial " + preCondition.MaterialNumber);

                                            // Precondition material is the same as current item
                                            if (currentItem.MaterialNumber.Equals(preCondition.MaterialNumber))
                                            {
                                                await AddDealConditionMetAsync(currentItem.DocumentItemNumber, conditionID);
                                                count++;
                                            }
                                        }
                                    }
                                }


                                // A warning is present for current item don't procee more items
                                if (count > 1)
                                {
                                    this.LogDebug("*** :ERROR - Material met more than once in a DC pre-conditions");
                                    //throw new PricingException("No es permitido mas de una condición de ventas activa para el producto [" + currentItem.MaterialNumber + "]");
                                    WarningMessage = WarningMessage + "\n" + currentItem.MaterialNumber + " - (" + count + ")";
                                    warningMet = true;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (itemEnum != null) itemEnum.Dispose();
                }
            }

            return await base.PriceOrderAsync(visit, activity, order, orderItems, conditions);
        }

        private async Task AddDealConditionMetAsync(int orderLineNumber, string conditionID)
        {
            try
            {
                this.LogDebug("== :AddDealConditionMet - Start: " + orderLineNumber + " => " + conditionID);

                // Use to get the DC header and later use it to get the type
                IList<string> conditionIDs = new List<string>() { conditionID };

                 if (!dealConditionsMetList.ContainsKey(conditionID))
                {
                    this.LogDebug("== :AddDealConditionMet before FindDealConditionHeader");

                    var result = await DealConditionDAL.FindDealConditionHeadersByDealConditionIDWithoutDateCheckAsync(conditionIDs).ConfigureAwait(false);
                    if (result != null)
                    {
                        if (result.Count > 0)
                        {
                            this.LogDebug("== :AddDealConditionMet result found - add then");

                            dealConditionsMetList.Add(conditionID, result[0]);
                        }
                    }
                }

                this.LogDebug("== :AddDealConditionMet add to lineItemConditionMet. ");

                if (!lineItemConditionMet.ContainsKey(orderLineNumber))
                {
                    lineItemConditionMet.Add(orderLineNumber, conditionID);
                }

                this.LogDebug("== :AddDealConditionMet Met Exit. ");
            }
            catch(Exception all)
            {
                this.LogDebug("== :AddDealConditionMet " + all.Message);
                this.LogError(all);
            }    
        }


        /*
        public async override Task<IPricingInputDocumentBase> PrepareInputDocumentAsync(PricingParameters pricingParameters) 
        {
            this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync - count " + (pricingParameters.Conditions.Count()));
            var inputDocument = await base.PrepareInputDocumentAsync(pricingParameters);

            IEnumerator<DocumentCondBO> enumerator = pricingParameters.Conditions.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    DocumentCondBO current = enumerator.Current;
                    this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync " + current.DealConditionNumber);
                    this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync " + current.ConditionType);
                    this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync " + current.ItemNumber);
                    this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync " + current.PromotionDiscount);
                    this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync " + current.PercentageType);
                    this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync " + current.Amount + " " + current.Currency);

                }
            }
            catch {
                
            }

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
                    /*
                    this.LogDebug("*** ZPricingManager:FillInputDocumentItem - count " + (documentCondBos.Count()));

                    if (documentCondBos.Count() > 1)
                    {
                        throw new PricingException("No es permitido mas de una condición de ventas activa para el mismo producto [" + material.MaterialNumber + "]");
                    }
                    */

                    // Add Manual conditions
                    IEnumerator<DocumentCondBO> enumerator = documentCondBos.GetEnumerator();
                    try
                    {
                        // This loop will assume there is only one deal condition applied to this line item
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

            // Add DC type to be used by routine 959
            if (lineItemConditionMet.TryGetValue(inputItem.ItemId, out string conditionID))
            {
                if (dealConditionsMetList.TryGetValue(conditionID, out DealConditionHeaderBO dealConditionHeaderBO))
                {
                    this.LogDebug("*** ZPricingManager:FillInputDocumentItem - Add contition type for " + inputItem.ItemId + "/" + dealConditionHeaderBO.DealConditionID + "/" + dealConditionHeaderBO.DealConditionType);
                    inputItem.AddAttribute("ZZTYPE", dealConditionHeaderBO.DealConditionType);  // value to be used by routine 959.                               
                }
            }

            /*
            MaterialTaxBO materialTax = materialTaxes.FirstOrDefault<MaterialTaxBO>((Func<MaterialTaxBO, bool>)(mt =>
           {
               if (mt.MaterialNumber == documentItem.MaterialNumber)
                   return mt.Country == customer.Country;
               return false;
           }));
           */

            MaterialTaxBO materialTax = materialTaxes.FirstOrDefault<MaterialTaxBO>((Func<MaterialTaxBO, bool>)
                                                                                    (mt =>((mt.MaterialNumber == documentItem.MaterialNumber) && (mt.Country == customer.Country))));
       
            if (materialTax != null) {
                this.LogDebug("*** ZPricingManager:FillInputDocumentItem-CustomerCountry - " + materialTax.Country);
                this.LogDebug("*** ZPricingManager:FillInputDocumentItem-TAXM1 - " + materialTax.TaxClass1);
                this.LogDebug("*** ZPricingManager:FillInputDocumentItem-TAXM2 - " + materialTax.TaxClass2);
                this.LogDebug("*** ZPricingManager:FillInputDocumentItem-TAXM3 - " + materialTax.TaxClass3);
                inputItem.AddMaterialTaxInfo(materialTax);
            }

            string str2;
            if (materialCampaigns.TryGetValue(documentItem.MaterialNumber, out str2))
                inputItem.AddAttribute("CMPGN_ID", (object)str2);
            else
                inputItem.AddAttribute("CMPGN_ID", (object)string.Empty);
            inputItem.AddAttribute("SDATE", (object)DateTime.Now);

            try
            {                              
                if (MaterialExt.Count > 0) 
                {
                    FieldExtensionBO fieldExtension = MaterialExt.FirstOrDefault<FieldExtensionBO>((Func<FieldExtensionBO, bool>)(m => m.ElementKey == documentItem.MaterialNumber));
   
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
                this.LogError("ZPricingManager:FillInputDocumentItem - Linea " + inputItem.ItemId + " Material: " + material.MaterialNumber, ex);
            }


            /*
            if (inputDocument.GetStringHeaderAttribute("HIENR") != null) 
            {
                inputDocument.AddAttribute("HIENR", inputDocument.GetStringHeaderAttribute("HIENR01"));
            }

            /*
            if (inputDocument.GetStringHeaderAttribute("HIENR02") != null)
            {
                inputDocument.AddAttribute("HIENR46", inputDocument.GetStringHeaderAttribute("HIENR02"));
            }
            */

            //this.LogDebug("*** ZPricingManager:FillInputDocumentItemHIENR03 - " + inputItem.GetStringAttribute("HIENR03"));
            //this.LogDebug("*** ZPricingManager:FillInputDocumentItemPMATN - " + inputItem.GetStringAttribute("PMATN"));
        }

 
        public override PricingResultEntity ProcessOutputDocument(PricingParameters pricingParameters, IPricingOutputDocumentBase outputDocument, string currency)
        {
            if (outputDocument != null)
            {
                /*
                outputDocument.GetType().GetRuntimeProperty("PricingSeverity").SetValue(outputDocument, PricingSeverity.Warning);
                Message message = new Message(this.GetType(), "ProcessOutputDocument", SeverityCode.Warning, "My Message");
                MethodInfo method = outputDocument.GetType().GetMethod("AddMessage", new [] {message.GetType()});

                this.LogDebug("*** ZPricingManager:Before Invoke " + (method != null) + " " + outputDocument.Messages.Count());
                method.Invoke(outputDocument, new object[] { message });
                this.LogDebug("*** ZPricingManager:After Invoke " + outputDocument.Messages.Count());
                */

                
                if (warningMet)
                {
                    this.LogDebug("*** ZPricingManager:ShowAlert=> " + WarningMessage);
                    Dialog.ShowAlertAsync("Condiciones De Ventas", WarningMessage);
                    //Dialog.ShowErrorAsync("Condiciones De Ventas", WarningMessage);
                }
            }

            return base.ProcessOutputDocument(pricingParameters, outputDocument, currency);
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
