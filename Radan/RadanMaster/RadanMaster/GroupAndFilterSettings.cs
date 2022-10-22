using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster
{
    class GroupAndFilterSettings
    {
        public bool ShowBatches { get; set; }
        public bool ShowOrders { get; set; }
        public bool ShowComplete { get; set; }
        public bool ShowOnlyItemsInRadan { get; set; }
        public bool GroupByBatchAndThickness { get; set; }
        public bool GroupByScheduleAndThickness { get; set; }
        public bool ShowAllCompletedOrders { get; set; }
        public bool ShowCompletedOrdersFromLastNDays { get; set; }
        public int NumberOfDays { get; set; }

        public bool LoadSettingsFromFile()
        {
            try
            {
                ShowBatches = ((string) AppSettings.AppSettings.Get("ShowBatchesCheckBox") == "checked") ? true : false;
                ShowOrders = ((string)AppSettings.AppSettings.Get("ShowOrdersCheckBox") == "checked") ? true : false;
                ShowComplete = ((string)AppSettings.AppSettings.Get("ShowCompleteCheckBox") == "checked") ? true : false;
                ShowOnlyItemsInRadan = ((string)AppSettings.AppSettings.Get("ShowOnlyItemsInRadanCheckBox") == "checked") ? true : false;
                GroupByBatchAndThickness = ((string)AppSettings.AppSettings.Get("GroupByBatchAndThicknessCheckBox") == "checked") ? true : false;
                GroupByScheduleAndThickness = ((string)AppSettings.AppSettings.Get("GroupByScheduleAndThicknessCheckBox") == "checked") ? true : false;
                ShowAllCompletedOrders = ((string)AppSettings.AppSettings.Get("ShowAllCompletedOrdersRadioButton") == "checked") ? true : false;
                ShowCompletedOrdersFromLastNDays = ((string)AppSettings.AppSettings.Get("ShowCompletedOrdersFromLastNDaysRadioButton") == "checked") ? true : false;
                NumberOfDays = (int) AppSettings.AppSettings.Get("NumberOfDaysToShowCompletedOrdersFrom");

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }

        }

        public bool SaveSettingsToFile()
        {
            try
            {
                if (ShowBatches)
                    AppSettings.AppSettings.Set("ShowBatchesCheckBox", "checked");
                else
                    AppSettings.AppSettings.Set("ShowBatchesCheckBox", "unchecked");

                if (ShowOrders)
                    AppSettings.AppSettings.Set("ShowOrdersCheckBox", "checked");
                else
                    AppSettings.AppSettings.Set("ShowOrdersCheckBox", "unchecked");

                if (ShowComplete)
                    AppSettings.AppSettings.Set("ShowCompleteCheckBox", "checked");
                else
                    AppSettings.AppSettings.Set("ShowCompleteCheckBox", "unchecked");

                if (ShowOnlyItemsInRadan)
                    AppSettings.AppSettings.Set("ShowOnlyItemsInRadanCheckBox", "checked");
                else
                    AppSettings.AppSettings.Set("ShowOnlyItemsInRadanCheckBox", "unchecked");

                if (GroupByBatchAndThickness)
                    AppSettings.AppSettings.Set("GroupByBatchAndThicknessCheckBox", "checked");
                else
                    AppSettings.AppSettings.Set("GroupByBatchAndThicknessCheckBox", "unchecked");

                if (GroupByScheduleAndThickness)
                    AppSettings.AppSettings.Set("GroupByScheduleAndThicknessCheckBox", "checked");
                else
                    AppSettings.AppSettings.Set("GroupByScheduleAndThicknessCheckBox", "unchecked");

                if (ShowAllCompletedOrders)
                    AppSettings.AppSettings.Set("ShowAllCompletedOrdersRadioButton", "checked");
                else
                    AppSettings.AppSettings.Set("ShowAllCompletedOrdersRadioButton", "unchecked");

                if (ShowCompletedOrdersFromLastNDays)
                    AppSettings.AppSettings.Set("ShowCompletedOrdersFromLastNDaysRadioButton", "checked");
                else
                    AppSettings.AppSettings.Set("ShowCompletedOrdersFromLastNDaysRadioButton", "unchecked");

                AppSettings.AppSettings.Set("NumberOfDaysToShowCompletedOrdersFrom", NumberOfDays);

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }


    }
}
