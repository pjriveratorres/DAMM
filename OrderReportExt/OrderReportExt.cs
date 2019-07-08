using System;
using System.Threading.Tasks;

using SAPCD.DSD.MobileClient.Business.Reports;
using SAPCD.DSD.MobileClient.Common.Components;
using SAPCD.DSD.MobileClient.Common.Interfaces;
using SAPCD.DSD.MobileClient.Business.Interfaces.Components;
using SAPCD.DSD.MobileClient.Business.Components;
using SAPCD.DSD.MobileClient.Business.Entities.Order;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using SAPCD.DSD.MobileClient.Business.Entities.Common;
using SAPCD.DSD.MobileClient.Pricing.Interfaces.Enums;

namespace Capgemini.DSD.Reports.Extensions
{
    [ResolveParameterized]
    public class OrderReportExt : OrderConfirmationReport, ICanLog
    {
        // Used to extract pricing result
        private OrderCreationProcessor _orderCreationProcessor;

        public void Init(IOrderCreationProcessor processor) 
        {
            base.Init(processor);

            this.LogError("OrderReportExt:InitializeAsync: Init");

            _orderCreationProcessor = (SAPCD.DSD.MobileClient.Business.Components.OrderCreationProcessor)processor;
        }


        // this method is used for loading data asynchronously
        public async override Task InitializeAsync()
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            // load data from base OrderConfirmation base report
            await base.InitializeAsync();

            /*
            foreach (OrderItemEntity item in OrderItemEntityList)
            {
                // Calculate new columns and set other properties 
                item.TaxRate1 = 2.99M;
                item.TaxRate2 = item.Price - item.TaxRate1;
            }
            */

                IList<PricingConditionResultItemEntity> conditions = this._orderCreationProcessor.PricingConditionResults;

                this.LogTrace("OrderReportExt:InitializeAsync: " + (conditions != null));

                foreach (OrderItemEntity item in OrderItemEntityList)
                {
                    try
                    {
                        this.LogTrace("OrderReportExt:Item: " + item.DocumentItemNumber);

                        decimal discount = 0M;
                        item.TaxRate1 = 0M;
                        item.TaxRate2 = 0M;
                        item.TaxRate3 = 0M;

                        foreach (PricingConditionResultItemEntity condition in conditions)
                        {
                            // for current item.... get base price and discounts
                            if (condition.ItemNumber == item.DocumentItemNumber)
                            {
                                if (condition.ConditionClass == ConditionClass.Prices)
                                {
                                    item.TaxRate1 = roundDecimals((decimal)condition.ConditionValueInternal / item.ActualQuantity);
                                }
                                 

                                if ((condition.ConditionClass == ConditionClass.DiscountSurcharge) && (!condition.IsInactive))
                                {
                                    this.LogTrace("OrderReportExt:Discount: " + condition.ConditionType + " = " + condition.ConditionValueInternal);

                                    // consider positive and negative discounts
                                    discount = discount + roundDecimals((decimal)condition.ConditionValueInternal/item.ActualQuantity);
                                }
                             }
                        }

                        // Calculate new columns and set other properties 
                        item.TaxRate2 = (discount * -1);
                        item.TaxRate3 = item.TaxRate1 - item.TaxRate2;
                        item.TaxRate4 = item.TaxRate3 * item.ActualQuantity;
                      }
                    catch (Exception ex)
                    {
                        this.LogError("OrderReportExt:InitializeAsync: Init =" + ex.Message);
                        this.LogError(ex);
                    }
                }
            }


        private decimal roundDecimals(decimal value)
        {
            string strNumber = String.Format("{0:0.00}", value);
            this.LogDebug("OrderReportExt: roundDecimals =" + strNumber);
            return Convert.ToDecimal(strNumber);
        }
    }
}
