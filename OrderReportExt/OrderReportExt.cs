using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using SAPCD.DSD.MobileClient.Business.Reports;
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Business.Objects.Common;
using SAPCD.DSD.MobileClient.Business.Interfaces.Components;
using SAPCD.DSD.MobileClient.Business.Objects;
using SAPCD.DSD.MobileClient.Business.Components;
using SAPCD.DSD.MobileClient.Business.Entities.Common;
using SAPCD.DSD.MobileClient.Pricing.Interfaces.Enums;
using SAPCD.DSD.MobileClient.Business.Entities.Order;

namespace Capgemini.DSD.Reports.Extensions
{
    [ResolveParameterized]
    public class OrderReportExt : OrderConfirmationReport, ICanLog
    {
        // Used to extract pricing result
        private OrderCreationProcessor _orderCreationProcessor;


        public override string CustomizingKey { get { return Constants.CUSTOMIZING_REPORT_KEY_INVOICE; } }


        public void Init(IOrderCreationProcessor processor) 
        {
            base.Init(processor);

            _orderCreationProcessor = (SAPCD.DSD.MobileClient.Business.Components.OrderCreationProcessor)processor;
        }


        // this method is used for loading data asynchronously
        public async override Task InitializeAsync()
        {
            IList<PricingConditionResultItemEntity> conditions = this._orderCreationProcessor.PricingConditionResults;

            foreach (OrderItemEntity item in OrderItemEntityList)
            {
                try
                {
                    decimal discount = 0M;

                    foreach (PricingConditionResultItemEntity condition in conditions)
                    {
                        this.LogTrace("OrderReportExt:Condition: " + condition.ConditionType, condition.ConditionType);

                        // for current item.... get base price and discounts
                        if (condition.ItemNumber == item.DocumentItemNumber)
                        {
                            /*
                            if (condition.ConditionClass == ConditionClass.Prices)
                            {
                                item.Price = (decimal)condition.ConditionValueInternal;
                            }
                            else 
                            */
                            if (condition.ConditionClass == ConditionClass.DiscountSurcharge)
                            {
                                // Is this a discount?
                                if (condition.ConditionValueInternal < 0) 
                                {
                                    discount = discount + (decimal)condition.ConditionValueInternal;
                                }
                            }
                         }
                    }

                    // Calculate new columns and set other properties 
                    item.TaxRate1 = discount;
                    item.TaxRate2 = item.Price - discount;
                }
                catch (Exception ex)
                {
                    this.LogError("OrderReportExt:InitializeAsync: Init =" + ex.Message);
                    this.LogError(ex);

                    item.ActualUnitOfMeasure = "ERR " + ex.Message;
                }
            }
        }


        private decimal roundDecimals(decimal value)
        {
            string strNumber = String.Format("{0:0.00}", value);
            this.LogDebug("OrderReportExt:: roundDecimals =" + strNumber);
            return Convert.ToDecimal(strNumber);
        }


        private async Task<string> GetCustomizingValue(string custKey, string keyValue)
        {
            string addValue = "";

            try
            {
                this.LogTrace("OrderReportExt:GetCustomizingValue:{0}", keyValue, 216);
                CustomizingBO customizingBO = await this.Customizing.FindCustomizingAsync("PEDIDO", keyValue).ConfigureAwait(false);

                if (customizingBO != null)
                {
                    if (!string.IsNullOrEmpty(customizingBO.KeyValueCode))
                    {
                        addValue = customizingBO.KeyValueCode;
                    }
                }
            }
            catch (Exception ex) 
            {
                addValue = keyValue + " " + ex.Message;   
            }
 
            return addValue;
        }
    }
}
