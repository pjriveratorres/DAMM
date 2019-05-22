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

        public async override Task<IPricingInputDocumentBase> PrepareInputDocumentAsync(PricingParameters pricingParameters)
        {
            /*
            this.LogDebug("Starting to prepare pricing engine input document", nameof(PrepareInputDocumentAsync), 242);
            VisitEntity visit = pricingParameters.Visit;
            ActivityEntity activity = pricingParameters.Activity;
            IPriceableDocumentEntity document = pricingParameters.Document;
            IList<IDocumentItemEntity> documentItems = pricingParameters.DocumentItems;
            IList<DocumentCondBO> conditions = pricingParameters.Conditions;
            CustomizingSalesAreaBO customizingSalesArea = pricingParameters.CustomizingSalesArea;
            IPricingInputDocumentBase inputDocument = this.PricingHost.CreateNewPricingInputDocument();
            IList<DocumentCondBO> conditionsForHeader = (IList<DocumentCondBO>)null;
            IList<DocumentCondBO> conditionsForItems = (IList<DocumentCondBO>)null;
            if (conditions != null)
            {
                conditionsForHeader = (IList<DocumentCondBO>)conditions.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => x.ItemNumber == 0)).ToList<DocumentCondBO>();
                conditionsForItems = (IList<DocumentCondBO>)conditions.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => (uint)x.ItemNumber > 0U)).ToList<DocumentCondBO>();
            }
            this.LogDebug("Reading necessary customizing for pricing engine input document", nameof(PrepareInputDocumentAsync), 263);
            Task<CustomizingCountryBO> customizingCountryTask = this.Customizing.GetCustomizingCountryAsync();
            Task<CustomizingBO> isoCodeCurrencyCustomizingTask = this.Customizing.FindCustomizingAsync("USE_ISOCODE", "CURR");
            Task<CustomizingBO> currencyCustomizingTask = this.Customizing.FindCustomizingAsync("CURRENCY", "CURRENCY_VAL");
            Task<CustomizingBO> isoCodeCustomizingTask = this.Customizing.FindCustomizingAsync("USE_ISOCODE", "UOM");
            await Task.WhenAll((Task)customizingCountryTask, (Task)isoCodeCurrencyCustomizingTask, (Task)currencyCustomizingTask, (Task)isoCodeCustomizingTask).ConfigureAwait(false);
            bool isRunningOnIsoCode = false;
            if (isoCodeCustomizingTask.Result != null)
                isRunningOnIsoCode = isoCodeCustomizingTask.Result.KeyValueCode == "X";
            string currency = (string)null;
            if (currencyCustomizingTask.Result != null && isoCodeCurrencyCustomizingTask.Result != null)
                currency = isoCodeCurrencyCustomizingTask.Result.KeyValueCode == "X" ? currencyCustomizingTask.Result.AdditionalKeyValue : currencyCustomizingTask.Result.KeyValueCode;
            this.LogDebug("Reading tour and master data for pricing engine input document", nameof(PrepareInputDocumentAsync), 280);
            CustomerPartnerFuncBO customerPartnerFunction = await this.CustomerDal.FindCustomerPartnerFunctionAsync(visit.CustomerNumber, visit.SalesOrg, visit.DistributionChannel, visit.Division).ConfigureAwait(false);
            string customerNumber = this.DetermineCustomerNumber(document, visit, customizingCountryTask.Result, customerPartnerFunction);
            List<string> materialNumbers = documentItems.Select<IDocumentItemEntity, string>((Func<IDocumentItemEntity, string>)(di => di.MaterialNumber)).ToList<string>();
            CustomerBO customer = await this.CustomerDal.FindCustomerAsync(customerNumber).ConfigureAwait(false);
            Task<TourEntity> tourTask = this.TourManager.GetCurrentTourAsync();
            Task<CustomerTaxBO> customerTaxTask = this.CustomerDal.FindCustomerTaxAsync(customerNumber, customer.Country);
            Task<CustomerSalesAreaBO> customerSalesAreaTask = this.CustomerDal.FindSalesAreaForCustomerAsync(customerNumber, visit.SalesOrg, visit.DistributionChannel, visit.Division);
            Task<CustomerHierarchyBO> customerHierarchyTask = this.CustomerDal.FindCustomerHierarchyAsync(customerNumber, visit.SalesOrg, visit.DistributionChannel, visit.Division);
            Task<IList<MaterialBO>> materialsTask = this.MaterialDal.FindMaterialsAsync((IEnumerable<string>)materialNumbers);
            Task<IList<MaterialAltUomBO>> materialAltUomsTask = this.MaterialDal.FindMaterialAltUoMsAsync((IEnumerable<string>)materialNumbers);
            Task<IList<MaterialSalesOrgBO>> materialSalesOrgsTask = this.MaterialDal.FindMaterialSalesOrgsAsync((IEnumerable<string>)materialNumbers, visit.SalesOrg, visit.DistributionChannel);
            Task<IList<MaterialTaxBO>> materialTaxesTask = this.MaterialDal.FindMaterialTaxesAsync((IEnumerable<string>)materialNumbers);
            Task<IList<PhysicalUnitBO>> physicalUnitsTask = this.PhsyicalUnitDal.FindAllPhysicalUnitsAsync();
            await Task.WhenAll((Task)tourTask, (Task)customerTaxTask, (Task)customerSalesAreaTask, (Task)customerHierarchyTask, (Task)materialsTask, (Task)materialAltUomsTask, (Task)materialSalesOrgsTask, (Task)materialTaxesTask, (Task)physicalUnitsTask).ConfigureAwait(false);
            IList<PhysicalUnitBO> source1 = await physicalUnitsTask;
            IDictionary<string, string> physicalUnitDictionary = (IDictionary<string, string>)null;
            if (source1 != null)
            {
                IEnumerable<PhysicalUnitBO> source2 = source1.GroupBy<PhysicalUnitBO, string>((Func<PhysicalUnitBO, string>)(p => p.IsoCode)).Select<IGrouping<string, PhysicalUnitBO>, PhysicalUnitBO>((Func<IGrouping<string, PhysicalUnitBO>, PhysicalUnitBO>)(g => g.First<PhysicalUnitBO>()));
                Func<PhysicalUnitBO, string> func = (Func<PhysicalUnitBO, string>)(p => p.IsoCode);
                Func<PhysicalUnitBO, string> keySelector;
                physicalUnitDictionary = (IDictionary<string, string>)source2.ToDictionary<PhysicalUnitBO, string, string>(keySelector, (Func<PhysicalUnitBO, string>)(p => p.CommercialCode));
            }
            else
                physicalUnitDictionary = (IDictionary<string, string>)new Dictionary<string, string>();
            this.LogDebug("Reading campaign data for pricing engine input document", nameof(PrepareInputDocumentAsync), 306);
            IList<CampaignItemBO> source3 = await this.CampaignDal.FindItemsForCampaignsAsync(await this.CampaignDal.FindAllCampaignsForCurrentCustomerAndSalesAreaAsync(customerNumber, visit.SalesOrg, visit.Division, visit.DistributionChannel).ConfigureAwait(false)).ConfigureAwait(false);
            IEnumerable<string> strings = source3.Select<CampaignItemBO, string>((Func<CampaignItemBO, string>)(ci => ci.MaterialNumber)).Distinct<string>();
            IDictionary<string, string> materialCampaigns = (IDictionary<string, string>)new Dictionary<string, string>();
            IEnumerator<string> enumerator1 = strings.GetEnumerator();
            try
            {
                while (enumerator1.MoveNext())
                {
                    string materialNumber = enumerator1.Current;
                    string str = string.Join(";", source3.Where<CampaignItemBO>((Func<CampaignItemBO, bool>)(ci => ci.MaterialNumber == materialNumber)).Select<CampaignItemBO, string>((Func<CampaignItemBO, string>)(ci => ci.CampaignId)));
                    materialCampaigns.Add(materialNumber, str);
                }
            }
            finally
            {
                if (enumerator1 != null)
                    enumerator1.Dispose();
            }
            this.LogDebug("Starting to apply data to pricing engine input document", nameof(PrepareInputDocumentAsync), 324);
            DeliveryEntity deliveryEntity = document as DeliveryEntity;
            string soldToNumber = !visit.IsNewOneTimeCustomer ? (deliveryEntity != null && !document.IsCreatedOnHandHeld && !string.IsNullOrEmpty(deliveryEntity.SoldTo) ? deliveryEntity.SoldTo : customerPartnerFunction.SoldToNumber) : visit.CustomerNumber;
            inputDocument.AddCustomerPartnerFunctionInfo(customerPartnerFunction, soldToNumber);
            inputDocument.AddCustomerSalesAreaInfo(customerSalesAreaTask.Result);
            inputDocument.AddCustomerHierarchyInfo(customerHierarchyTask.Result);
            inputDocument.AddCustomerTaxInfo(customerTaxTask.Result);
            inputDocument.AddAttribute("ROUTE", (object)tourTask.Result.RouteId);
            inputDocument.AddCurrency(currency);
            if (conditionsForHeader != null)
            {
                IEnumerator<DocumentCondBO> enumerator2 = conditionsForHeader.GetEnumerator();
                try
                {
                    while (enumerator2.MoveNext())
                    {
                        DocumentCondBO current = enumerator2.Current;
                        inputDocument.AddManualHeaderCondRecord(current.ConditionType, current.Amount, current.Currency);
                    }
                }
                finally
                {
                    if (enumerator2 != null)
                        enumerator2.Dispose();
                }
            }
            await Task.WhenAll(this.AddDocumentTypeToInputDocumentAsync(document, inputDocument), this.ApplyCustomerHeaderToInputDocumentAsync(inputDocument, customerNumber, customer, document), this.ApplyDatesToInputDocumentAsync(visit, document, inputDocument, tourTask.Result)).ConfigureAwait(false);
            Dictionary<string, CustomizingBO> dictionary = (await this.Customizing.GetCustomizingsWithKeyValuesAsync("CHG_REAS", documentItems.Where<IDocumentItemEntity>((Func<IDocumentItemEntity, bool>)(item =>
            {
                if (item.ChangeReason != null)
                    return !string.IsNullOrEmpty(item.ChangeReason.Code);
                return false;
            })).Select<IDocumentItemEntity, string>((Func<IDocumentItemEntity, string>)(item => item.ChangeReason.Code)).Distinct<string>()).ConfigureAwait(false)).ToDictionary<CustomizingBO, string>((Func<CustomizingBO, string>)(c => c.KeyValue));
            IEnumerator<IDocumentItemEntity> enumerator3 = documentItems.GetEnumerator();
            try
            {
                while (enumerator3.MoveNext())
                {
                    IDocumentItemEntity current = enumerator3.Current;
                    this.LogDebug(string.Format("Adding data to input document item for material '{0}' with item number '{1}'", (object[])new object[2]
                    {
            (object) current.MaterialNumber,
            (object) current.DocumentItemNumber
                    }), nameof(PrepareInputDocumentAsync), 365);
                    this.FillInputDocumentItem(inputDocument, current, activity, customizingSalesArea, materialsTask.Result, materialAltUomsTask.Result, materialSalesOrgsTask.Result, materialTaxesTask.Result, customerSalesAreaTask.Result, customer, isRunningOnIsoCode, conditionsForItems, materialCampaigns, physicalUnitDictionary, (IDictionary<string, CustomizingBO>)dictionary);
                }
            }
            finally
            {
                if (enumerator3 != null)
                    enumerator3.Dispose();
            }
            */
            var inputDocument = await base.PrepareInputDocumentAsync(pricingParameters);

            IList<DocumentCondBO> conditions = pricingParameters.Conditions;
            this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync - conditions = " + (conditions.Count));

            if (conditions != null)
            {
                IList<DocumentCondBO> conditionsForHeader = (IList<DocumentCondBO>)null;
                IList<DocumentCondBO> conditionsForItems = (IList<DocumentCondBO>)null;

                conditionsForHeader = (IList<DocumentCondBO>)conditions.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => x.ItemNumber == 0)).ToList<DocumentCondBO>();
                conditionsForItems = (IList<DocumentCondBO>)conditions.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => (uint)x.ItemNumber > 0U)).ToList<DocumentCondBO>();
      
                this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync - conditionsForHeader =" + (conditionsForHeader.Count));
                this.LogDebug("*** ZPricingManager:PrepareInputDocumentAsync - conditionsForItems =" + (conditionsForItems.Count));
            
            }

            return inputDocument;
        }

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

            int conditionCount = 0;
            if (conditionsForItems != null)
            {
                IEnumerable<DocumentCondBO> documentCondBos = conditionsForItems.Where<DocumentCondBO>((Func<DocumentCondBO, bool>)(x => x.ItemNumber == inputItem.ItemId));

                this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + (documentCondBos != null));

                if (documentCondBos != null)
                {
                    /*
                    if (conditionCount == 0)
                    {
                        throw new PricingException("No es permitido mas de una condición de ventas activa a la misma vez.");
                    }
                    */

                    IEnumerator<DocumentCondBO> enumerator = documentCondBos.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            conditionCount = conditionCount + 1;
                            DocumentCondBO current = enumerator.Current;

                            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - [" +inputItem.ItemId + "] " + current.ConditionType + current.Amount + " " + current.Currency);

                            inputItem.AddManualCondRecord(current.ConditionType, current.Amount, current.Currency);

                            if (conditionCount > 1) {
                                throw new PricingException("No es permitido mas de una condición de ventas activa a la misma vez.");
                            }
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


            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + inputItem.GetStringAttribute("HIENR01"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + inputItem.GetStringAttribute("HIENR02"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + inputItem.GetStringAttribute("HIENR03"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + inputItem.GetStringAttribute("HIENR04"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + inputItem.GetStringAttribute("HIENR05"));
            this.LogDebug("*** ZPricingManager:FillInputDocumentItem - " + inputItem.GetStringAttribute("PMATN"));

        }

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
